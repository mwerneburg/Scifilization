// CivOne tests
//
// The Dome's completion notice gave the landfall date as "Olvir landfall" whoever was coming:
// the user finished the Dome expecting the Evaluators (Nov 2026). OlvirArrivalTurn is every
// visitor's landfall; the notice now names nobody, as the approach warning does.
//
// Pinned on the source: an advisor message cannot be read back from the task queue
// (see CultureBlockedByWarTests).

namespace CivOne.Tests
{
	public class DomeNoticeTests
	{
		[Fact]
		public void TheDomeNoticeDoesNotPromiseTheOlvir()
		{
			var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			string src = System.IO.File.ReadAllText(System.IO.Path.Combine(dir!.FullName, "src", "Game.cs"));
			int at = src.IndexOf("private void CheckDomeComplete()");
			Assert.True(at > 0, "CheckDomeComplete not found");
			string body = src.Substring(at, src.IndexOf("// ── Economic dominance", at) - at);

			Assert.Contains("The visitors arrive about", body);
			Assert.DoesNotContain("\"Olvir", body);
		}
	}
}
