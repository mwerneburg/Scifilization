// CivOne tests
//
// The Crossing Fever (Oct 2026): one Olvir landfall in four, curses on, carries a plague of
// both worlds. Warned ten turns ahead; then a city with a Hospital and a Sewer is spared, one
// with either loses half, one with neither falls to a single citizen, and Settlers homed in a
// stricken city die. The Olvir carry it and the machines cannot catch it.

using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class OlvirPlagueTests
	{
		private sealed class World
		{
			public Game G = null!;
			public Player Earth = null!;
			public City Both = null!, HospitalOnly = null!, SewerOnly = null!, Neither = null!, Olvir = null!, Machine = null!;
		}

		// Six size-10 cities on a plains strip (shields, so the planner runs): four Earth cities with every mix of the two
		// buildings, one Olvir and one Skynet city with neither.
		private static World AWorld()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			Map.Instance.RecalculateContinentsIfDirty();

			Player earth = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			foreach (IAdvance a in Common.Advances) earth.AddAdvance(a, false);
			earth.Government = new Governments.Monarchy();
			earth.Explore(40, 25, range: 40);   // a city works only tiles its owner has seen
			Player Add(System.Type civ)
			{
				var p = new Player(Common.Civilizations.First(c => c.GetType() == civ), "Faction");
				g.AddPlayer(p);
				return p;
			}
			Player olvir = Add(typeof(Civilizations.Olvir)), skynet = Add(typeof(Civilizations.Skynet));

			int id = 0;
			City At(Player p, int x, params IBuilding[] bs)
			{
				City c = g.AddCity(p, id++, x, 25)!;
				c.Size = 10;
				c.ResetResourceTiles();   // works its tiles: a city with no shields only builds a defender
				foreach (IBuilding b in bs) c.AddBuilding(b);
				return c;
			}
			var w = new World
			{
				G = g, Earth = earth,
				Both         = At(earth, 14, new Hospital(), new SewerSystem()),
				HospitalOnly = At(earth, 20, new Hospital()),
				SewerOnly    = At(earth, 26, new SewerSystem()),
				Neither      = At(earth, 32),
				Olvir        = At(olvir, 50),
				Machine      = At(skynet, 60),
			};
			Sim.ClearTasks();
			return w;
		}

		private static void Process(Game g) =>
			typeof(Game).GetMethod("ProcessOlvirPlague", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(g, null);

		[Fact]
		public void ACityWithBothIsSpared()
		{
			World w = AWorld();
			w.G.StrikeOlvirPlague();
			Assert.Equal(10, w.Both.Size);
		}

		[Fact]
		public void EitherOneHalvesIt()
		{
			World w = AWorld();
			w.G.StrikeOlvirPlague();
			Assert.Equal((5, 5), ((int)w.HospitalOnly.Size, (int)w.SewerOnly.Size));
		}

		[Fact]
		public void NeitherLeavesOneCitizen()
		{
			World w = AWorld();
			w.G.StrikeOlvirPlague();
			Assert.Equal(1, w.Neither.Size);
		}

		[Fact]
		public void TheOlvirAndTheMachinesAreImmune()
		{
			World w = AWorld();
			w.G.StrikeOlvirPlague();
			Assert.Equal((10, 10), ((int)w.Olvir.Size, (int)w.Machine.Size));
		}

		[Fact]
		public void TheSettlersOfAStrickenCityDie()
		{
			World w = AWorld();
			byte e = w.G.PlayerNumber(w.Earth);
			IUnit sick = w.G.CreateUnit(UnitType.Settlers, w.Neither.X, w.Neither.Y, e)!;
			sick.SetHome(w.Neither);
			IUnit well = w.G.CreateUnit(UnitType.Settlers, w.Both.X, w.Both.Y, e)!;
			well.SetHome(w.Both);

			w.G.StrikeOlvirPlague();

			Assert.DoesNotContain(sick, w.G.GetUnits());
			Assert.Contains(well, w.G.GetUnits());
		}

		// ── the roll ─────────────────────────────────────────────────────────

		private static int Rolls(Game g, bool cursed)
		{
			bool was = Settings.Instance.CursedWonders;
			Settings.Instance.CursedWonders = cursed;
			try
			{
				int hits = 0;
				for (int i = 0; i < 200; i++)
				{
					g.OlvirPlagueTurn = 0;
					g.RollOlvirPlague();
					if (g.OlvirPlagueTurn == g.GameTurn + Game.PlagueOnsetTurns) hits++;
				}
				return hits;
			}
			finally { Settings.Instance.CursedWonders = was; }
		}

		// About 50 of 200 at one in four; outside 10..120 is past any honest dice.
		[Fact]
		public void OneLandfallInFourCarriesIt()
		{
			World w = AWorld();
			int hits = Rolls(w.G, cursed: true);
			Assert.InRange(hits, 10, 120);
		}

		[Fact]
		public void NeverWithCursesOff()
		{
			World w = AWorld();
			Assert.Equal(0, Rolls(w.G, cursed: false));
		}

		// ── the warning ──────────────────────────────────────────────────────

		// A content city: EarnsItsKeep refuses it a Hospital in ordinary times.
		[Fact]
		public void WarnedCitiesBuildAHospitalFirst()
		{
			World w = AWorld();
			w.Neither.Size = 3;
			w.G.OlvirPlagueTurn = (uint)(w.G.GameTurn + Game.PlagueWarningTurns);
			Assert.IsType<Hospital>(AI.Instance(w.Earth).ProductionPlan(w.Neither)[0]);
		}

		[Fact]
		public void NotBeforeTheWarning()
		{
			World w = AWorld();
			w.Neither.Size = 3;
			w.G.OlvirPlagueTurn = (uint)(w.G.GameTurn + Game.PlagueWarningTurns + 1);
			Assert.IsNotType<Hospital>(AI.Instance(w.Earth).ProductionPlan(w.Neither)[0]);
		}

		// The planner runs only on an empty queue, so the warning turn re-plans every city.
		[Fact]
		public void TheWarningTurnsCitiesToHospitalsAndIsAnnounced()
		{
			World w = AWorld();
			w.Neither.Size = 3;                  // content: a city in disorder makes nothing to re-plan
			w.Neither.ResetResourceTiles();
			w.Neither.SetProduction(new Militia());
			w.G.OlvirPlagueTurn = (uint)(w.G.GameTurn + Game.PlagueWarningTurns);

			Process(w.G);

			Assert.IsType<Hospital>(w.Neither.CurrentProduction);
			Assert.Contains("Olvir physicians sound alarm!", Sim.PendingMessageLines());
			Assert.Equal(3, w.Neither.Size);
		}

		[Fact]
		public void ItStrikesOnItsTurnAndOnlyOnce()
		{
			World w = AWorld();
			w.G.GameTurn = 400;                  // 0 would read as "no plague coming"
			w.G.OlvirPlagueTurn = w.G.GameTurn;

			Process(w.G);

			Assert.Equal(1, w.Neither.Size);
			Assert.Equal(0u, w.G.OlvirPlagueTurn);
			Assert.Contains("The Crossing Fever strikes!", Sim.PendingMessageLines());
		}

		[Fact]
		public void ThePendingPlagueSurvivesASave()
		{
			World w = AWorld();
			w.G.OlvirPlagueTurn = 412;
			string path = Path.Combine(Settings.Instance.SavesDirectory, "plague.cos");
			w.G.SaveCos(path);

			Sim.ResetState();
			Assert.True(Game.LoadCos(path));
			Assert.Equal(412u, Game.Instance.OlvirPlagueTurn);
		}

		[Fact]
		public void ThePlagueArtIsShipped()
		{
			string path = Path.Combine(Sim.RepoRoot(), "runtime", "sdl", "Resources",
				"defaults", "data", "event_art", "OlvirPlague.png");
			Assert.True(File.Exists(path), $"plague art is missing: {path}");
		}
	}
}
