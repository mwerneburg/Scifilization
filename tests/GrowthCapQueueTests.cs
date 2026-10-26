// CivOne tests
//
// The AI planner queues its whole plan and only replans on an empty queue. A city that
// reached its growth cap partway through worked through ten-odd stale items before the
// Aqueduct: in game c58808e6 (turns 353-440) a fresh plan for a blocked city chose the
// Aqueduct or Sewer 120 times in 125, the stale queue 62 times in 1,131. Now a blocked AI
// city replans when it moves to its next queued item.

using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class GrowthCapQueueTests
	{
		// An AI city that has just finished a Barracks, with a queue planned before it hit its cap.
		private static City AnAiCityFinishingABuild(int size)
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 15; y <= 35; y++)
			for (int x = 20; x <= 60; x++)
			{
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
				Map.Instance[x, y].Irrigation = true;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			ai.Government = new Governments.Monarchy();
			ai.AddAdvance(new Construction(), false);
			ai.Explore(40, 25, range: 20);
			City c = g.AddCity(ai, 0, 40, 25)!;
			c.Size = (byte)size;
			c.ResetResourceTiles();
			g.CreateUnit(UnitType.Militia, 40, 25, g.PlayerNumber(ai), false);
			c.SetProduction(new Barracks());
			c.Shields = c.ProductionCost(c.CurrentProduction);
			c.EnqueueProduction(new Granary());
			c.EnqueueProduction(new Temple());
			Sim.ClearTasks();
			return c;
		}

		// Not necessarily first: the fresh plan may put a garrison ahead of it, as it would for
		// any city. But ahead of everything the stale queue held.
		[Fact]
		public void ACityAtItsCapBuildsTheAqueductBeforeTheStaleQueue()
		{
			City c = AnAiCityFinishingABuild(size: 7);

			c.NewTurn();

			Assert.True(c.HasBuilding<Barracks>(), "fixture: the Barracks did not finish");
			Assert.True(c.GrowthBlocked, "fixture: the city is not at its cap");
			var order = new[] { c.CurrentProduction }.Concat(c.ProductionQueue).ToList();
			int aqueduct = order.FindIndex(p => p is Aqueduct);
			int stale = order.FindIndex(p => p is Granary or Temple);
			Assert.True(aqueduct >= 0 && (stale < 0 || aqueduct < stale),
				"aqueduct is not ahead of the stale queue: " + string.Join(", ", order.Select(p => p.GetType().Name)));
		}

		// Control: below the cap the queue is the AI's plan and is followed as it stands.
		[Fact]
		public void ACityBelowItsCapKeepsItsQueue()
		{
			City c = AnAiCityFinishingABuild(size: 5);

			c.NewTurn();

			Assert.True(c.HasBuilding<Barracks>(), "fixture: the Barracks did not finish");
			Assert.IsType<Granary>(c.CurrentProduction);
		}
	}
}
