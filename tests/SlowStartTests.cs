// CivOne tests
//
// A tiny empire's capital that would take more than 20 turns over a Granary founds first.
// The planner queues its whole plan and the city follows it before replanning, so a
// Granary and Harbour at its head were 100 shields before a settler was considered. On the
// Earth map (game 396337f3) Kyoto, on hills at 1-2 shields, spent turns 10-105 on the two
// and built its first Settlers on turn 125; Japan had 4 cities at 1050 AD.

using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class SlowStartTests
	{
		// A one-city civ inland on open grassland with food to spare and no settler in the
		// field. Shield specials decide its output: without them it makes the city centre's 1
		// shield. The Granary costs 90 here, half again for want of Salt.
		private static (Player p, City c) ACapital(bool shieldSpecials, int size = 3)
		{
			// Prince, as played: Chieftain front-loads a settler of its own at size 4.
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			for (int y = 15; y <= 35; y++)
			for (int x = 20; x <= 60; x++)
			{
				Map.Instance.ChangeTileType(x, y, shieldSpecials ? Terrain.Grassland2 : Terrain.Grassland1);
				((CivOne.Tiles.BaseTile)Map.Instance[x, y]).Special = shieldSpecials;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			p.Government = new Governments.Despotism();
			p.AddAdvance(new Pottery(), false);
			p.Explore(40, 25, range: 20);
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == g.PlayerNumber(p)).ToArray()) g.DisbandUnit(u);
			City c = g.AddCity(p, 0, 40, 25)!;
			c.Size = (byte)size;
			c.ResetResourceTiles();
			Sim.ClearTasks();
			return (p, c);
		}

		private static IProduction[] Plan(Player p, City c)
		{
			var plan = new System.Collections.Generic.List<IProduction>();
			System.Type stance = typeof(AI).GetNestedType("StrategyStance",
				System.Reflection.BindingFlags.NonPublic)!;
			typeof(AI).GetMethod("PlanProductionInto",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.Invoke(AI.Instance(p), new object[] { plan, c, System.Enum.Parse(stance, "Expand") });
			return plan.ToArray();
		}

		private static string Show(IProduction[] plan) => string.Join(", ", plan.Select(x => x.GetType().Name));

		[Fact]
		public void ASlowCapitalPlansSettlersBeforeItsGranary()
		{
			(Player p, City c) = ACapital(shieldSpecials: false);
			Assert.True(c.ShieldIncome * 20 < c.ProductionCost(new Granary()), $"fixture is not slow: {c.ShieldIncome} shields");
			Assert.True(c.FoodIncome >= 1, $"fixture cannot afford a settler: food {c.FoodIncome}");

			IProduction[] plan = Plan(p, c);

			int granary = System.Array.FindIndex(plan, x => x is Granary);
			int settlers = System.Array.FindIndex(plan, x => x is Settlers);
			Assert.True(settlers >= 0 && (granary < 0 || settlers < granary), Show(plan));
		}

		// Control: a capital that can finish a Granary in good time still builds it first.
		[Fact]
		public void AProductiveCapitalStillBuildsItsGranaryFirst()
		{
			(Player p, City c) = ACapital(shieldSpecials: true, size: 5);
			Assert.False(c.ShieldIncome * 20 < c.ProductionCost(new Granary()), $"fixture is slow: {c.ShieldIncome} shields");

			IProduction[] plan = Plan(p, c);

			int granary = System.Array.FindIndex(plan, x => x is Granary);
			int settlers = System.Array.FindIndex(plan, x => x is Settlers);
			Assert.True(granary >= 0 && (settlers < 0 || granary < settlers), Show(plan));
		}

		private static IWonder? Wonder(Player p, City c)
		{
			System.Type stance = typeof(AI).GetNestedType("StrategyStance",
				System.Reflection.BindingFlags.NonPublic)!;
			return (IWonder?)typeof(AI).GetMethod("SelectWonder",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.Invoke(AI.Instance(p), new object[] { c, System.Enum.Parse(stance, "Expand") });
		}

		// Game aa0eb1a5: Kyoto planned the Colossus at exactly the 100-turn horizon, shipped
		// its first Settlers, and spent turns 57-132 on the wonder at 0-2 shields.
		[Fact]
		public void AOneCityCivWithRoomStartsNoWonder()
		{
			(Player p, City c) = ACapital(shieldSpecials: true, size: 5);
			p.AddAdvance(new Masonry(), false);

			Assert.Null(Wonder(p, c));
		}

		// Control: the same capital, once the empire has its third city, takes one.
		[Fact]
		public void AThreeCityCivsCapitalStillTakesAWonder()
		{
			(Player p, City c) = ACapital(shieldSpecials: true, size: 5);
			p.AddAdvance(new Masonry(), false);
			Game.Instance.AddCity(p, 1, 28, 18);
			Game.Instance.AddCity(p, 2, 52, 32);

			Assert.NotNull(Wonder(p, c));
		}
	}
}
