// CivOne tests
//
// The player's settler is spent when the new city's screen closes (Orders.CityManagerClosed).
// Under Autopilot nobody closes it in a headless run, so the settler lived on: one unit
// founded Aachen, Paris, Tours, Metz and Orleans in 32 turns, and the harness's player slot
// reached 23 cities by turn 100 against 3 to 6 for each AI. Every headless game that compared
// the player's civ with an AI was measuring that (the user's three-civ question, Nov 2026).
// Autopilot now founds as the AI does, as the naming dialog beside it already did.

using CivOne.Enums;
using CivOne.Tasks;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AutopilotFoundingTests
	{
		private static (City? city, bool settlerLeft) Found(bool autopilot)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player human = g.HumanPlayer;
			human.Explore(40, 25, range: 4);
			Settings.Instance.Autopilot = autopilot;

			IUnit settler = g.CreateUnit(UnitType.Settlers, 40, 25, g.PlayerNumber(human))!;
			settler.MovesLeft = settler.Move;
			Sim.ClearTasks();

			GameTask.Enqueue(Orders.FoundCity(settler as Settlers));
			for (int i = 0; i < 12 && GameTask.Any(); i++) GameTask.Update();

			return (Map.Instance[40, 25].City, System.Array.IndexOf(g.GetUnits(), settler) >= 0);
		}

		[Fact]
		public void UnderAutopilotTheSettlerIsSpent()
		{
			(City? city, bool settlerLeft) = Found(autopilot: true);

			Assert.NotNull(city);
			Assert.False(settlerLeft, "the settler outlived the city it founded");
		}

		// Played by hand the player is still asked for a name, and the settler waits.
		[Fact]
		public void PlayedByHandThePlayerIsStillAsked()
		{
			(City? _, bool settlerLeft) = Found(autopilot: false);

			Assert.True(Common.HasScreenType<CivOne.Screens.CityName>());
			Assert.True(settlerLeft);
		}
	}
}
