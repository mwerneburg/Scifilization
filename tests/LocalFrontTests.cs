// CivOne tests
//
// A front on every landmass (AI.TargetFor). The civ-wide attack target is one city; an
// attacker on another continent was sent to a staging tile it could not walk to, failed,
// fortified, and — MarshalStrikeGroup waking only the target's continent — never woke again.
// Measured on the user's turn-365 save (Oct 2026): the Federation aimed at Jaipur while five
// Hover Tanks sat fortified two tiles from Lakota towns on full moves.

using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class LocalFrontTests : System.IDisposable
	{
		private readonly bool _auto;
		public LocalFrontTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// West island x 20..32, east island x 48..60. We hold a city on each, so the main target
		// on the east is walkable from there and no airlift is called for.
		private static (Game g, Player us, City main, City local, AI ai) TwoFronts()
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
			{
				bool land = y >= 20 && y <= 30 && ((x >= 20 && x <= 32) || (x >= 48 && x <= 60));
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Grassland1 : Terrain.Ocean);
			}
			Map.Instance.RecalculateContinentsIfDirty();

			Player[] ps = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			Player us = ps[0], them = ps[1];
			foreach (Player p in new[] { us, them }) { p.Government = new CivOne.Governments.Monarchy(); p.Explore(26, 25, range: 10); p.Explore(54, 25, range: 10); }
			us.AddAdvance(new CivOne.Advances.Automobile(), false);
			byte me = g.PlayerNumber(us), they = g.PlayerNumber(them);
			int id = 0;
			foreach (int x in new[] { 22, 50 })
			{
				City c = g.AddCity(us, id++, x, 25)!;
				c.Size = 6;
				g.CreateUnit(UnitType.Musketeers, x, 25, me);
			}
			City local = g.AddCity(them, id++, 30, 22)!;
			City main = g.AddCity(them, id++, 57, 25)!;
			foreach (City c in new[] { local, main }) { c.Size = 4; g.CreateUnit(UnitType.Phalanx, c.X, c.Y, they); }
			Assert.NotEqual(main.Tile.ContinentId, local.Tile.ContinentId);
			us.DeclareWar(them);
			Sim.ClearTasks();
			AI ai = AI.Instance(us)!;
			typeof(AI).GetField("_attackTarget", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(ai, main);
			return (g, us, main, local, ai);
		}

		[Fact]
		public void AnAttackerOffTheMainFrontIsWoken()
		{
			(Game g, Player us, City main, City _, AI ai) = TwoFronts();
			IUnit armor = g.CreateUnit(UnitType.Armor, 26, 25, g.PlayerNumber(us))!;
			armor.Fortify = true;

			ai.ConsiderWar();

			Assert.False(armor.Fortify, "an attacker on the second front stayed asleep");
		}

		[Fact]
		public void ItAimsAtTheEnemyOnItsOwnLandmass()
		{
			(Game g, Player us, City main, City local, AI ai) = TwoFronts();
			IUnit armor = g.CreateUnit(UnitType.Armor, 26, 25, g.PlayerNumber(us))!;

			typeof(AI).GetMethod("AssignMission", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ai, new object[] { armor });

			Assert.False(armor.Goto.IsEmpty, "fixture: the attacker was given nowhere to go");
			Assert.True(Common.DistanceToTile(armor.Goto.X, armor.Goto.Y, local.X, local.Y) <= 1,
				$"sent to {armor.Goto}, not the enemy town at {local.X},{local.Y} on its own island");
		}

		// The half that must not change: on the main target's ground it still goes for the main
		// target, however close a lesser town stands.
		[Fact]
		public void OnTheMainFrontTheMainTargetStands()
		{
			(Game g, Player us, City main, City local, AI ai) = TwoFronts();
			City lesser = g.AddCity(local.Player, 9, 54, 28)!;   // empty: what a fresh pick would take
			Sim.ClearTasks();
			IUnit armor = g.CreateUnit(UnitType.Armor, 52, 25, g.PlayerNumber(us))!;

			typeof(AI).GetMethod("AssignMission", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ai, new object[] { armor });

			Assert.True(Common.DistanceToTile(armor.Goto.X, armor.Goto.Y, main.X, main.Y) <= 1, $"sent to {armor.Goto}");
		}
	}
}
