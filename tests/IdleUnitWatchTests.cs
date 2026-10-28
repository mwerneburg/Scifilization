// CivOne tests
//
// AI civilian units that stand still are logged. In game c1175c79 Japan's settler stood in
// Kyoto with a full move from turn 100 to 138, and loading the save moved it at once:
// whatever held it lived only in the running game, which writes no runtime log.

using System.Linq;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class IdleUnitWatchTests : System.IDisposable
	{
		private readonly bool _auto;
		public IdleUnitWatchTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		private static void Watch() => typeof(Game).GetMethod("WatchIdleUnits",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(Game.Instance, null);

		private static IUnit AnExplorer(bool human)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			Player p = human ? g.HumanPlayer : g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
			Map.Instance.ChangeTileType(41, 25, Terrain.Grassland1);
			return g.CreateUnit(UnitType.Explorer, 40, 25, g.PlayerNumber(p), false)!;
		}

		private static void StandStill(int turns)
		{
			for (int i = 0; i < turns; i++) { Game.Instance.GameTurn++; Watch(); }
		}

		[Fact]
		public void AnAiExplorerThatStaysPutIsCounted()
		{
			IUnit u = AnExplorer(human: false);
			Watch();
			StandStill(Game.IdleUnitTurns);

			Assert.Equal(Game.IdleUnitTurns, Game.Instance.IdleTurns(u));
		}

		[Fact]
		public void MovingStartsTheCountAgain()
		{
			IUnit u = AnExplorer(human: false);
			Watch();
			StandStill(5);
			Assert.Equal(5, Game.Instance.IdleTurns(u));
			u.X = 41;
			StandStill(1);

			Assert.Equal(0, Game.Instance.IdleTurns(u));
		}

		// The player's own units are the player's business, unless Autopilot is steering them.
		[Fact]
		public void ThePlayersUnitsAreLeftAlone()
		{
			IUnit u = AnExplorer(human: true);
			Watch();
			StandStill(Game.IdleUnitTurns);

			Assert.Equal(0, Game.Instance.IdleTurns(u));
		}

		// ...and the report is actually written, on the turn the count is reached.
		[Fact]
		public void TheWatcherWritesTheRecord()
		{
			string src = System.IO.File.ReadAllText(System.IO.Path.Combine(Root(), "src", "Game.cs"));
			Assert.Contains("Tick(\"IdleUnits\", WatchIdleUnits);", src);
			Assert.Contains("DecisionLogger.LogIdleUnit(u, idle);", src);
		}

		private static string Root()
		{
			var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			return dir!.FullName;
		}
	}
}
