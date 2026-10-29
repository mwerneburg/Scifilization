// CivOne tests
//
// The stalled-city settler (AI.SettlerIsTheOnlyWayOut) was for one- or two-city civs only.
// Game 100847b1: the Babylonians held four cities from t200 to t500 at food 0-1 on bare
// Plains and Hills, no settler alive, and 344 of their picks were Research Grants; the
// Ottomans and Khmer stalled the same way at 5-7. Expand wanted size 4 and a surplus, and a
// small civ outside Develop gets no worker quota, so nothing ever irrigated their land.

using System.Collections.Generic;
using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Units;

namespace CivOne.Tests
{
	public class StalledSmallCivTests
	{
		private static (Player p, City[] cities) FourStalledCities()
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 10; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			int[] xs = { 20, 28, 36, 44 };
			foreach (int x in xs) Map.Instance.ChangeTileType(x + 1, 25, Terrain.River);
			Map.Instance.RecalculateContinentsIfDirty();

			Player p = g.Players.First(q => q is not null && g.PlayerNumber(q) != 0 && q != g.HumanPlayer);
			p.Government = new Despotism();
			p.Explore(35, 25, range: 30);
			byte id = g.PlayerNumber(p);
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == id).ToArray()) g.DisbandUnit(u);

			City[] cities = xs.Select((x, i) => g.AddCity(p, i, x, 25)!).ToArray();
			foreach (City c in cities)
			{
				c.AddBuilding(new Granary());
				c.Size = 2;
				c.ResetResourceTiles();
				// Bonus resources survive ChangeTileType; grow until genuinely stalled.
				for (int guard = 0; guard < 6 && c.FoodIncome > 1; guard++)
				{
					c.Size = (byte)(c.Size + 1);
					c.ResetResourceTiles();
				}
				// Something to defend it, so the plan is not just a garrison.
				g.CreateUnit(UnitType.Militia, c.X, c.Y, id)!.SetHome(c);
			}
			Sim.ClearTasks();
			return (p, cities);
		}

		private static List<IProduction> Plan(Player p, City c) => AI.Instance(p).ProductionPlan(c).ToList();

		[Fact]
		public void AStalledFourCityCivBuildsASettler()
		{
			(Player p, City[] cities) = FourStalledCities();
			City c = cities[0];
			Assert.True(c.FoodIncome <= 1 && c.Size >= 2, $"precondition: stalled, size {c.Size} food {c.FoodIncome}");

			Assert.Contains(Plan(p, c), x => x is Settlers);
		}

		// The hatch counts settlers alive, so without this every stalled city queues one.
		[Fact]
		public void OneCityRaisesItAtATime()
		{
			(Player p, City[] cities) = FourStalledCities();
			cities[1].SetProduction(new Settlers());

			Assert.DoesNotContain(Plan(p, cities[0]), x => x is Settlers);
		}
	}
}
