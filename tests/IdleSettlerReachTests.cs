// CivOne tests
//
// Late in game c8d7d2e8 every civ still had hundreds of tiles of routable work, yet settlers
// stood still for the rest of the game. On the t570 save, seven Mongol settlers sat in Erdenet:
// every tile within six of them was claimed by another worker, and the improvement scan looked
// no further. Three Lakota settlers re-picked a tile with two Aztec cannons on it every turn,
// unable to enter it.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class IdleSettlerReachTests
	{
		// Plains with a river at x=39 for an irrigation source: plains, because an AI starts as a
		// despot and DespotBlocksIrrigation skips grassland (see SettlerClaimAgreementTests).
		private static (Game g, Player ai) APlainWithARiver()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 15; y <= 35; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			for (int y = 20; y <= 30; y++)
				Map.Instance.ChangeTileType(39, y, Terrain.River);
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player ai = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			ai.Explore(40, 25, range: 25);
			return (g, ai);
		}

		// Dry plains around the settler's city; the only farm work is beside the river, at the
		// next city twelve tiles east.
		[Fact]
		public void ASettlerWithNothingNearbyLooksFurther()
		{
			var (g, ai) = APlainWithARiver();
			g.AddCity(ai, 0, 26, 25);
			g.AddCity(ai, 1, 37, 25);
			Sim.ClearTasks();
			var idle = (Settlers)g.CreateUnit(UnitType.Settlers, 26, 25, g.PlayerNumber(ai))!;

			ITile? site = AI.Instance(ai).BestImproveSite(idle);

			Assert.NotNull(site);
			Assert.True(Common.DistanceToTile(26, 25, site!.X, site.Y) > 6, $"precondition: work within 6 at ({site.X},{site.Y})");
		}

		[Fact]
		public void ATileHeldByAForeignUnitIsNotOffered()
		{
			var (g, ai) = APlainWithARiver();
			g.AddCity(ai, 0, 40, 25);
			Sim.ClearTasks();
			var idle = (Settlers)g.CreateUnit(UnitType.Settlers, 42, 25, g.PlayerNumber(ai))!;
			ITile? first = AI.Instance(ai).BestImproveSite(idle);
			Assert.NotNull(first);

			g.CreateUnit(UnitType.Militia, first!.X, first.Y, g.PlayerNumber(g.HumanPlayer));
			ITile? second = AI.Instance(ai).BestImproveSite(idle);

			Assert.NotNull(second);
			Assert.False(second!.X == first.X && second.Y == first.Y, $"offered ({first.X},{first.Y}) with a foreign unit on it");
		}
	}
}
