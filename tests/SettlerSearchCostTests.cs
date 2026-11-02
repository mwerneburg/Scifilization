// CivOne tests
//
// A late settler's turn was 12.8 ms a move in game cea9d583, and on its t653 save the search
// for work cost 22 ms a settler: it asked every tile in its box whether a foreign unit stood
// there before asking anything cheaper, and a tile's units are a scan of every unit in the
// game, 9,895 of them. The search for a city site likewise scanned every city in the world
// per tile. Both now ask a handful of whole-world questions per search, not one per tile.
// Counted rather than timed, so a busy machine cannot fail them.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class SettlerSearchCostTests
	{
		private const int AFewScans = 10;

		private static (Game g, Player ai, IUnit settler) APlain(bool river)
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			if (river)
				for (int y = 20; y <= 30; y++) Map.Instance.ChangeTileType(39, y, Terrain.River);
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			ai.Explore(40, 25, range: 25);
			g.AddCity(ai, 0, 26, 25);
			g.AddCity(ai, 1, 37, 25);
			Sim.ClearTasks();
			return (g, ai, g.CreateUnit(UnitType.Settlers, 26, 25, g.PlayerNumber(ai))!);
		}

		// Dry plains near the settler: every pass of both boxes runs, the worst case.
		[Fact]
		public void TheSearchForWork_AsksAFewWorldScans()
		{
			var (_, ai, settler) = APlain(river: true);
			long before = Game.WorldScans;

			ITile? site = AI.Instance(ai).BestImproveSite(settler);

			Assert.NotNull(site);   // the river work twelve tiles out: the far box was searched
			long scans = Game.WorldScans - before;
			Assert.True(scans <= AFewScans, $"{scans} whole-world scans for one search");
		}

		[Fact]
		public void TheSearchForACitySite_AsksAFewWorldScans()
		{
			var (_, ai, settler) = APlain(river: false);
			long before = Game.WorldScans;

			ITile? site = AI.Instance(ai).BestSettleSite(settler);

			Assert.NotNull(site);
			long scans = Game.WorldScans - before;
			Assert.True(scans <= AFewScans, $"{scans} whole-world scans for one search");
		}
	}
}
