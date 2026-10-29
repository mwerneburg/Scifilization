// CivOne tests
//
// Japan held one city for 500 turns in three games running, its units idle from the first
// turn (game 6d2f0cca: a Settlers asleep in Kyoto t68-t488, never ordered). Japan sat in the
// slot right after the human, who was on Autopilot. At the end of the human's turn
// Game.Update read ActiveUnit, whose getter queued a Turn.End, then queued its own: the
// first ended the human's turn, the second ended Japan's before a unit of it moved. Its
// cities and research still ran, so nothing looked wrong but the map.

using System.Linq;
using CivOne.Enums;
using CivOne.Screens;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AutopilotTurnHandoffTests : System.IDisposable
	{
		private readonly bool _auto;
		public AutopilotTurnHandoffTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		[Fact]
		public void TheCivAfterAnAutopilotHumanGetsItsTurn()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			GamePlay screen = new GamePlay();
			Common.AddScreen(screen);
			try
			{
				Settings.Instance.Autopilot = true;
				byte human = g.PlayerNumber(g.HumanPlayer);
				var cur = typeof(Game).GetField("_currentPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
				cur.SetValue(g, System.Convert.ChangeType(human, cur.FieldType));
				// A unit of ours with its moves spent: the getter's end-of-turn case.
				IUnit spent = g.CreateUnit(UnitType.Militia, 40, 25, human)!;
				spent.MovesLeft = 0;
				foreach (IUnit u in g.GetUnits().Where(u => u.Owner == human && u != spent)) u.MovesLeft = 0;
				Sim.ClearTasks();

				g.Update();
				Sim.Settle();

				Assert.Equal(human + 1, g.PlayerNumber(g.CurrentPlayer));
			}
			finally { Common.DestroyScreen(screen); }
		}
	}
}
