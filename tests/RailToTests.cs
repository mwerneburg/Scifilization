// CivOne tests
//
// "Build Rail To..." (Shift+O, Nov 2026): the Road To order with RoadToRail set. A tile has
// work until it carries rail, so a settler walking an existing road upgrades it as it goes,
// and the last tile gets rail too (Road To clears on arrival and leaves its last tile bare).

using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class RailToTests
	{
		// Grassland, with a road one row north of the straight line from (30,25) to (40,25):
		// it leaves at (31,24) and rejoins at (39,24), so it is no longer than the straight walk.
		private static (Game g, Settlers s) ARoadBesideTheLine()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 18; y <= 32; y++)
			for (int x = 25; x <= 50; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			for (int x = 31; x <= 39; x++) Map.Instance[x, 24].Road = true;

			Game g = Game.Instance;
			Player human = g.HumanPlayer;
			human.Explore(35, 25, range: 20);
			human.AddAdvance(new RailRoad(), false);
			// BuildRoad asks the player whose turn it is for Railroad; in play that is the
			// owner of the settler being moved.
			var cur = typeof(Game).GetField("_currentPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
			cur.SetValue(g, System.Convert.ChangeType(g.PlayerNumber(human), cur.FieldType));
			var s = (Settlers)g.CreateUnit(UnitType.Settlers, 30, 25, g.PlayerNumber(human))!;
			s.RoadTo = new Point(40, 25);
			s.RoadToRail = true;
			s.Goto = s.RoadTo;
			Sim.ClearTasks();
			return (g, s);
		}

		// What the game loop does for one settler: a new turn, then walk its Goto while it can.
		private static void PlayTurns(Settlers s, int turns)
		{
			for (int turn = 0; turn < turns && !s.RoadTo.IsEmpty; turn++)
			{
				s.NewTurn();
				for (int step = 0; step < 10 && !s.Goto.IsEmpty && (s.MovesLeft > 0 || s.PartMoves > 0); step++)
				{
					ITile? next = Common.GotoStep(s, s.Goto.X, s.Goto.Y);
					if (next is null) break;
					s.MoveTo(next.X - s.X, next.Y - s.Y);
					Sim.Settle();
				}
			}
		}

		[Fact]
		public void ItRailsTheRoadItWalksAlongAndTheLastTile()
		{
			(_, Settlers s) = ARoadBesideTheLine();

			PlayTurns(s, 80);

			Assert.True(s.RoadTo.IsEmpty, $"the order never finished; settler at ({s.X},{s.Y})");
			Assert.All(Enumerable.Range(31, 9), x => Assert.True(Map.Instance[x, 24].RailRoad, $"({x},24) has no rail"));
			Assert.True(Map.Instance[40, 25].RailRoad, "the last tile has no rail");
			Assert.All(Enumerable.Range(32, 7), x => Assert.False(Map.Instance[x, 25].Road, $"({x},25) was built beside the road"));
		}

		[Fact]
		public void ARailOrderSurvivesASaveAndLoad()
		{
			(Game g, Settlers s) = ARoadBesideTheLine();
			string path = System.IO.Path.Combine(Settings.Instance.SavesDirectory, "railto.cos");
			g.SaveCos(path);

			Sim.ResetState();
			Assert.True(Game.LoadCos(path), "LoadCos should succeed");

			Settlers back = Game.Instance.GetUnits().OfType<Settlers>().Single(u => u.X == 30 && u.Y == 25);
			Assert.Equal(new Point(40, 25), back.RoadTo);
			Assert.True(back.RoadToRail, "the save turned the rail order into a road order");
		}

		[Fact]
		public void TheMenuOffersRailToOnlyWithRailroad()
		{
			(_, Settlers s) = ARoadBesideTheLine();
			Assert.Contains(s.MenuItems, m => m?.Text == "Build Rail To...");

			Game.Instance.HumanPlayer.DeleteAdvance(new RailRoad());
			Assert.DoesNotContain(s.MenuItems, m => m?.Text == "Build Rail To...");
		}
	}
}
