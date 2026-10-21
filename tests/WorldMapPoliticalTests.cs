// CivOne tests
//
// The World Map's political page (Tab): flat land, and every known city a 3x3 block of its
// owner's colour, clipped to what the human has seen.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class WorldMapPoliticalTests : IDisposable
	{
		// Nothing resets the screen stack between tests, so each takes its map off again.
		public void Dispose()
		{
			foreach (IScreen s in Common.Screens)
				if (s is WorldMap) Common.DestroyScreen(s);
		}

		private (WorldMap screen, Func<int, int, byte> tile, City city) AMapWithACity()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 30; x <= 50; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Player rival = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			City city = g.AddCity(rival, 0, 40, 25)!;
			g.HumanPlayer.Explore(40, 25, range: 3);
			var screen = new WorldMap();
			Common.AddScreen(screen);
			int px = Math.Max(1, Math.Min(screen.Bitmap.Width / Map.WIDTH, screen.Bitmap.Height / Map.HEIGHT));
			int ox = (screen.Bitmap.Width - Map.WIDTH * px) / 2, oy = (screen.Bitmap.Height - Map.HEIGHT * px) / 2;
			// Tab used to close the map, and a closed screen's bitmap is freed: reading it
			// crashed the test host rather than failing the test.
			return (screen, (x, y) =>
			{
				Assert.Contains(screen, Common.Screens);
				return screen.Bitmap[ox + x * px, oy + y * px];
			}, city);
		}

		[Fact]
		public void TabPaintsTheCityAndItsRingInTheOwnersColour()
		{
			(WorldMap screen, Func<int, int, byte> tile, City city) = AMapWithACity();
			Assert.NotEqual(Common.ColourLight[city.Owner], tile(41, 26));   // terrain page: the ring is just land

			screen.KeyDown(new KeyboardEventArgs(Key.Tab, KeyModifier.None));

			Assert.Equal(Common.CivPattern(city.Owner, 40, 25), tile(40, 25));
			Assert.Equal(Common.CivPattern(city.Owner, 39, 24), tile(39, 24));
			Assert.Equal(Common.CivPattern(city.Owner, 41, 26), tile(41, 26));
			Assert.Equal(CassetteTheme.BG3, tile(43, 25));   // plain known land beyond the ring
			Assert.Equal(CassetteTheme.BG0, tile(46, 25));   // and the fog stays fog
		}

		// The block is the banner's pair, two tiles of primary to one of accent: six primaries
		// for 17 civs had put Russia, Mali and the Mongols in one orange.
		[Fact]
		public void TheBlockShowsTheBannerPair()
		{
			(WorldMap screen, Func<int, int, byte> tile, City city) = AMapWithACity();
			screen.KeyDown(new KeyboardEventArgs(Key.Tab, KeyModifier.None));

			byte[] block = (from dy in new[] { -1, 0, 1 } from dx in new[] { -1, 0, 1 }
			                select tile(city.X + dx, city.Y + dy)).ToArray();
			Assert.Equal(6, block.Count(c => c == Common.ColourLight[city.Owner]));
			Assert.Equal(3, block.Count(c => c == Common.BannerSecondary[city.Owner]));
		}

		// Every civ slot reads differently by eye: compared as colour PROPORTIONS over an area,
		// since that is what a dither looks like. A 1:1 mix fails this — 13/14 and 14/13 are
		// both half amber, half green.
		[Fact]
		public void NoTwoCivsShareAMix()
		{
			string Mix(int slot) => string.Join(",",
				(from y in Enumerable.Range(0, 6) from x in Enumerable.Range(0, 6) select Common.CivPattern(slot, x, y))
				.GroupBy(c => c).OrderBy(g => g.Key).Select(g => $"{g.Key}x{g.Count()}"));

			var mixes = Enumerable.Range(1, Common.ColourLight.Length - 1).Select(Mix).ToArray();
			Assert.Equal(mixes.Length, mixes.Distinct().Count());
		}

		[Fact]
		public void TabAgainReturnsToTerrain()
		{
			(WorldMap screen, Func<int, int, byte> tile, City _) = AMapWithACity();
			byte terrain = tile(41, 26);

			screen.KeyDown(new KeyboardEventArgs(Key.Tab, KeyModifier.None));
			screen.KeyDown(new KeyboardEventArgs(Key.Tab, KeyModifier.None));

			Assert.Equal(terrain, tile(41, 26));
		}
	}
}
