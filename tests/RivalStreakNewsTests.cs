// CivOne tests
//
// A rival's run at a streak victory is news (the user, Nov 2026). Only the claimant was ever
// told its streak had started or was half way, so a rival's Cultural Ascendancy first showed
// as 53/75 on the score report. The economic, cultural and Diaspora streaks now run the
// newspaper for rivals when they start and at half way, and a rival's first spaceship part
// does too.

using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;


namespace CivOne.Tests
{
	public class RivalStreakNewsTests
	{
		private static void Check(Game g, string rule) =>
			typeof(Game).GetMethod(rule, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(g, null);

		private static bool Reported(string line) => Sim.PendingMessageLines().Contains(line);

		private static (Game g, Player human, Player[] rivals) AWorld(Terrain land)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 15; y <= 35; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, land);
			Map.Instance.RecalculateContinentsIfDirty();
			Player human = g.HumanPlayer;
			Player[] rivals = g.Players.Where(p => p is not null && p != human && g.PlayerNumber(p) != 0).Take(3).ToArray();
			return (g, human, rivals);
		}

		// StreakGraceTests' merchant world, with a rival as the merchant: the human and the
		// other two pay it tribute, which both binds them and lifts its output.
		[Theory]
		[InlineData(0u, "dominate world trade.")]
		[InlineData(Game.EconomicHoldTurns / 2 - 1, "are half way to hegemony:")]
		public void ARivalsEconomicStreakIsNews(uint before, string line)
		{
			(Game g, Player human, Player[] rivals) = AWorld(Terrain.River);
			Player merchant = rivals[0];
			merchant.AddAdvance(new Banking(), false);
			g.AddCity(merchant, 0, 40, 25)!.Size = 4;
			int id = 1;
			foreach (Player p in rivals.Skip(1).Append(human))
			{
				City c = g.AddCity(p, id, 24 + id * 8, 30)!;
				id++;
				c.Size = 12;
				c.ResetResourceTiles();
				p.Gold = 30000;
				p.EstablishTribute(merchant, 1000);
			}
			g.Progress(g.PlayerNumber(merchant)).EconStreak = before;
			Sim.ClearTasks();

			Check(g, "CheckPaxMercatoria");

			Assert.Equal(before + 1, g.Progress(g.PlayerNumber(merchant)).EconStreak);
			Assert.True(Reported(line), $"no news of the {merchant.TribeNamePlural}' streak: {string.Join(" | ", Sim.PendingMessageLines())}");
		}

		// CultureBlockedByWarTests' admired world, with a rival as the admired one.
		[Theory]
		[InlineData(0u, "lead the world in culture.")]
		[InlineData(Game.CultureHoldTurns / 2 - 1, "are half way to ascendancy:")]
		public void ARivalsCulturalStreakIsNews(uint before, string line)
		{
			(Game g, Player human, Player[] rivals) = AWorld(Terrain.Grassland1);
			Player admired = rivals[0];
			Player[] others = rivals.Skip(1).Append(human).ToArray();
			foreach (Player p in others.Append(admired))
			{
				p.Government = new CivOne.Governments.Monarchy();
				p.Explore(40, 25, range: 20);
			}
			admired.AddAdvance(new Philosophy(), false);
			g.AddCity(admired, 0, 40, 25)!.Size = 6;
			int id = 1;
			foreach (Player p in others) g.AddCity(p, id, 34 + id++ * 3, 30)!.Size = 6;
			admired.SetCulture(6000);
			foreach (Player p in others)
			{
				p.SetCulture(600);
				admired.EstablishEmbassy(p);
			}
			Sim.ElectronicsForTheWorld(admired);
			g.Progress(g.PlayerNumber(admired)).CultureStreak = before;
			Sim.ClearTasks();

			Check(g, "CheckCulturalAscendancy");

			Assert.Equal(before + 1, g.Progress(g.PlayerNumber(admired)).CultureStreak);
			Assert.True(Reported(line), $"no news of the {admired.TribeNamePlural}' streak: {string.Join(" | ", Sim.PendingMessageLines())}");
		}

		// RivalVictoryTests' rival colony, held from Mission Control.
		[Theory]
		[InlineData(0u, "are resupplying a colony.")]
		[InlineData(Game.DiasporaStreakTarget / 2 - 1, "are half way to independence:")]
		public void ARivalsDiasporaIsNews(uint before, string line)
		{
			(Game g, Player human, Player[] rivals) = AWorld(Terrain.Grassland1);
			Player colonist = rivals[0];
			colonist.AddAdvance(new SpaceFlight(), false);
			City hq = g.AddCity(colonist, 0, 40, 25)!;
			hq.Size = 4;
			hq.AddBuilding(new MissionControl());
			g.Progress(g.PlayerNumber(colonist)).ColonyFounded = true;
			g.Progress(g.PlayerNumber(colonist)).DiasporaStreak = before;
			Sim.ClearTasks();

			Check(g, "CheckDiaspora");

			Assert.True(Reported(line), $"no news of the {colonist.TribeNamePlural}' colony: {string.Join(" | ", Sim.PendingMessageLines())}");
		}

		// The human's own streak keeps its advisor and is not also run as world news.
		[Fact]
		public void TheHumansOwnStreakIsNotWorldNews()
		{
			(Game g, Player human, Player[] _) = AWorld(Terrain.Grassland1);
			human.AddAdvance(new SpaceFlight(), false);
			City hq = g.AddCity(human, 0, 40, 25)!;
			hq.Size = 4;
			hq.AddBuilding(new MissionControl());
			g.Progress(g.PlayerNumber(human)).ColonyFounded = true;
			Sim.ClearTasks();

			Check(g, "CheckDiaspora");

			Assert.DoesNotContain("are resupplying a colony.", Sim.PendingMessageLines());
		}

		// ── the space race ───────────────────────────────────────────────────────

		// SpaceshipLaunchAccountingTests' rival yard. Its first part is news; its second is not,
		// and neither is the human's first.
		[Theory]
		[InlineData(false, 0, true)]
		[InlineData(false, 1, false)]
		[InlineData(true,  0, false)]
		public void ARivalsFirstSpaceshipPartIsNews(bool human, int structurals, bool news)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = human ? g.HumanPlayer : g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			p.Government = new CivOne.Governments.Monarchy();
			p.Explore(40, 25, range: 15);
			for (int y = 20; y <= 30; y++)
			for (int x = 34; x <= 46; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			City c = g.AddCity(p, 0, 40, 25)!;
			c.Size = 8;
			p.AddAdvance(new SpaceFlight(), false);
			g.Progress(g.PlayerNumber(p)).SpaceshipStructural = structurals;
			c.SetProduction(new SSStructural());
			c.Shields = (short)((int)new SSStructural().Price * 10);
			Sim.ClearTasks();

			c.NewTurn();

			Assert.Equal(structurals + 1, g.Progress(g.PlayerNumber(p)).SpaceshipStructural);
			Assert.Equal(news, Sim.PendingMessageLines().Contains("have started a spaceship!"));
		}
	}
}
