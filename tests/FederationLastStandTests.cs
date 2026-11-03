// CivOne tests
//
// The Federation's last stand. Down to fifteen cities it builds warheads, one for every three
// cities, with or without a Manhattan Project on Earth, and flies them at any enemy city in
// reach, without warning (the user, Nov 2026). The Others never do.

using System.Drawing;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class FederationLastStandTests : System.IDisposable
	{
		private readonly bool _auto;
		public FederationLastStandTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// An invader with `cities` cities on a strip of grassland, three tiles apart; no
		// Manhattan Project anywhere.
		private static (Game g, Player p) AnInvader(int cities, bool others = false)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 10; y <= 40; y++)
			for (int x = 5; x <= 75; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player p = new Player(Common.Civilizations.First(c => others ? c is Civilizations.TheOthers : c is Civilizations.Federation), "Faction");
			g.AddPlayer(p);
			foreach (IAdvance a in Common.Advances) p.AddAdvance(a, false);
			p.Explore(40, 25, range: 40);
			for (int i = 0; i < cities; i++)
			{
				City c = g.AddCity(p, i, 8 + 3 * (i % 20), 12 + 3 * (i / 20))!;
				c.Size = 6;
			}
			Assert.False(g.WonderBuilt<CivOne.Wonders.ManhattanProject>());
			Sim.ClearTasks();
			return (g, p);
		}

		private static bool PlansAWarhead(Player p) =>
			AI.Instance(p).ProductionPlan(p.Cities[0]).Any(x => x is Nuclear);

		// ── the build ────────────────────────────────────────────────────────

		[Fact]
		public void AtFifteenCitiesTheFederationBuildsAWarhead()
		{
			(Game _, Player p) = AnInvader(15);
			Assert.True(PlansAWarhead(p));
		}

		[Fact]
		public void AtSixteenItDoesNot()
		{
			(Game _, Player p) = AnInvader(16);
			Assert.False(PlansAWarhead(p));
		}

		// One for every three cities: five on hand at fifteen is the stockpile.
		[Fact]
		public void TheStockpileStopsAtOnePerThreeCities()
		{
			(Game g, Player p) = AnInvader(15);
			for (int i = 0; i < 5; i++) g.CreateUnit(UnitType.Nuclear, p.Cities[1].X, p.Cities[1].Y, g.PlayerNumber(p));
			Assert.False(PlansAWarhead(p));
		}

		[Fact]
		public void TheOthersNeverBuildOne()
		{
			(Game _, Player p) = AnInvader(5, others: true);
			Assert.False(PlansAWarhead(p));
		}

		// A spacefaring power brought its own; an Earth civ with every advance still needs the
		// Manhattan Project.
		[Fact]
		public void TheFederationNeedsNoManhattanProject()
		{
			(Game g, Player p) = AnInvader(1);
			Player earth = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != p);
			foreach (IAdvance a in Common.Advances) earth.AddAdvance(a, false);

			Assert.True(p.ProductionAvailable(new Nuclear()));
			Assert.False(earth.ProductionAvailable(new Nuclear()));
			Assert.Contains(p.Cities[0].AvailableProduction, x => x is Nuclear);
		}

		// ── the launch ───────────────────────────────────────────────────────

		// No attack target, no warning: the warhead goes for the enemy city in reach. An Earth
		// civ's warhead in the same place waits for its war's target.
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void AWarheadStrikesAnyEnemyCityInReach(bool federation)
		{
			(Game g, Player fed) = AnInvader(1);
			Player earth = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != fed && x != g.HumanPlayer);
			Player shooter = federation ? fed : earth, victim = federation ? earth : fed;
			if (!federation) g.AddCity(earth, 1, 8, 20);   // a home for the Earth warhead
			City home = shooter.Cities[0];
			City mark = g.AddCity(victim, 2, home.X + 8, home.Y)!;
			g.CreateUnit(UnitType.Musketeers, mark.X, mark.Y, g.PlayerNumber(victim));
			shooter.Explore(home.X, home.Y, range: 20);
			shooter.DeclareWar(victim);
			typeof(AI).GetField("_attackTarget", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(AI.Instance(shooter), null);
			IUnit nuke = g.CreateUnit(UnitType.Nuclear, home.X, home.Y, g.PlayerNumber(shooter))!;

			typeof(AI).GetMethod("FlyStrike", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(AI.Instance(shooter), new object[] { nuke });

			if (federation) Assert.Equal(new Point(mark.X, mark.Y), nuke.Goto);
			else Assert.NotEqual(new Point(mark.X, mark.Y), nuke.Goto);
		}
	}
}
