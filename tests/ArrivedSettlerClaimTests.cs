// CivOne tests
//
// A settler that has just arrived at its work tile spends its move getting there and starts
// work next turn, so it is neither Busy nor going anywhere. The site scan did not count it,
// so a second settler was routed to the same tile, found it taken next turn and left
// (Tonawanda, game f37aa663).

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class ArrivedSettlerClaimTests
	{
		[Fact]
		public void ASettlerIsNotSentWhereAnotherHasJustArrived()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			p.Government = new CivOne.Governments.Monarchy();
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == g.PlayerNumber(p)).ToArray()) g.DisbandUnit(u);

			// A tundra islet, ocean all round: one city, and one farmable tile beside it.
			for (int y = 15; y <= 35; y++)
			for (int x = 30; x <= 50; x++)
			{
				bool land = System.Math.Abs(x - 40) <= 2 && System.Math.Abs(y - 25) <= 2;
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Tundra : Terrain.Ocean);
				((BaseTile)Map.Instance[x, y]).Special = false;
			}
			Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
			Map.Instance.ChangeTileType(42, 25, Terrain.Plains);
			((BaseTile)Map.Instance[42, 25]).Special = false;
			// Its water: a river already irrigated, so the river offers no work of its own.
			Map.Instance.ChangeTileType(42, 24, Terrain.River);
			Map.Instance[42, 24].Irrigation = true;
			Map.Instance.RecalculateContinentsIfDirty();
			p.Explore(40, 25, range: 6);
			g.AddCity(p, 0, 40, 25);
			byte id = g.PlayerNumber(p);
			ITile farm = Map.Instance[42, 25];
			Assert.True(AI.Instance(p).WorkAvailable(farm).Irrigation, "fixture: the plain is not farm work");

			IUnit arrived = g.CreateUnit(UnitType.Settlers, 42, 25, id, false)!;
			arrived.MovesLeft = 0;
			IUnit second = g.CreateUnit(UnitType.Settlers, 40, 25, id, false)!;
			second.MovesLeft = second.Move;
			Sim.ClearTasks();

			AI.Instance(p).Move(second);

			Assert.NotEqual((42, 25), (second.Goto.X, second.Goto.Y));
		}
	}
}
