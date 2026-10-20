// CivOne tests
//
// Two ways the user's autopiloted Democracy, at war with the Federation in turn 394, failed to
// fight it (Oct 2026):
//
//   * It sat in Consolidate — 51 of 74 cities merely unhappy, none rioting — and strike groups
//     and airlifts are marshalled only in Militarize. Twelve Hover Tanks stood in Cyrene beside
//     an undefended Federation city. An invader holding a city on our continent now outranks
//     discontent; real rioting still consolidates.
//   * Its Cruise Missiles were given the ground troops' order — gather on the staging tile —
//     and with one turn of fuel ran dry on that empty square, or over the sea on the way.

using System.Drawing;
using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class InvasionWarFootingTests
	{
		private static string StanceOf(Player p)
			=> typeof(AI).GetMethod("GetStance", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(AI.Instance(p), null)!.ToString()!;

		// A Democracy leaning on its luxury slider, which on its own means Consolidate.
		private static (Game g, Player p, Player federation) AnUneasyDemocracy(bool invaderAshore)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			p.Government = new Governments.Democracy();
			p.TaxesRate = 4; p.LuxuriesRate = 4;
			g.AddCity(p, 0, 25, 25)!.Size = 4;

			var federation = new Player(Common.Civilizations.First(c => c is Civilizations.Federation), "Federation");
			g.AddPlayer(federation);
			if (invaderAshore) g.AddCity(federation, 1, 50, 25)!.Size = 4;
			federation.DeclareWar(p);
			Sim.ClearTasks();
			return (g, p, federation);
		}

		[Fact]
		public void AnInvaderAshoreOutranksDiscontent()
		{
			(Game _, Player p, Player _) = AnUneasyDemocracy(invaderAshore: true);
			Assert.Equal("Militarize", StanceOf(p));
		}

		[Fact]
		public void WithTheInvaderGoneDiscontentStillConsolidates()
		{
			(Game _, Player p, Player _) = AnUneasyDemocracy(invaderAshore: false);
			Assert.Equal("Consolidate", StanceOf(p));
		}

		// ── missiles ────────────────────────────────────────────────────────

		private static (Player p, IUnit missile) AMissileAtHome()
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			p.Government = new Governments.Monarchy();
			p.Explore(40, 25, range: 40);
			City home = g.AddCity(p, 0, 20, 25)!;
			home.Size = 4;
			IUnit missile = g.CreateUnit(UnitType.CruiseMissile, 20, 25, g.PlayerNumber(p))!;
			Sim.ClearTasks();
			return (p, missile);
		}

		private static void Assign(Player p, IUnit unit)
			=> typeof(AI).GetMethod("AssignMission", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(AI.Instance(p), new object[] { unit });

		[Fact]
		public void AMissileIsSentAtAnEnemyInRange()
		{
			(Player p, IUnit missile) = AMissileAtHome();
			Game.Instance.CreateUnit(UnitType.Militia, 25, 25, 0);

			Assign(p, missile);

			Assert.Equal(new Point(25, 25), missile.Goto);
		}

		// The target is forty tiles off: out of a missile's reach, and the old order sent it
		// toward the staging square beside the walls anyway.
		[Fact]
		public void AMissileWithNothingInRangeStaysHome()
		{
			(Player p, IUnit missile) = AMissileAtHome();
			Game g = Game.Instance;
			Player rival = g.Players.First(x => x is not null && x != p && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			City far = g.AddCity(rival, 1, 60, 25)!;
			far.Size = 3;
			g.CreateUnit(UnitType.Militia, 60, 25, g.PlayerNumber(rival));
			p.DeclareWar(rival);   // at war, so Militarize: the stance that handed out the staging order
			Sim.ClearTasks();
			Assert.Equal("Militarize", StanceOf(p));

			Assign(p, missile);

			Assert.True(missile.Goto.IsEmpty, $"the missile was sent to {missile.Goto}");
			Assert.Equal((20, 25), (missile.X, missile.Y));
		}
	}
}
