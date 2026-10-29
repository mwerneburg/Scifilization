// CivOne tests
//
// Ships refit in a Shipyard as land units do in Barracks: Trireme → Sail → Frigate →
// Transport, Ironclad → Cruiser → Battleship. Nothing at sea had an upgrade path, so a civ
// with Industrialization still sailed the Triremes it built in antiquity (game a82cf446).

using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.UserInterface;
using CivOne.Units;

namespace CivOne.Tests
{
	public class ShipUpgradeTests : System.IDisposable
	{
		private readonly bool _auto;
		public ShipUpgradeTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// The human's port at (40,25), sea to the east, with gold and the given building.
		private static (Game g, Player human, City port) APort(IBuilding building)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
			Map.Instance.ChangeTileType(41, 25, Terrain.Ocean);
			Game g = Game.Instance;
			Player human = g.HumanPlayer;
			human.Explore(40, 25, range: 5);
			City port = g.AddCity(human, 0, 40, 25)!;
			port.AddBuilding(building);
			human.Gold = 5000;
			human.AddAdvance(new Navigation(), false);
			Sim.ClearTasks();
			return (g, human, port);
		}

		private static string? UpgradeOrder(IUnit u) => u.MenuItems
			.FirstOrDefault(m => m is not null && m.Text.StartsWith("Upgrade to"))?.Text;

		[Fact]
		public void ATriremeInAShipyardPortRefitsAsASail()
		{
			(Game g, Player human, _) = APort(new Shipyard());
			IUnit ship = g.CreateUnit(UnitType.Trireme, 40, 25, g.PlayerNumber(human))!;

			Assert.StartsWith("Upgrade to Sail", UpgradeOrder(ship));
		}

		// Barracks are the army's; a ship needs the yard.
		[Fact]
		public void BarracksDoNotRefitAShip()
		{
			(Game g, Player human, _) = APort(new Barracks());
			IUnit ship = g.CreateUnit(UnitType.Trireme, 40, 25, g.PlayerNumber(human))!;

			Assert.Null(UpgradeOrder(ship));
		}

		// ...and the yard does not drill infantry.
		[Fact]
		public void AShipyardDoesNotRefitInfantry()
		{
			(Game g, Player human, _) = APort(new Shipyard());
			human.AddAdvance(new Conscription(), false);
			IUnit foot = g.CreateUnit(UnitType.Musketeers, 40, 25, g.PlayerNumber(human))!;

			Assert.Null(UpgradeOrder(foot));
		}

		// Cargo aboard: 'u' must still mean Unload, so no refit is offered.
		[Fact]
		public void ALoadedShipIsNotOfferedARefit()
		{
			(Game g, Player human, _) = APort(new Shipyard());
			IUnit ship = g.CreateUnit(UnitType.Trireme, 40, 25, g.PlayerNumber(human))!;
			g.CreateUnit(UnitType.Settlers, 40, 25, g.PlayerNumber(human))!.Sentry = true;

			Assert.Null(UpgradeOrder(ship));
		}

		// An obsolete step is skipped, as on land: with Magnetism the Sail is gone.
		[Fact]
		public void ARefitSkipsAnObsoleteClass()
		{
			(Game g, Player human, _) = APort(new Shipyard());
			human.AddAdvance(new Magnetism(), false);
			IUnit ship = g.CreateUnit(UnitType.Trireme, 40, 25, g.PlayerNumber(human))!;

			Assert.StartsWith("Upgrade to Frigate", UpgradeOrder(ship));
		}

		// ── the AI ───────────────────────────────────────────────────────────

