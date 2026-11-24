// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.IO;
using System.Linq;
using CivOne.Events;
using CivOne.Screens.Dialogs;
using Xunit;

namespace CivOne.Tests
{
	// Vesta, the newcomer's guide (Guide.cs, Nov 2026): one note per moment, once a game, on the
	// Instant Advice switch. Sim.NewGame plays at Chieftain, where the switch defaults on.
	public class GuideTests
	{
		private static GuideNote[] AllNotes => (GuideNote[])Enum.GetValues(typeof(GuideNote));

		// Measured at draw time with the game's own font, not by counting characters: the
		// width of a line is the font's business (see the font-width trap of Nov 2026).
		// "Constantinople" stands in for the longest city name a note can carry.
		[Fact]
		public void EveryNoteFitsOnTheScreen()
		{
			Sim.EnsureRuntime();
			foreach (GuideNote n in AllNotes)
			{
				string[] text = Guide.Text(n, "Constantinople");
				Assert.True(text.Length > 0, $"{n} has no text");
				int left = GuideMessage.LeftFor(text), width = GuideMessage.WidthFor(text);
				int bottom = GuideMessage.TopFor + GuideMessage.HeightFor(text) + 2;
				Assert.True(left >= 0 && left + width + 2 <= 320, $"{n} is {width} px wide");
				Assert.True(bottom <= 200, $"{n} runs to y={bottom}");
			}
		}

		// Her badge ([guide] in advisor_badges.txt) is held by TileSectionCoverageTests, which
		// reads every badge GuideMessage names against the shipped file.

		[Fact]
		public void ANoteShowsOnceAGame()
		{
			Sim.NewGame();
			Assert.NotNull(Guide.Take(GuideNote.Hut));
			Assert.Null(Guide.Take(GuideNote.Hut));
		}

		// Off means off, and a note held back by the switch is still owed if it comes back on.
		[Fact]
		public void TheSwitchHoldsNotesBack()
		{
			Sim.NewGame();
			Game.Instance.InstantAdvice = false;
			Assert.Null(Guide.Take(GuideNote.Hut));

			Game.Instance.InstantAdvice = true;
			Assert.NotNull(Guide.Take(GuideNote.Hut));
		}

		[Fact]
		public void PressingNStopsTheNotes()
		{
			Sim.NewGame();
			GuideMessage note = Guide.Take(GuideNote.Hut)!;

			note.KeyDown(new KeyboardEventArgs('n'));

			Assert.False(Game.Instance.InstantAdvice);
		}

		[Fact]
		public void ShownNotesSurviveASave()
		{
			Sim.NewGame();
			Guide.Take(GuideNote.Monarchy);
			string path = Path.Combine(Settings.Instance.SavesDirectory, "guide.cos");
			Game.Instance.SaveCos(path);

			Sim.ResetState();
			Assert.True(Game.LoadCos(path));

			Assert.Contains(GuideNote.Monarchy, Game.Instance.GuideNotesShown);
			Assert.DoesNotContain(GuideNote.Hut, Game.Instance.GuideNotesShown);
		}

		// ── the hooks ──────────────────────────────────────────────────────────

		[Fact]
		public void TheFirstCityIsTheHumansNotTheWorlds()
		{
			Sim.NewGame();
			Game g = Game.Instance;
			Player rival = g.Players.First(p => p is not null && p != g.HumanPlayer && g.PlayerNumber(p) != 0);

			g.AddCity(rival, 0, 10, 10);
			Assert.DoesNotContain(GuideNote.FirstCity, g.GuideNotesShown);

			g.AddCity(g.HumanPlayer, 1, 20, 20);
			Assert.Contains(GuideNote.FirstCity, g.GuideNotesShown);
		}

		[Fact]
		public void LearningMonarchyPointsToTheRevolution()
		{
			Sim.NewGame();
			Game g = Game.Instance;
			Player rival = g.Players.First(p => p is not null && p != g.HumanPlayer && g.PlayerNumber(p) != 0);

			rival.AddAdvance(new Advances.Monarchy(), false);
			Assert.DoesNotContain(GuideNote.Monarchy, g.GuideNotesShown);

			g.HumanPlayer.AddAdvance(new Advances.Monarchy(), false);
			Assert.Contains(GuideNote.Monarchy, g.GuideNotesShown);
		}

		// Civ 1's dated notes read HELP.TXT, which went with the DOS files: every one popped an
		// empty box at Chieftain. Nothing may ask for them now.
		[Fact]
		public void NoEmptyHelpNotesRemain()
		{
			string src = Path.Combine(Sim.RepoRoot(), "src");
			string[] askers = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
				.Where(f => File.ReadAllText(f).Contains("GetGameText(\"HELP/")).ToArray();
			Assert.Empty(askers);
		}
	}
}
