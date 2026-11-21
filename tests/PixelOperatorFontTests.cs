// CivOne tests
//
// The game's text face is Pixel Operator 8 (CC0), compiled into the assembly as glyph grids
// (resources/pixel_operator_8.txt). It replaced a 1-pixel-stroke fallback the user found
// "wireframe" (Nov 2026). The reports' DOS-era icon punctuation (# $ { | } \ ~) became icon
// characters of their own, and the trade and city status reports got a blank pixel row
// between data rows.

using System.Linq;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Screens.Reports;

namespace CivOne.Tests
{
	public class PixelOperatorFontTests
	{
		// A missing resource falls back to the old face in silence, so check it is there.
		[Fact]
		public void TheFontShipsWithEveryPrintableCharacter()
		{
			GlyphFont? font = GlyphFont.Load();

			Assert.NotNull(font);
			Assert.Equal(8, font!.FontHeight);
			Assert.All(Enumerable.Range(32, 95), c => Assert.True(font.Has((char)c), $"no glyph for '{(char)c}'"));
		}

		// The city, discovery and conquest headlines (font 5) and the title (font 4) space their
		// lines 15 pixels apart, for a face that size; with the text face they were 8-pixel
		// lines in 15-pixel slots. A missing resource would fall back to the text face in silence.
		[Fact]
		public void HeadlinesUseTheSixteenPixelFace()
		{
			Sim.EnsureRuntime();
			GlyphFont? headline = GlyphFont.Load(GlyphFont.HeadlineResourceName);

			Assert.NotNull(headline);
			Assert.All(Enumerable.Range(32, 95), c => Assert.True(headline!.Has((char)c), $"no glyph for '{(char)c}'"));
			Assert.Equal(16, Resources.Instance.GetFontHeight(4));
			Assert.Equal(16, Resources.Instance.GetFontHeight(5));
			Assert.Equal(8, Resources.Instance.GetFontHeight(0));
		}

		[Fact]
		public void AnIconCharacterDrawsTheIcon()
		{
			Sim.EnsureRuntime();
			Bytemap drawn = Resources.Instance.GetText(IconGlyphs.Food.ToString(), 0, CassetteTheme.INK_HIGH).Bitmap;
			Bytemap icon = Icons.Food.Bitmap;

			for (int y = 0; y < icon.Height; y++)
			for (int x = 0; x < icon.Width; x++)
				Assert.Equal(icon[x, y], drawn[x, drawn.Height - icon.Height + y]);
		}

		// The coin was drawn in the DOS palette's yellow (14), which in ours is the green
		// status colour: the sidebar's gold read as a green dot (Nov 2026).
		[Fact]
		public void TheGoldCoinIsAmber()
		{
			Sim.EnsureRuntime();
			Bytemap coin = Icons.Taxes.Bitmap;
			int amber = 0;
			for (int y = 0; y < coin.Height; y++)
			for (int x = 0; x < coin.Width; x++)
			{
				Assert.NotEqual(CassetteTheme.OK, coin[x, y]);
				if (coin[x, y] == CassetteTheme.PHOS_GLOW) amber++;
			}
			Assert.True(amber > 20, $"{amber} bright amber pixels");
		}

		// The trade icon's right arrow was the DOS palette's light green (10), in ours the faint
		// amber of a selection background, near-invisible on a dark panel (Nov 2026).
		[Fact]
		public void BothTradeArrowsShowOnADarkPanel()
		{
			Sim.EnsureRuntime();
			Bytemap trade = Icons.Trade.Bitmap;
			for (int y = 0; y < trade.Height; y++)
			for (int x = 0; x < trade.Width; x++)
				Assert.NotEqual(CassetteTheme.PHOS_FAINT, trade[x, y]);
		}

		// "Future Tech #3" and the Hall of Fame's # column mean the character itself.
		[Fact]
		public void AHashIsStillAHash()
		{
			Sim.EnsureRuntime();
			Bytemap drawn = Resources.Instance.GetText("#", 0, CassetteTheme.INK_HIGH).Bitmap;
			Bytemap hash = GlyphFont.Load()!.GetLetter('#', CassetteTheme.INK_HIGH);

			Assert.Equal(hash.Width + 1, drawn.Width);   // GetText adds the gap after each letter
			for (int y = 0; y < hash.Height; y++)
			for (int x = 0; x < hash.Width; x++)
				Assert.Equal(hash[x, y], drawn[x, y]);
		}

		// Two cities whose names have no descenders: the second name starts one font height
		// plus one blank row below the first.
		[Fact]
		public void CityStatusRowsHaveABlankRowBetweenThem()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Map.Instance.ChangeTileType(30, 25, Enums.Terrain.Grassland1);
			Map.Instance.ChangeTileType(40, 25, Enums.Terrain.Grassland1);
			g.CityNames[g.AddCity(g.HumanPlayer, 0, 30, 25)!.NameId] = "Rome";
			g.CityNames[g.AddCity(g.HumanPlayer, 1, 40, 25)!.NameId] = "Ostia";
			Sim.ClearTasks();

			var screen = new CityStatus();
			typeof(CityStatus).GetMethod("HasUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.Invoke(screen, new object[] { 0u });

			Bytemap b = screen.Bitmap;
			bool Named(int y) => Enumerable.Range(0, 60).Any(x => b[x, y] == CassetteTheme.PHOS);
			int first = Enumerable.Range(32, b.Height - 32).First(Named);
			int gap = Enumerable.Range(first, b.Height - first).First(y => !Named(y));
			int second = Enumerable.Range(gap, b.Height - gap).First(Named);

			Assert.Equal(Resources.Instance.GetFontHeight(0) + 1, second - first);
		}
	}
}
