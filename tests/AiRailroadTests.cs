// CivOne tests
//
// Game c3bd1512: about 140 AI settlers stood on one tile for 20-120 turns, each civ in a
// wave just as it learned Railroad. Finishing a railroad asked whether the HUMAN knew
// Railroad, not the settler's owner: the job was cancelled, re-ordered the same turn,
// and the wave cleared only when the human learned it (t387).

using System.Linq;
using CivOne;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AiRailroadTests
	{
		[Fact]
		public void AnAiSettlerFinishesItsRailroadThoughTheHumanLacksRailroad()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			ai.AddAdvance(new RailRoad(), false);
			Assert.False(g.HumanPlayer.HasAdvance<RailRoad>(), "precondition: the human has no railroad");

			int x = 40, y = 25;
			Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			ITile tile = Map.Instance[x, y];
			tile.Road = true;

			var cur = typeof(Game).GetField("_currentPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
			cur.SetValue(g, System.Convert.ChangeType(g.PlayerNumber(ai), cur.FieldType));
			var s = (Settlers)g.CreateUnit(UnitType.Settlers, x, y, g.PlayerNumber(ai))!;
			Assert.True(s.BuildRoad(), "precondition: the railroad is started");

			for (int i = 0; i < 2; i++) s.NewTurn();

			Assert.True(tile.RailRoad);
		}
	}
}
