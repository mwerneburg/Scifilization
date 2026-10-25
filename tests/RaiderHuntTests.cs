// CivOne tests
//
// Monster hunt, phase 3 (Oct 2026). The hunt took only barbarian megafauna, so a rival's tanks
// could stand beside an AI city for as long as they liked: in the turn-365 save of the user's
// game about a hundred hostile combat units stood in the open within eight tiles of an AI city
// at war with them, three quarters of them still there five turns later. Now anything hostile
// and armed within three tiles of one of our cities is quarry — at war only, from close by,
// and only on odds of at least even by the combat's own arithmetic.

using System.Linq;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class RaiderHuntTests
	{
		// An AI city at (40,25) on open grassland, at war with a rival AI civ.
		private static (Game g, Player ai, Player rival) AWar()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 15; y <= 35; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			Player rival = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer && p != ai);
			ai.Explore(40, 25, range: 20);
			g.AddCity(ai, 0, 40, 25);
			ai.DeclareWar(rival);
			Sim.ClearTasks();
			return (g, ai, rival);
		}

		private static IUnit Unit(Game g, Player p, UnitType t, int x, int y) => g.CreateUnit(t, x, y, g.PlayerNumber(p))!;

		// Armor (8 x 10 = 80) against Armor in the open (5 x 2 x 4 = 40), two tiles from our city.
		[Fact]
		public void EnemyTanksBesideOurCityAreQuarry()
		{
			(Game g, Player ai, Player rival) = AWar();
			IUnit theirs = Unit(g, rival, UnitType.Armor, 42, 25);
			IUnit ours = Unit(g, ai, UnitType.Armor, 41, 25);

			Assert.Same(theirs, AI.Instance(ai).HuntQuarry(ours));
		}

		// A barbarian raiding party is quarry too, not only the monsters.
		[Fact]
		public void ABarbarianRaiderAtTheGatesIsQuarry()
		{
			(Game g, Player ai, Player _) = AWar();
			IUnit knights = g.CreateUnit(UnitType.Knights, 42, 25, 0)!;
			IUnit ours = Unit(g, ai, UnitType.Armor, 41, 25);

			Assert.Same(knights, AI.Instance(ai).HuntQuarry(ours));
		}

		// Mech Inf fortified on a mountain: 6 x 6 x 6 = 216 against 80. Nominal attack 10 beats
		// nominal defence 6, which is exactly the test that would have sent it.
		[Fact]
		public void NotOnBadOdds()
		{
			(Game g, Player ai, Player rival) = AWar();
			Map.Instance.ChangeTileType(42, 25, Terrain.Mountains);
			IUnit theirs = Unit(g, rival, UnitType.MechInf, 42, 25);
			theirs.Fortify = true;
			IUnit ours = Unit(g, ai, UnitType.Armor, 41, 25);

			Assert.Null(AI.Instance(ai).HuntQuarry(ours));
		}

		[Fact]
		public void NotAtPeace()
		{
			(Game g, Player ai, Player rival) = AWar();
			ai.MakePeace(rival);
			Unit(g, rival, UnitType.Armor, 42, 25);
			IUnit ours = Unit(g, ai, UnitType.Armor, 41, 25);

			Assert.Null(AI.Instance(ai).HuntQuarry(ours));
		}

		// Close to the city but far from this unit: a tank across the continent does not
		// march back for a raider that will have moved on.
		[Fact]
		public void NotFromAcrossTheContinent()
		{
			(Game g, Player ai, Player rival) = AWar();
			Unit(g, rival, UnitType.Armor, 42, 25);
			IUnit ours = Unit(g, ai, UnitType.Armor, 50, 25);

			Assert.Null(AI.Instance(ai).HuntQuarry(ours));
		}

		// A fortified unit is never offered a decision, so the hunt has to wake it, as it does
		// for monsters (SleepingHunterTests).
		[Fact]
		public void AFortifiedTankIsWokenForTheRaid()
		{
			(Game g, Player ai, Player rival) = AWar();
			Unit(g, rival, UnitType.Armor, 42, 25);
			IUnit ours = Unit(g, ai, UnitType.Armor, 41, 25);
			ours.Fortify = true;

			AI.Instance(ai).WakeHunters();

			Assert.False(ours.Fortify);
		}
	}
}
