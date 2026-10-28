// CivOne tests
//
// A finished game did not record who won it.
//
// `game_outcome` carried the victory TYPE, the human's score and `human_won`. When the human
// lost, that left no way to say who beat them. Reading a 1987 AD "Cultural Ascendancy" out of
// the live log meant inferring the winner from whichever civ's streak was longest in the last
// victory_standings row, five turns before the end, and then confirming it against the save.
//
// The record now names the winner in the same NamePlural spelling victory_standings uses, so
// the two join, plus the leader.

using System.Linq;
using System.Text.Json;

namespace CivOne.Tests
{
	public class GameOutcomeWinnerTests
	{
		// Read back the record of a kind written AFTER `before` such lines, PARSED. EndGame
		// only enqueues: the line reaches disk on the logger's background task, so reading the
		// last line straight away sometimes found the PREVIOUS test's record (a "?" winner read
		// back as "Romans"). Count first, then wait for the new one. Parsing rather than
		// substring-matching also proves the record is still valid JSON — the first draft of
		// this file matched on "\"winner\": \"" and failed on every case, because the writer
		// emits compact JSON with no space after the colon.
		private static string Needle(string type) => $"\"type\":\"{type}\"";
		private static int Count(string type) => Sim.DecisionLineCount(Needle(type));
		private static JsonElement NextRecord(string type, int before)
			=> JsonDocument.Parse(Sim.AwaitDecisionLine(Needle(type), before)).RootElement;

		private static string Str(JsonElement e, string field) => e.GetProperty(field).GetString()!;

		private static JsonElement EndAndRead(string victory, bool humanWon, Player? winner)
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			int before = Count("game_outcome");
			DecisionLogger.EndGame(1234, victory, humanWon, turns: 300, winner);
			return NextRecord("game_outcome", before);
		}

		// The reported gap: a loss now says who won it.
		[Fact]
		public void ALossNamesTheVictor()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Game g = Game.Instance;
			Player rival = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);

			int before = Count("game_outcome");
			DecisionLogger.EndGame(1234, "Cultural Ascendancy", humanWon: false, turns: 537, rival);
			JsonElement rec = NextRecord("game_outcome", before);

			Assert.Equal(rival.Civilization.NamePlural, Str(rec, "winner"));
			Assert.Equal(rival.LeaderName, Str(rec, "winner_leader"));
			Assert.False(rec.GetProperty("human_won").GetBoolean());
		}

		// The spelling has to match victory_standings or the two cannot be joined, which was
		// the whole reason the winner had to be inferred from the standings in the first place.
		[Fact]
		public void TheNameMatchesTheStandingsSpelling()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Game g = Game.Instance;
			Player rival = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);

			int standings = Count("victory_standings"), outcomes = Count("game_outcome");
			DecisionLogger.LogVictoryStandings(300, rival, cities: 16, culture: 19166,
				reach: 11, shadow: 0, bestNeighbour: 0, observatories: 0, hasFuel: false,
				populace: 166, peakPopulace: 166, artists: 0, grossOutput: 8871, worldOutput: 166765,
				econStreak: 0, cultStreak: 75, structural: 0, component: 0, module: 0,
				launchTurn: 0, missionControl: false);
			DecisionLogger.EndGame(1234, "Cultural Ascendancy", humanWon: false, turns: 537, rival);

			Assert.Equal(Str(NextRecord("victory_standings", standings), "civ"),
			             Str(NextRecord("game_outcome", outcomes), "winner"));
		}

		// A win by the human names the human. "human_won: true implies it was you" is true but
		// leaves the field inconsistent, and every consumer would need the special case.
		[Fact]
		public void AWinNamesTheHumanToo()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Player me = Game.Instance.HumanPlayer;

			int before = Count("game_outcome");
			DecisionLogger.EndGame(9999, "Economic Dominance", humanWon: true, turns: 400, me);
			JsonElement rec = NextRecord("game_outcome", before);

			Assert.Equal(me.Civilization.NamePlural, Str(rec, "winner"));
			Assert.True(rec.GetProperty("human_won").GetBoolean());
		}

		// The one case with genuinely nobody to name: the human's last city and unit are gone
		// and the conqueror is not knowable at that call site. The field must still be present
		// and parseable rather than absent, or a reader has to handle two record shapes.
		[Fact]
		public void AnUnknowableWinnerIsStillAField()
		{
			JsonElement rec = EndAndRead("Destroyed", humanWon: false, winner: null);

			Assert.Equal("?", Str(rec, "winner"));
			Assert.Equal("?", Str(rec, "winner_leader"));
		}

		// Every ending must carry the field — a consumer that has to ask "does this victory
		// type have a winner?" is the bug this fixes, one layer up.
		[Theory]
		[InlineData("Diaspora")]
		[InlineData("Conquest")]
		[InlineData("Score")]
		[InlineData("Dome")]
		public void EveryEndingCarriesTheField(string victory)
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			JsonElement rec = EndAndRead(victory, humanWon: true, Game.Instance.HumanPlayer);

			Assert.True(rec.TryGetProperty("winner", out JsonElement w) && w.GetString() != "?",
				$"{victory} recorded no winner");
			Assert.Equal(victory, Str(rec, "victory"));
		}

		// No call site may quietly go back to omitting it: the parameter is required, so the
		// compiler enforces this, and this pins that nobody re-added a defaulted overload.
		[Fact]
		public void TheWinnerParameterIsRequired()
		{
			var methods = typeof(DecisionLogger)
				.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
				.Where(m => m.Name == "EndGame").ToArray();

			Assert.Single(methods);
			var last = methods[0].GetParameters().Last();
			Assert.Equal("winner", last.Name);
			Assert.False(last.IsOptional, "an optional winner is a winner that gets forgotten");
		}
	}
}
