// CivOne tests
//
// Explorers left huts standing. The hut bonus in BestExploreTile decayed past a flat 3 tiles
// from home, a Standard-map number, so on Epic it was gone 15 tiles out; and a hut counted only
// inside the Explorer's own 8-tile scan, so one it had walked away from stayed for good. At t473
// of game c332af23, 28 of the 99 huts were left, every one already seen. An Explorer now clears
// the huts near home first, "near" scaled to the map (the user, Oct 2026).

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class ExplorerHutTests
	{
		// Plains, no huts but the one placed; the AI's city at (cx,cy) and its Explorer ten tiles
		// east of the hut, beyond its scan, with unexplored ground all round it.
		private static (Player ai, IUnit explorer) AHutBehindTheExplorer(int width, int height, int cx, int cy, int hutX)
		{
			Sim.NewGame(width: width, height: height);
			for (int y = cy - 15; y <= cy + 15; y++)
			for (int x = cx - 15; x <= cx + 35; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			Map.Instance.RecalculateContinentsIfDirty();
			for (int y = 0; y < Map.HEIGHT; y++)
			for (int x = 0; x < Map.WIDTH; x++)
				Map.Instance[x, y].Hut = false;
			Map.Instance[hutX, cy].Hut = true;

			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			g.AddCity(ai, 0, cx, cy);
			IUnit explorer = g.CreateUnit(UnitType.Explorer, hutX + 10, cy, g.PlayerNumber(ai))!;
			Sim.ClearTasks();
			return (ai, explorer);
		}

		[Fact]
		public void AnExplorerClearsTheHutsNearHomeFirst()
		{
			var (ai, explorer) = AHutBehindTheExplorer(80, 50, cx: 30, cy: 25, hutX: 32);

			ITile? dest = AI.Instance(ai).BestExploreTile(explorer);

			Assert.NotNull(dest);
			Assert.True(dest!.X == 32 && dest.Y == 25, $"went for ({dest.X},{dest.Y}), not the hut two tiles from home");
		}

		// Eight tiles from home is close on a 320-wide map: inside the scaled radius of 12, well
		// past the old flat 3.
		[Fact]
		public void OnEpicNearHomeIsTwelveTiles()
		{
			var (ai, explorer) = AHutBehindTheExplorer(320, 200, cx: 110, cy: 100, hutX: 118);

			ITile? dest = AI.Instance(ai).BestExploreTile(explorer);

			Assert.NotNull(dest);
			Assert.True(dest!.X == 118 && dest.Y == 100, $"went for ({dest.X},{dest.Y}), not the hut eight tiles from home");
		}
	}
}
