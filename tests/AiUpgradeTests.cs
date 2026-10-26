// CivOne tests
//
// The AI never upgraded a unit: at turn 381 of game c58808e6 the Lakota could build Riflemen
// yet kept 45 Militia and Phalanxes. ConsiderUpgrades now refits units standing in a city
// with a Barracks, keeping a 50g reserve.

using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AiUpgradeTests
	{
		private static (Player ai, IUnit militia) AnAiCityWithAMilitia(short gold)
		{
			Sim.NewGame(width: 80, height: 50);
			Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			ai.Government = new Governments.Monarchy();
			ai.AddAdvance(new Conscription(), false);
			City c = g.AddCity(ai, 0, 40, 25)!;
			c.AddBuilding(new Barracks());
			IUnit militia = g.CreateUnit(UnitType.Militia, 40, 25, g.PlayerNumber(ai), false)!;
			ai.Gold = gold;
			Sim.ClearTasks();
			return (ai, militia);
		}

		[Fact]
		public void AnAiRefitsAMilitiaWhenItCanFieldRiflemen()
		{
			(Player ai, IUnit militia) = AnAiCityWithAMilitia(gold: 200);

			ai.AI!.ConsiderUpgrades();

			IUnit here = Map.Instance[40, 25].Units.Single();
			Assert.IsType<Riflemen>(here);
			Assert.Equal(170, ai.Gold);
		}

		// The refit would leave 30g, under the 50g reserve.
		[Fact]
		public void AnAiKeepsItsReserve()
		{
			(Player ai, IUnit militia) = AnAiCityWithAMilitia(gold: 60);

			ai.AI!.ConsiderUpgrades();

			Assert.IsType<Militia>(Map.Instance[40, 25].Units.Single());
			Assert.Equal(60, ai.Gold);
		}
	}
}
