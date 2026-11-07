// CivOne tests
//
// An AI explorer with nothing left to scout sentries, and AI units never wake from Sentry, so
// every explorer alive at the end of game f37aa663 (27 of them) was asleep, drawing upkeep.
// Once the build rule would not raise another, they are stood down.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class SleepingExplorerTests
	{
		// An islet of the given radius at (40,25), its city in the middle, charted out to `seen`.
		private static (Player p, IUnit scout) AnIslet(int radius, int seen, int difficulty = 0)
		{
			Sim.NewGame(width: 80, height: 50, difficulty: difficulty);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			for (int y = 10; y <= 40; y++)
			for (int x = 25; x <= 55; x++)
			{
				bool land = System.Math.Abs(x - 40) <= radius && System.Math.Abs(y - 25) <= radius;
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Grassland1 : Terrain.Ocean);
			}
			Map.Instance.RecalculateContinentsIfDirty();
			p.Explore(40, 25, range: seen);
			g.AddCity(p, 0, 40, 25);
			IUnit scout = g.CreateUnit(UnitType.Explorer, 41, 25, g.PlayerNumber(p), false)!;
			scout.Sentry = true;
			Sim.ClearTasks();
			return (p, scout);
		}

		[Fact]
		public void AChartedCivStandsItsSleepingExplorerDown()
		{
			(Player p, IUnit scout) = AnIslet(radius: 3, seen: 5);

			AI.Instance(p).ConsiderExplorers();

			Assert.DoesNotContain(scout, Game.Instance.GetUnits());
		}

		// The fallback's random pick ignored the gate, so a charted civ kept minting explorers that
		// slept at once and were stood down: on Epic Earth seed 4242 the Romans built 16 and
		// disbanded all 16, and 51 of the world's 72 explorer losses were this loop.
		[Fact]
		public void TheFallbackBuildsNoExplorerForAChartedCiv()
		{
			(Player p, IUnit scout) = AnIslet(radius: 3, seen: 5, difficulty: 1);   // Chieftain asks for Militia
			Game.Instance.DisbandUnit(scout);
			City city = p.Cities[0];
			// Garrisoned, so no rule asks for a defender: little left but the fallback.
			for (int i = 0; i < 2; i++) Game.Instance.CreateUnit(UnitType.Musketeers, 40, 25, Game.Instance.PlayerNumber(p));
			Assert.Contains(city.AvailableProduction, x => x is Explorer);

			// The fallback picks at random, so once proves nothing.
			for (int i = 0; i < 100; i++)
				Assert.DoesNotContain(AI.Instance(p).ProductionPlan(city), x => x is Explorer);
		}

		// Control: with its home still in fog the build rule wants explorers, so it keeps one.
		[Fact]
		public void FogAtHomeKeepsIt()
		{
			(Player p, IUnit scout) = AnIslet(radius: 12, seen: 2);

			AI.Instance(p).ConsiderExplorers();

			Assert.Contains(scout, Game.Instance.GetUnits());
		}
	}
}
