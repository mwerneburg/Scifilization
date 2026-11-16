// CivOne tests
//
// A wonder another civ finished first keeps its shields banked in the player's city until
// the player chooses another (the user, Nov 2026). It used to roll over to the first wonder
// in Id order, obsolete or not, and finish it at once if the bank covered it, spending
// shields saved for Darwin's Voyage a few turns off. AI cities still roll over: one left on
// a dead wonder would never re-plan.

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
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player rival = g.Players.First(x => x is not null && x != p && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			g.AddCity(rival, 1, 46, 25)!.AddWonder(new GreatLibrary());

			p.Government = new CivOne.Governments.Monarchy();
			p.AddAdvance(new Literacy(), false);
			p.AddAdvance(new Masonry(), false);   // so the old rollover has somewhere to go
			City c = g.AddCity(p, 0, 36, 25)!;
			c.Size = 6;
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
