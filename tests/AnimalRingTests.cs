// CivOne tests
//
// The animal specials (sheep, deer, cassowary, wallaby) and the swamp oil carry a ring round
// them that lifts them off a busy tile. It was drawn in slot 41, a forest green the new
// palette made neon (66,156,64), and 41 is the forest sprites' own green, so it could not
// simply be toned down. The ring has its own slot, 48, a sage the user chose (Nov 2026).

using System.IO;
using System.Linq;
using CivOne.Graphics;

namespace CivOne.Tests
{
	public class AnimalRingTests
	{
		private const int Ring = 48;

		private static int[] Section(string name)
		{
			string[] lines = File.ReadAllLines(Path.Combine(Sim.RepoRoot(), Common.RepoArtDir, "free_tiles.txt"));
			int start = System.Array.IndexOf(lines, $"[{name}]");
			Assert.True(start >= 0, $"[{name}] is missing");
			return lines.Skip(start + 1).TakeWhile(l => !l.StartsWith("[") && !l.StartsWith("#"))
				.SelectMany(l => l.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)).Select(int.Parse).ToArray();
		}

		[Theory]
		[InlineData("special_plains")]
		[InlineData("special_forest")]
		[InlineData("special_forest_cassowary")]
		[InlineData("special_forest_wallaby")]
		[InlineData("special_swamp")]
		public void TheRingIsDrawnInItsOwnSlot(string section)
		{
			int[] pixels = Section(section);
			Assert.Contains(Ring, pixels);
			Assert.DoesNotContain(41, pixels);
		}

		// Sage: a little green, but nowhere near the saturated greens of the forest.
		[Fact]
		public void TheRingIsMuted()
		{
			Sim.EnsureRuntime();
			Colour c = Common.GetPalette256[Ring];
			int max = System.Math.Max(c.R, System.Math.Max(c.G, c.B)), min = System.Math.Min(c.R, System.Math.Min(c.G, c.B));
			Assert.True(c.G >= c.R && c.G > c.B && max - min < 48, $"slot {Ring} is #{c.R:x2}{c.G:x2}{c.B:x2}, not a muted green");
		}
	}
}
