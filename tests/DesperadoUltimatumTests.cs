// CivOne tests
//
// The desperado's ultimatum (the user, Nov 2026). A Fascist state builds up to three
// warheads, demands a city of the human ("cede it, or in ten turns we launch"), and launches
// when the clock runs out. Ceding the city, or the regime falling, ends the threat; one threat
// at a time, thirty quiet turns after one ends. Against AI rivals there is no threat to make:
// the warheads answer only a city taken. And the threat survives a save.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Screens;
using CivOne.Units;

namespace CivOne.Tests
{
	public class DesperadoUltimatumTests : IDisposable
	{
		private readonly bool _auto;
		public DesperadoUltimatumTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// One strip of land. The desperado's city at 30 holds the warhead; the human's at 40
		// (in reach) and 60 (out of it); an AI rival's at 20. The Manhattan Project stands.
		private static (Game g, Player us, Player human, Player rival, IUnit? nuke) AWorld(bool warhead = true)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 5; x <= 75; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();

			Player human = g.HumanPlayer;
			Player[] ais = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != human).ToArray();
			Player us = ais[0], rival = ais[1];
			foreach (IAdvance a in Common.Advances) us.AddAdvance(a, false);
			us.Government = new CivOne.Governments.Fascism();
			us.Explore(40, 25, range: 40);

