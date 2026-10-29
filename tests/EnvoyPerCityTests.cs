// CivOne tests
//
// One envoy of each kind per city at a time. Diplomats and caravans are spent on arrival, so a
// second only stacks up behind one that is stuck: at turn 550 of game a82cf446 the Federation
// held 64 diplomats from 39 cities, up to 5 apiece, most of them on islands with no ship.

using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class EnvoyPerCityTests
	{
		// The Federation, four cities: its agent cap is one per two cities, so two.
		private static (Player p, City[] cities) AFederation()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player p = new Player(Common.Civilizations.First(c => c is Civilizations.Federation), "Faction");
			g.AddPlayer(p);
			foreach (IAdvance a in Common.Advances) p.AddAdvance(a, false);
			p.Explore(40, 25, range: 40);
			City[] cities = new[] { 14, 20, 26, 32 }.Select((x, i) => { City c = g.AddCity(p, i, x, 25)!; c.Size = 6; return c; }).ToArray();
			g.CreateUnit(UnitType.Diplomat, 14, 25, g.PlayerNumber(p), false)!.SetHome(cities[0]);
			Sim.ClearTasks();
			return (p, cities);
		}

		[Fact]
		public void ACityWithADiplomatAfootDoesNotBuildAnother()
		{
			(Player p, City[] cities) = AFederation();
			Assert.DoesNotContain(AI.Instance(p).ProductionPlan(cities[0]), x => x is Diplomat);
		}

		// Control: one agent of two, so a city with none of its own still builds one.
		[Fact]
		public void ACityWithoutOneStillBuildsIt()
		{
			(Player p, City[] cities) = AFederation();
			Assert.Contains(AI.Instance(p).ProductionPlan(cities[1]), x => x is Diplomat);
		}
	}
}
