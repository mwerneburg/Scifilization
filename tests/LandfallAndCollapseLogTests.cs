// CivOne tests
//
// Two mechanisms green in tests had never been seen in a finished game because nothing
// logged them: a Longboat's landfall and the empire-size-scaled disorder collapse. Each now
// writes a record. Pinned on the source, as in SalvageTests and DefectionTests: the logger
// appends to the user's real decisions.jsonl, so a test must not emit rows.

namespace CivOne.Tests
{
	public class LandfallAndCollapseLogTests
	{
		private static string Src(params string[] path)
		{
			var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			Assert.NotNull(dir);
			return System.IO.File.ReadAllText(System.IO.Path.Combine(dir!.FullName, System.IO.Path.Combine(path)));
		}

		private static string Between(string src, string start, string end)
		{
			int at = src.IndexOf(start);
			Assert.True(at >= 0, $"'{start}' has moved or been renamed");
			return src.Substring(at, src.IndexOf(end, at) - at);
		}

		// Logged only once the landing is legal, and before the boat is gone.
		[Fact]
		public void ALandfallIsRecorded()
		{
			string m = Between(Src("src", "Units", "Longboat.cs"), "internal bool GoAshore(", "\n\t\t}");
			int guard = m.IndexOf("if (!CanLandOn(site)) return false;");
			int log = m.IndexOf("DecisionLogger.LogLandfall(");
			int gone = m.IndexOf("Game.DisbandUnit(this)");
			Assert.True(log > 0, "a landfall is not recorded");
			Assert.True(guard >= 0 && guard < log, "a refused landing would be recorded");
			Assert.True(log < gone, "the record is written after the boat is disbanded");
		}

		// Both outcomes: "held" is the only evidence the scaling ever saved an empire.
		[Fact]
		public void TheThirdTurnOfRiotIsRecordedEitherWay()
		{
			string c3 = Between(Src("src", "City.cs"), "case 3:", "break;");
			Assert.Contains("DecisionLogger.LogDisorderCollapse(Player, collapses)", c3);
			Assert.True(c3.IndexOf("LogDisorderCollapse") < c3.IndexOf("Player.Revolt()"),
				"the collapse is logged only after the revolt, or not on the held path");
		}

		[Theory]
		[InlineData("\"landfall\"),", "civ", "x", "y")]
		[InlineData("\"disorder_collapse\"),", "civ", "government", "cities", "rioting", "outcome")]
		public void TheRecordsCarryTheirFields(string type, params string[] fields)
		{
			string record = Between(Src("src", "DecisionLogger.cs"), type, "}));");
			foreach (string f in fields)
				Assert.Contains($"KV(\"{f}\"", record);
		}
	}
}
