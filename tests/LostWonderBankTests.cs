// CivOne tests
//
// A wonder another civ finished first keeps its shields banked in the player's city until
// the player chooses another (the user, Nov 2026). It used to roll over to the first wonder
// in Id order, obsolete or not, and finish it at once if the bank covered it, spending
// shields saved for Darwin's Voyage a few turns off. AI cities still roll over: one left on
// a dead wonder would never re-plan. A finished wonder's excess shields now carry over, and
// each rollover is written to the decision log.

using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class LostWonderBankTests
	{
		private const string Notice = "Shields kept until you choose.";

		// A city of p's building the Great Library with a full box, after a third civ built it.
		private static (City c, Player p) ALostLibrary(bool human)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = human ? g.HumanPlayer : g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			for (int y = 20; y <= 30; y++)
			for (int x = 30; x <= 50; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);   // shields, or a city with no income re-plans every turn and hides the rules here
			Map.Instance.RecalculateContinentsIfDirty();
			Player rival = g.Players.First(x => x is not null && x != p && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			g.AddCity(rival, 1, 46, 25)!.AddWonder(new GreatLibrary());

			p.Government = new CivOne.Governments.Monarchy();
			p.AddAdvance(new Literacy(), false);
			p.AddAdvance(new Masonry(), false);   // so the old rollover has somewhere to go
			p.Explore(36, 25, range: 3);   // a city works only tiles its owner has seen
			City c = g.AddCity(p, 0, 36, 25)!;
			c.Size = 6;
			c.ResetResourceTiles();
			c.SetProduction(new GreatLibrary());
			c.Shields = c.ProductionCost(new GreatLibrary());
			Sim.ClearTasks();
			return (c, p);
		}

		[Fact]
		public void OurCityKeepsBankingOnALostWonder()
		{
			(City c, Player _) = ALostLibrary(human: true);
			int banked = c.Shields;

			c.NewTurn();

			Assert.IsType<GreatLibrary>(c.CurrentProduction);
			Assert.True(c.Shields >= banked, $"the bank fell from {banked} to {c.Shields}");
			Assert.Contains(Notice, Sim.PendingMessageLines());

			// Told once, not every turn it goes on banking.
			Sim.ClearTasks();
			c.NewTurn();
			Assert.IsType<GreatLibrary>(c.CurrentProduction);
			Assert.DoesNotContain(Notice, Sim.PendingMessageLines());
		}

		// The point of banking: the wonder the player was waiting for, once it is in reach.
		[Fact]
		public void TheBankBuildsTheWonderWeWereWaitingFor()
		{
			(City c, Player human) = ALostLibrary(human: true);
			c.NewTurn();
			Sim.ClearTasks();

			human.AddAdvance(new RailRoad(), false);
			c.SetProduction(new DarwinsVoyage());
			c.NewTurn();

			Assert.True(c.HasWonder<DarwinsVoyage>(), $"Darwin's Voyage not built on a bank of {c.Shields}");
		}

		// ── the excess carries over ──────────────────────────────────────────────

		// A city of p's that finishes Darwin's Voyage this turn, 100 shields over.
		private static (City c, int over) DarwinWithRoomToSpare(bool human)
		{
			(City c, Player p) = ALostLibrary(human);
			p.AddAdvance(new RailRoad(), false);
			c.SetProduction(new DarwinsVoyage());
			c.Shields = c.ProductionCost(new DarwinsVoyage()) + 100;
			return (c, 100);
		}

		[Fact]
		public void AWondersExcessShieldsCarryOver()
		{
			(City c, int over) = DarwinWithRoomToSpare(human: true);

			c.NewTurn();

			Assert.True(c.HasWonder<DarwinsVoyage>());
			Assert.True(c.Shields >= over, $"{over} shields over the cost, {c.Shields} kept");
		}

		// ...into whatever comes next, not only another wonder: moving on from a finished
		// wonder is not a change of plan.
		[Fact]
		public void TheExcessSurvivesChoosingABuilding()
		{
			(City c, int over) = DarwinWithRoomToSpare(human: true);
			c.NewTurn();

			c.SetProduction(new CivOne.Buildings.Library());

			Assert.True(c.Shields >= over, $"choosing a Library after the wonder left {c.Shields}");
		}

		// The box is not empty after a wonder now, so the AI must still be asked what's next.
		// The Pyramids, not Darwin's Voyage: Darwin's hands out advances, and a new advance
		// re-plans the city by another road, which hid this one.
		[Fact]
		public void AnAICityReplansAfterAWonderWithShieldsInHand()
		{
			(City c, Player _) = ALostLibrary(human: false);
			c.SetProduction(new Pyramids());
			c.Shields = c.ProductionCost(new Pyramids()) + 100;
			// A city earning nothing re-plans every turn regardless, which would hide this.
			Assert.True(c.ShieldIncome > 0, $"scenario is broken: the city earns no shields ({c.ShieldTotal} - {c.ShieldCosts}, units {c.Units.Length}, size {c.Size}, tile {Map.Instance[c.X, c.Y].GetType().Name})");

			c.NewTurn();

			Assert.True(c.HasWonder<Pyramids>());
			Assert.IsNotType<Pyramids>(c.CurrentProduction);
			Assert.True(c.Shields >= 100, $"100 over, {c.Shields} kept");
		}

		// ── the decision log ─────────────────────────────────────────────────────

		// Pinned on the source, as DefectionTests explains: running the logger would append to
		// the player's real decisions.jsonl.
		private static string Source(string file)
		{
			var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			return System.IO.File.ReadAllText(System.IO.Path.Combine(dir!.FullName, "src", file));
		}

		[Theory]
		[InlineData("city")]
		[InlineData("outcome")]
		[InlineData("lost_wonder")]
		[InlineData("next_wonder")]
		[InlineData("shields")]
		public void TheRolloverRecordCarriesItsFields(string field)
		{
			string src = Source("DecisionLogger.cs");
			int at = src.IndexOf("\"wonder_rollover\"),");
			Assert.True(at > 0, "no wonder_rollover record");
			Assert.Contains($"KV(\"{field}\"", src.Substring(at, src.IndexOf("}));", at) - at));
			Assert.Contains("\"wonder_rollover\"", src.Substring(0, src.IndexOf("namespace CivOne")));
		}

		// Every way a lost wonder can go is written down.
		[Theory]
		[InlineData("held")]
		[InlineData("rolled")]
		[InlineData("none")]
		public void EveryRolloverOutcomeIsLogged(string outcome)
		{
			Assert.Contains($"DecisionLogger.LogWonderRollover(this, \"{outcome}\"", Source("City.cs"));
		}

		// The control: an AI city still rolls over.
		[Fact]
		public void AnAICityStillRollsOver()
		{
			(City c, Player _) = ALostLibrary(human: false);

			c.NewTurn();

			Assert.IsNotType<GreatLibrary>(c.CurrentProduction);
		}
	}
}
