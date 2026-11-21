// CivOne tests
//
// Gold is written as the number followed by a coin (IconGlyphs.Gold), as the sidebar and the
// reports show it. A few dialogs, messages and the King's offers still wrote "$120", a dollar
// sign in front, until Nov 2026 (the user asked for coins everywhere).

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CivOne.Tests
{
	public class GoldCoinTextTests
	{
		// A dollar sign in a string, in front of a number or an interpolated value: "$120",
		// $"...${price}". An interpolated string's own leading $ is not matched.
		private static readonly Regex DollarAmount = new Regex(@"""[^""\n]*?(?<![{$])\$(?:\d|\{)[^""\n]*""");

		[Fact]
		public void NoTextWritesGoldWithADollarSign()
		{
			string root = Sim.RepoRoot();
			var hits = new[] { "src", "runtime" }
				.SelectMany(d => Directory.GetFiles(Path.Combine(root, d), "*.cs", SearchOption.AllDirectories))
				.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
				         && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
				.SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, i, line)))
				.Where(x => !x.line.TrimStart().StartsWith("//") && DollarAmount.IsMatch(x.line))
				.Select(x => $"{Path.GetRelativePath(root, x.f)}:{x.i + 1}: {x.line.Trim()}")
				.ToArray();

			Assert.True(hits.Length == 0, "gold written with a dollar sign:\n" + string.Join("\n", hits));
		}
	}
}
