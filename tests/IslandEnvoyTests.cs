// CivOne tests
//
// Island civs' diplomats sat in their own cities for 170-320 turns (game c3bd1512: five
// Maori, seven Japanese). A diplomat only looked for targets on its own landmass, and no
// ship ever carried one. Now it boards a hull, and the hull sets it down beside a foreign port.
// Island caravans had the same trouble and take the same ship.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class IslandEnvoyTests : System.IDisposable
	{
		private readonly bool _auto;
		public IslandEnvoyTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// Our island (20-29) with a city on its east coast at (29,25); theirs (45-54) with a
		// city on its west coast at (45,25); open sea between.
		private static (Game g, Player mine) TwoIslands()
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			for (int y = 10; y < 40; y++)
			for (int x = 10; x < 70; x++)
			{
				bool land = y is >= 20 and <= 30 && (x is >= 20 and <= 29 || x is >= 45 and <= 54);
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Grassland1 : Terrain.Ocean);
				((BaseTile)Map.Instance[x, y]).Special = false;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player[] ps = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			Player mine = ps[0], theirs = ps[1];
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == g.PlayerNumber(mine)).ToArray()) g.DisbandUnit(u);
			mine.Explore(37, 25, range: 20);
			g.AddCity(mine, 0, 29, 25);
			g.AddCity(theirs, 1, 45, 25);
			Sim.ClearTasks();
			return (g, mine);
		}

		[Theory]
		[InlineData(UnitType.Diplomat)]
		[InlineData(UnitType.Caravan)]
		public void AnIslandEnvoyHeadsForAShip(UnitType kind)
		{
			(Game g, Player mine) = TwoIslands();
			byte id = g.PlayerNumber(mine);
			g.CreateUnit(UnitType.Sail, 30, 25, id, false);
			IUnit dip = g.CreateUnit(kind, 29, 25, id, false)!;
			dip.MovesLeft = dip.Move;
			for (ushort t = 0; t < 8; t++)
			{
				g.GameTurn = t;
				if (!AI.Instance(mine).TestIdleRetryDeferred(dip)) break;
			}
			Sim.ClearTasks();

			AI.Instance(mine).Move(dip);
			for (int i = 0; i < 20 && GameTask.Any(); i++) GameTask.Update();

			Assert.True((dip.X, dip.Y) == (30, 25) || (dip.Goto.X, dip.Goto.Y) == (30, 25),
				$"{kind} at ({dip.X},{dip.Y}) with goto {dip.Goto}");
		}

		private static (Player mine, IUnit ship, IUnit dip) AnEnvoyAboardAt(UnitType kind, int x)
		{
			(Game g, Player mine) = TwoIslands();
			byte id = g.PlayerNumber(mine);
			IUnit ship = g.CreateUnit(UnitType.Sail, x, 25, id, false)!;
			IUnit dip = g.CreateUnit(kind, x, 25, id, false)!;
			dip.Sentry = true;
			Sim.ClearTasks();
			return (mine, ship, dip);
		}

		[Theory]
		[InlineData(UnitType.Diplomat)]
		[InlineData(UnitType.Caravan)]
		public void AShipCarryingAnEnvoySailsForAForeignPort(UnitType kind)
		{
			(Player mine, IUnit ship, _) = AnEnvoyAboardAt(kind, 33);

			AI.Instance(mine).Move(ship);

			Assert.Equal((45, 25), (ship.Goto.X, ship.Goto.Y));
		}

		[Theory]
		[InlineData(UnitType.Diplomat)]
		[InlineData(UnitType.Caravan)]
		public void BesideThePortTheEnvoyIsSetDown(UnitType kind)
		{
			(Player mine, IUnit ship, IUnit dip) = AnEnvoyAboardAt(kind, 44);

			AI.Instance(mine).Move(ship);

			Assert.False(dip.Sentry, "the envoy is still stowed");

			// ...and given its move, it steps ashore beside the port.
			dip.MovesLeft = dip.Move;
			AI.Instance(mine).Move(dip);
			for (int i = 0; i < 20 && GameTask.Any(); i++) GameTask.Update();
			Assert.False(dip.Tile.IsOcean, $"still aboard at ({dip.X},{dip.Y})");
		}
	}
}
