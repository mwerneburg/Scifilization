// CivOne tests
//
// A settler its ship had unloaded beside its own coast sat aboard for up to 120 turns (game
// 453977e5, four Japanese settlers). Two things held it. Standing on water it belonged to no
// landmass, so every site on land read as unreachable; and the work check offered it a ROAD
// on the water under the hull, which it ordered every turn, spending the move it needed to
// step ashore.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AboardSettlerTests : System.IDisposable
	{
		private readonly bool _auto;
		public AboardSettlerTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// An island of open grassland, and an AI settler unloaded on a Trireme off its west coast.
		private static (Player p, IUnit settler) ASettlerAboardOffItsCoast()
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			Settings.Instance.Autopilot = false;
			for (int y = 10; y < 40; y++)
			for (int x = 10; x < 70; x++)
			{
				Map.Instance.ChangeTileType(x, y, x is >= 30 and <= 50 && y is >= 18 and <= 32 ? Terrain.Grassland1 : Terrain.Ocean);
				((BaseTile)Map.Instance[x, y]).Special = false;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == g.PlayerNumber(p)).ToArray()) g.DisbandUnit(u);
			p.Explore(40, 25, range: 15);
			byte id = g.PlayerNumber(p);
			g.CreateUnit(UnitType.Trireme, 29, 25, id, false);
			IUnit s = g.CreateUnit(UnitType.Settlers, 29, 25, id, false)!;
			Sim.ClearTasks();
			return (p, s);
		}

		[Fact]
		public void FromAboardTheShoreIsReachable()
		{
			(_, IUnit s) = ASettlerAboardOffItsCoast();
			Assert.True(Map.NamedContinent(Map.Instance[35, 25].ContinentId), "fixture: the island is not a named landmass");

			Assert.True(AI.LandReachable(s, Map.Instance[35, 25]));
		}

		[Fact]
		public void TheSeaOffersNoLandWork()
		{
			(Player p, IUnit s) = ASettlerAboardOffItsCoast();

			Assert.False(AI.Instance(p).WorkAvailable(s.Tile).Road, "a road on the water under the ship");
		}

		// Both together: given its move, the settler heads ashore.
		[Fact]
		public void AnUnloadedSettlerHeadsAshore()
		{
			(Player p, IUnit s) = ASettlerAboardOffItsCoast();

			AI.Instance(p).Move(s);

			Assert.True(!s.Tile.IsOcean || (!s.Goto.IsEmpty && !Map.Instance[s.Goto.X, s.Goto.Y].IsOcean),
				$"still aboard at ({s.X},{s.Y}) with goto {s.Goto}");
		}
	}
}
