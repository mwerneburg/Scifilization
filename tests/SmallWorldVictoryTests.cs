// CivOne tests
//
// Pax Mercatoria and Cultural Ascendancy needed three rivals standing. A world that began
// with three civs has two from its first turn, so neither win could ever start: the user's
// Epic Earth game led the world in culture per head, every other clause met, with the
// streak at zero (Nov 2026). A world that began with five civs or fewer now needs two;
// a larger one still needs three.

using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class SmallWorldVictoryTests
	{
		// The player holds every clause of both wins but the rival count: a size-6 coastal city,
		// Philosophy and Banking, culture, an embassy and a defence pact with every rival, and
		// the world past the Electronics gate. Rivals beyond `alive` lose their units, so they
		// count as destroyed.
		private static (Game g, Player human) AWorldOf(int competition, int alive)
		{
			Sim.NewGame(width: 80, height: 50, competition: competition);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 30; x <= 50; x++)
				Map.Instance.ChangeTileType(x, y, x < 40 ? Terrain.Grassland1 : Terrain.Ocean);
			Map.Instance.RecalculateContinentsIfDirty();

			Player human = g.HumanPlayer;
			Player[] rivals = g.Players.Where(p => p is not null && p != human && g.PlayerNumber(p) != 0).ToArray();
			foreach (Player gone in rivals.Skip(alive))
				foreach (IUnit u in g.GetUnits().Where(u => u.Owner == g.PlayerNumber(gone)).ToArray())
					g.DisbandUnit(u);
			rivals = rivals.Take(alive).ToArray();
			Assert.Equal(alive, g.Players.Count(p => p is not null && p != human && g.PlayerNumber(p) != 0 && !p.IsDestroyed()));

			human.Government = new CivOne.Governments.Monarchy();
			human.AddAdvance(new Philosophy(), false);
			human.AddAdvance(new Banking(), false);
			human.Explore(39, 25, range: 4);
			City c = g.AddCity(human, 0, 39, 25)!;
			c.Size = 6;
			c.ResetResourceTiles();
			Assert.True(c.EconomicOutput > 0, "scenario is broken: the city earns nothing");
			human.SetCulture(900);
			foreach (Player r in rivals)
			{
				human.EstablishEmbassy(r);
				r.SetDefensePact(human, 50);
			}
			Sim.ElectronicsForTheWorld(human);
			Sim.ClearTasks();
			return (g, human);
		}

		private static void Check(Game g, string method) =>
			typeof(Game).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(g, null);

		[Theory]
		[InlineData("CheckCulturalAscendancy")]
		[InlineData("CheckPaxMercatoria")]
		public void AThreeCivWorldCanStartTheClock(string method)
		{
			(Game g, Player human) = AWorldOf(competition: 3, alive: 2);

			Check(g, method);

			var progress = g.Progress(g.PlayerNumber(human));
			Assert.Equal(1u, method == "CheckPaxMercatoria" ? progress.EconStreak : progress.CultureStreak);
		}

		// The control: a world that began larger still wants three standing.
		[Theory]
		[InlineData("CheckCulturalAscendancy")]
		[InlineData("CheckPaxMercatoria")]
		public void ALargerWorldDownToTwoCannot(string method)
		{
			(Game g, Player human) = AWorldOf(competition: 7, alive: 2);

			Check(g, method);

			var progress = g.Progress(g.PlayerNumber(human));
			Assert.Equal(0u, method == "CheckPaxMercatoria" ? progress.EconStreak : progress.CultureStreak);
		}

		// ...and holds with three, so the control fails for the rule, not the fixture.
		[Theory]
		[InlineData("CheckCulturalAscendancy")]
		[InlineData("CheckPaxMercatoria")]
		public void ALargerWorldWithThreeCan(string method)
		{
			(Game g, Player human) = AWorldOf(competition: 7, alive: 3);

			Check(g, method);

			var progress = g.Progress(g.PlayerNumber(human));
			Assert.Equal(1u, method == "CheckPaxMercatoria" ? progress.EconStreak : progress.CultureStreak);
		}
	}
}
