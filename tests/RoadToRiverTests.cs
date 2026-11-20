// CivOne tests
//
// A settler laying a road (RoadTo) cannot road a river before Bridge Building, so its route
// should go round one where it can (the user, Nov 2026). Where it cannot, it used to stop on
// the river for good: BuildRoad refused, and the order waited for a road that never started.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class RoadToRiverTests
	{
		// Open grassland with a short north-south river across the straight route.
		private static Settlers Roader(int x, int y, bool bridges)
		{
			Sim.NewGame(width: 80, height: 50);
			for (int ty = 18; ty <= 32; ty++)
			for (int tx = 30; tx <= 55; tx++)
				Map.Instance.ChangeTileType(tx, ty, Terrain.Grassland1);
			for (int ty = 23; ty <= 27; ty++)
				Map.Instance.ChangeTileType(42, ty, Terrain.River);
			Map.Instance.RecalculateContinentsIfDirty();

			Game g = Game.Instance;
			Player human = g.HumanPlayer;
			human.Explore(42, 25, range: 20);
			if (bridges) human.AddAdvance(new BridgeBuilding(), false);
			var s = (Settlers)g.CreateUnit(UnitType.Settlers, x, y, g.PlayerNumber(human))!;
			s.RoadTo = new Point(46, 25);
			s.Goto = s.RoadTo;
			Sim.ClearTasks();
			return s;
		}

		private static List<ITile> Walk(IUnit unit, int gx, int gy)
		{
			var route = new List<ITile>();
			for (int i = 0; i < 40; i++)
			{
				ITile? step = Common.GotoStep(unit, gx, gy);
				if (step is null) break;
				route.Add(step);
				unit.X = step.X; unit.Y = step.Y;
				if (unit.X == gx && unit.Y == gy) break;
			}
			return route;
		}

		[Fact]
		public void WithoutBridgesTheRoadGoesRoundTheRiver()
		{
			Settlers s = Roader(38, 25, bridges: false);

			List<ITile> route = Walk(s, 46, 25);

			Assert.Equal((46, 25), (route.Last().X, route.Last().Y));
			Assert.DoesNotContain(route, t => t is River);
		}

		// The detour is for the road's sake, not the walk's: with bridges it goes straight.
		[Fact]
		public void WithBridgesItCrosses()
		{
			Settlers s = Roader(38, 25, bridges: true);

			Assert.Contains(Walk(s, 46, 25), t => t is River);
		}

		[Fact]
		public void ASettlerOnARiverWalksOn()
		{
			Settlers s = Roader(42, 25, bridges: false);
			s.Goto = Point.Empty;

			s.NewTurn();

			Assert.Equal(new Point(46, 25), s.Goto);
		}
	}
}
