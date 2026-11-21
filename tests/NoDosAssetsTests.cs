// CivOne tests
//
// The game once loaded the original DOS files (pictures, palettes, the font, the text) when a
// player put them in the data folder, and fell back to its own art when they were missing. The
// user's own install still had the DOS font in use, months after the art was replaced, so the
// game they tested was not the one they shipped (Nov 2026). That path is gone. This keeps any
// of its file names from coming back as a string the code looks up.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CivOne.Tests
{
	public class NoDosAssetsTests
	{
		// A string literal (or the start of an interpolated one) naming a DOS file.
		private static readonly Regex DosName = new Regex(
			@"\$?""(?:SP25[67]|SP299|TER257|SPRITES|ICONPG|CITYPIX|SLAM[12]|NUKE1|DIFFS|ARCH|BIRTH|LOGO|KING\d|GOVT|DISCOVR|BLURB|STORY|CREDITS|FONTS)(?:[0-9A-Z]*)(?:\{[^""]*\})?(?:\.(?:PIC|PAL|TXT|CV))?""|""[^""\n]*\.(?:PIC|PAL|CVL?)""|""CIV\.EXE""");

		[Fact]
		public void NoCodeLooksUpADosFile()
		{
			string root = Sim.RepoRoot();
			var hits = new[] { "src", "runtime", "shared" }
				.SelectMany(d => Directory.GetFiles(Path.Combine(root, d), "*.cs", SearchOption.AllDirectories))
				.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
				         && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
				.SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, i, line)))
				.Where(x => !x.line.TrimStart().StartsWith("//") && DosName.IsMatch(x.line))
				.Select(x => $"{Path.GetRelativePath(root, x.f)}:{x.i + 1}: {x.line.Trim()}")
				.ToArray();

			Assert.True(hits.Length == 0, "DOS file names in code:\n" + string.Join("\n", hits));
		}
	}
}
