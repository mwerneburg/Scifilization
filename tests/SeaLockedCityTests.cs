// CivOne tests
//
// A city with no land beside it. The Others' last city in game cea9d583, Tauranga, filled a
// one-tile island and stood for 38 turns with eighteen civs at war with it: no target picker
// chose it, no land unit could attack from a ship, and the air struck only what was in reach.
// Now a land unit goes in from the boats there and nowhere else, the AI picks such a city,
// lands beside it rather than two tiles off, and its warheads, missiles and Bombers go for it.

using System.Drawing;
using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class SeaLockedCityTests : System.IDisposable
	{
		private readonly bool _auto;
		public SeaLockedCityTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// Our island runs x 20..38 on rows 20..30, cities at 22, 28 and 38. Theirs is the one
		// tile at 44,25, six tiles east of our nearest city, with a Phalanx in it.
		private static (Game g, Player us, Player them, City island) AWorld(bool shore = false)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 60; x++)
			{
				bool land = (y >= 20 && y <= 30 && x >= 20 && x <= 38) || (x == 44 && y == 25) || (shore && x == 45 && y == 25);
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Grassland1 : Terrain.Ocean);
			}
			Map.Instance.RecalculateContinentsIfDirty();

			Player[] ps = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			Player us = ps[0], them = ps[1];
			us.Government = new CivOne.Governments.Monarchy();
			us.Explore(40, 25, range: 25);
			us.AddAdvance(new CivOne.Advances.Flight(), false);
			foreach ((int x, int id) in new[] { (22, 0), (28, 1), (38, 2) })
			{
				City c = g.AddCity(us, id, x, 25)!;
				c.Size = 6;
				g.CreateUnit(UnitType.Musketeers, x, 25, g.PlayerNumber(us));
			}
			City island = g.AddCity(them, 3, 44, 25)!;
			island.Size = 4;
			g.CreateUnit(UnitType.Phalanx, 44, 25, g.PlayerNumber(them));
			Assert.Equal(!shore, island.SeaLocked);
			us.DeclareWar(them);
			Sim.ClearTasks();
			return (g, us, them, island);
		}

		private static object? Call(Player p, string method, params object?[] args) =>
			typeof(AI).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(AI.Instance(p), args);

		private static void SetTarget(Player p, City? c) =>
			typeof(AI).GetField("_attackTarget", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(AI.Instance(p), c);

		// ── the rule ─────────────────────────────────────────────────────────

		[Fact]
		public void ALandUnitAttacksASeaLockedCityFromItsShip()
		{
			(Game g, Player us, Player _, City _) = AWorld();
			g.CreateUnit(UnitType.Transport, 43, 25, g.PlayerNumber(us));
			IUnit armor = g.CreateUnit(UnitType.Armor, 43, 25, g.PlayerNumber(us))!;

			armor.MoveTo(1, 0);   // Confront returns false win or lose; the fight is the Movement

			Assert.NotNull(armor.Movement);
		}

		[Fact]
		public void ACityWithAShoreIsStillTakenFromTheShore()
		{
			(Game g, Player us, Player _, City _) = AWorld(shore: true);
			g.CreateUnit(UnitType.Transport, 43, 25, g.PlayerNumber(us));
			IUnit armor = g.CreateUnit(UnitType.Armor, 43, 25, g.PlayerNumber(us))!;

			armor.MoveTo(1, 0);

			Assert.Null(armor.Movement);
		}

		// ── the target ───────────────────────────────────────────────────────

		[Fact]
		public void ASeaLockedCityIsPickedAsATarget()
		{
			(Game _, Player us, Player _, City island) = AWorld();

			Assert.Same(island, Call(us, "PickAttackTarget", new object?[] { null }));
		}

		// Nowhere for a Dirigible to set anybody down: the group goes by sea.
		[Fact]
		public void ASeaLockedCityIsNotAirlifted()
		{
			(Game _, Player us, Player _, City island) = AWorld();

			Assert.False((bool)Call(us, "Airlift", island)!);
		}

		[Fact]
		public void ATransportUnloadsBesideASeaLockedCityNotTwoTilesOff()
		{
			(Game g, Player us, Player _, City island) = AWorld();
			SetTarget(us, island);
			IUnit ship = g.CreateUnit(UnitType.Transport, 42, 25, g.PlayerNumber(us))!;
			IUnit armor = g.CreateUnit(UnitType.Armor, 42, 25, g.PlayerNumber(us))!;
			armor.Sentry = true;

			Call(us, "AssignMission", ship);

			Assert.True(armor.Sentry, "unloaded two tiles off, where there is no land to step onto");
			Assert.Equal(1, Common.DistanceToTile(ship.Goto.X, ship.Goto.Y, 44, 25));
		}

		// ── the air ──────────────────────────────────────────────────────────

		// The civ is at war over nothing in particular, and the warhead still goes.
		[Fact]
		public void ANuclearMissileStrikesASeaLockedCity()
		{
			(Game g, Player us, Player _, City _) = AWorld();
			SetTarget(us, null);
			IUnit nuke = g.CreateUnit(UnitType.Nuclear, 38, 25, g.PlayerNumber(us))!;

			Call(us, "FlyStrike", nuke);

			Assert.Equal(new Point(44, 25), nuke.Goto);
		}

		// A tank in the open nearer to hand loses to the city no army can walk up to.
		[Fact]
		public void ABomberPrefersTheSeaLockedCity()
		{
			(Game g, Player us, Player them, City _) = AWorld();
			SetTarget(us, null);
			IUnit bomber = g.CreateUnit(UnitType.Bomber, 38, 25, g.PlayerNumber(us))!;
			g.CreateUnit(UnitType.Armor, 35, 25, g.PlayerNumber(them));

			Call(us, "FlyStrike", bomber);

			Assert.Equal(new Point(44, 25), bomber.Goto);
		}

		// From 22 it is out of reach; the airfield at 28 is within this turn's flight and nearer.
		[Fact]
		public void ABomberOutOfReachMovesUpToTheNearestAirfield()
		{
			(Game g, Player us, Player _, City _) = AWorld();
			SetTarget(us, null);
			IUnit bomber = g.CreateUnit(UnitType.Bomber, 22, 25, g.PlayerNumber(us))!;

			Call(us, "FlyStrike", bomber);

			Assert.Equal(new Point(28, 25), bomber.Goto);
		}
	}
}
