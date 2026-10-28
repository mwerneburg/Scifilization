// CivOne tests
//
// Settlers looped through the same few tiles around Tonawanda every turn (game f37aa663),
// but only in the running game: every replay of its saves was calm. So the game logs it:
// a settler that moved 4+ steps and ended within a tile of where its turn began writes a
// `round_trip` record with its trail.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class RoundTripLogTests
	{
		[Fact]
		public void ARideOutAndBackIsLogged()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			p.AddAdvance(new CivOne.Advances.RailRoad(), false);
			for (int x = 38; x <= 45; x++)
			{
				Map.Instance.ChangeTileType(x, 25, Terrain.Plains);
				Map.Instance[x, 25].Road = true;
				Map.Instance[x, 25].RailRoad = true;
			}
			var s = (Settlers)g.CreateUnit(UnitType.Settlers, 40, 25, g.PlayerNumber(p))!;
			s.NewTurn();   // the turn begins here
			Sim.ClearTasks();

			foreach (int dx in new[] { 1, 1, 1, -1, -1, -1 })
			{
				s.MovesLeft = s.Move;
				s.MoveTo(dx, 0);
				Sim.Settle();
			}
			Assert.Equal((40, 25), ((int)s.X, (int)s.Y));

			int before = Sim.DecisionLineCount("\"type\":\"round_trip\"");
			s.NewTurn();
			string line = Sim.AwaitDecisionLine("\"type\":\"round_trip\"", before);

			Assert.Contains("\"steps\":6", line);
			Assert.Contains("\"from\":\"40,25\"", line);
		}
	}
}
