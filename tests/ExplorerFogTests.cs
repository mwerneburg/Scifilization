// CivOne tests
//
// An Explorer looks 8 tiles around itself, and went to sleep for good when that box was charted.
// On the Epic map that left explorers asleep with thousands of unseen tiles on their own continent.
// It now walks to the nearest unseen land it can reach (AI.NearestFog).

using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class ExplorerFogTests
	{
		private const int Row = 100, West = 40, East = 120;

		// A land strip with ocean all round it, an Explorer near its east end, and only the east
		// end charted. East, so that the first fog a row-by-row scan meets is the far west end
		// and not the nearest.
		private static (Player ai, IUnit explorer) ACharteredBox()
		{
			Sim.NewGame(width: 320, height: 200);
			for (int y = Row - 20; y <= Row + 20; y++)
			for (int x = West - 20; x <= East + 40; x++)
			{
				bool land = y >= Row - 3 && y <= Row + 3 && x >= West && x <= East;
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Plains : Terrain.Ocean);
			}
			Map.Instance.RecalculateContinentsIfDirty();
			for (int y = 0; y < Map.HEIGHT; y++)
			for (int x = 0; x < Map.WIDTH; x++)
				Map.Instance[x, y].Hut = false;
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			for (int x = East - 25; x <= East; x++) ai.Explore(x, Row, range: 5);
			IUnit explorer = g.CreateUnit(UnitType.Explorer, East - 5, Row, g.PlayerNumber(ai), false)!;
			Sim.ClearTasks();
			return (ai, explorer);
		}

		[Fact]
		public void WithTheBoxChartedAnExplorerHeadsForTheNearestFog()
		{
			(Player ai, IUnit explorer) = ACharteredBox();
			Assert.Null(AI.Instance(ai).BestExploreTile(explorer));

			ITile? fog = AI.Instance(ai).NearestFog(explorer);

			Assert.NotNull(fog);
			Assert.False(ai.Visible(fog!.X, fog.Y));
			Assert.InRange(fog.X, East - 31, East - 26);
		}

		[Fact]
		public void ItWalksInsteadOfSleeping()
		{
			(Player ai, IUnit explorer) = ACharteredBox();

			typeof(AI).GetMethod("AssignMission", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(AI.Instance(ai), new object[] { explorer });

			Assert.False(explorer.Sentry);
			Assert.InRange(explorer.Goto.X, East - 31, East - 26);
		}

		// Unseen land on another island is nearer than anything, and still out of reach on foot.
		[Fact]
		public void FogAcrossTheWaterIsNoReasonToSetOff()
		{
			(Player ai, IUnit explorer) = ACharteredBox();
			for (int x = West; x < East - 25; x++)
			for (int y = Row - 3; y <= Row + 3; y++)
				ai.Explore(x, y, range: 1);
			for (int y = Row - 2; y <= Row + 2; y++)
			for (int x = East + 20; x <= East + 24; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			Map.Instance.RecalculateContinentsIfDirty();

			Assert.Null(AI.Instance(ai).NearestFog(explorer));
		}

		// Chieftain Militia share the short scan, and must not march across the world on it.
		[Fact]
		public void TheShortScanStaysShort()
		{
			(Player ai, IUnit explorer) = ACharteredBox();

			Assert.Null(AI.Instance(ai).BestExploreTile(explorer));
		}
	}
}
