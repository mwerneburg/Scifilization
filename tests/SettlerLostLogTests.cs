// CivOne tests
//
// The Ottomans of game 48114c9e kept about four worker settlers against a quota of fourteen
// while their cities kept raising them, and nothing in the log said where they went: a
// settler killed in combat, or lost with its home city, left no record. Every Settlers that
// leaves the world now writes one, naming its cause. Pinned on the source, as in
// LandfallAndCollapseLogTests: the logger appends to the user's real decisions.jsonl.

namespace CivOne.Tests
{
	public class SettlerLostLogTests
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

		// Every removal path, named by its caller, and written while the unit still has its
		// tile and home.
		[Fact]
		public void EveryDisbandedSettlerIsRecorded()
		{
			string m = Between(Src("src", "Game.cs"), "public void DisbandUnit(", "\n\t\t}");
			Assert.Contains("[System.Runtime.CompilerServices.CallerMemberName] string cause", m);
			int log = m.IndexOf("if (unit is Units.Settlers) DecisionLogger.LogSettlerLost(unit, cause);");
			Assert.True(log > 0, "a disbanded settler is not recorded");
			Assert.True(log < m.IndexOf("unit.SetHome(null);"), "the record is written after the unit is gone");
		}

		// A combat death names the civ that made it.
		[Fact]
		public void AKilledSettlerNamesItsKiller()
		{
			string m = Between(Src("src", "Screens", "DestroyUnit.cs"), "private static void Kill(", "\n\t\t}");
			Assert.Contains("Game.DisbandUnit(u, credit is null ? nameof(Kill) : $\"killed_by:{credit.Civilization.NamePlural}\");", m);
		}

		// A destroyed city drops its units without DisbandUnit.
		[Fact]
		public void ASettlerLostWithItsCityIsRecorded()
		{
			string m = Between(Src("src", "Game.cs"), "public void DestroyCity(", "_cities.Remove(city);");
			int log = m.IndexOf("DecisionLogger.LogSettlerLost(unit, nameof(DestroyCity));");
			Assert.True(log > 0, "a settler lost with its city is not recorded");
			Assert.True(log < m.IndexOf("unit.SetHome(null);"), "the record is written after the unit is gone");
		}

		[Fact]
		public void TheRecordCarriesItsFields()
		{
			string record = Between(Src("src", "DecisionLogger.cs"), "\"settler_lost\"),", "}));");
			foreach (string f in new[] { "civ", "cause", "x", "y", "in_city", "home", "home_size" })
				Assert.Contains($"KV(\"{f}\"", record);
		}
	}
}
