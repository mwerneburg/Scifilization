// CivOne tests
//
// The stalemate exit read one clock for the whole civ, so thirty turns at war with anybody made
// every new war a stalemate from its first turn: peace in a few turns, and free to declare again.
// Japan opened 28 wars on the Khmer in game 7c41d897, a median of 6 turns apart. The clock is
// now kept per enemy.

using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Governments;

namespace CivOne.Tests
{
	public class WarClockTests
	{
		// Solvent, undefeated and far from anyone: the stalemate is its only way out of a war.
		private static (Player us, Player a, Player b) ThreeCivs()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player[] ais = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			(Player us, Player a, Player b) = (ais[0], ais[1], ais[2]);
			foreach (Player p in new[] { us, a, b }) p.Government = new Monarchy();
			int id = 0;
			for (int i = 0; i < 4; i++) g.AddCity(us, id++, 14 + i * 4, 14)!.Size = 8;
			g.AddCity(a, id++, 14, 30)!.Size = 8;
			g.AddCity(b, id++, 40, 30)!.Size = 8;
			Sim.ClearTasks();
			return (us, a, b);
		}

		private static void Turn(Player us)
		{
			us.Gold = 1000;
			AI.Instance(us)!.ConsiderWar();
		}

		private static void SetCivClock(Player us, int turns)
		{
			typeof(AI).GetField("_turnsAtWar", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(AI.Instance(us), turns);
			typeof(AI).GetField("_peacetimeCities", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(AI.Instance(us), us.Cities.Length);
		}

		// Long at war with one enemy, and the new one gets a war of its own.
		[Fact]
		public void ALongWarElsewhereDoesNotEndANewOne()
		{
			(Player us, Player old, Player fresh) = ThreeCivs();
			us.DeclareWar(old);
			SetCivClock(us, 100);
			us.DeclareWar(fresh);

			for (int t = 0; t < 25; t++) Turn(us);

			Assert.True(us.IsAtWar(fresh), "a war under 30 turns old ended as a stalemate");
		}

		[Fact]
		public void AWarWithOneEnemyEndsInStalemateAfterThirtyTurns()
		{
			(Player us, Player them, Player _) = ThreeCivs();
			SetCivClock(us, 0);
			us.DeclareWar(them);

			int turns = 0;
			while (us.IsAtWar(them) && turns < 200) { Turn(us); turns++; }

			Assert.False(us.IsAtWar(them), "the stalemate never ended");
			Assert.True(turns > 30, $"it ended after {turns} turns");
		}

		// Peace winds the clock back, so the next war with the same enemy starts from nothing.
		[Fact]
		public void PeaceRestartsTheClock()
		{
			(Player us, Player them, Player other) = ThreeCivs();
			SetCivClock(us, 0);
			us.DeclareWar(them);
			int turns = 0;
			while (us.IsAtWar(them) && turns < 200) { Turn(us); turns++; }
			Assert.False(us.IsAtWar(them), "the fixture's war never ended");

			us.DeclareWar(them);
			for (int t = 0; t < 25; t++) Turn(us);

			Assert.True(us.IsAtWar(them), "the second war inherited the first war's clock");
		}

		// A buddy respawn hands the slot to a newcomer, who owes nothing for its predecessor's war.
		[Fact]
		public void ANewcomerInTheSlotStartsAFreshClock()
		{
			(Player us, Player them, Player _) = ThreeCivs();
			us.DeclareWar(them);
			us.WarTurnsWith[Game.Instance.PlayerNumber(them)] = 100;

			us.ForgetSlot(Game.Instance.PlayerNumber(them));

			Assert.Empty(us.WarTurnsWith);
		}
	}
}
