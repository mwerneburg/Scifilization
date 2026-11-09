// CivOne tests
//
// The decision log is for development. It was always on, appending about 20 MB a game to every
// player's data folder for good; it now writes only when CIVONE_DECISION_LOG=1, which build.sh
// and the tests set.

namespace CivOne.Tests
{
	public class DecisionLogOptInTests
	{
		private static int LinesOfANewGame(string? setting)
		{
			Sim.EnsureRuntime();
			System.Environment.SetEnvironmentVariable(DecisionLogger.EnableVariable, setting);
			try
			{
				Sim.NewGame(width: 80, height: 50);
				string id = DecisionLogger.GameId;
				Assert.False(string.IsNullOrEmpty(id), "the game has no id, and saves need one");
				DecisionLogger.LogAdvance(Game.Instance.HumanPlayer, "Opt-in probe");
				System.Threading.Thread.Sleep(400);
				return Sim.DecisionLineCount($"\"game_id\":\"{id}\"");
			}
			finally { System.Environment.SetEnvironmentVariable(DecisionLogger.EnableVariable, "1"); }
		}

		[Fact]
		public void APlayersGameWritesNothing() => Assert.Equal(0, LinesOfANewGame(null));

		[Fact]
		public void AnythingButOneIsOff() => Assert.Equal(0, LinesOfANewGame("yes"));

		[Fact]
		public void TheDeveloperGetsTheLog() => Assert.True(LinesOfANewGame("1") > 0);

		// One writer for the session. Every new game or load started another writer loop and
		// none ever stopped: each held a pool thread while it waited, and all of them wrote
		// through one StreamWriter, which is not thread-safe. About 2,000 games into a test
		// run, a line took longer than the tests' five seconds to reach the disk
		// (GameOutcomeWinnerTests.TheNameMatchesTheStandingsSpelling, Nov 2026).
		private static object? WriterTask() => typeof(DecisionLogger)
			.GetField("_writerTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
			.GetValue(null);

		[Fact]
		public void ANewGameKeepsTheOneWriter()
		{
			Sim.NewGame(width: 40, height: 25);
			object? first = WriterTask();
			Sim.NewGame(width: 40, height: 25);
			Assert.NotNull(first);
			Assert.Same(first, WriterTask());
		}

		// Its control: a quiet game lets the writer stop, and the next logged game starts it again.
		[Fact]
		public void AGameAfterAQuietOneStillLogs()
		{
			LinesOfANewGame(null);
			System.Threading.Thread.Sleep(1000);
			Assert.True(LinesOfANewGame("1") > 0);
		}
	}
}
