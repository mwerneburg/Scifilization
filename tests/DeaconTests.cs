// CivOne tests
//
// Deacon, cursed wonder #13 (docs/cursed_wonders.md), the user's brief, Nov 2026: a war
// computer whose owner's units defend with advantage, and which one time in four counts
// DEFCON down to a launch at the builder's largest rival, unless the builder makes peace.

using System.IO;
using System.Linq;
using CivOne;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class DeaconTests
	{
		// Two AI civs with cities: the builder (Deacon in its capital) and a bigger rival.
		// Neither is the human, whose messages and Senate would only get in the way.
		private static (Game g, Player builder, Player rival, City deacon) World()
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Game g = Game.Instance;
			Player[] ps = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			Player builder = ps[0], rival = ps[1];

			builder.Explore(15, 20, range: 6);   // or its citizens have no tiles to work
			City deacon = g.AddCity(builder, 0, 15, 20)!;
			deacon.AddWonder(new Deacon());
			for (int i = 0; i < 7; i++)
				g.AddCity(rival, (byte)(i + 1), 40 + (i % 4) * 5, 20 + (i / 4) * 5)!.Size = (byte)(4 + i);
			Sim.ClearTasks();
			return (g, builder, rival, deacon);
		}

		private static void RunClock(Game g, int turns)
		{
			for (int i = 0; i < turns; i++) g.ProcessDeacon();
			Sim.ClearTasks();
		}

		// A missing PNG degrades silently to the sprite-sheet icon, so demand the file.
		[Fact]
		public void TheArtIsShipped()
		{
			string path = Path.Combine(Sim.RepoRoot(), "runtime", "sdl", "Resources",
				"defaults", "data", "improvement_art", "deacon.png");

			Assert.True(File.Exists(path), $"deacon art is missing: {path}");
		}

		// The blessing: a lost defence is fought again. Even odds become one in four.
		[Fact]
		public void ItsOwnersUnitsDefendWithAdvantage()
		{
			var (g, builder, rival, _) = World();
			Player bystander = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0
				&& p != builder && p != rival && p != g.HumanPlayer);
			IUnit attacker = g.CreateUnit(UnitType.Phalanx, 21, 30, g.PlayerNumber(rival))!;
			// The same unit on the same ground, once for the Deacon holder and once for a civ
			// without it.
			IUnit blessedDefender = g.CreateUnit(UnitType.Phalanx, 20, 30, g.PlayerNumber(builder))!;
			IUnit plainDefender = g.CreateUnit(UnitType.Phalanx, 22, 30, g.PlayerNumber(bystander))!;

			double Rate(IUnit defender)
			{
				int wins = 0;
				for (int i = 0; i < 4000; i++)
				{
					attacker.Veteran = defender.Veteran = false;
					if (((BaseUnit)attacker).TestAttackOutcome(defender)) wins++;
				}
				return wins / 4000.0;
			}

			double blessed = Rate(blessedDefender);
			double plain = Rate(plainDefender);

			Assert.True(plain > 0.1, $"scenario is broken: the attacker never wins ({plain:P0})");
			Assert.True(blessed < plain * 0.8,
				$"attacker won {blessed:P0} against a Deacon holder, {plain:P0} without it");
		}

		// The curse names the builder's largest rival and starts at DEFCON 5.
		[Fact]
		public void ArmingAimsAtTheLargestRival()
		{
			var (g, builder, rival, deacon) = World();
			Player minor = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0
				&& p != builder && p != rival && p != g.HumanPlayer);
			minor.Explore(60, 35, range: 3);
			g.AddCity(minor, 9, 60, 35);
			Assert.True(rival.Score > minor.Score, $"scenario is broken: rival {rival.Score}, minor {minor.Score}");

			Assert.True(g.ArmDeacon(deacon));

			Assert.Equal(1, g.DeaconState);
			Assert.Equal(5, g.DeaconDefcon);
			Assert.Equal(g.PlayerNumber(rival), g.DeaconTarget);
		}

		// At peace with everyone when the clock runs out: the machine stands down for good,
		// and the blessing stays.
		[Fact]
		public void ABuilderAtPeace_IsSpared()
		{
			var (g, builder, rival, deacon) = World();
			g.ArmDeacon(deacon);
			int[] before = rival.Cities.Select(c => (int)c.Size).ToArray();

			RunClock(g, 5);

			Assert.Equal(2, g.DeaconState);
			Assert.Equal(before, rival.Cities.Select(c => (int)c.Size).ToArray());
			Assert.True(g.DeaconBlesses(builder));
		}

		// Not one turn early: five turns of warning, DEFCON 5 to 1, before it decides.
		[Fact]
		public void ItDecidesOnlyAfterDefconOne()
		{
			var (g, builder, rival, deacon) = World();
			g.ArmDeacon(deacon);
			builder.DeclareWar(rival);

			RunClock(g, 4);

			Assert.Equal(1, g.DeaconState);
			Assert.Equal(1, g.DeaconDefcon);
		}

		// At war with someone it could have made peace with: five strikes at the rival's five
		// largest cities, the rival at war with it, its people angry, and the machine dark.
		// The war is with a bystander, so the rival's war can only come from the launch.
		[Fact]
		public void ABuilderAtWar_Launches()
		{
			var (g, builder, rival, deacon) = World();
			g.ArmDeacon(deacon);
			Player bystander = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0
				&& p != builder && p != rival && p != g.HumanPlayer);
			builder.DeclareWar(bystander);
			Assert.False(rival.IsAtWar(builder), "scenario is broken: already at war with the rival");
			City[] largest = rival.Cities.OrderByDescending(c => c.Size).Take(5).ToArray();
			int[] sizes = largest.Select(c => (int)c.Size).ToArray();
			City spared = rival.Cities.OrderBy(c => c.Size).First();
			byte sparedSize = spared.Size;
			deacon.Size = 8;
			deacon.InvalidateCache();
			int calm = deacon.UnhappyCitizens;

			RunClock(g, 5);

			Assert.Equal(3, g.DeaconState);
			for (int i = 0; i < largest.Length; i++)
				Assert.True(largest[i].Size == System.Math.Max(1, sizes[i] / 2),
					$"{largest[i].Name} was size {sizes[i]}, now {largest[i].Size}");
			Assert.Equal(sparedSize, spared.Size);
			Assert.True(rival.IsAtWar(builder));
			Assert.False(g.DeaconBlesses(builder), $"the blessing outlived the launch: state {g.DeaconState}");
			deacon.InvalidateCache();
			Assert.True(deacon.UnhappyCitizens > calm, $"no anger: {calm} unhappy before, {deacon.UnhappyCitizens} after; {string.Join(",", deacon.Citizens)} until {g.DeaconAngerUntil} turn {Game.Instance.GameTurn} owner {deacon.Owner} builder {g.DeaconBuilder}");
		}

		// A leader who has renounced the builder will never treat, so that war is no fault of
		// the builder's.
		[Fact]
		public void AWarNobodyCouldEnd_DoesNotCount()
		{
			var (g, builder, rival, deacon) = World();
			g.ArmDeacon(deacon);
			Player renouncer = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0
				&& p != builder && p != rival && p != g.HumanPlayer);
			builder.DeclareWar(renouncer);
			renouncer.SetImplacable(builder);

			RunClock(g, 5);

			Assert.Equal(2, g.DeaconState);
		}

		// A rival holding the Fusion Core shoots every missile down; the launch still happened.
		[Fact]
		public void TheFusionCore_InterceptsTheLaunch()
		{
			var (g, builder, rival, deacon) = World();
			g.ArmDeacon(deacon);
			builder.DeclareWar(rival);
			rival.Cities[0].AddWonder(new FusionCore());
			int[] before = rival.Cities.Select(c => (int)c.Size).ToArray();

			RunClock(g, 5);

			Assert.Equal(3, g.DeaconState);
			Assert.Equal(before, rival.Cities.Select(c => (int)c.Size).ToArray());
			Assert.True(rival.IsAtWar(builder));
		}

		// Take the city and the clock stops.
		[Fact]
		public void TheCityChangingHands_StopsTheClock()
		{
			var (g, builder, rival, deacon) = World();
			g.ArmDeacon(deacon);
			builder.DeclareWar(rival);
			RunClock(g, 2);

			deacon.Owner = g.PlayerNumber(rival);
			RunClock(g, 3);

			Assert.Equal(2, g.DeaconState);
			Assert.True(g.DeaconBlesses(rival), "the new keepers should inherit a machine that watches");
		}

		[Fact]
		public void TheClockSurvivesASave()
		{
			var (g, builder, rival, deacon) = World();
			g.ArmDeacon(deacon);
			RunClock(g, 2);
			string path = Path.Combine(Settings.Instance.SavesDirectory, "deacon.cos");
			g.SaveCos(path);

			Sim.ResetState();
			Assert.True(Game.LoadCos(path));

			Game h = Game.Instance;
			Assert.Equal(1, h.DeaconState);
			Assert.Equal(3, h.DeaconDefcon);
			Assert.Equal(deacon.X, h.DeaconX);
			Assert.Equal(deacon.Y, h.DeaconY);
			Assert.Equal(g.PlayerNumber(builder), h.DeaconBuilder);
			Assert.Equal(g.PlayerNumber(rival), h.DeaconTarget);
		}
	}
}
