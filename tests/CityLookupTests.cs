// CivOne tests
//
// ITile.City is Game.GetCity, and it was a scan of every city in the world. The pathfinder
// reads it several times per neighbour it examines, so at 787 cities one node cost 75us and
// a Hover Tank's route 125ms; a replayed late turn of game 6d2f0cca took 32s, 6s indexed.
// The index is only worth having if it never goes stale, so most of this file is about that.

using System.Diagnostics;
using System.IO;
using System.Linq;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class CityLookupTests
	{
		private static (Game g, Player p) ALand()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 1; x < 79; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			p.Explore(40, 25, range: 40);
			return (g, p);
		}

		[Fact]
		public void AFoundedCityIsSeenAtOnce()
		{
			(Game g, Player p) = ALand();
			Assert.Null(Map.Instance[40, 25].City);   // the index is built here, without it

			City c = g.AddCity(p, 0, 40, 25)!;

			Assert.Same(c, Map.Instance[40, 25].City);
		}

		[Fact]
		public void ARazedCityIsGone()
		{
			(Game g, Player p) = ALand();
			City c = g.AddCity(p, 0, 40, 25)!;
			Assert.Same(c, Map.Instance[40, 25].City);

			g.DestroyCity(c);

			Assert.Null(Map.Instance[40, 25].City);
		}

		// Player.Cities and every reader before the index treat a size-0 city as no city.
		[Fact]
		public void AnEmptiedCityIsNoCity()
		{
			(Game g, Player p) = ALand();
			City c = g.AddCity(p, 0, 40, 25)!;
			Assert.Same(c, Map.Instance[40, 25].City);

			c.Size = 0;

			Assert.Null(Map.Instance[40, 25].City);
		}

		// One city razed and another founded between two lookups leaves the count where it
		// was; the roster version is what notices.
		[Fact]
		public void ARazingAndAFoundingBetweenLookups()
		{
			(Game g, Player p) = ALand();
			City old = g.AddCity(p, 0, 20, 25)!;
			Assert.Null(Map.Instance[40, 25].City);

			g.DestroyCity(old);
			City c = g.AddCity(p, 1, 40, 25)!;

			Assert.Same(c, Map.Instance[40, 25].City);
		}

		[Fact]
		public void EveryCitySurvivesASaveAndLoad()
		{
			(Game g, Player p) = ALand();
			int[] xs = { 10, 20, 30, 40, 50 };
			foreach ((int x, int i) in xs.Select((x, i) => (x, i))) g.AddCity(p, i, x, 25);
			string path = Path.Combine(Settings.Instance.SavesDirectory, "citylookup.cos");
			g.SaveCos(path);

			Sim.ResetState();
			Assert.True(Game.LoadCos(path));

			Assert.All(xs, x => Assert.NotNull(Map.Instance[x, 25].City));
		}

		// The cost: a lookup must not grow with the number of cities. Measured here at 800
		// cities: 0.6-1.1x indexed, 6.8x for the old scan (tile access pads the baseline), so a
		// 2.5x bar is wide on both sides (see WaterBodyCostTests for that lesson).
		[Fact]
		public void ALookupDoesNotScaleWithCityCount()
		{
			(Game g, Player p) = ALand();
			g.AddCity(p, 0, 40, 25);
			double Time()
			{
				var sw = Stopwatch.StartNew();
				for (int i = 0; i < 200000; i++) _ = Map.Instance[1 + i % 78, 20 + i % 11].City;
				return sw.Elapsed.TotalMilliseconds;
			}
			Time();
			double one = Time();

			int made = 1;
			for (int y = 20; y <= 30; y++)
			for (int x = 1; x < 79 && made < 800; x++)
				if (g.AddCity(p, made % 250, x, y) is not null) made++;
			Assert.True(made > 600, $"needed a crowded world, only placed {made}");
			Time();
			double crowded = Time();

			Assert.True(crowded < one * 2.5, $"GetCity scaled with city count: {one:F1}ms with one city, {crowded:F1}ms with {made}");
		}
	}
}
