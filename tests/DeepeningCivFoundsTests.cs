// CivOne tests
//
// GetStance puts a builder-path civ (Commerce, Culture, Diaspora) into Develop at half its city
// target, and Develop planned no founding settler for a civ of three or more cities: only the
// worker quota's one. China at peace in game abb9ba24, Commerce with a target of 15, held 7
// cities from t125 to t300 with room around it; 19-24 in games where wars kept it in
// Militarize, which still builds settlers.

using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class DeepeningCivFoundsTests
	{
		[Fact]
		public void ADeepeningCivWithRoomStillPlansASettler()
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 15; y <= 35; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();

			var flags = BindingFlags.NonPublic | BindingFlags.Instance;
			int Target(Player q) => (int)typeof(AI).GetMethod("CityTarget", flags)!.Invoke(AI.Instance(q), null)!;
			Player p = g.Players.First(q => q is not null && q != g.HumanPlayer && g.PlayerNumber(q) != 0 && Target(q) > 6);
			byte id = g.PlayerNumber(p);
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == id).ToArray()) g.DisbandUnit(u);
			p.Explore(40, 25, range: 30);
			City[] cities = new[] { 20, 25, 30, 35, 40 }.Select((x, i) => g.AddCity(p, i, x, 25)!).ToArray();
			City c = cities[0];
			c.Size = 5;
			c.ResetResourceTiles();
			foreach (City each in cities) g.CreateUnit(UnitType.Militia, each.X, each.Y, id)!.SetHome(each);
			g.CreateUnit(UnitType.Militia, c.X, c.Y, id)!.SetHome(c);
			// The worker quota's one settler, alive and homed elsewhere.
			g.CreateUnit(UnitType.Settlers, 41, 26, id)!.SetHome(cities[4]);

			AI ai = AI.Instance(p);
			System.Type pathType = typeof(AI).Assembly.GetTypes().First(t => t.Name == "VictoryPath");
			typeof(AI).GetField("_path", flags)!.SetValue(ai, System.Enum.Parse(pathType, "Commerce"));
			typeof(AI).GetField("_pathChosenTurn", flags)!.SetValue(ai, (int)g.GameTurn);
			Sim.ClearTasks();
			Assert.Equal("Develop", typeof(AI).GetMethod("GetStance", flags)!.Invoke(ai, null)!.ToString());
			Assert.True((bool)typeof(AI).GetMethod("HasExpansionRoom", flags)!.Invoke(ai, null)!);
			Assert.True(c.FoodIncome >= 1, $"precondition: city can afford a settler, food {c.FoodIncome}");

			Assert.Contains(ai.ProductionPlan(c), x => x is Settlers);
		}
	}
}
