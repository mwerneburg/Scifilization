// CivOne tests
//
// The Tonawanda loop (game f37aa663): under Autopilot, RuntimeHandler.AutopilotTick presses
// Enter on the map every ~0.1s, and GamePlay cancels the active unit's GoTo on any key during
// the human's turn. So every AI-driven settler of the human's lost its order after one step,
// re-decided, stepped, lost it again — 50 hops a turn each until the circuit breaker, seven
// settlers at once. Headless replays have no screen and were calm, which hid it.

using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Screens;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AutopilotGotoTests : System.IDisposable
	{
		private readonly bool _auto;
		public AutopilotGotoTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		private static IUnit KeyOnTheMap(bool autopilot)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			GamePlay screen = new GamePlay();
			Settings.Instance.Autopilot = autopilot;
			var cur = typeof(Game).GetField("_currentPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
			cur.SetValue(g, System.Convert.ChangeType(g.PlayerNumber(g.HumanPlayer), cur.FieldType));
			IUnit s = g.CreateUnit(UnitType.Settlers, 40, 25, g.PlayerNumber(g.HumanPlayer))!;
			s.MovesLeft = s.Move;
			s.Goto = new Point(45, 25);
			g.ActiveUnit = s;
			Sim.ClearTasks();

			screen.KeyDown(new KeyboardEventArgs(Key.Enter, KeyModifier.None));
			return s;
		}

		[Fact]
		public void AutopilotsEnterLeavesTheOrderStanding()
		{
			IUnit s = KeyOnTheMap(autopilot: true);
			Assert.Equal(new Point(45, 25), s.Goto);
		}

		// Control: a player at the keys still cancels a GoTo with any key.
		[Fact]
		public void APlayersKeyStillCancelsIt()
		{
			IUnit s = KeyOnTheMap(autopilot: false);
			Assert.True(s.Goto.IsEmpty, $"goto {s.Goto} survived the key");
		}
	}
}
