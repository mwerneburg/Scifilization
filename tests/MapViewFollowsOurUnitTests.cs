// CivOne tests
//
// Where the map looks when it is the human's turn (the user, Nov 2026).
//
// With Enemy Moves on, the view follows an enemy's move and stayed there: the human's first
// unit was only brought back into view if it had gone off screen, so early in the game, with
// the barbarians close, the turn began looking at them.
//
// And a popup sent the view somewhere unrelated. While a task is up Game.ActiveUnit holds
// whatever unit its index last pointed at, of any owner, moved or not; when that was one of
// ours far away, fortified, the map centred on it behind the news.

using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Screens.GamePlayPanels;
using CivOne.Units;

namespace CivOne.Tests
{
	public class MapViewFollowsOurUnitTests : System.IDisposable
	{
		private readonly bool _reveal;
		public MapViewFollowsOurUnitTests() { Sim.EnsureRuntime(); _reveal = Settings.Instance.RevealWorld; }
		public void Dispose() => Settings.Instance.RevealWorld = _reveal;

		private static void Turn(Game g, byte player)
		{
			var cur = typeof(Game).GetField("_currentPlayer", BindingFlags.NonPublic | BindingFlags.Instance)!;
			cur.SetValue(g, System.Convert.ChangeType(player, cur.FieldType));
		}

		private static void PointAt(Game g, IUnit unit)
		{
			var units = (System.Collections.IList)typeof(Game).GetField("_units", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(g)!;
			typeof(Game).GetField("_activeUnit", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(g, units.IndexOf(unit));
		}

		private static bool CentredOn(GameMap map, IUnit unit) => map.X == unit.X - map.TilesX / 2;

		// The human with one unit to move at (20, 25), nothing else of theirs able to move.
		private static (Game g, byte human, IUnit ours) AWorld()
		{
			Sim.NewGame(width: 80, height: 50, competition: 3);
			Game g = Game.Instance;
			for (int y = 15; y <= 35; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			byte human = g.PlayerNumber(g.HumanPlayer);
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == human)) u.MovesLeft = 0;
			IUnit ours = g.CreateUnit(UnitType.Militia, 20, 25, human)!;
			Settings.Instance.RevealWorld = true;
			g.EnemyMoves = true;
			Turn(g, human);
			Sim.ClearTasks();
			return (g, human, ours);
		}

		// The map checks its unit on even ticks and on odd ones by separate paths.
		[Theory]
		[InlineData(4u)]
		[InlineData(5u)]
		public void OurTurnBeginsOnOurUnitAfterAnEnemyMove(uint tick)
		{
			(Game g, byte human, IUnit ours) = AWorld();
			var map = new GameMap();
			map.CenterOnPoint(ours.X, ours.Y);
			// A barbarian a few tiles off: its move pulls the view over, leaving ours on screen.
			IUnit raider = g.CreateUnit(UnitType.Militia, ours.X + map.TilesX / 2 - 1, ours.Y, 0)!;
			Turn(g, 0);
			g.ActiveUnit = raider;
			map.MustUpdate(0);
			raider.MoveTo(1, 0);
			Sim.Settle();
			Assert.False(CentredOn(map, ours), "scenario is broken: the enemy move did not move the view");
			map.MustUpdate(2);

			Turn(g, human);
			g.ActiveUnit = ours;
			map.MustUpdate(tick);

			Assert.True(CentredOn(map, ours), $"the view stayed on the enemy's move: X {map.X}, our unit at {ours.X}");
		}

		// The control: our own units moving about do not recentre a view they are still in.
		[Fact]
		public void OurNextUnitOnScreenLeavesTheViewAlone()
		{
			(Game g, byte human, IUnit ours) = AWorld();
			IUnit next = g.CreateUnit(UnitType.Militia, ours.X + 3, ours.Y, human)!;
			var map = new GameMap();
			map.CenterOnPoint(ours.X, ours.Y);
			g.ActiveUnit = ours;
			map.MustUpdate(0);

			ours.MovesLeft = 0;
			g.ActiveUnit = next;
			map.MustUpdate(2);
			map.MustUpdate(3);

			Assert.True(CentredOn(map, ours));
		}

		[Fact]
		public void APopupDoesNotSendTheViewToAFortifiedUnit()
		{
			(Game g, byte human, IUnit ours) = AWorld();
			IUnit faraway = g.CreateUnit(UnitType.Militia, 60, 25, human)!;
			faraway.Fortify = true;
			var map = new GameMap();
			map.CenterOnPoint(ours.X, ours.Y);
			g.ActiveUnit = ours;
			map.MustUpdate(0);

			GameTask.Enqueue(CivOne.Tasks.Message.Newspaper(null!, "Elsewhere,", "a thing happened."));
			PointAt(g, faraway);
			Assert.Same(faraway, g.ActiveUnit);   // scenario: the getter holds the stale unit
			map.MustUpdate(2);
			map.MustUpdate(3);
			Sim.ClearTasks();

			Assert.True(CentredOn(map, ours), $"the popup moved the view to X {map.X}");
		}
	}
}
