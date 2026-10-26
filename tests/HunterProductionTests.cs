// CivOne tests
//
// The Haida ended a game in Democracy with 83 advances and 61 units, every one at attack 3
// or less, while defence-6 harvesters drank their land. HuntQuarry had nobody to send:
// every attacker entry in the production planner waited on SenateRaisesArmies, and a
// Democracy only counts as invaded by the Federation, the Others or Skynet.
//
// The rule pinned here: quarry near a city, and nothing on the continent able to kill it,
// buys a hunter — whatever the government — and only one per beast.

using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class HunterProductionTests
	{
		private static (Game g, Player p, City c) ADemocracy()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 15; y <= 35; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();

			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			p.Government = new Governments.Democracy();
			p.AddAdvance(new Automobile(), false);
			p.Explore(40, 25, range: 20);
			City c = g.AddCity(p, 0, 40, 25)!;
			g.CreateUnit(UnitType.Riflemen, 40, 25, g.PlayerNumber(p), false);
			Sim.ClearTasks();
			return (g, p, c);
		}

		private static List<IProduction> PlanFor(Player p, City c)
		{
			var method = typeof(AI).GetMethod("PlanProductionInto",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var stanceType = typeof(AI).GetNestedType("StrategyStance", System.Reflection.BindingFlags.NonPublic);
			object develop = System.Enum.Parse(stanceType!, "Develop");
			return (List<IProduction>)method!.Invoke(AI.Instance(p), new object[] { new List<IProduction>(), c, develop })!;
		}

		[Fact]
		public void ADemocracyBuildsAHunterForAHarvesterInItsFields()
		{
			var (g, p, c) = ADemocracy();
			g.CreateUnit(UnitType.Harvester, 45, 25, 0);

			Assert.Contains(PlanFor(p, c), x => x is Armor);
		}

		[Fact]
		public void NoQuarryNoHunter()
		{
			var (_, p, c) = ADemocracy();

			Assert.DoesNotContain(PlanFor(p, c), x => x is Armor);
		}

		// One beast, one hunter: an Armor already standing on the continent is the answer.
		[Fact]
		public void AHunterAlreadyAfootIsEnough()
		{
			var (g, p, c) = ADemocracy();
			g.CreateUnit(UnitType.Harvester, 45, 25, 0);
			g.CreateUnit(UnitType.Armor, 38, 25, g.PlayerNumber(p), false);

			Assert.DoesNotContain(PlanFor(p, c), x => x is Armor);
		}

		// Gozira (defence 24) is nobody's quarry; building Armor at it is feeding the wall.
		[Fact]
		public void NoHunterIsBuiltForWhatItCannotBeat()
		{
			var (g, p, c) = ADemocracy();
			g.CreateUnit(UnitType.Gozira, 45, 25, 0);

			Assert.DoesNotContain(PlanFor(p, c), x => x is Armor);
		}
	}
}
