// CivOne tests
//
// AI warships had no order but "chart the world" and "go to the nearest port of ours". In the
// 1996 save of game c58808e6 all ten Battleships sat in port. Now one sails at the nearest
// enemy ship or garrisoned port it can see and can at least match (AI.SeaPrey).

using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class WarshipTests : System.IDisposable
	{
		private readonly bool _auto;
		public WarshipTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		private const int PortX = 26, EnemyX = 40, SeaY = 25;

		// Two islands in one sea: our port at (26,25), theirs at (40,25), both in sight, both
		// on the shore. The enemy port is empty until a test garrisons it.
		private static (Game g, Player us, Player them, City port, City enemy) ASeaWar(bool war = true, int width = 80)
		{
			Sim.NewGame(width: width, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 5; y <= 45; y++)
			for (int x = 5; x <= 70; x++)
			{
				bool land = y >= 20 && y <= 30 && ((x >= 20 && x <= PortX) || (x >= EnemyX && x <= 46));
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Grassland1 : Terrain.Ocean);
			}
			Map.Instance.RecalculateContinentsIfDirty();

			Player[] ps = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			Player us = ps[0], them = ps[1];
			us.Explore(EnemyX - 7, SeaY, range: 25);
			City port = g.AddCity(us, 0, PortX, SeaY)!;
			City enemy = g.AddCity(them, 1, EnemyX, SeaY)!;
			port.Size = enemy.Size = 6;
			g.CreateUnit(UnitType.Musketeers, PortX, SeaY, g.PlayerNumber(us));
			if (war) us.DeclareWar(them);
			Sim.ClearTasks();
			return (g, us, them, port, enemy);
		}

		private static IUnit Make(Game g, Player p, UnitType t, int x, int y) => g.CreateUnit(t, x, y, g.PlayerNumber(p), false)!;

		[Fact]
		public void ABattleshipInPortSailsAtAnEnemyPort()
		{
			(Game g, Player us, Player them, City _, City enemy) = ASeaWar();
			Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			IUnit ship = Make(g, us, UnitType.Battleship, PortX, SeaY);

			AI.Instance(us).Move(ship);
			Sim.Settle();

			Assert.Equal(new Point(enemy.X, enemy.Y), ship.Goto);
			Assert.True(ship.X > PortX, "it stayed in port");
		}

		[Fact]
		public void BesideThePortItFightsTheGarrison()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			IUnit phalanx = Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			IUnit ship = Make(g, us, UnitType.Battleship, EnemyX - 1, SeaY);

			AI.Instance(us).Move(ship);
			Sim.Settle();

			// One of the two must die: a fight has no other ending.
			IUnit[] left = g.GetUnits();
			Assert.True(!left.Contains(ship) || !left.Contains(phalanx), "no fight was fought");
		}

		// A ship cannot take a city, so one emptied under it must drop the order and move on.
		// Keeping the Goto it would wait beside that city for the rest of the game.
		[Fact]
		public void AnEmptiedPortIsLetGo()
		{
			(Game g, Player us, Player _, City _, City enemy) = ASeaWar();
			IUnit ship = Make(g, us, UnitType.Battleship, EnemyX - 1, SeaY);
			ship.Goto = new Point(enemy.X, enemy.Y);

			AI.Instance(us).Move(ship);
			Sim.Settle();

			Assert.True(ship.Goto.IsEmpty, "it is still waiting at the empty city");
			Assert.Equal(g.PlayerNumber(enemy.Player), enemy.Owner);
		}

		[Fact]
		public void AnEmptyPortIsNotPrey()
		{
			(Game g, Player us, Player _, City _, City _) = ASeaWar();
			IUnit ship = Make(g, us, UnitType.Battleship, PortX, SeaY);

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}

		[Fact]
		public void AShipAtSeaIsPrey()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			Make(g, them, UnitType.Transport, 33, SeaY);
			IUnit ship = Make(g, us, UnitType.Battleship, PortX, SeaY);

			ITile? prey = AI.Instance(us).SeaPrey(ship);

			Assert.NotNull(prey);
			Assert.Equal((33, SeaY), (prey!.X, prey.Y));
		}

		[Fact]
		public void TheNearestPreyComesFirst()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			Make(g, them, UnitType.Transport, 30, SeaY + 2);
			IUnit ship = Make(g, us, UnitType.Battleship, PortX, SeaY);

			ITile? prey = AI.Instance(us).SeaPrey(ship);

			Assert.Equal((30, SeaY + 2), (prey!.X, prey.Y));
		}

		[Fact]
		public void AtPeaceNothingIsPrey()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar(war: false);
			Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			Make(g, them, UnitType.Transport, 33, SeaY);
			IUnit ship = Make(g, us, UnitType.Battleship, PortX, SeaY);

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}

		[Fact]
		public void WhatItCannotMatchIsNotPrey()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			Make(g, them, UnitType.Battleship, 33, SeaY);
			IUnit ship = Make(g, us, UnitType.Ironclad, PortX, SeaY);

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}

		[Fact]
		public void WhatItCannotSeeIsNotPrey()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			// Past the fog, inside the reach. Explored 25 around x=33 reaches x=58, so stand
			// the ship east of its own view rather than move the enemy out of range.
			IUnit ship = Make(g, us, UnitType.Battleship, 56, SeaY);
			Make(g, them, UnitType.Transport, 66, SeaY);
			Assert.False(us.Visible(66, SeaY), "fixture: the prey is in sight");

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}

		[Fact]
		public void WhatIsOutOfReachIsNotPrey()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			IUnit ship = Make(g, us, UnitType.Battleship, 8, 8);
			Make(g, them, UnitType.Transport, 8 + AI.SeaStrikeReach + 1, 8);
			us.Explore(8 + AI.SeaStrikeReach + 1, 8, range: 2);

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}

		// A flat 16 tiles left every warship of a 320x200 game with nothing in reach.
		[Fact]
		public void TheReachGrowsWithTheMap()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar(width: 200);
			Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			IUnit ship = Make(g, us, UnitType.Battleship, 10, SeaY);
			Assert.True(Common.DistanceToTile(10, SeaY, EnemyX, SeaY) > 16, "fixture: the port is within the old reach");

			ITile? prey = AI.Instance(us).SeaPrey(ship);

			Assert.NotNull(prey);
			Assert.Equal((EnemyX, SeaY), (prey!.X, prey.Y));
		}

		[Fact]
		public void APortOnAnotherSeaIsNotPrey()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			// A lake inside our island, with our ship on it, in reach of the enemy port.
			Map.Instance.ChangeTileType(24, 22, Terrain.Ocean);
			Map.Instance.RecalculateContinentsIfDirty();
			Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			IUnit ship = Make(g, us, UnitType.Battleship, 24, 22);
			Assert.True(Common.DistanceToTile(24, 22, EnemyX, SeaY) <= AI.SeaStrikeReach, "fixture: the port is out of reach");

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}

		// Only three candidates get a route search, so a nearer town no ship can reach must
		// not use up a place.
		[Fact]
		public void InlandTownsDoNotCrowdOutAPort()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			for (int i = 0; i < 3; i++)
			{
				g.AddCity(them, 2 + i, 43, 23 + 2 * i);
				Make(g, them, UnitType.Phalanx, 43, 23 + 2 * i);
			}
			IUnit ship = Make(g, us, UnitType.Battleship, 48, SeaY);

			ITile? prey = AI.Instance(us).SeaPrey(ship);

			Assert.NotNull(prey);
			Assert.Equal((EnemyX, SeaY), (prey!.X, prey.Y));
		}

		// Aircraft in the open can only be fought by aircraft (BaseUnit.MoveTo), so a ship sent
		// after one would be refused at the last step, every turn.
		[Fact]
		public void AnAircraftAtSeaIsNotPrey()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			Make(g, them, UnitType.Bomber, 33, SeaY);
			IUnit ship = Make(g, us, UnitType.Battleship, PortX, SeaY);

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}

		[Fact]
		public void ASubmarineHuntsShipsAndLeavesPorts()
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			Make(g, them, UnitType.Phalanx, EnemyX, SeaY);
			IUnit sub = Make(g, us, UnitType.Submarine, PortX, SeaY);
			Assert.Null(AI.Instance(us).SeaPrey(sub));

			Make(g, them, UnitType.Transport, 33, SeaY);
			Assert.Equal((33, SeaY), (AI.Instance(us).SeaPrey(sub)!.X, AI.Instance(us).SeaPrey(sub)!.Y));
		}

		// Prey each could beat: armed, and they still stay out of it.
		[Theory]
		[InlineData(UnitType.Carrier)]
		[InlineData(UnitType.Frigate)]
		public void CarriersAndHullsDoNotHunt(UnitType type)
		{
			(Game g, Player us, Player them, City _, City _) = ASeaWar();
			Make(g, them, UnitType.Trireme, 33, SeaY);
			IUnit ship = Make(g, us, type, PortX, SeaY);
			Assert.True(((BaseUnit)ship).EvenOrBetter(Map.Instance[33, SeaY]), "fixture: it could not win anyway");

			Assert.Null(AI.Instance(us).SeaPrey(ship));
		}
	}
}
