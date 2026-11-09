// CivOne tests
//
// When every rule declines, the planner's backstop adds a defender. A city already holding
// four foot defenders disbands any fifth at once (the per-city trim in AI.cs), unrefunded
// while a unit is in production: Kyoto built a Militia every two or three turns from turn
// 70 to 91 and lost nine (seeded Epic Earth 4242); 32 of the AI's 370 disbands in game
// 7c41d897 were this loop. A full garrison builds the cheapest building it can instead.

using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class FullGarrisonTests
	{
		private static IProduction[] PlanWithGarrison(int defenders, bool templeToo = false, int size = 1)
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);   // Kyoto's; Chieftain has a Militia rule of its own
			Settings.Instance.Autopilot = true;
			Game g = Game.Instance;
			Player us = g.HumanPlayer;
			foreach (IAdvance a in us.Advances) us.DeleteAdvance(a);
			us.AddAdvance(new CeremonialBurial());   // a Temple, which a content city declines
			us.LuxuriesRate = 0;
			// Grassland, explored, its tiles picked: a settler needs a food surplus.
			ITile site = Map.Instance.AllTiles().First(t => t.Type == Terrain.Grassland1 && t.City is null && t.Y > 5 && t.Y < Map.HEIGHT - 5);
			us.Explore(site.X, site.Y, range: 3);
			City city = g.AddCity(us, 0, site.X, site.Y)!;
			city.AddBuilding(new Barracks());        // Kyoto built its own by turn 60
			if (templeToo) city.AddBuilding(new Temple());
			city.Size = (byte)size;
			city.ResetResourceTiles();
			// Japan's shape: four towns, each other one held, and a second settler out, which is
			// the empire's settler budget for four. Fewer towns and these units are a "unit glut",
			// and the fallback's glut rule sends a settler on its own; under budget, so does
			// the ordinary settler rule.
			ITile[] land = Map.Instance.AllTiles().Where(t => !t.IsOcean && t.Y > 5 && t.Y < Map.HEIGHT - 5).ToArray();
			var towns = new System.Collections.Generic.List<ITile> { site };
			for (int i = 0; i < 3; i++)
			{
				ITile t = land.First(x => towns.All(y => Common.DistanceToTile(x.X, x.Y, y.X, y.Y) > 8));
				towns.Add(t);
				Assert.NotNull(g.AddCity(us, 0, t.X, t.Y));
				g.CreateUnit(UnitType.Militia, t.X, t.Y, g.PlayerNumber(us), false);
			}
			g.CreateUnit(UnitType.Settlers, towns[1].X, towns[1].Y, g.PlayerNumber(us), false);
			for (int i = 0; i < 2; i++) g.CreateUnit(UnitType.Explorer, site.X, site.Y, g.PlayerNumber(us), false);   // Japan's two, the cap
			for (int i = 0; i < defenders; i++) g.CreateUnit(UnitType.Militia, site.X, site.Y, g.PlayerNumber(us), false);
			return AI.Instance(us).ProductionPlan(city);
		}

		private static bool FootDefender(IProduction p) => p is Militia || p is Phalanx || p is Musketeers || p is Riflemen || p is MechInf;

		// The plan's tail is a random pick, so plan many times.
		[Fact]
		public void AFullGarrisonBuildsInstead()
		{
			for (int i = 0; i < 25; i++)
			{
				IProduction[] plan = PlanWithGarrison(4);
				Assert.NotEmpty(plan);
				Assert.DoesNotContain(plan, FootDefender);
			}
		}

		// Its control: one short of the limit, the same city still raises a defender.
		[Fact]
		public void OneShortStillRaisesADefender() =>
			Assert.Contains(Enumerable.Range(0, 25).SelectMany(_ => PlanWithGarrison(3)), FootDefender);

		// With every building up, it sends out a settler instead (Kyoto after its Temple).
		[Fact]
		public void AFullGarrisonWithNothingToBuildSendsASettler()
		{
			IProduction[] plan = PlanWithGarrison(4, templeToo: true, size: 3);
			Assert.DoesNotContain(plan, FootDefender);
			Assert.Contains(plan, p => p is Settlers);
		}
	}
}
