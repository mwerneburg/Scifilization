// CivOne tests
//
// The user's Guarani and the Romans launched on the same turn and both land in 1903 (Nov 2026).
// The slot order decided everything: the Romans, slot 1, took the first-arrival rank, and
// when both Diaspora holds reached twenty on the same turn, the loop met them first. Ships
// that land together now share the rank, and a tied hold goes to the larger colony.

using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class SimultaneousColonyTests
	{
		// Two rivals in slot order, each with a standing Mission Control. The first has the
		// smaller ship, so slot order and colony size disagree.
		private static (Game g, Player first, Player second) TwoColonists()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();

			Player[] rivals = g.Players.Where(p => p is not null && p != g.HumanPlayer && g.PlayerNumber(p) != 0)
				.OrderBy(p => g.PlayerNumber(p)).Take(2).ToArray();
			int x0 = 30;
			foreach (Player r in rivals)
			{
				r.Explore(x0, 25, range: 4);
				City c = g.AddCity(r, 0, x0, 25)!;
				c.Size = 4;
				c.AddBuilding(new MissionControl());
				x0 += 15;
			}
			Set(g, rivals[0], module: 3, component: 2);
			Set(g, rivals[1], module: 12, component: 16);
			Assert.True(Game.SpaceshipScore(12, 16) > Game.SpaceshipScore(3, 2), "fixture: second ship should be the larger");
			Sim.ClearTasks();
			return (g, rivals[0], rivals[1]);
		}

		private static void Set(Game g, Player p, int module, int component)
		{
			var pr = g.Progress(g.PlayerNumber(p));
			pr.SpaceshipModule = module;
			pr.SpaceshipComponent = component;
			pr.SpaceshipStructural = 50;
		}

		private static object? Call(Game g, string method) =>
			typeof(Game).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(g, null);

		[Fact]
		public void ShipsLandingTogetherShareTheRank()
		{
			(Game g, Player first, Player second) = TwoColonists();
			g.GameTurn = 453; // an arrival turn of 0 means no ship in flight
			g.Progress(g.PlayerNumber(first)).SpaceshipArrivalTurn = g.GameTurn;
			g.Progress(g.PlayerNumber(second)).SpaceshipArrivalTurn = g.GameTurn;

			Call(g, "LandSpaceships");

			Assert.Equal(1, g.Progress(g.PlayerNumber(first)).ColonyOrder);
			Assert.Equal(1, g.Progress(g.PlayerNumber(second)).ColonyOrder);
		}

		// Both holds reach twenty on the same turn. The check stops at the first winner, so
		// the loser's streak is left at 19.
		[Fact]
		public void ATiedHoldGoesToTheLargerColony()
		{
			(Game g, Player first, Player second) = TwoColonists();
			foreach (Player p in new[] { first, second })
			{
				var pr = g.Progress(g.PlayerNumber(p));
				pr.ColonyFounded = true;
				pr.ColonyOrder = 1;
				pr.DiasporaStreak = Game.DiasporaStreakTarget - 1;
			}

			Assert.True((bool)Call(g, "CheckDiaspora")!);

			Assert.Equal(Game.DiasporaStreakTarget, g.Progress(g.PlayerNumber(second)).DiasporaStreak);
			Assert.Equal(Game.DiasporaStreakTarget - 1, g.Progress(g.PlayerNumber(first)).DiasporaStreak);
		}
	}
}
