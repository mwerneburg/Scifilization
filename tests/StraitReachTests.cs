// CivOne tests
//
// Continent equality is not walking distance. Africa and Eurasia are one continent, joined
// at Sinai, so in game 453977e5 Malian settlers near San picked work in Spain, three tiles
// across the Strait of Gibraltar, and the pathfinder marched them east through the Sahara for
// fifteen turns before they gave up and picked again. Site finders now take only what the
// settler can walk to without leaving the neighbourhood it is searching.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class StraitReachTests
	{
		// A south shore of mountains (nowhere to found, the settler included) and a fine north
		// shore of river land across a one-tile strait, joined only by an isthmus far to the
		// east: one continent.
		private static (Player p, IUnit settler) TwoShoresJoinedFarAway()
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			for (int y = 10; y < 40; y++)
			for (int x = 10; x < 80; x++)
			{
				Terrain t = y is >= 20 and <= 23 && x is >= 15 and <= 75 ? Terrain.River        // north shore
				          : y is >= 25 and <= 30 && x is >= 15 and <= 75 ? Terrain.Mountains    // south shore
				          : x is >= 72 and <= 75 && y is >= 20 and <= 30 ? Terrain.Grassland1   // the isthmus
				          : Terrain.Ocean;
				Map.Instance.ChangeTileType(x, y, t);
				((BaseTile)Map.Instance[x, y]).Special = false;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			foreach (IUnit u in g.GetUnits().Where(u => u.Owner == g.PlayerNumber(p)).ToArray()) g.DisbandUnit(u);
			IUnit s = g.CreateUnit(UnitType.Settlers, 25, 27, g.PlayerNumber(p), false)!;
			Sim.ClearTasks();
			return (p, s);
		}

		[Fact]
		public void TheFarShoreIsOneContinentButNotWithinReach()
		{
			(_, IUnit s) = TwoShoresJoinedFarAway();
			Assert.Equal(Map.Instance[25, 27].ContinentId, Map.Instance[25, 22].ContinentId);   // fixture: one landmass

			var near = AI.LandWithin(s, 12);

			Assert.Contains(26 * Map.WIDTH + 25, near);                // its own shore
			Assert.DoesNotContain(22 * Map.WIDTH + 25, near);          // across the strait
		}

		// The settler has nowhere to found on its own shore, and must not be sent the long way
		// round to the better one.
		[Fact]
		public void ASettlerIsNotSentTheLongWayRoundToTheFarShore()
		{
			(Player p, IUnit s) = TwoShoresJoinedFarAway();

			ITile? site = AI.Instance(p).BestSettleSite(s);

			Assert.True(site is null || site.Y >= 25, $"sent across the strait to ({site?.X},{site?.Y})");
		}

		// What game 453977e5 actually did: workers sent across the strait to improve land
		// beside a city they could not walk to.
		[Fact]
		public void AWorkerIsNotSentTheLongWayRoundToImproveTheFarShore()
		{
			(Player p, IUnit s) = TwoShoresJoinedFarAway();
			p.Explore(25, 24, range: 12);
			City c = Game.Instance.AddCity(p, 0, 25, 21)!;
			c.Size = 3;
			p.Government = new Governments.Monarchy();
			Sim.ClearTasks();

			ITile? site = AI.Instance(p).BestImproveSite(s);

			Assert.True(site is null || site.Y >= 25, $"sent across the strait to ({site?.X},{site?.Y})");
		}
	}
}