			City home = g.AddCity(us, 0, 30, 25)!;
			home.Size = 8;
			home.AddWonder(new CivOne.Wonders.ManhattanProject());
			g.CreateUnit(UnitType.Musketeers, 30, 25, g.PlayerNumber(us));
			foreach ((Player p, int id, int x) in new[] { (human, 1, 40), (human, 2, 60), (rival, 3, 20) })
			{
				g.AddCity(p, id, x, 25)!.Size = 6;
				g.CreateUnit(UnitType.Musketeers, x, 25, g.PlayerNumber(p));
			}
			us.DeclareWar(human);
			us.DeclareWar(rival);
			IUnit? nuke = warhead ? g.CreateUnit(UnitType.Nuclear, 30, 25, g.PlayerNumber(us)) : null;
			Sim.ClearTasks();
			return (g, us, human, rival, nuke);
		}

		private static void FlyStrike(Player p, IUnit unit) =>
			typeof(AI).GetMethod("FlyStrike", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(AI.Instance(p), new object[] { unit });

		private static bool PlansAWarhead(Player p) =>
			AI.Instance(p).ProductionPlan(p.Cities[0]).Any(x => x is Nuclear);

		// ── the stockpile ────────────────────────────────────────────────────

		[Fact]
		public void AFascistStateBuildsAWarhead()
		{
			(Game _, Player us, Player _, Player _, IUnit? _) = AWorld(warhead: false);
			Assert.True(PlansAWarhead(us));
		}

		[Fact]
		public void AMonarchyWithTheSameArsenalDoesNot()
		{
			(Game _, Player us, Player _, Player _, IUnit? _) = AWorld(warhead: false);
			us.Government = new CivOne.Governments.Monarchy();
			Assert.False(PlansAWarhead(us));
		}

		[Fact]
		public void ThreeIsTheStockpile()
		{
			(Game g, Player us, Player _, Player _, IUnit? _) = AWorld(warhead: true);
			Assert.True(PlansAWarhead(us));
			g.CreateUnit(UnitType.Nuclear, 30, 25, g.PlayerNumber(us));
			g.CreateUnit(UnitType.Nuclear, 30, 25, g.PlayerNumber(us));
			Assert.False(PlansAWarhead(us));
		}

		// ── the ultimatum ────────────────────────────────────────────────────

		[Fact]
		public void TheUltimatumNamesTheCityInReach()
		{
			(Game g, Player us, Player _, Player _, IUnit? _) = AWorld();

			AI.Instance(us).ConsiderUltimatum();

			Assert.Equal((40, 25), (us.ThreatX, us.ThreatY));
			Assert.Equal(g.GameTurn + AI.UltimatumTurns, us.ThreatLaunchTurn);
		}

		// The turn itself asks, not only a test.
		[Fact]
		public void TheTurnMakesTheThreat()
		{
			(Game _, Player us, Player _, Player _, IUnit? _) = AWorld();
			us.NewTurn();
			Assert.Equal((40, 25), (us.ThreatX, us.ThreatY));
		}

		// Only the city at 60 is left, thirty tiles off: no warhead can reach it, so no threat.
		[Fact]
		public void NoCityInReachNoThreat()
		{
			(Game g, Player us, Player _, Player rival, IUnit? _) = AWorld();
			Map.Instance[40, 25].City!.Owner = g.PlayerNumber(rival);

			AI.Instance(us).ConsiderUltimatum();

			Assert.Equal(0, us.ThreatLaunchTurn);
		}

		[Fact]
		public void NoWarheadNoThreat()
		{
			(Game _, Player us, Player _, Player _, IUnit? _) = AWorld(warhead: false);
			AI.Instance(us).ConsiderUltimatum();
			Assert.Equal(0, us.ThreatLaunchTurn);
		}

		[Theory]
		[InlineData(29, false)]
		[InlineData(30, true)]
		public void ThirtyQuietTurnsAfterAThreatEnds(int since, bool threatens)
		{
			(Game g, Player us, Player _, Player _, IUnit? _) = AWorld();
			g.GameTurn = 100;
			us.LastThreatTurn = 100 - since;

			AI.Instance(us).ConsiderUltimatum();

			Assert.Equal(threatens, us.ThreatLaunchTurn != 0);
		}

		[Fact]
		public void TheThreatDiesWithTheRegime()
		{
			(Game _, Player us, Player _, Player _, IUnit? _) = AWorld();
			AI.Instance(us).ConsiderUltimatum();
			us.Government = new CivOne.Governments.Monarchy();

			AI.Instance(us).ConsiderUltimatum();

			Assert.Equal(0, us.ThreatLaunchTurn);
		}

		// ── the audience ─────────────────────────────────────────────────────

		private static (King king, AIDemand demand) TheAudience(Player us)
		{
			AI.Instance(us).ConsiderUltimatum();
			City city = Map.Instance[us.ThreatX, us.ThreatY].City!;
			var demand = new AIDemand(AIDemandKind.NuclearUltimatum, city: city, duration: AI.UltimatumTurns);
			return (new King(us, aiInitiated: true, demands: new List<AIDemand> { demand }), demand);
		}

		private static string Speech(King k) => string.Join(" ",
			(string[])typeof(King).GetField("_speechLines", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(k)!);

		[Fact]
		public void CedingTheCityEndsTheThreat()
		{
			(Game g, Player us, Player _, Player _, IUnit? _) = AWorld();
			(King king, AIDemand demand) = TheAudience(us);

			typeof(King).GetMethod("FulfillDemand", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(king, new object[] { demand });

			Assert.Equal(g.PlayerNumber(us), demand.City!.Owner);
			Assert.Equal(0, us.ThreatLaunchTurn);
		}

		[Fact]
		public void RefusingLeavesTheClockRunning()
		{
			(Game _, Player us, Player _, Player _, IUnit? _) = AWorld();
			(King king, AIDemand _) = TheAudience(us);
			Assert.Contains("We have the bomb", Speech(king));

			typeof(King).GetMethod("DeclineAllDemands", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(king, new object?[] { null, EventArgs.Empty });

			Assert.Contains($"has {AI.UltimatumTurns} turns", Speech(king));
			Assert.NotEqual(0, us.ThreatLaunchTurn);
		}

		// ── the launch ───────────────────────────────────────────────────────

		[Fact]
		public void TheWarheadWaitsOutTheClock()
		{
			(Game _, Player us, Player _, Player _, IUnit? nuke) = AWorld();
			AI.Instance(us).ConsiderUltimatum();

			FlyStrike(us, nuke!);

			Assert.NotEqual(new Point(40, 25), nuke!.Goto);
		}

		[Fact]
		public void AndFliesWhenItRunsOut()
		{
			(Game g, Player us, Player _, Player _, IUnit? nuke) = AWorld();
			g.GameTurn = 100;   // a launch turn of 0 reads as no threat at all
			AI.Instance(us).ConsiderUltimatum();
			us.ThreatLaunchTurn = g.GameTurn;

			FlyStrike(us, nuke!);

			Assert.Equal(new Point(40, 25), nuke!.Goto);
			Assert.Equal(0, us.ThreatLaunchTurn);
			Assert.Equal(g.GameTurn, us.LastThreatTurn);
		}

		// The war's attack target is not licence: without the ultimatum, nothing flies.
		[Fact]
		public void NoStrikeWithoutWarning()
		{
			(Game _, Player us, Player _, Player _, IUnit? nuke) = AWorld();
			typeof(AI).GetField("_attackTarget", BindingFlags.NonPublic | BindingFlags.Instance)!
				.SetValue(AI.Instance(us), Map.Instance[40, 25].City);

			FlyStrike(us, nuke!);

			Assert.NotEqual(new Point(40, 25), nuke!.Goto);
		}

		// ── retaliation ──────────────────────────────────────────────────────

		// A rival holding a city we founded is fair game: its own city, not the one it took.
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void ARivalHoldingOurCityIsStruck(bool holdsOne)
		{
			(Game g, Player us, Player _, Player rival, IUnit? nuke) = AWorld();
			if (holdsOne)
			{
				City taken = g.AddCity(rival, 4, 25, 25)!;
				g.CreateUnit(UnitType.Musketeers, 25, 25, g.PlayerNumber(rival));
				taken.OriginalOwner = g.PlayerNumber(us);
			}

			FlyStrike(us, nuke!);

			if (holdsOne) Assert.Equal(new Point(20, 25), nuke!.Goto);
			else Assert.NotEqual(new Point(20, 25), nuke!.Goto);
		}

		// The human is threatened first, never struck in reprisal: the city at 60 was ours, and
		// the one in reach at 40 is still spared.
		[Fact]
		public void TheHumanHoldingOurCityIsNotStruckUnwarned()
		{
			(Game g, Player us, Player _, Player _, IUnit? nuke) = AWorld();
			Map.Instance[60, 25].City!.OriginalOwner = g.PlayerNumber(us);

			FlyStrike(us, nuke!);

			Assert.NotEqual(new Point(40, 25), nuke!.Goto);
		}

		// ── the save ─────────────────────────────────────────────────────────

		[Fact]
		public void TheThreatSurvivesASave()
		{
			(Game g, Player us, Player _, Player _, IUnit? _) = AWorld();
			AI.Instance(us).ConsiderUltimatum();
			us.LastThreatTurn = 7;
			int slot = g.PlayerNumber(us), launch = us.ThreatLaunchTurn;
			string path = Path.Combine(Settings.Instance.SavesDirectory, "ultimatum.cos");
			g.SaveCos(path);

			Sim.ResetState();
			Assert.True(Game.LoadCos(path));

			Player back = Game.Instance.GetPlayer((byte)slot);
			Assert.Equal((40, 25, launch, 7), (back.ThreatX, back.ThreatY, back.ThreatLaunchTurn, back.LastThreatTurn));
		}
	}
}
