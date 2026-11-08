// CivOne tests
//
// Colony sites were registered only by a hull at sea, and the hull rule, once a civ had seen 60%
// of the world's land, opened only on a registered site: no ship, no site, so no ship. On Epic
// Earth map trades pass 60% early, so every overseas city on seed 4242 came from a Trireme the
// production fallback happened to roll; banning that roll put no ship to sea in 320 turns. A civ
// now reads sites off its own map, and the fallback rolls no cargo hull.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class ChartedColonySiteTests
	{
		// An island civ in an empty ocean, the whole world charted, with or without a second
		// island in sight. Chieftain front-loads Militia, so difficulty 1; the garrison keeps
		// any rule from asking for a defender.
		private static City AnIslandPort(bool landInSight)
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 1);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			for (int y = 0; y < 50; y++)
			for (int x = 0; x < 80; x++)
			{
				bool home = System.Math.Abs(x - 40) <= 3 && System.Math.Abs(y - 25) <= 3;
				bool far = landInSight && System.Math.Abs(x - 56) <= 2 && System.Math.Abs(y - 25) <= 2;
				Map.Instance.ChangeTileType(x, y, home || far ? Terrain.Grassland1 : Terrain.Ocean);
			}
			Map.Instance.RecalculateContinentsIfDirty();
			p.Explore(40, 25, range: 80);
			p.AddAdvance(new Advances.MapMaking(), false);
			City port = g.AddCity(p, 0, 43, 25)!;   // on the shore
			for (int i = 0; i < 2; i++) g.CreateUnit(UnitType.Musketeers, 43, 25, g.PlayerNumber(p));
			Sim.ClearTasks();
			Assert.True(p.ExploredLandFraction >= 0.60, "fixture: the hull rule's exploring window is still open");
			Assert.Contains(port.AvailableProduction, x => x is Trireme);
			return port;
		}

		[Fact]
		public void ACoastInSightGetsAShip()
		{
			City port = AnIslandPort(landInSight: true);
			Assert.Contains(AI.Instance(port.Player).ProductionPlan(port), x => x is IBoardable);
		}

		// Its own island's far shore is no reason to sail, and the fallback no longer rolls one.
		[Fact]
		public void NoCoastInSightNoShip()
		{
			City port = AnIslandPort(landInSight: false);
			for (int i = 0; i < 100; i++)
				Assert.DoesNotContain(AI.Instance(port.Player).ProductionPlan(port), x => x is IBoardable);
		}
	}
}
