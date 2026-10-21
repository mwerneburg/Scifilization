// CivOne tests
//
// Bombers (Oct 2026). No AI chose to build one: BestAttacker returns land units, and the
// unmanned rule names only missiles and drones. And anything that must fly home was given
// half its fuel from WHERE IT STOOD, so a second sortie could take it past the way back.

using System.Drawing;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class BomberTests
	{
		// A Monarchy with every advance, four cities on a grassland strip, at war with a rival
		// whose city stands six tiles east of the last: only a city within 8 tiles of a
		// hostile builds for the war (LocalStance), so the tests ask that one.
		private static (Game g, Player p, City[] cities) AnAirPowerAtWar(Civilizations.ICivilization? civ = null)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player rival = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			Player p;
			if (civ is null)
				p = g.Players.First(x => x is not null && x != rival && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			else { p = new Player(civ, "Faction"); g.AddPlayer(p); }
			foreach (IAdvance a in Common.Advances) p.AddAdvance(a, false);
			p.Government = new Governments.Monarchy();
			p.Explore(40, 25, range: 40);
			City[] cities = new[] { 14, 20, 26, 32 }.Select((x, i) => { City c = g.AddCity(p, i, x, 25)!; c.Size = 6; return c; }).ToArray();
			g.AddCity(rival, 9, 38, 25)!.Size = 4;
			p.DeclareWar(rival);
			Sim.ClearTasks();
			return (g, p, cities);
		}

		[Fact]
		public void ACivAtWarPlansABomber()
		{
			(Game _, Player p, City[] cities) = AnAirPowerAtWar();
			Assert.Contains(AI.Instance(p).ProductionPlan(cities[3]), x => x is Bomber);
		}

		// Four cities: a cap of one, and one is already on order.
		[Fact]
		public void TheCapCountsTheOnesOnOrder()
		{
			(Game _, Player p, City[] cities) = AnAirPowerAtWar();
			cities[0].SetProduction(new Bomber());
			Assert.DoesNotContain(AI.Instance(p).ProductionPlan(cities[3]), x => x is Bomber);
		}

		[Fact]
		public void TheFederationPlansABomber()
		{
			(Game _, Player p, City[] cities) = AnAirPowerAtWar(Common.Civilizations.First(c => c is Civilizations.Federation));
			Assert.Contains(AI.Instance(p).ProductionPlan(cities[3]), x => x is Bomber);
		}

		private static void FlyStrike(Player p, IUnit unit) =>
			typeof(AI).GetMethod("FlyStrike", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(AI.Instance(p), new object[] { unit });

		// Eight tiles out from the nearest city at 32,25, with eight left: a tank four tiles
		// further on is in reach of half the fuel, and twelve tiles from home.
		[Fact]
		public void ABomberOutOnItsFuelGoesHomeInsteadOfOn()
		{
			(Game g, Player p, City[] _) = AnAirPowerAtWar();
			var bomber = (Bomber)g.CreateUnit(UnitType.Bomber, 40, 25, g.PlayerNumber(p))!;
			bomber.FuelLeft = 8;
			Player rival = g.Players.First(x => x is not null && x != p && p.IsAtWar(x));
			g.CreateUnit(UnitType.Armor, 44, 25, g.PlayerNumber(rival));

			FlyStrike(p, bomber);

			Assert.Equal(new Point(32, 25), bomber.Goto);
		}

		// Ten tiles out is in reach of the fuel — home to the city at 32,25 is four more — but
		// not of one turn's eight moves: it would spend the night in the open short of the mark.
		[Fact]
		public void ABomberStrikesOnlyWhatItReachesThisTurn()
		{
			(Game g, Player p, City[] _) = AnAirPowerAtWar();
			var bomber = (Bomber)g.CreateUnit(UnitType.Bomber, 26, 25, g.PlayerNumber(p))!;
			Player rival = g.Players.First(x => x is not null && x != p && p.IsAtWar(x));
			g.CreateUnit(UnitType.Armor, 36, 25, g.PlayerNumber(rival));

			FlyStrike(p, bomber);

			Assert.True(bomber.Goto.IsEmpty, $"sent to {bomber.Goto}");
		}

		// A full tank at home: out six and back six (the tank stands in the rival's city).
		[Fact]
		public void AFullBomberStrikesWhatItCanReturnFrom()
		{
			(Game g, Player p, City[] _) = AnAirPowerAtWar();
			var bomber = (Bomber)g.CreateUnit(UnitType.Bomber, 32, 25, g.PlayerNumber(p))!;
			Player rival = g.Players.First(x => x is not null && x != p && p.IsAtWar(x));
			g.CreateUnit(UnitType.Armor, 38, 25, g.PlayerNumber(rival));

			FlyStrike(p, bomber);

			Assert.Equal(new Point(38, 25), bomber.Goto);
		}
	
		// ── only aircraft reach aircraft ─────────────────────────────────────

		private static (Game g, Player p, Player rival) TwoSides()
		{
			(Game g, Player p, City[] _) = AnAirPowerAtWar();
			Common.Random = new CivOne.Random(7);
			return (g, p, g.Players.First(x => x is not null && x != p && p.IsAtWar(x)));
		}

		[Fact]
		public void ATankCannotAttackABomberInTheOpen()
		{
			(Game g, Player p, Player rival) = TwoSides();
			IUnit bomber = g.CreateUnit(UnitType.Bomber, 35, 22, g.PlayerNumber(rival))!;
			IUnit tank = g.CreateUnit(UnitType.Armor, 34, 22, g.PlayerNumber(p))!;

			Assert.False(tank.MoveTo(1, 0));
			Sim.Settle();

			Assert.Contains(bomber, g.GetUnits());
			Assert.Contains(tank, g.GetUnits());
		}

		[Fact]
		public void ADroneStillCan()
		{
			(Game g, Player p, Player rival) = TwoSides();
			IUnit bomber = g.CreateUnit(UnitType.Bomber, 35, 22, g.PlayerNumber(rival))!;
			IUnit drone = g.CreateUnit(UnitType.ReaperDrone, 34, 22, g.PlayerNumber(p))!;

			drone.MoveTo(1, 0);
			Sim.Settle();

			Assert.False(g.GetUnits().Contains(bomber) && g.GetUnits().Contains(drone), "no fight took place");
		}

		// ── the year's campaign ──────────────────────────────────────────────

		// Three Militia, size 8, five improvements, a Palace and a Wonder. Air odds against
		// Militia without a SAM are ~48 to 1, and the seed is fixed.
		[Fact]
		public void ARaidKillsTheGarrisonAndItsBuildingsButSparesTheWonder()
		{
			(Game g, Player p, Player rival) = TwoSides();
			City target = g.GetCities().First(c => c.Owner == g.PlayerNumber(rival));
			target.Size = 8;
			foreach (IBuilding b in new IBuilding[] { new Granary(), new Library(), new MarketPlace(), new Temple(), new Barracks() })
				target.AddBuilding(b);
			if (!target.HasBuilding<Palace>()) target.AddBuilding(new Palace());
			target.AddWonder(new Wonders.HangingGardens());
			int buildings = target.Buildings.Length;
			for (int i = 0; i < 3; i++) g.CreateUnit(UnitType.Militia, target.X, target.Y, g.PlayerNumber(rival));
			var bomber = (Bomber)g.CreateUnit(UnitType.Bomber, target.X - 1, target.Y, g.PlayerNumber(p))!;

			bomber.MoveTo(1, 0);
			Sim.Settle();

			Assert.Empty(g.GetUnits().Where(u => u.X == target.X && u.Y == target.Y));
			Assert.Equal(5, target.Size);
			Assert.Equal(buildings - 3, target.Buildings.Length);
			Assert.True(target.HasBuilding<Palace>());
			Assert.Single(target.Wonders);
			Assert.Contains(bomber, g.GetUnits());
			Assert.Equal((32, 25), (bomber.X, bomber.Y));   // home: our nearest city
			Assert.Equal(bomber.TotalFuel, bomber.FuelLeft);
		}
	
		// A fortified Mech Inf under a SAM: 6 x2 from the air, keeps its x6/4 fortification,
		// then x3 — 54 against the Bomber's 96, where the graded SAM alone gave 18.
		[Fact]
		public void ASamTriplesADefenderAgainstABomber()
		{
			(Game g, Player p, Player rival) = TwoSides();
			City target = g.GetCities().First(c => c.Owner == g.PlayerNumber(rival));
			target.AddBuilding(new SamBattery());
			IUnit mech = g.CreateUnit(UnitType.MechInf, target.X, target.Y, g.PlayerNumber(rival))!;
			mech.Fortify = true;
			IUnit bomber = g.CreateUnit(UnitType.Bomber, target.X - 1, target.Y, g.PlayerNumber(p))!;
			int d = (int)typeof(BaseUnit).GetMethod("DefendStrength", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(bomber, new object[] { mech, bomber })!;

			Assert.Equal(54, d);
		}
	}
}
