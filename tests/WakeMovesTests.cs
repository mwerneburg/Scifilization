// CivOne tests
//
// Waking a unit hands back what sleep took, never a fresh turn.
//
// Going to sleep zeroes a unit's moves (BaseUnit.Sentry), and every wake and unload site
// made up for it by refilling to a full Move. Every unit is refilled at NewTurn anyway, so
// those refills only ever mattered for a unit put to sleep THIS turn — which made them a
// loophole. Reported from a game: a Dirigible carrying four Hover Tanks flew Gibraltar →
// Corduba → Kolokani → Ansongo → Goundam in one turn, waking in each city, and Bombers
// were woken in their base to strike city after city.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CivOne;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class WakeMovesTests
	{
		private static (Game g, byte me) AWorld()
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
			Map.Instance.ChangeTileType(41, 25, Terrain.Grassland1);
			Sim.ClearTasks();
			return (Game.Instance, Game.Instance.PlayerNumber(Game.Instance.HumanPlayer));
		}

		[Fact]
		public void WakingTheSameTurnGivesBackOnlyWhatWasLeft()
		{
			(Game g, byte me) = AWorld();
			IUnit tank = g.CreateUnit(UnitType.HoverTank, 40, 25, me)!;
			tank.MovesLeft = 2;

			tank.Sentry = true;
			Assert.Equal(0, (int)tank.MovesLeft);
			tank.Busy = false;

			Assert.Equal(2, (int)tank.MovesLeft);
		}

		// The half that must not change.
		[Fact]
		public void AUnitAsleepSinceLastTurnWakesFull()
		{
			(Game g, byte me) = AWorld();
			IUnit tank = g.CreateUnit(UnitType.HoverTank, 40, 25, me)!;
			tank.MovesLeft = 1;
			tank.Sentry = true;

			tank.NewTurn();
			tank.Sentry = false;

			Assert.Equal((int)tank.Move, (int)tank.MovesLeft);
		}

		[Fact]
		public void DirigibleCargoStepsOffWithWhatItBoardedWith()
		{
			(Game g, byte me) = AWorld();
			IUnit tank = g.CreateUnit(UnitType.HoverTank, 40, 25, me)!;
			tank.MovesLeft = 1;
			tank.Sentry = true;
			Dirigible d = (Dirigible)g.CreateUnit(UnitType.Dirigible, 40, 25, me)!;

			Assert.True(d.MoveTo(1, 0), "fixture: the move was refused");
			Sim.Settle();
			Assert.Equal((41, 25), (tank.X, tank.Y));
			d.Unload();

			Assert.False(tank.Sentry);
			Assert.Equal(1, (int)tank.MovesLeft);
		}

		// The click paths (city garrison, unit stack, W key) are screens with no headless
		// handle, so they are held to it by source: outside unit creation, the only full
		// refill left is pulling a working Settler off its job, whose moves the job zeroes
		// every turn it works.
		[Fact]
		public void NothingElseRefillsAWokenUnit()
		{
			string src = Path.Combine(Sim.RepoRoot(), "src");
			var refills = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
				.SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, line, i)))
				.Where(x => Regex.IsMatch(x.line, @"MovesLeft = [\w\[\]]+\.Move;"))
				.Where(x => !x.line.Contains("Settlers") && !x.line.Contains("working"))
				.Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}: {x.line.Trim()}")
				.ToList();

			// BaseUnit: the enemy-adjacent alarm, which fires on the enemy's turn, ahead of the
			// owner's own NewTurn. Game: CreateUnit.
			Assert.Equal(new[] { "BaseUnit.cs", "Game.cs" },
				refills.Select(r => r.Split(':')[0]).OrderBy(s => s).ToArray());
		}
	}
}
