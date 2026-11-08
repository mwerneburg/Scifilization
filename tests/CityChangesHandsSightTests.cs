// CivOne tests
//
// Only units reveal the map, so a city that changes hands with nobody walking in left its new
// owner blind to it: the Internet's schism makes a brand-new civ and hands it half the builder's
// cities, and that civ had seen none of them. CityRadius drops every tile its owner cannot see,
// the centre with them, which is how the Khmer's Ctesiphon deadlocked the citizen governor at
// turn 328 (Aug 2026), and how it went on working seven tiles on six citizens once that was
// stopped. Defections, gifts, Skynet's seizures and the Thing's captures go the same way.

using System.Linq;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class CityChangesHandsSightTests
	{
		private static bool WorksItsCentre(City c) => c.ResourceTiles.Any(t => t.X == c.X && t.Y == c.Y);

		[Fact]
		public void ASchismsNewCivSeesTheCitiesItIsGiven()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player builder = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			builder.Explore(40, 25, range: 20);
			City capital = g.AddCity(builder, 0, 25, 25)!;
			capital.AddBuilding(new CivOne.Buildings.Palace());
			City wonder = g.AddCity(builder, 1, 30, 25)!;
			City[] towns = { g.AddCity(builder, 2, 35, 25)!, g.AddCity(builder, 3, 40, 25)! };
			foreach (City c in towns) c.Size = 4;

			Player? splinter = g.ExecuteSocialMediaSchism(builder, wonder);
			Sim.ClearTasks();

			Assert.NotNull(splinter);
			City[] seceded = splinter!.Cities;
			Assert.NotEmpty(seceded);
			foreach (City c in seceded)
			{
				Assert.True(splinter.Visible(c.X, c.Y), $"{c.Name} is invisible to its own civ");
				Assert.True(WorksItsCentre(c), $"{c.Name} lost its centre from its worked tiles");
			}
		}

		[Fact]
		public void ACityHandedToAStrangerIsSeenByIt()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Game g = Game.Instance;
			Player[] civs = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			Player from = civs[0], to = civs[1];
			from.Explore(40, 25, range: 3);
			City c = g.AddCity(from, 0, 40, 25)!;
			c.Size = 4;
			Assert.False(to.Visible(40, 25), "fixture: the new owner has already seen the city");

			c.Owner = g.PlayerNumber(to);

			Assert.True(to.Visible(40, 25));
			Assert.True(WorksItsCentre(c));
			// Its whole working radius, not just the square it stands on.
			Assert.Equal(21, c.CityTiles.Count());
		}

		// Founding is not a change of hands. The constructor sets Owner before X and Y, so a
		// reveal there lit up the map's corner at (0,0) for every civ that founded a city.
		[Fact]
		public void FoundingACityRevealsNothing()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			int Seen() => Enumerable.Range(0, 80).Sum(x => Enumerable.Range(0, 50).Count(y => p.Visible(x, y)));
			int before = Seen();

			g.AddCity(p, 0, 40, 25);

			Assert.Equal(before, Seen());
		}
	}
}
