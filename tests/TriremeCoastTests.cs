// CivOne tests
//
// A Trireme out of sight of land at the end of a turn sinks one time in two. The AI's
// explore and ferry targets sent it there anyway: game 396337f3 built 62 Triremes and ended
// with 12, and the Japanese sailed theirs into the open Pacific. An AI Trireme's routes now
// keep to water beside land, and it explores only such water.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class TriremeCoastTests : System.IDisposable
	{
		private readonly bool _auto;
		public TriremeCoastTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// Two coasts facing each other across fourteen tiles of open sea, the ship on the
		// west one's shore.
		private static (Game g, IUnit ship) ATriremeFacingOpenSea(bool human)
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			Settings.Instance.Autopilot = false;
			for (int y = 0; y < 50; y++)
			for (int x = 0; x < 80; x++)
			{
				Map.Instance.ChangeTileType(x, y, x is >= 15 and <= 25 || x is >= 41 and <= 50 ? Terrain.Grassland1 : Terrain.Ocean);
				((BaseTile)Map.Instance[x, y]).Special = false;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player p = human ? g.HumanPlayer : g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == g.PlayerNumber(p)).ToArray()) g.DisbandUnit(u);
			IUnit ship = g.CreateUnit(UnitType.Trireme, 26, 25, g.PlayerNumber(p), false)!;
			Sim.ClearTasks();
			return (g, ship);
		}

		[Fact]
		public void AnAiTriremeHasNoRouteAcrossOpenSea()
		{
			(_, IUnit ship) = ATriremeFacingOpenSea(human: false);

			Assert.Null(Common.GotoStep(ship, 40, 25));
		}

		// Control: the player's own orders keep the old risk.
		[Fact]
		public void ThePlayersTriremeMayStillTryIt()
		{
			(_, IUnit ship) = ATriremeFacingOpenSea(human: true);

			Assert.NotNull(Common.GotoStep(ship, 40, 25));
		}

		// The far coast is out of the twelve-tile window and home is charted, so the fog in
		// reach is out at sea. The richest of it is out of sight of land, where the route
		// rule alone would find no way there and leave the ship in port; it should chart
		// what it can see from the shore instead.
		[Fact]
		public void AnAiTriremeExploresBesideLand()
		{
			(Game g, IUnit ship) = ATriremeFacingOpenSea(human: false);
			Player p = g.GetPlayer(ship.Owner);
			for (int y = 0; y < 50; y += 4) p.Explore(20, y, range: 6);   // charted to the shore, x 14-26

			ITile? dest = AI.Instance(p).BestSeaExploreTile(ship);

			Assert.NotNull(dest);
			Assert.True(dest!.GetBorderTiles().Any(b => b is not null && !b.IsOcean), $"sent to open sea at ({dest.X},{dest.Y})");
		}
	}
}
