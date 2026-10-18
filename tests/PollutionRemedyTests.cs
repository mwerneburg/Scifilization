// CivOne tests
//
// Two remedies for a problem only Settlers used to touch once it was on the ground, and the
// page that shows the player where they stand.
//
//   * The Kyoto Accord raises the warming threshold half again, for everybody — except
//     against the Reprocessor, which the machines never signed up to.
//   * Carbon Capture cleans one polluted tile in its city's radius each turn.
//   * The Environmental Advisor reads Game.WarmingThreshold, so the margin it promises must
//     be the one HandleGlobalWarming honours, to the tile.

using System.IO;
using System.Linq;
using CivOne;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Screens;
using CivOne.Screens.Reports;
using CivOne.Tiles;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class PollutionRemedyTests
	{
		private static int Pollute(int n)
		{
			int done = 0;
			foreach (ITile tile in Map.Instance.AllTiles().Where(t => !t.IsOcean && t.City is null && !t.Pollution))
			{
				if (done >= n) break;
				tile.Pollution = true;
				done++;
			}
			return done;
		}

		private static City ACity(Player owner, int x, int y = 25)
		{
			for (int ty = y - 3; ty <= y + 3; ty++)
			for (int tx = x - 3; tx <= x + 3; tx++)
				Map.Instance.ChangeTileType(tx, ty, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			owner.Explore(x, y, range: 3);
			City c = Game.Instance.AddCity(owner, x + y, x, y)!;
			c.Size = 6;
			return c;
		}

		private static Game AWorld()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			g.GlobalWarmingCount = 0;
			return g;
		}

		// ── the Accord ──────────────────────────────────────────────────────

		[Fact]
		public void TheAccordHoldsBackAWarmingThatWouldOtherwiseCome()
		{
			Game g = AWorld();
			int bare = g.WarmingThreshold;
			ACity(g.HumanPlayer, 40).AddWonder(new KyotoAccord());

			Assert.Equal(bare * 3 / 2, g.WarmingThreshold);
			Pollute(bare * 6 / 5);   // over the old bar, under the new one
			g.HandleGlobalWarming();
			Assert.Equal(0, g.GlobalWarmingCount);
		}

		[Fact]
		public void WithoutTheAccordTheSameSmokeWarms()
		{
			Game g = AWorld();
			ACity(g.HumanPlayer, 40);
			Pollute(g.WarmingThreshold * 6 / 5);
			g.HandleGlobalWarming();
			Assert.Equal(1, g.GlobalWarmingCount);
		}

		[Fact]
		public void TheMachinesNeverSignedIt()
		{
			Game g = AWorld();
			int bare = g.WarmingThreshold;
			Player m = new Player(Common.Civilizations.First(c => c is Skynet));
			g.AddPlayer(m);
			ACity(m, 20).AddWonder(new TheReprocessor());
			ACity(g.HumanPlayer, 50).AddWonder(new KyotoAccord());
			g.GameTurn++;   // ReprocessorActive is cached per turn

			Assert.True(g.ReprocessorActive);
			Assert.Equal(bare / 2, g.WarmingThreshold);
		}

		// ── Carbon Capture ──────────────────────────────────────────────────

		private static (City city, ITile tile) APollutedCity(bool capture)
		{
			Game g = AWorld();
			City c = ACity(g.HumanPlayer, 40);
			if (capture) c.AddBuilding(new CarbonCapture());
			ITile tile = Map.Instance[41, 26];
			tile.Pollution = true;
			Sim.ClearTasks();
			return (c, tile);
		}

		[Fact]
		public void CarbonCaptureCleansAPollutedTile()
		{
			(City c, ITile tile) = APollutedCity(capture: true);
			c.NewTurn();
			Assert.False(tile.Pollution);
		}

		[Fact]
		public void WithoutCarbonCaptureTheTileStaysPolluted()
		{
			(City c, ITile tile) = APollutedCity(capture: false);
			c.NewTurn();
			Assert.True(tile.Pollution);
		}

		// ── the Environmental Advisor ───────────────────────────────────────

		// The margin on the page is the margin in the climate: one tile short of the advertised
		// number holds, the number itself warms. With the Accord, so the page cannot quote the
		// old bar.
		[Fact]
		public void WarmingComesExactlyAtTheAdvertisedThreshold()
		{
			Game g = AWorld();
			ACity(g.HumanPlayer, 40).AddWonder(new KyotoAccord());
			int advertised = g.WarmingThreshold;

			Pollute(advertised - 1);
			g.HandleGlobalWarming();
			Assert.Equal(0, g.GlobalWarmingCount);

			Pollute(1);
			g.HandleGlobalWarming();
			Assert.Equal(1, g.GlobalWarmingCount);
		}

		[Fact]
		public void TheAdvisorDrawsWithACityAndTheAccord()
		{
			Game g = AWorld();
			City c = ACity(g.HumanPlayer, 40);
			c.AddWonder(new KyotoAccord());
			c.AddBuilding(new CarbonCapture());
			Map.Instance[41, 26].Pollution = true;

			var screen = new EnvironmentalAdvisor();
			Assert.True(screen.Update(0));
		}

		// ── art ─────────────────────────────────────────────────────────────

		[Theory]
		[InlineData("Kyoto Accord")]
		[InlineData("Carbon Capture")]
		public void TheArtIsShipped(string name)
		{
			DirectoryInfo? dir = new DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			if (dir is null) return;
			string path = Path.Combine(dir.FullName, "runtime", "sdl", "Resources", "defaults", "data",
				"improvement_art", name.ToLower().Replace(' ', '_') + ".png");
			Assert.True(File.Exists(path), $"improvement art is missing: {path}");
		}
	}
}
