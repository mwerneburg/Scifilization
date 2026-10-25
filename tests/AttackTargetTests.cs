// CivOne tests
//
// Where a strike group goes (Oct 2026). Weakest-first anywhere on the continent sent the
// Federation's army past held border towns to whichever city stood empty that turn, twenty
// tiles deep; and a garrison counted by defenders alone read a city of tanks as a three-tank
// job. Now: the frontier first, a Wonder weighs in there, and every fighter in the city counts.

using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AttackTargetTests
	{
		// Our city at 20,25 on a long strip of grassland, at war with a rival.
		private static (Game g, Player p, Player rival) AWar()
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			Player rival = g.Players.First(x => x is not null && x != p && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			p.Government = new Governments.Monarchy();
			p.Explore(40, 25, range: 40);
			g.AddCity(p, 0, 20, 25)!.Size = 4;
			p.DeclareWar(rival);
			return (g, p, rival);
		}

		private static City ACity(Game g, Player owner, int nameId, int x, int y, UnitType unit, int count)
		{
			City c = g.AddCity(owner, nameId, x, y)!;
			c.Size = 4;
			for (int i = 0; i < count; i++) g.CreateUnit(unit, x, y, g.PlayerNumber(owner));
			return c;
		}

		private static City Pick(Player p)
		{
			Sim.ClearTasks();
			return (City)typeof(AI).GetMethod("PickAttackTarget", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(AI.Instance(p), new object?[] { null })!;   // continent: null = anywhere
		}

		[Fact]
		public void AHeldBorderTownComesBeforeAnEmptyCityDeep()
		{
			(Game g, Player p, Player rival) = AWar();
			City border = ACity(g, rival, 1, 26, 25, UnitType.MechInf, 2);
			City deep = ACity(g, rival, 2, 50, 25, UnitType.MechInf, 0);
			deep.AddWonder(new Wonders.HangingGardens());   // not even a Wonder draws them that far

			Assert.Equal(border, Pick(p));
		}

		[Fact]
		public void AWonderOnTheFrontierIsWorthTheFight()
		{
			(Game g, Player p, Player rival) = AWar();
			ACity(g, rival, 1, 26, 25, UnitType.MechInf, 1);
			City wonder = ACity(g, rival, 2, 27, 29, UnitType.MechInf, 3);
			wonder.AddWonder(new Wonders.HangingGardens());

			Assert.Equal(wonder, Pick(p));
		}

		[Fact]
		public void ACityOfTanksNeedsMoreThanThree()
		{
			(Game g, Player _, Player rival) = AWar();
			City tanks = ACity(g, rival, 1, 26, 25, UnitType.Armor, 6);
			int threshold = (int)typeof(AI).GetMethod("StrikeThreshold", BindingFlags.NonPublic | BindingFlags.Static)!
				.Invoke(null, new object[] { tanks })!;

			Assert.Equal(7, threshold);
		}
	}
}
