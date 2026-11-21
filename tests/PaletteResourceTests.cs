// CivOne tests
//
// palette.txt is built into the game (Nov 2026). Without it the game fell back to a palette
// generated from the EGA 16 colours, which drew the tile art in the wrong colours and said
// nothing; the tests themselves ran on that fallback, since they find no palette.txt on disk.
//
// Slots 19-255 held the DOS SP257 colours until design/build_palette.py replaced them with
// our own: the slots the tiles paint with keep their number and lightness and take their hue
// from a family; the rest come from our own art. A re-run must not move a slot's meaning.

using System.Linq;
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
		// theme's own colours in slots 1-18 and the waves in 96-103, not the EGA blue the
		// fallback put in slot 1.
		[Fact]
		public void TheGameDrawsWithTheShippedPalette()
		{
			Sim.EnsureRuntime();
			using Palette theme = CassetteTheme.CreatePalette();

			foreach (int i in System.Linq.Enumerable.Range(1, 18).Concat(System.Linq.Enumerable.Range(96, 8)))
				Assert.Equal(theme[i], Common.GetPalette256[i]);
		}

		// The colours most of the map is drawn in still mean what the tiles were painted as.
		[Theory]
		[InlineData(35, "green")]   // grassland, a third of the map
		[InlineData(34, "green")]
		[InlineData(38, "green")]   // forest
		[InlineData(232, "warm")]   // desert
		[InlineData(54, "warm")]
		[InlineData(66, "blue")]    // lakes
		[InlineData(67, "blue")]    // rivers
		[InlineData(22, "grey")]    // arctic, tundra, rock
		public void APaintedSlotKeepsItsMeaning(int slot, string kind)
		{
			Sim.EnsureRuntime();
			Colour c = Common.GetPalette256[slot];
			int max = System.Math.Max(c.R, System.Math.Max(c.G, c.B)), min = System.Math.Min(c.R, System.Math.Min(c.G, c.B));
			bool ok = kind switch
			{
				"green" => c.G > c.R && c.G > c.B,
				"warm"  => c.R > c.G && c.G > c.B,
				"blue"  => c.B > c.R && c.B > c.G,
				_       => max - min < 24,
			};
			Assert.True(ok, $"slot {slot} is #{c.R:x2}{c.G:x2}{c.B:x2}, not {kind}");
		}

		// Forest is darker than the grassland around it, or the woods do not read as woods.
		[Fact]
		public void ForestIsDarkerThanGrassland()
		{
			Sim.EnsureRuntime();
			static int Luma(Colour c) => c.R * 299 + c.G * 587 + c.B * 114;
			Assert.True(Luma(Common.GetPalette256[38]) < Luma(Common.GetPalette256[35]));
		}
	}
}
