// CivOne tests
//
// A city of the player's that has seen the King in Yellow shows its size numeral in yellow
// (the user, Nov 2026). Nothing else on the map says which city saw the play, and only a
// Cathedral cures it. Rioting red still wins; rivals' cities stay amber, since news of them
// does not always reach the player.

using System.Linq;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Tests
{
	public class KingInYellowNumeralTests
	{
		private static City ACity(Player p, int x)
		{
			p.Explore(x, 25, range: 4);
			City c = Game.Instance.AddCity(p, 0, x, 25)!;
			c.Size = 4;
			return c;
		}

		private static (City ours, City theirs) TwoCities()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 20; y <= 30; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player rival = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			City ours = ACity(g.HumanPlayer, 30), theirs = ACity(rival, 50);
			Sim.ClearTasks();
			return (ours, theirs);
		}

		private static void SeeThePlay(City c) => Game.Instance.YellowCities.Add((c.X, c.Y));

		[Fact]
		public void OurCityThatSawThePlayIsYellow()
		{
			(City ours, City _) = TwoCities();
			Assert.Equal(CassetteTheme.PHOS, Icons.CityNumeralColour(ours));

			SeeThePlay(ours);

			Assert.Equal(CassetteTheme.SIGN, Icons.CityNumeralColour(ours));
		}

		[Fact]
		public void ARivalsCityKeepsItsSecret()
		{
			(City _, City theirs) = TwoCities();
			SeeThePlay(theirs);

			Assert.Equal(CassetteTheme.PHOS, Icons.CityNumeralColour(theirs));
		}

		[Fact]
		public void ARiotStillShowsRed()
		{
			(City ours, City _) = TwoCities();
			SeeThePlay(ours);
			ours.Size = 20;
			ours.InvalidateCache();
			Assert.True(ours.IsInDisorder, "fixture: should be rioting");

			Assert.Equal(CassetteTheme.ALERT, Icons.CityNumeralColour(ours));
		}

		// The numeral is baked into the tile, so the change must ask for a repaint.
		[Fact]
		public void SeeingThePlayAsksForARepaint()
		{
			(City ours, City _) = TwoCities();
			ours.AppearanceChanged();

			SeeThePlay(ours);

			Assert.True(ours.AppearanceChanged());
		}

		// The slot is the base palette's own yellow, not a cassette token: an edit to
		// palette.txt that moves it would turn the numeral some other colour, silently.
		[Fact]
		public void TheSignSlotIsYellow()
		{
			var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "CivOne.csproj")))
				dir = dir.Parent;
			string line = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir!.FullName, Common.RepoArtDir, "palette.txt"))
				.Single(l => l.StartsWith($"{CassetteTheme.SIGN} "));
			int[] rgb = line.Split('#')[0].Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(int.Parse).ToArray();

			Assert.True(rgb[0] > 180 && rgb[1] > 180 && rgb[2] < 60, $"slot {CassetteTheme.SIGN} is {line}");
		}
	}
}
