// CivOne tests
//
// Every capital that starts on Hills is smoothed to irrigated Plains with a road
// (Game.PrepareStartingTile), the human's included. The human used to be left on the hill:
// on Epic Earth the Ottoman start was a hill in central Anatolia, and the human Ottomans of
// game 72c60037 sat at size 1-2 on 0-1 food, learned one advance in 440 turns and were
// destroyed, while every AI Ottoman got the smoothed tile.
//
// Since then an Earth start also moves to the best site nearby (EarthStartSiteTests), which
// takes the Ottomans off their hill altogether. So this asks the smoothing directly, on the
// human's own start tile made a hill, rather than through a start that no longer lands there.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class HumanHillStartTests
	{
		[Fact]
		public void AHumanHillStartIsSmoothedLikeAnAIOne()
		{
			Sim.NewGame(map: "earth-epic", competition: 16);
			Game g = Game.Instance;
			IUnit start = g.GetUnits().First(u => u.Owner == g.PlayerNumber(g.HumanPlayer) && u is Settlers);
			Map.Instance.ChangeTileType(start.X, start.Y, Terrain.Hills);
			Map.Instance[start.X, start.Y].Irrigation = false;
			Map.Instance[start.X, start.Y].Road = false;

			typeof(Game).GetMethod("PrepareStartingTile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.Invoke(g, new object[] { (int)start.X, (int)start.Y });

			ITile tile = Map.Instance[start.X, start.Y];
			Assert.True(tile is Plains && tile.Irrigation && tile.Road,
				$"human start at ({start.X},{start.Y}) is {tile.GetType().Name} irr={tile.Irrigation} road={tile.Road}");
		}
	}
}
