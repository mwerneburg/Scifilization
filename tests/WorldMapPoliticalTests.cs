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
			byte colour = Common.ColourLight[city.Owner];
			Assert.NotEqual(colour, tile(41, 26));   // terrain page: the ring is just land

			screen.KeyDown(new KeyboardEventArgs(Key.Tab, KeyModifier.None));

			Assert.Equal(colour, tile(40, 25));
			Assert.Equal(colour, tile(39, 24));
			Assert.Equal(colour, tile(41, 26));
			Assert.Equal(CassetteTheme.BG3, tile(43, 25));   // plain known land beyond the ring
			Assert.Equal(CassetteTheme.BG0, tile(46, 25));   // and the fog stays fog
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
