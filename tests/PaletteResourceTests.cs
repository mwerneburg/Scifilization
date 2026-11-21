// CivOne tests
//
// palette.txt is built into the game (Nov 2026). Without it the game fell back to a palette
// generated from the EGA 16 colours, which drew the tile art in the wrong colours and said
// nothing; the tests themselves ran on that fallback, since they find no palette.txt on disk.

using CivOne.Graphics;

namespace CivOne.Tests
{
	public class PaletteResourceTests
	{
		[Fact]
		public void ThePaletteShipsInTheAssembly()
		{
			Assert.NotNull(Common.EmbeddedPaletteLines());
		}

		// The tests find no palette.txt on disk, so what they get is the built-in copy: the
		// theme's own colours in slots 1-18, not the EGA blue the fallback put in slot 1.
		[Fact]
		public void TheGameDrawsWithTheShippedPalette()
		{
			Sim.EnsureRuntime();
			using Palette theme = CassetteTheme.CreatePalette();

			for (int i = 1; i <= 18; i++)
				Assert.Equal(theme[i], Common.GetPalette256[i]);
		}
	}
}
