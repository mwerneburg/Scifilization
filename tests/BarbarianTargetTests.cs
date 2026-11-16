// CivOne tests
//
// A barbarian with no enemy beside it marches on the nearest city it can walk to. It used to
// pick the nearest city of all, and when that one was across water its path failed every
// turn: a hut horde in Greenland (the user's game, turn 308) stood by the hut for good,
// counting against the barbarian cap.

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
	}
}
