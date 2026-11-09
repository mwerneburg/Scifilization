// CivOne tests
//
// The standing army. Game cea9d583 reached 9,631 units by turn 650 with no war on Earth for
// 150 turns: 2,761 Haudenosaunee, 21 per city. AI moves, the task queue and the screen all
// scale with the count, and turns went from 7.6 s at t500 to 54.6 s at t650. An AI civ now
// stops raising military units past a ceiling (3 a city at peace, 6 at war) and retires a
// few of the surplus each turn; a unit retired inside a city building an improvement gives
// half its cost back to it, for the player as well (the user, Nov 2026).

using System.Linq;
using CivOne;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class StandingArmyTests
	{
		// An AI civ with two size-6 cities and the early military advances.
		private static (Game g, Player p, AI ai, City a, City b) Empire()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player p = g.Players.First(q => q is not null && g.PlayerNumber(q) != 0 && q != g.HumanPlayer);
			p.Explore(30, 25, range: 10);
			City a = g.AddCity(p, 0, 25, 25)!, b = g.AddCity(p, 1, 32, 25)!;
			a.Size = 6; b.Size = 6;
			foreach (IAdvance adv in new IAdvance[] { new BronzeWorking(), new Masonry(), new Pottery(), new Feudalism(), new Monarchy() })
				p.AddAdvance(adv, false);
			Sim.ClearTasks();
			return (g, p, AI.Instance(p), a, b);
		}

		private static void Garrison(Game g, Player p, City c, UnitType type, int n)
		{
			for (int i = 0; i < n; i++) g.CreateUnit(type, c.X, c.Y, g.PlayerNumber(p));
		}

		private static bool PlansMilitary(AI ai, City c) => ai.ProductionPlan(c).OfType<IUnit>().Any(AI.IsMilitary);

		// ── the refund ───────────────────────────────────────────────────────────

		[Fact]
		public void ARetiredUnit_PaysHalfItsCostIntoAnImprovement()
		{
			var (g, p, _, a, _) = Empire();
			a.SetProduction(new Library());
			a.Shields = 0;
			IUnit musket = g.CreateUnit(UnitType.Musketeers, a.X, a.Y, g.PlayerNumber(p))!;
			int cost = musket.Price * 10;

			int refund = g.DecommissionUnit(musket);

			Assert.Equal(cost / 2, refund);
			Assert.Equal(cost / 2, a.Shields);
			Assert.DoesNotContain(musket, g.GetUnits());
		}

		// Toward an improvement only: not another unit, and not a wonder.
		[Fact]
		public void ARetiredUnit_PaysNothingTowardAUnitOrAWonder()
		{
			var (g, p, _, a, _) = Empire();
			foreach (IProduction building in new IProduction[] { new Phalanx(), new Wonders.Pyramids() })
			{
				a.SetProduction(building);
				a.Shields = 0;
				g.DecommissionUnit(g.CreateUnit(UnitType.Musketeers, a.X, a.Y, g.PlayerNumber(p))!);

				Assert.Equal(0, a.Shields);
			}
		}

		// ── the ceiling ──────────────────────────────────────────────────────────

		[Fact]
		public void PastTheCeiling_ACityRaisesNoMoreSoldiers()
		{
			var (g, p, ai, a, _) = Empire();
			Assert.True(PlansMilitary(ai, a), "scenario is broken: the city plans no soldiers even below the ceiling");

			Garrison(g, p, a, UnitType.Phalanx, ai.ArmyCap(fresh: true));
			ai.ArmySize(fresh: true);

			Assert.False(PlansMilitary(ai, a), "plan: " + string.Join(",", ai.ProductionPlan(a).Select(x => x.GetType().Name)));
		}

		// An empty city still raises its first defender, whatever the count elsewhere.
		[Fact]
		public void AnEmptyCity_StillRaisesADefender()
		{
			var (g, p, ai, a, b) = Empire();
			Garrison(g, p, a, UnitType.Phalanx, ai.ArmyCap(fresh: true));
			ai.ArmySize(fresh: true);

			// A soldier: an Explorer also counts as Defense by its stats.
			Assert.Contains(ai.ProductionPlan(b).OfType<IUnit>(), u => AI.IsMilitary(u) && u.Role == UnitRole.Defense);
		}

		// An enemy within three tiles lifts the ceiling for that city.
		[Fact]
		public void ACityWithAnEnemyNear_MayStillArm()
		{
			var (g, p, ai, a, b) = Empire();
			// The army at the ceiling, but held mostly in the other city: a city already past
			// four foot defenders would disband any fifth (AI.cs), so it plans none either way.
			Garrison(g, p, a, UnitType.Phalanx, 1);
			Garrison(g, p, b, UnitType.Phalanx, ai.ArmyCap(fresh: true) - 1);
			ai.ArmySize(fresh: true);
			g.CreateUnit(UnitType.Legion, a.X - 2, a.Y, 0);   // barbarians: always hostile

			Assert.True(PlansMilitary(ai, a), "plan: " + string.Join(",", ai.ProductionPlan(a).Select(x => x.GetType().Name)));
		}

		// A war raises the ceiling only for the cities on its front. Every civ was at war with
		// the Others in game cea9d583, whose last towns were scattered world-wide.
		[Fact]
		public void AWarRaisesTheCeiling_OnlyOnItsFront()
		{
			var (g, p, ai, a, b) = Empire();   // a at x=25, b at x=32
			Player enemy = g.Players.First(q => q is not null && g.PlayerNumber(q) != 0 && q != p && q != g.HumanPlayer);
			enemy.Explore(68, 25, range: 2);
			City far = g.AddCity(enemy, 5, 68, 25)!;     // 36 tiles from b
			p.DeclareWar(enemy);
			Assert.True(p.IsAtWar(enemy), "scenario is broken: not at war");

			Assert.Equal(2 * AI.PeaceArmyPerCity, ai.ArmyCap(fresh: true));

			g.AddCity(enemy, 6, 42, 25);                 // 10 tiles from b, 17 from a
			Assert.Equal(2 * AI.PeaceArmyPerCity + (AI.WarArmyPerCity - AI.PeaceArmyPerCity), ai.ArmyCap(fresh: true));
		}

		// ── standing down ────────────────────────────────────────────────────────

		// Over the ceiling, the weakest go first and a city building an improvement is paid.
		[Fact]
		public void TheSurplus_IsRetiredWeakestFirst()
		{
			var (g, p, ai, a, b) = Empire();
			a.SetProduction(new Granary());
			a.Shields = 0;
			int cap = ai.ArmyCap(fresh: true);
			Garrison(g, p, a, UnitType.Musketeers, cap);
			Garrison(g, p, a, UnitType.Legion, 2);    // 3 + 1
			Garrison(g, p, a, UnitType.Militia, 3);   // 1 + 1, the weakest

			ai.ConsiderDecommission();

			Assert.Equal(cap + 4, ai.ArmySize(fresh: true));     // 5 over: one retired this turn
			Assert.Equal(2, a.Tile.Units.Count(u => u is Militia));
			Assert.True(a.Shields > 0, "the retired unit paid nothing into the Granary");
		}

		// At or under the ceiling, nobody is stood down.
		[Fact]
		public void UnderTheCeiling_NobodyIsRetired()
		{
			var (g, p, ai, a, _) = Empire();
			int cap = ai.ArmyCap(fresh: true);
			Garrison(g, p, a, UnitType.Musketeers, cap);

			ai.ConsiderDecommission();

			Assert.Equal(cap, ai.ArmySize(fresh: true));
		}

		// Two defenders stay in every city, however far over the ceiling.
		[Fact]
		public void EveryCity_KeepsTwoDefenders()
		{
			var (g, p, ai, a, b) = Empire();
			Garrison(g, p, a, UnitType.Musketeers, 60);
			Garrison(g, p, b, UnitType.Musketeers, 2);
			// b is building an improvement, so its soldiers are first in line to be retired.
			a.SetProduction(new Phalanx());
			b.SetProduction(new Granary());

			for (int i = 0; i < 20; i++) ai.ConsiderDecommission();

			Assert.Equal(2, b.Tile.Units.Count(u => u.Role == UnitRole.Defense));
		}

		// No city gives up its soldiers with an enemy at the gate.
		[Fact]
		public void ACityUnderThreat_KeepsItsSoldiers()
		{
			var (g, p, ai, a, _) = Empire();
			Garrison(g, p, a, UnitType.Legion, ai.ArmyCap(fresh: true) + 20);
			int before = a.Tile.Units.Length;
			g.CreateUnit(UnitType.Legion, a.X + 1, a.Y, 0);

			ai.ConsiderDecommission();

			Assert.Equal(before, a.Tile.Units.Length);
		}
	}
}
