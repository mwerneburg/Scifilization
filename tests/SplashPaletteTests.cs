// CivOne tests
//
// The splash is median-cut to an indexed palette, and Credits draws it behind the main menu:
// margins cleared with index 0, the menu box and text in fixed low indices. When the image
// owned every index, the Scifilization splash put yellow in all of them and the menu vanished
// into a yellow screen. The low indices are now black and the Cassette theme.

using System.Linq;
using CivOne.Graphics;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class SplashPaletteTests
	{
		private static Picture AYellowSplash()
		{
			Sim.EnsureRuntime(); Sim.ResetState();
			const int w = 64, h = 40;
			var rgba = new byte[w * h * 4];
			for (int i = 0; i < w * h; i++)
			{
				rgba[i * 4] = 246; rgba[i * 4 + 1] = (byte)(200 + i % 40); rgba[i * 4 + 2] = 74; rgba[i * 4 + 3] = 255;
			}
			SplashData before = Resources.SplashRawImage;
			try
			{
				Resources.SplashRawImage = new SplashData(w, h, rgba);
				return Splash.MakePicture(320, 200)!;
			}
			finally { Resources.SplashRawImage = before; }
		}

		[Fact]
		public void TheMarginColourIsBlackWhateverTheImage()
		{
			Picture pic = AYellowSplash();

			Colour c = pic.Palette[0];
			Assert.Equal((0, 0, 0), (c.R, c.G, c.B));
		}

		[Fact]
		public void TheMenuColoursAreTheTheme()
		{
			Picture pic = AYellowSplash();

			using Palette theme = CassetteTheme.CreatePalette();
			foreach (int i in new[] { 5, 7, 8, 11, 15 })
				Assert.Equal((theme[i].R, theme[i].G, theme[i].B), (pic.Palette[i].R, pic.Palette[i].G, pic.Palette[i].B));
		}

		// Layers skip index 0 as transparent, so an image pixel there was a hole in the splash.
		[Fact]
		public void NoImagePixelUsesAReservedIndex()
		{
			Picture pic = AYellowSplash();

			for (int y = 0; y < 200; y++)
			for (int x = 0; x < 320; x++)
				Assert.True(pic.Bitmap[x, y] > CassetteTheme.OCEAN, $"pixel {x},{y} is index {pic.Bitmap[x, y]}");
		}
	}
}