		// An AI islet at (40,25), charted, sea all round to x 30-50; Navigation known.
		private static (Game g, Player p, City port) AnAiPort(bool navigation = true)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			for (int y = 15; y <= 35; y++)
			for (int x = 30; x <= 50; x++)
			{
				bool land = System.Math.Abs(x - 40) <= 2 && System.Math.Abs(y - 25) <= 2;
				Map.Instance.ChangeTileType(x, y, land ? Terrain.Grassland1 : Terrain.Ocean);
			}
			Map.Instance.RecalculateContinentsIfDirty();
			p.Explore(40, 25, range: 4);
			p.Government = new CivOne.Governments.Monarchy();
			p.AddAdvance(new MapMaking(), false);
			if (navigation) p.AddAdvance(new Navigation(), false);
			City port = g.AddCity(p, 0, 42, 25)!;
			port.Size = 6;
			p.Gold = 5000;
			Sim.ClearTasks();
			return (g, p, port);
		}

		private static List<IProduction> PlanFor(Player p, City c)
		{
			var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
			var stanceType = typeof(AI).GetNestedType("StrategyStance", System.Reflection.BindingFlags.NonPublic);
			return (List<IProduction>)typeof(AI).GetMethod("PlanProductionInto", flags)!.Invoke(AI.Instance(p),
				new object[] { new List<IProduction>(), c, System.Enum.Parse(stanceType!, "Develop") })!;
		}

		// No AI civ had a Shipyard by asking; one goes up where an outdated ship lies in port.
		[Fact]
		public void AnAiPortWithAnOutdatedShipBuildsAShipyard()
		{
			(Game g, Player p, City port) = AnAiPort();
			g.CreateUnit(UnitType.Trireme, 42, 25, g.PlayerNumber(p), false);

			Assert.Contains(PlanFor(p, port), x => x is Shipyard);
		}

		// Control: a civ that already has one does not build a second for it.
		[Fact]
		public void OneShipyardIsEnough()
		{
			(Game g, Player p, City port) = AnAiPort();
			g.AddCity(p, 1, 38, 25)!.AddBuilding(new Shipyard());
			g.CreateUnit(UnitType.Trireme, 42, 25, g.PlayerNumber(p), false);

			Assert.DoesNotContain(PlanFor(p, port), x => x is Shipyard);
		}

		// ...and where it is out at sea, which is where most of them were: waiting for one to
		// dock meant most civs never built a yard (game b0247e2c).
		[Fact]
		public void AnOutdatedHullAtSeaStillGetsAYard()
		{
			(Game g, Player p, City port) = AnAiPort();
			g.CreateUnit(UnitType.Trireme, 46, 25, g.PlayerNumber(p), false);

			Assert.Contains(PlanFor(p, port), x => x is Shipyard);
		}

		// One at a time: another port already building it is enough.
		[Fact]
		public void OneYardIsBuiltAtATime()
		{
			(Game g, Player p, City port) = AnAiPort();
			g.AddCity(p, 1, 38, 25)!.SetProduction(new Shipyard());
			g.CreateUnit(UnitType.Trireme, 46, 25, g.PlayerNumber(p), false);

			Assert.DoesNotContain(PlanFor(p, port), x => x is Shipyard);
		}

		// The Ottomans of game b0247e2c: Magnetism makes a Frigate the refit, but the yard
		// needs Navigation, so it is not asked for.
		[Fact]
		public void NoYardIsAskedForWithoutNavigation()
		{
			(Game g, Player p, City port) = AnAiPort(navigation: false);
			p.AddAdvance(new Magnetism(), false);
			g.CreateUnit(UnitType.Trireme, 42, 25, g.PlayerNumber(p), false);

			Assert.DoesNotContain(PlanFor(p, port), x => x is Shipyard);
		}

		[Fact]
		public void TheAiRefitsAShipInItsShipyard()
		{
			(Game g, Player p, City port) = AnAiPort();
			port.AddBuilding(new Shipyard());
			g.CreateUnit(UnitType.Trireme, 42, 25, g.PlayerNumber(p), false);

			AI.Instance(p).ConsiderUpgrades();

			Assert.Contains(g.GetUnits(), u => u.Owner == g.PlayerNumber(p) && u is Sail);
			Assert.DoesNotContain(g.GetUnits(), u => u.Owner == g.PlayerNumber(p) && u is Trireme);
		}

		// An outdated, empty hull puts in at the yard — the far port, past a nearer one without.
		[Fact]
		public void AnOutdatedShipSailsForTheShipyard()
		{
			(Game g, Player p, City port) = AnAiPort();
			g.AddCity(p, 1, 38, 25)!.AddBuilding(new Shipyard());
			// Two tiles out: a Trireme plots no course through open sea.
			IUnit ship = g.CreateUnit(UnitType.Trireme, 44, 25, g.PlayerNumber(p), false)!;

			typeof(AI).GetMethod("AssignMission", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.Invoke(AI.Instance(p), new object[] { ship });

			Assert.Equal((38, 25), (ship.Goto.X, ship.Goto.Y));
		}

		// The hull rule asks for the newest class the port can launch, not always a Trireme.
		[Fact]
		public void TheAiBuildsTheNewestHullItCan()
		{
			(Game g, Player p, City port) = AnAiPort();
			p.AddAdvance(new Magnetism(), false);

			List<IProduction> plan = PlanFor(p, port);

			Assert.Contains(plan, x => x is Frigate);
			Assert.DoesNotContain(plan, x => x is Trireme);
		}

		[Fact]
		public void AnIroncladRefitsAsACruiser()
		{
			(Game g, Player human, _) = APort(new Shipyard());
			human.AddAdvance(new Combustion(), false);
			IUnit ship = g.CreateUnit(UnitType.Ironclad, 40, 25, g.PlayerNumber(human))!;

			Assert.StartsWith("Upgrade to Cruiser", UpgradeOrder(ship));
		}
	}
}
