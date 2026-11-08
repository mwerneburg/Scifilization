// CivOne tests
//
// A seed is meant to pin a whole game, so that two runs of one build can be compared and two
// builds can be compared on the same history. Seed 1789 on Epic Earth split between two runs
// of the same build at turn 117-119 in two of seven tries: an idle Diplomat's retry slot was
// its object identity hash, which the runtime hands out afresh every run, so one run sent it
// to a city and the other kept it home. Every A/B the harness had measured carried that noise.

using System.Linq;

namespace CivOne.Tests
{
	public class SeedReplayTests
	{
		private static string PlayAndDescribe(int turns)
		{
			Sim.NewGame(width: 80, height: 50, seed: 1789);
			bool autopilot = Settings.Instance.Autopilot, cursed = Settings.Instance.CursedWonders;
			// As the harness plays: nobody at the human's controls, and no curses loose.
			Settings.Instance.Autopilot = true;
			Settings.Instance.CursedWonders = false;
			try { Sim.RunTurns(turns, _ => Sim.KeepHumanResearching()); }
			finally { Settings.Instance.Autopilot = autopilot; Settings.Instance.CursedWonders = cursed; }
			Game g = Game.Instance;
			return string.Join("\n",
				g.GetUnits().Select(u => $"u {u.Owner} {u.GetType().Name} {u.X},{u.Y} {u.MovesLeft} {u.Goto.X},{u.Goto.Y}")
				.Concat(g.GetCities().Select(c => $"c {c.Owner} {c.Name} {c.Size} {c.Shields} {c.CurrentProduction?.GetType().Name}"))
				.Concat(new[] { $"r {Common.Random.Next(30000)}" }));
		}

		[Fact]
		public void OneSeedPlaysOneGame()
		{
			string first = PlayAndDescribe(150);
			string second = PlayAndDescribe(150);
			Assert.True(first.Split('\n').Length > 50, "fixture: the world is too small to show anything");
			Assert.Equal(first, second);
		}
	}
}
