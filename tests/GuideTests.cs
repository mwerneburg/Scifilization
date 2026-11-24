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
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Governments;
using CivOne.Tasks;
using CivOne.Units;
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
		// Each note carries the longest subject it can: "Constantinople" stands in for a city.
		private static string Longest(GuideNote n) => (n switch
		{
			GuideNote.Wonder => Common.Wonders.Select(w => w.Name),
			GuideNote.Materials => Enum.GetNames(typeof(StrategicResource)),
			_ => new[] { "Constantinople" },
		}).OrderByDescending(x => x.Length).First();

		[Fact]
		public void EveryNoteFitsOnTheScreen()
		{
			Sim.EnsureRuntime();
			foreach (GuideNote n in AllNotes)
			{
				string[] text = Guide.Text(n, Longest(n));
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

		// ── the hooks (each fixture starts on the player's own grassland) ─────────

		// A human town at (40,25) on irrigated grassland, Instant Advice on whatever the level.
		private static (Game g, Player human, City city) AHumanTown(int size, int difficulty = 0)
		{
			Sim.NewGame(width: 80, height: 50, difficulty: difficulty);
			for (int y = 15; y <= 35; y++)
			for (int x = 20; x <= 60; x++)
			{
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
				Map.Instance[x, y].Irrigation = true;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			g.InstantAdvice = true;
			Player human = g.HumanPlayer;
			human.Explore(40, 25, range: 10);
			City c = g.AddCity(human, 0, 40, 25)!;
			c.Size = (byte)size;
			c.ResetResourceTiles();
			Sim.ClearTasks();
			return (g, human, c);
		}

		[Fact]
		public void ALoneCityReachingSizeThreeIsToldToSpread()
		{
			var (g, _, city) = AHumanTown(2);
			city.Food = city.FoodRequired;

			city.NewTurn();

			Assert.Equal(3, (int)city.Size);
			Assert.Contains(GuideNote.Grow, g.GuideNotesShown);
		}

		[Fact]
		public void ACityGoingIntoDisorderIsExplained()
		{
			var (g, _, city) = AHumanTown(10, difficulty: 4);
			Assert.True(city.IsInDisorder, "fixture: the city is calm");

			city.NewTurn();

			Assert.Contains(GuideNote.Disorder, g.GuideNotesShown);
		}

		[Fact]
		public void TheFirstSettlersBuiltHearsAboutTerrainWork()
		{
			var (g, _, city) = AHumanTown(4);
			city.SetProduction(new Settlers());
			city.Shields = city.ProductionCost(city.CurrentProduction);

			city.NewTurn();

			Assert.Contains(GuideNote.Settlers, g.GuideNotesShown);
		}

		// Every outcome of a hut goes through the same message, so the roll does not matter.
		[Fact]
		public void EnteringAHutIsExplained()
		{
			var (g, human, _) = AHumanTown(1);
			Map.Instance[43, 25].Hut = true;
			IUnit scout = g.CreateUnit(UnitType.Militia, 42, 25, g.PlayerNumber(human))!;
			scout.MovesLeft = scout.Move;

			scout.MoveTo(1, 0);
			Sim.Settle();

			Assert.Contains(GuideNote.Hut, g.GuideNotesShown);
		}

		// A barbarian boat with a Legion aboard, beside the shore three tiles from our town.
		[Fact]
		public void RaidersLandingNearUsAreExplained()
		{
			var (g, _, _) = AHumanTown(1);
			for (int y = 20; y <= 30; y++)
			for (int x = 44; x <= 50; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Ocean);
			Map.Instance.RecalculateContinentsIfDirty();
			g.HumanPlayer.Explore(44, 25, range: 3);
			IUnit boat = g.CreateUnit(UnitType.Trireme, 44, 25, 0)!;
			g.CreateUnit(UnitType.Legion, 44, 25, 0);

			AI.Instance(g.GetPlayer(0)).Move(boat);

			Assert.Contains(GuideNote.Barbarians, g.GuideNotesShown);
		}

		[Fact]
		public void ALowTreasuryIsExplained()
		{
			var (g, human, city) = AHumanTown(4);
			city.AddBuilding(new Cathedral());
			city.AddBuilding(new Temple());
			human.Gold = 5;

			human.NewTurn();

			Assert.Contains(GuideNote.Treasury, g.GuideNotesShown);
		}

		// On top of the choice it explains: it is added after the ChooseTech screen.
		[Fact]
		public void TheFirstResearchChoiceIsExplainedOnTopOfIt()
		{
			Sim.NewGame(width: 40, height: 30, competition: 4);
			Player human = Game.Instance.HumanPlayer;
			human.CurrentResearch = null;
			human.Science = 1;
			Sim.ClearTasks();

			new TechSelect(human).Run();
			try
			{
				Assert.Contains(GuideNote.Research, Game.Instance.GuideNotesShown);
				Assert.IsType<GuideMessage>(Common.Screens.Last());
			}
			finally
			{
				foreach (var s in Common.Screens.Where(s => s is GuideMessage || s is CivOne.Screens.ChooseTech).ToArray())
					Common.DestroyScreen(s);
			}
		}

		[Fact]
		public void ARivalDeclaringWarOnUsIsExplained()
		{
			Sim.NewGame();
			Game g = Game.Instance;
			Player rival = g.Players.First(p => p is not null && p != g.HumanPlayer && g.PlayerNumber(p) != 0);
			Player third = g.Players.First(p => p is not null && p != g.HumanPlayer && p != rival && g.PlayerNumber(p) != 0);

			rival.DeclareWar(third);
			Assert.DoesNotContain(GuideNote.War, g.GuideNotesShown);

			rival.DeclareWar(g.HumanPlayer);
			Assert.Contains(GuideNote.War, g.GuideNotesShown);
		}

		// The first advance that opens a wonder, and only ours.
		[Fact]
		public void TheFirstWonderWeCanBuildIsExplained()
		{
			Sim.NewGame();
			Game g = Game.Instance;
			Player rival = g.Players.First(p => p is not null && p != g.HumanPlayer && g.PlayerNumber(p) != 0);
			Advances.IAdvance opener = Common.Wonders.Select(w => w.RequiredTech).First(a => a is not null)!;

			rival.AddAdvance(opener, false);
			Assert.DoesNotContain(GuideNote.Wonder, g.GuideNotesShown);

			g.HumanPlayer.AddAdvance(opener, false);
			Assert.Contains(GuideNote.Wonder, g.GuideNotesShown);
		}

		// Chosen on the production screen: Tab to the units, a letter to jump, Enter to order.
		private static void Order(City city, char letter)
		{
			var screen = new CivOne.Screens.CityChooseProduction(city);
			try
			{
				screen.KeyDown(new KeyboardEventArgs(Key.Tab));
				screen.KeyDown(new KeyboardEventArgs(letter));
				screen.KeyDown(new KeyboardEventArgs(Key.Enter));
			}
			finally { Common.DestroyScreen(screen); }
		}

		// A Phalanx without copper costs half again; Militia costs nothing extra.
		[Fact]
		public void OrderingWorkWithoutItsMaterialIsExplained()
		{
			var (g, human, city) = AHumanTown(1);
			human.AddAdvance(new Advances.BronzeWorking(), false);
			Assert.False(g.HasResource(human, StrategicResource.Copper), "fixture: the town works copper");

			Order(city, 'm');
			Assert.IsType<Militia>(city.CurrentProduction);
			Assert.DoesNotContain(GuideNote.Materials, g.GuideNotesShown);

			Order(city, 'p');
			Assert.IsType<Phalanx>(city.CurrentProduction);
			Assert.Contains(GuideNote.Materials, g.GuideNotesShown);
		}

		[Fact]
		public void TheFourthCityHearsTheWaysToWin()
		{
			var (g, human, _) = AHumanTown(1);
			g.AddCity(human, 1, 30, 20);
			g.AddCity(human, 2, 50, 20);
			Assert.DoesNotContain(GuideNote.Victory, g.GuideNotesShown);

			g.AddCity(human, 3, 30, 30);
			Assert.Contains(GuideNote.Victory, g.GuideNotesShown);
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
