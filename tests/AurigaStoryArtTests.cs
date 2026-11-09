// CivOne tests
//
// The Auriga's story shows a full-screen picture before a passage of its log, a nod to the
// original game's intro (the user, Nov 2026). A picture whose file is missing is passed over,
// so the story reads as text until the art is in.
//
// A missing picture degrades silently, so each one shipped is checked against the repository
// defaults (as CulturalAscendancyTests.TheEndingArtIsShipped does).

using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class AurigaStoryArtTests
	{
		private static string[] StoryLines() => (string[])typeof(AurigaIntroScreen)
			.GetField("Lines", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

		private static string ArtDir() => Path.Combine(Settings.Instance.DataDirectory, "event_art");

		private static void Tick(TerminalScreen story, uint tick) =>
			story.GetType().GetMethod("HasUpdate", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(story, new object[] { tick });

		private static int Printed(TerminalScreen story) => (int)typeof(TerminalScreen)
			.GetField("_printedChars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(story)!;

		private static EventArtScreen? Showing() => Common.Screens.OfType<EventArtScreen>().LastOrDefault();

		// Each scene opens a passage that is in the log; edited text would orphan it silently.
		[Fact]
		public void EveryScenePointsAtALineOfTheStory()
		{
			string[] lines = StoryLines();
			foreach (var scene in AurigaIntroScreen.Scenes)
				Assert.Contains(scene.firstLine, lines);
		}

		// With its file in, the typewriter stops at the start of the passage and shows it; it
		// types on once the picture is dismissed.
		[Fact]
		public void APictureStopsTheLogAtItsPassage()
		{
			Sim.NewGame(width: 40, height: 25);
			string[] lines = StoryLines();
			int StartOf(string first) => lines.Take(System.Array.IndexOf(lines, first)).Sum(l => l.Length + 1);
			var scene = AurigaIntroScreen.Scenes.First(s => s.key == "AurigaThirdPlanet");
			Directory.CreateDirectory(ArtDir());
			string file = Path.Combine(ArtDir(), scene.key + ".png");
			File.Copy(Path.Combine(RepoRoot(), "runtime", "sdl", "Resources", "defaults", "data", "event_art", "Gozira.png"), file, true);
			try
			{
				int start = StartOf(scene.firstLine);

				var story = new AurigaIntroScreen();
				uint t = 0;
				while (Showing() is null && t < 5000) Tick(story, ++t);
				EventArtScreen? art = Showing();
				Assert.NotNull(art);
				Assert.Equal(scene.key, art!.ArtKey);
				Assert.Equal(start, Printed(story));

				Tick(story, ++t);
				Assert.Equal(start, Printed(story));   // held while the picture is up

				art.KeyDown(null!);
				Tick(story, ++t);
				Assert.True(Printed(story) > start, "the log did not go on after the picture");
			}
			finally
			{
				File.Delete(file);
				foreach (EventArtScreen s in Common.Screens.OfType<EventArtScreen>()) s.KeyDown(null!);
			}
		}

		// A passage that starts at an odd offset: the typewriter steps two characters from zero,
		// so it carries past the start, and is wound back to it. Every passage of today's story
		// starts at an even one, so this guards the next edit to its text.
		private class OddLog : TerminalScreen
		{
			public OddLog() { _lines = new[] { "A", "BB", "CCCC" }; InitTypewriter(); }   // "CCCC" starts at 5
			protected override byte ColorFor(int lineIndex, string text) => 0;
			protected override (string key, string caption)? ArtBefore(int lineIndex) =>
				lineIndex == 2 ? ("StoryArtProbe", "probe") : null;
		}

		[Fact]
		public void APassageThatTheStepCarriesPastIsWoundBack()
		{
			Sim.NewGame(width: 40, height: 25);
			Directory.CreateDirectory(ArtDir());
			string file = Path.Combine(ArtDir(), "StoryArtProbe.png");
			File.Copy(Path.Combine(RepoRoot(), "runtime", "sdl", "Resources", "defaults", "data", "event_art", "Gozira.png"), file, true);
			try
			{
				var log = new OddLog();
				uint t = 0;
				while (Showing() is null && t < 100) Tick(log, ++t);
				Assert.NotNull(Showing());
				Assert.Equal(5, Printed(log));
			}
			finally
			{
				File.Delete(file);
				foreach (EventArtScreen s in Common.Screens.OfType<EventArtScreen>()) s.KeyDown(null!);
			}
		}

		[Theory]
		[InlineData("AurigaAdrift")]
		[InlineData("AurigaGasGiant")]
		[InlineData("AurigaThirdPlanet")]
		[InlineData("AurigaLifeboats")]
		[InlineData("AurigaCaches", Skip = "art owed by the user (Nov 2026)")]
		[InlineData("AurigaAwakening")]
		public void TheSceneArtIsShipped(string key)
		{
			string path = Path.Combine(RepoRoot(), "runtime", "sdl", "Resources", "defaults", "data", "event_art", key + ".png");
			Assert.True(File.Exists(path), $"story art is missing: {path}");
		}

		// Its control: with no art in, the log types straight through.
		[Fact]
		public void WithoutTheArtTheLogTypesThrough()
		{
			Sim.NewGame(width: 40, height: 25);
			foreach (var scene in AurigaIntroScreen.Scenes)
				Assert.Null(EventArtScreen.FindPath(scene.key));
			var story = new AurigaIntroScreen();
			int total = StoryLines().Sum(l => l.Length + 1);
			for (uint t = 1; t < 5000 && Printed(story) < total; t++) Tick(story, t);
			Assert.Equal(total, Printed(story));
			Assert.Null(Showing());
		}

		private static string RepoRoot()
		{
			var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			Assert.NotNull(dir);
			return dir!.FullName;
		}
	}
}
