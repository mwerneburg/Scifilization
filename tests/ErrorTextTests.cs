// CivOne tests
//
// Every refusal has words. Asset-free mode ships no ERROR.TXT, so a key missing from
// TextFile._fallback pops an empty red box: a non-Settler told to build a road got exactly
// that (the user, Oct 2026). Keys are read from the source, both as "ERROR/X" literals and
// as Orders.Error("X"), which builds the key at runtime, so a new one cannot ship blank.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CivOne.Tests
{
	public class ErrorTextTests
	{
		private static string[] Keys()
		{
			string src = Path.Combine(Sim.RepoRoot(), "src");
			string all = string.Concat(Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
			string orders = File.ReadAllText(Path.Combine(src, "Tasks", "Orders.cs"));
			return Regex.Matches(all, "\"ERROR/([A-Z]+)\"").Cast<Match>()
				.Concat(Regex.Matches(orders, "Error\\(\"([A-Z]+)\"\\)").Cast<Match>())
				.Select(m => m.Groups[1].Value).Distinct().OrderBy(k => k).ToArray();
		}

		[Fact]
		public void EveryErrorKeyHasText()
		{
			Sim.EnsureRuntime();
			string[] keys = Keys();
			Assert.Contains("SETTLERS", keys);   // scenario: the scan still sees Orders.Error

			string[] blank = keys.Where(k => !IO.TextFile.Instance.GetGameText($"ERROR/{k}").Any(l => l.Length > 0)).ToArray();

			Assert.Empty(blank);
		}
	}
}
