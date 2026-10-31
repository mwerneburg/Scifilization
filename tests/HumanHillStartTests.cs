// CivOne tests
//
// Every capital that starts on Hills is smoothed to irrigated Plains with a road
// (Game.PrepareStartingTile), the human's included. The human used to be left on the hill:
// on Epic Earth the Ottoman start is a hill in central Anatolia, and the human Ottomans of
// game 72c60037 sat at size 1-2 on 0-1 food, learned one advance in 440 turns and were
// destroyed, while every AI Ottoman got the smoothed tile.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class HumanHillStartTests
	{
		[Fact]
		public void AHumanOttomanStartIsSmoothedLikeAnAIOne()
		{
			Sim.NewGame(map: "earth-epic");   // stages the Earth board
			Sim.ResetState();
			Assert.True(Map.Instance.LoadEarthEpic());
			while (!Map.Instance.Ready) System.Threading.Thread.Sleep(20);
			Assert.True(Map.Instance[191, 67] is Hills, "precondition: the Ottoman start is no longer a hill");

			Game.CreateGame(0, 16, Common.Civilizations.First(c => c is Civilizations.Ottoman), "Tester", "Test", "Testers");

			Game g = Game.Instance;
			Assert.True(g.HumanPlayer.Civilization is Civilizations.Ottoman, "precondition: the human is not the Ottomans");
			IUnit start = g.GetUnits().First(u => u.Owner == g.PlayerNumber(g.HumanPlayer) && u is Settlers);
			ITile tile = Map.Instance[start.X, start.Y];
			Assert.True(tile is Plains && tile.Irrigation && tile.Road,
				$"human start at ({start.X},{start.Y}) is {tile.GetType().Name} irr={tile.Irrigation} road={tile.Road}");
		}
	}
}
