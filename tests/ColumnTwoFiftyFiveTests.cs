// CivOne tests
//
// (255,255) marks a destroyed city or unit, and that is safe only because Y = 255 is off the map.
// Six places tested X instead, which is a real column on the 320-wide Epic map. City.Size ignored
// every write there, so a city founded in that column stayed at size 0, off its owner's roster,
// raising Militia for ever. Game 19a9f770: the human slot's Chinese founded Beijing at (255,71) and
// had no other city by turn 307. Every size-0 city in six saves stood in that column.

using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class ColumnTwoFiftyFiveTests : System.IDisposable
	{
		private readonly bool _auto;
		public ColumnTwoFiftyFiveTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		private const int Column = 255, Row = 100;

		// An Epic-width map, with grassland around the column.
		private static (Game g, Player us, Player them) AWideMap()
		{
			Sim.NewGame(width: 320, height: 200);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = Row - 15; y <= Row + 15; y++)
			for (int x = Column - 20; x <= Column + 20; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player[] ps = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			foreach (Player p in ps.Take(2)) p.Explore(Column, Row, range: 25);
			Sim.ClearTasks();
			return (g, ps[0], ps[1]);
		}

		// ── the city ─────────────────────────────────────────────────────────

		[Fact]
		public void ACityInTheColumnHasASizeAndIsOnItsOwnersRoster()
		{
			(Game g, Player us, Player _) = AWideMap();

			City city = g.AddCity(us, 0, Column, Row)!;
			city.Size = 3;

			Assert.Equal(3, city.Size);
			Assert.Contains(city, us.Cities);
		}

		// The guard that went wrong has a job: a destroyed city must not be written to.
		[Fact]
		public void ADestroyedCityIsStillIgnored()
		{
			(Game g, Player us, Player _) = AWideMap();
			City city = g.AddCity(us, 0, Column - 5, Row)!;
			g.DestroyCity(city);

			city.Size = 5;

			Assert.Equal(255, city.Y);
			Assert.NotEqual(5, city.Size);
		}

		// ── the routes ───────────────────────────────────────────────────────

		// River, because a route pays by trade and grassland makes none: with a floor of 1 at each
		// end these routes round down to 0, and every assertion below would pass on a zero.
		private static (City inColumn, City other) TwoCities(Game g, Player a, Player b)
		{
			for (int y = Row - 5; y <= Row + 5; y++)
			for (int x = Column - 18; x <= Column + 5; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.River);
			Map.Instance.RecalculateContinentsIfDirty();
			City inColumn = g.AddCity(a, 0, Column, Row)!;
			City other = g.AddCity(b, 1, Column - 12, Row)!;
			inColumn.Size = other.Size = 16;
			inColumn.InvalidateCache();
			other.InvalidateCache();
			return (inColumn, other);
		}

		[Fact]
		public void ARouteToACityInTheColumnPays()
		{
			(Game g, Player us, Player _) = AWideMap();
			(City inColumn, City other) = TwoCities(g, us, us);

			other.AddTradeRoute(inColumn, "Hides");

			Assert.True(other.TradeRoutes.Single().Value > 0, "the route is treated as one to a dead city");
		}

		// Same continent, so both ends are halved alike. Reading the home end's column as dead
		// skipped the halving, and made the route worth more from one end than the other.
		[Fact]
		public void ARouteIsWorthTheSameFromEitherEnd()
		{
			(Game g, Player us, Player _) = AWideMap();
			(City inColumn, City other) = TwoCities(g, us, us);

			other.AddTradeRoute(inColumn, "Hides");
			inColumn.AddTradeRoute(other, "Hides");

			Assert.True(other.TradeRoutes.Single().Value > 0, "the fixture route pays nothing");
			Assert.Equal(other.TradeRoutes.Single().Value, inColumn.TradeRoutes.Single().Value);
		}

		// Worth nothing in wartime and kept, for the peace (WarTradeSuspensionTests).
		[Fact]
		public void AWartimeRouteToTheColumnIsSuspendedNotPruned()
		{
			(Game g, Player us, Player them) = AWideMap();
			(City inColumn, City other) = TwoCities(g, them, us);
			other.AddTradeRoute(inColumn, "Hides");
			us.DeclareWar(them);
			Sim.ClearTasks();

			other.PruneWorthlessRoutes();

			Assert.Equal(1, other.TradeRouteCount);
		}

		// ── the units ────────────────────────────────────────────────────────

		[Fact]
		public void ARaiderInTheColumnIsQuarry()
		{
			(Game g, Player us, Player _) = AWideMap();
			g.AddCity(us, 0, Column - 2, Row);
			IUnit knights = g.CreateUnit(UnitType.Knights, Column, Row, 0)!;
			IUnit ours = g.CreateUnit(UnitType.Armor, Column - 1, Row, g.PlayerNumber(us), false)!;

			Assert.Same(knights, AI.Instance(us).HuntQuarry(ours));
		}

		[Fact]
		public void ABeastInTheColumnGetsAHunterBuilt()
		{
			(Game g, Player us, Player _) = AWideMap();
			us.AddAdvance(new Automobile(), false);
			us.Government = new Governments.Democracy();
			City city = g.AddCity(us, 0, Column - 5, Row)!;
			g.CreateUnit(UnitType.Riflemen, city.X, city.Y, g.PlayerNumber(us), false);
			g.CreateUnit(UnitType.Harvester, Column, Row, 0);
			Sim.ClearTasks();

			MethodInfo plan = typeof(AI).GetMethod("PlanProductionInto", BindingFlags.NonPublic | BindingFlags.Instance)!;
			object develop = System.Enum.Parse(typeof(AI).GetNestedType("StrategyStance", BindingFlags.NonPublic)!, "Develop");
			var built = (System.Collections.Generic.List<IProduction>)plan.Invoke(AI.Instance(us), new object[] { new System.Collections.Generic.List<IProduction>(), city, develop })!;

			Assert.Contains(built, x => x is Armor);
		}

		[Fact]
		public void AShipInTheColumnIsPrey()
		{
			(Game g, Player us, Player them) = AWideMap();
			for (int x = Column - 6; x <= Column + 6; x++) Map.Instance.ChangeTileType(x, Row, Terrain.Ocean);
			Map.Instance.RecalculateContinentsIfDirty();
			g.CreateUnit(UnitType.Transport, Column, Row, g.PlayerNumber(them), false);
			IUnit ship = g.CreateUnit(UnitType.Battleship, Column - 4, Row, g.PlayerNumber(us), false)!;
			us.DeclareWar(them);
			Sim.ClearTasks();

			Assert.NotNull(AI.Instance(us).SeaPrey(ship));
		}

		// ── old saves ────────────────────────────────────────────────────────

		// Every Epic save has these. A size-0 city loaded through the fixed setter would be
		// destroyed in mid-load, so the load brings it back as the city it should have been.
		[Fact]
		public void AGhostCityInASaveComesBackAlive()
		{
			(Game g, Player us, Player _) = AWideMap();
			City ghost = g.AddCity(us, 0, Column, Row)!;
			byte id = g.PlayerNumber(us);
			typeof(City).GetField("_size", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(ghost, (byte)0);
			Assert.Equal(0, ghost.Size);

			string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"civone-ghost-{System.Guid.NewGuid():N}.cos");
			try
			{
				g.SaveCos(path);
				Sim.ResetState();
				Assert.True(Game.LoadCos(path), "the fixture could not read its own save");

				City back = Game.Instance.GetCities().Single(c => c.X == Column && c.Y == Row);
				Assert.Equal(1, back.Size);
				Assert.Contains(back, Game.Instance.GetPlayer(id).Cities);
			}
			finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
		}
	}
}
