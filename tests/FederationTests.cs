// CivOne tests
//
// The Federation (Oct 2026): humanity's earlier exodus, come home to find Earth in breach of
// its norms. Drawn only for a player who exterminated FederationBloodCount civilizations and
// prospered; then rolled, never certain. At arrival, an ultimatum and a choice:
//
//   SUBMIT                  — the game is lost (the user's call, Sep 2026)
//   REFUSE, no Dome         — something too big to be a space station; also lost
//   REFUSE, Dome complete   — the Federation lands as the Owners do, and destroying it wins

using System.Linq;
using System.Reflection;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class FederationTests : System.IDisposable
	{
		public FederationTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; Settings.Instance.Autopilot = false; }
		private readonly bool _auto;
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// Four civs on open ground, each AI with three cities so the landing has something
		// to take that is not a capital.
		private static Game AWorld()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player[] civs = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0).Take(4).ToArray();
			int id = 0;
			foreach ((Player p, int i) in civs.Select((p, i) => (p, i)))
				for (int k = 0; k < 3; k++)
				{
					int x = 15 + i * 14, y = 15 + k * 8;
					p.Explore(x, y, range: 3);
					g.AddCity(p, id++, x, y)!.Size = 6;
				}
			g.SETISignalReceived = true;
			g.VisitorType = VisitorArchetype.Conquerors;
			Sim.ClearTasks();
			return g;
		}

		private static void Kill(Game g, int victimId) =>
			g.AddReplayEvent(new ReplayData.CivilizationDestroyed(g.GameTurn, victimId, g.HumanPlayer.Civilization.Id));

		private static int AnAiCivId(Game g, int nth) => g.Players
			.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer)
			.Skip(nth).First().Civilization.Id;

		private static void DomeUp(Game g) =>
			typeof(Game).GetField("_domeVictoryFired", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(g, true);

		private static Player? Federation(Game g) =>
			g.Players.FirstOrDefault(p => p is not null && p.Civilization is Civilizations.Federation);

		// ── the draw ─────────────────────────────────────────────────────────

		[Fact]
		public void OneExterminationIsNotEnough()
		{
			Game g = AWorld();
			g.HumanPlayer.AwardMilestone(5000);
			Kill(g, AnAiCivId(g, 0));

			Assert.False(g.FederationEarned(g.HumanityNations()));
		}

		[Fact]
		public void TwoExterminationsAndStandingEarnIt()
		{
			Game g = AWorld();
			g.HumanPlayer.AwardMilestone(5000);
			Kill(g, AnAiCivId(g, 0));
			Kill(g, AnAiCivId(g, 1));

			Assert.True(g.FederationEarned(g.HumanityNations()));
		}

		// A ruined warlord reads as cargo; the Owners' draw answers that.
		[Fact]
		public void BloodWithoutStandingDoesNot()
		{
			Game g = AWorld();
			foreach (Player p in g.HumanityNations().Where(p => p != g.HumanPlayer))
				p.AwardMilestone(5000);
			Kill(g, AnAiCivId(g, 0));
			Kill(g, AnAiCivId(g, 1));

			Assert.False(g.FederationEarned(g.HumanityNations()));
		}

		// The horde and the story factions are not peoples.
		[Fact]
		public void KillingTheHordeOrTheMachinesIsNotExtermination()
		{
			Game g = AWorld();
			Kill(g, 15);   // the Barbarians' Id
			Kill(g, (int)Civilization.Skynet);
			Kill(g, (int)Civilization.TheOthers);

			Assert.Equal(0, g.CivilizationsDestroyedBy(g.HumanPlayer));
		}

		// ── the plates ───────────────────────────────────────────────────────

		[Theory]
		[InlineData((int)VisitorArchetype.Evaluators, "EvaluatorContact")]
		[InlineData((int)VisitorArchetype.Conquerors, "FederationContact")]
		public void EachArchetypeShowsItsOwnContactPlate(int archetype, string art)
		{
			Assert.Equal(art, Game.ContactPlate((VisitorArchetype)archetype).art);
		}

		// ── the answer ───────────────────────────────────────────────────────

		[Fact]
		public void SubmittingLosesTheGame()
		{
			Game g = AWorld();
			DomeUp(g);

			g.AnswerFederation(refuse: false);

			Assert.Null(Federation(g));
			Assert.Contains("Turn", Sim.PendingTaskTypes());
		}

		[Fact]
		public void RefusingWithoutTheDomeIsTheEnd()
		{
			Game g = AWorld();
			Assert.False(g.DomeComplete, "fixture: the Dome must be down");

			g.AnswerFederation(refuse: true);

			Assert.Null(Federation(g));
			Assert.Contains("Turn", Sim.PendingTaskTypes());
		}

		[Fact]
		public void RefusingWithTheDomeIsAWar()
		{
			Game g = AWorld();
			DomeUp(g);

			g.AnswerFederation(refuse: true);

			Player? fed = Federation(g);
			Assert.NotNull(fed);
			Assert.NotEmpty(fed!.Cities);
			Assert.True(fed.IsAtWar(g.HumanPlayer), "the Federation landed at peace with Earth");
			Assert.DoesNotContain("Turn", Sim.PendingTaskTypes());
		}

		// The landers are a faction, not a people: humanity's verdict does not count them.
		[Fact]
		public void TheFederationIsNotHumanity()
		{
			Game g = AWorld();
			DomeUp(g);
			g.AnswerFederation(refuse: true);

			Assert.DoesNotContain(Federation(g), g.HumanityNations());
		}
	}
}
