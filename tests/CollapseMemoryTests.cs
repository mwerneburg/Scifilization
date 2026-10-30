// CivOne tests
//
// A government brought down by disorder was re-adopted the moment Anarchy ended, with the same
// cities still rioting, and fell again a few turns later. The Malians of game 4a58d14c
// collapsed seven times between t452 and t504, every time out of Democracy; the Haudenosaunee
// seven times between t372 and t400. The AI now remembers the collapse and keeps clear of the
// governments that collapse in disorder for a while.

using System.Linq;
using CivOne.Enums;
using CivOne.Governments;

namespace CivOne.Tests
{
	public class CollapseMemoryTests
	{
		private static (Game g, Player p, AI ai) APeacefulRepublicanEmpire(int cities = 8)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			foreach (var a in new Advances.IAdvance[] { new Advances.Monarchy(), new Advances.Communism(),
				new Advances.TheRepublic(), new Advances.Democracy() })
				p.AddAdvance(a, false);
			for (int y = 18; y <= 32; y++)
			for (int x = 26; x <= 54; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			p.Explore(40, 25, range: 15);
			for (int i = 0; i < cities; i++) g.AddCity(p, (byte)i, 28 + 4 * (i % 7), 20 + 4 * (i / 7))!.Size = 4;
			Sim.ClearTasks();
			return (g, p, AI.Instance(p));
		}

		[Fact]
		public void AFallenDemocracyIsNotReadoptedAtOnce()
		{
			(Game g, Player p, AI ai) = APeacefulRepublicanEmpire();
			p.Government = new Anarchy();
			ai.ChooseGovernment();
			Assert.True(p.Government.CollapsesInDisorder, $"precondition: at peace it picks a republic, got {p.Government.Name}");

			p.Collapse();
			ai.ChooseGovernment();

			Assert.False(p.Government.CollapsesInDisorder, $"re-adopted {p.Government.Name} the turn it fell");
			Assert.False(p.Government is Despotism, "fell back further than it had to");
		}

		// With the republics out of reach, Monarchy and Communism tie outside a war. A large
		// empire takes Communism: Monarchy's corruption grows with distance from the capital.
		[Fact]
		public void ALargeEmpireFallsBackToCommunism()
		{
			(Game g, Player p, AI ai) = APeacefulRepublicanEmpire(14);
			// An Endurance civ with room expands; a builder path would deepen into Develop,
			// where Communism outscores Monarchy and there is no tie to break.
			var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
			typeof(AI).GetField("_path", flags)!.SetValue(ai, AI.VictoryPath.Endurance);
			typeof(AI).GetField("_pathChosenTurn", flags)!.SetValue(ai, (int)g.GameTurn);
			Assert.Equal(ai.GovernmentScoreForTest(new Monarchy()), ai.GovernmentScoreForTest(new Communism()));
			p.Government = new Democracy();
			p.Collapse();
			ai.ChooseGovernment();

			Assert.IsType<Communism>(p.Government);
		}

		// A memory, not a ban: once it has passed, the republics are back on the table.
		[Fact]
		public void TheMemoryFades()
		{
			(Game g, Player p, AI ai) = APeacefulRepublicanEmpire();
			p.Government = new Democracy();
			p.Collapse();
			g.GameTurn += 50;
			ai.ChooseGovernment();

			Assert.True(p.Government.CollapsesInDisorder, $"still avoiding republics 50 turns on: {p.Government.Name}");
		}

		// The memory is written by the collapse itself, not by any revolution.
		[Fact]
		public void TheDisorderCollapseIsWhatIsRemembered()
		{
			var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			string src = System.IO.File.ReadAllText(System.IO.Path.Combine(dir!.FullName, "src", "City.cs"));
			int at = src.IndexOf("if (collapses)");
			Assert.True(at >= 0, "the collapse branch has moved");
			Assert.Contains("Player.Collapse();", src.Substring(at, 120));
		}
	}
}
