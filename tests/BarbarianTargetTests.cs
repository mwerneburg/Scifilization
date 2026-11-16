// CivOne tests
//
// A barbarian with no enemy beside it marches on the nearest city it can walk to. It used to
// pick the nearest city of all, and when that one was across water its path failed every
// turn: a hut horde in Greenland (the user's game, turn 308) stood by the hut for good,
// counting against the barbarian cap.
//
// Sea raids never landed on an Epic map: the ship appeared on open ocean anywhere and was
// disbanded unless a coastal capital lay within 10 tiles (5 tiles of 38,121 in the user's
// game). It now appears near a coastal city and makes for any coastal city.

using System.Linq;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class BarbarianTargetTests
	{
		[Fact]
		public void AHordeMarchesOnACityItCanReach()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 17; y <= 26; y++)
			for (int x = 17; x <= 43; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Ocean);
			for (int x = 20; x <= 40; x++)
				Map.Instance.ChangeTileType(x, 20, Terrain.Grassland1);   // our continent, a strip
			for (int x = 30; x <= 34; x++)
				Map.Instance.ChangeTileType(x, 23, Terrain.Grassland1);   // another, across water
			Map.Instance.RecalculateContinentsIfDirty();

			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			City across = g.AddCity(ai, 0, 32, 23)!;    // 3 tiles away, over the water
			City along = g.AddCity(ai, 1, 20, 20)!;     // 12 tiles away, on foot
			Assert.NotEqual(across.Tile.ContinentId, along.Tile.ContinentId);
			IUnit legion = g.CreateUnit(UnitType.Legion, 32, 20, 0)!;
			Sim.ClearTasks();

			AI.Instance(g.GetPlayer(0)).Move(legion);

			Assert.Equal(new System.Drawing.Point(along.X, along.Y), legion.Goto);
		}

		// An ocean world with one island town at (40,25); no capital anywhere.
		private static (Game g, City town) AnIslandTown()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 0; y < 50; y++)
			for (int x = 0; x < 80; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Ocean);
			Map.Instance.ChangeTileType(39, 25, Terrain.Grassland1);
			Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			City town = g.AddCity(ai, 0, 40, 25)!;
			town.RemoveBuilding(new CivOne.Buildings.Palace());
			Sim.ClearTasks();
			return (g, town);
		}

		[Fact]
		public void ARaidShipAppearsWithinSailOfACoast()
		{
			(Game _, City town) = AnIslandTown();
			for (int i = 0; i < 50; i++)
			{
				CivOne.Tiles.ITile? spawn = CivOne.Civilizations.Barbarian.SeaSpawnPosition;
				Assert.NotNull(spawn);
				Assert.True(Common.DistanceToTile(spawn!.X, spawn.Y, town.X, town.Y) <= 10, $"spawned at ({spawn.X},{spawn.Y})");
			}
		}

		[Fact]
		public void ARaidShipMakesForATownThatIsNotACapital()
		{
			(Game g, City town) = AnIslandTown();
			Assert.False(town.HasBuilding<CivOne.Buildings.Palace>(), "scenario is broken: the town is a capital");
			IUnit ship = g.CreateUnit(UnitType.Sail, 46, 25, 0)!;
			g.CreateUnit(UnitType.Legion, 46, 25, 0);
			Sim.ClearTasks();

			AI.Instance(g.GetPlayer(0)).Move(ship);

			Assert.Contains(ship, g.GetUnits());
			Assert.True(ship.X < 46 || !ship.Goto.IsEmpty, $"the ship sat at ({ship.X},{ship.Y}) with no orders");
		}
	}
}
