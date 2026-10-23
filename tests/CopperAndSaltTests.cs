// CivOne tests
//
// Copper and salt (Oct 2026). Both ride the existing specials on a checkerboard over the
// lattice's 4x4 blocks (BaseTile.SecondSeam): half the hill seams are copper instead of coal,
// and the desert blocks that used to lose their special are salt pans. Copper soft-gates the
// bronze age and the wire age; salt soft-gates the Granary and, held anywhere, calms one
// citizen in every city.

using System.IO;
using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Graphics.Sprites;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class CopperAndSaltTests
	{
		[Fact]
		public void HalfTheHillSeamsAreCopper()
		{
			int coal = 0, copper = 0;
			for (int by = 0; by < 4; by++)
			for (int bx = 0; bx < 4; bx++)
			{
				StrategicResource r = Game.ResourceAt(new Hills(bx * 4, by * 4, special: true));
				if (r == StrategicResource.Coal) coal++;
				if (r == StrategicResource.Copper) copper++;
			}
			Assert.Equal((8, 8), (coal, copper));
		}

		// The salt pan keeps the desert's single shield and adds trade; the oil beside it is
		// still a four-shield field.
		[Fact]
		public void ASaltPanPaysInTradeNotShields()
		{
			var salt = new Desert(4, 0, special: true);   // SecondSeam
			var oil = new Desert(0, 0, special: true);
			Assert.Equal(StrategicResource.Salt, Game.ResourceAt(salt));
			Assert.Equal((1, 2), ((int)salt.Shield, (int)salt.Trade));
			Assert.Equal((4, 0), ((int)oil.Shield, (int)oil.Trade));
		}

		// One AI town (so no difficulty handicap on its content floor), far from a strip of
		// each deposit-bearing terrain.
		private static (Game g, Player p, City c) ATown()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			p.Government = new Governments.Monarchy();
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
			{
				Terrain t = y >= 30 ? Terrain.Hills : y >= 25 ? Terrain.Desert : Terrain.Grassland1;
				Map.Instance.ChangeTileType(x, y, t);
				if (t == Terrain.Grassland1) ((BaseTile)Map.Instance[x, y]).Special = false;
			}
			Map.Instance.RecalculateContinentsIfDirty();
			p.Explore(20, 15, range: 6);
			City c = g.AddCity(p, 0, 20, 15)!;
			Sim.ClearTasks();
			return (g, p, c);
		}

		// Camp the first deposit of that material on the strips.
		private static void Camp(Game g, Player p, StrategicResource r)
		{
			ITile tile = Map.Instance.AllTiles().First(t => t.Y >= 25 && t.Y <= 40 && Game.ResourceAt(t) == r);
			g.ResourceCamps[(tile.X, tile.Y)] = g.PlayerNumber(p);
		}

		[Fact]
		public void APhalanxCostsHalfAgainWithoutCopper()
		{
			(Game g, Player p, City c) = ATown();
			int without = c.ProductionCost(new Phalanx());
			Camp(g, p, StrategicResource.Copper);
			Assert.Equal((30, 20), (without, c.ProductionCost(new Phalanx())));
		}

		[Fact]
		public void AGranaryCostsHalfAgainWithoutSalt()
		{
			(Game g, Player p, City c) = ATown();
			int without = c.ProductionCost(new Granary());
			Camp(g, p, StrategicResource.Salt);
			Assert.Equal(without * 2 / 3, c.ProductionCost(new Granary()));
		}

		[Fact]
		public void SaltCalmsACitizen()
		{
			(Game g, Player p, City c) = ATown();
			c.Size = 9;
			c.InvalidateCache();
			int without = c.UnhappyCitizens;
			Camp(g, p, StrategicResource.Salt);
			c.InvalidateCache();
			Assert.Equal(without - 1, c.UnhappyCitizens);
		}

		private static ITile? CampSite(Player p, IUnit settler)
			=> (ITile?)typeof(AI).GetMethod("BestCampSite",
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.Invoke(AI.Instance(p), new object[] { settler });

		// A civ holding the three industrial materials still walks to the new ones.
		[Theory]
		[InlineData(StrategicResource.Copper)]
		[InlineData(StrategicResource.Salt)]
		public void TheAISeeksTheNewMaterials(StrategicResource want)
		{
			(Game g, Player p, City c) = ATown();
			Map.Instance.ChangeTileType(12, 12, Terrain.Mountains);
			((BaseTile)Map.Instance[12, 12]).Special = true;
			g.ResourceCamps[(12, 12)] = g.PlayerNumber(p);   // iron
			foreach (StrategicResource r in new[] { StrategicResource.Coal, StrategicResource.Oil,
			                                        StrategicResource.Copper, StrategicResource.Salt })
				if (r != want) Camp(g, p, r);
			Assert.True(g.HasResource(p, StrategicResource.Iron), "fixture: iron held");
			Map.Instance.RecalculateContinentsIfDirty();

			IUnit settler = g.CreateUnit(UnitType.Settlers, 20, 22, g.PlayerNumber(p))!;
			ITile? site = CampSite(p, settler);

			Assert.NotNull(site);
			Assert.Equal(want, Game.ResourceAt(site));
		}

		[Fact]
		public void TheMapShowsCopperAndSaltApart()
		{
			Assert.Same(MapTile.Copper, MapTile.TileSpecial(new Hills(4, 0, special: true)));
			Assert.Same(MapTile.Coal, MapTile.TileSpecial(new Hills(0, 0, special: true)));
			Assert.Same(MapTile.Salt, MapTile.TileSpecial(new Desert(4, 0, special: true)));
			Assert.Same(MapTile.DesertOil, MapTile.TileSpecial(new Desert(0, 0, special: true)));
		}

		// The art, not just the code path: a missing section falls back to the coal and oil
		// sprites silently, which is the invisibility this resource layer suffered for a year.
		[Theory]
		[InlineData("special_copper")]
		[InlineData("special_salt")]
		public void TheArtIsShipped(string section)
		{
			string tiles = File.ReadAllText(Path.Combine(Sim.RepoRoot(), "free_tiles.txt"));
			Assert.Contains($"[{section}]", tiles);
		}
	}
}
