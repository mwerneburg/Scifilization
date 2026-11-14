// CivOne tests
//
// The Cabinet Noir (Nov 2026, the user's request): a Police Station's guard against sabotage in
// every city of its owner's, from Invention, never obsolete, never cursed. Sabotage only —
// foreign agents still steal advances and incite revolts. An AI civ that holds it skips the
// Police Station unless the station still pays: under Democracy it stops the size leak.

using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class CabinetNoirTests
	{
		[Fact]
		public void TheArtIsShipped() =>
			Assert.True(File.Exists(Path.Combine(Sim.RepoRoot(), Common.RepoArtDir, "improvement_art", "cabinet_noir.png")),
				"improvement_art/cabinet_noir.png is missing");

		// Two neighbouring cities, ours (the human's) and an AI rival's, each with buildings to
		// lose, and an embassy each way so an agent is not spent opening one.
		private static (Game g, Player us, Player them, City ours, City theirs) Neighbours()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 30; x <= 50; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player us = g.HumanPlayer;
			Player them = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != us);
			City ours = g.AddCity(us, 0, 35, 25)!;
			City theirs = g.AddCity(them, 1, 45, 25)!;
			foreach (City c in new[] { ours, theirs })
			{
				c.Size = 4;
				c.AddBuilding(new Barracks());
				c.AddBuilding(new Granary());
			}
			us.EstablishEmbassy(them);
			them.EstablishEmbassy(us);
			Sim.ClearTasks();
			return (g, us, them, ours, theirs);
		}

		// The player's own agent, through the Industrial Sabotage menu item.
		[Theory]
		[InlineData(true)]
		[InlineData(false)]   // the control: without the wonder the agent sabotages
		public void ThePlayersAgentCannotSabotageAHolder(bool holds)
		{
			(Game g, Player us, Player them, City ours, City theirs) = Neighbours();
			if (holds) theirs.AddWonder(new CabinetNoir());
			var agent = (Diplomat)g.CreateUnit(UnitType.Diplomat, 44, 25, g.PlayerNumber(us), false)!;
			int buildings = theirs.Buildings.Length;
			theirs.Shields = 20;

			string outcome = agent.Sabotage(theirs);

			if (holds)
			{
				Assert.Equal("agent held by the Cabinet Noir", outcome);
				Assert.Equal(buildings, theirs.Buildings.Length);
				Assert.Equal(20, theirs.Shields);
			}
			else Assert.EndsWith("sabotaged", outcome);
		}

		private static System.Collections.IList Tasks() => (System.Collections.IList)typeof(GameTask)
			.GetField("_tasks", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!.GetValue(null)!;

		// An AI agent walks into the human's city: Diplomat.Confront, the AI's mission dispatch.
		private static void Enter(Diplomat agent, City city) =>
			typeof(Diplomat).GetMethod("Confront", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(agent, new object[] { city.X - agent.X, city.Y - agent.Y });

		// With nothing to steal and no gold to incite, an AI agent's only mission is sabotage.
		[Theory]
		[InlineData(true)]
		[InlineData(false)]   // the control: without the wonder the sabotage screen comes up
		public void AnAIAgentWithOnlySabotageLeftIsHeld(bool holds)
		{
			(Game g, Player us, Player them, City ours, City theirs) = Neighbours();
			if (holds) ours.AddWonder(new CabinetNoir());
			// Nothing to steal: they know all we know. (TechStolen stamps the turn, and on a
			// fresh game's turn 0 the stamp reads as never stolen.)
			foreach (IAdvance a in us.Advances) them.AddAdvance(a, false);
			them.Gold = 0;
			var agent = (Diplomat)g.CreateUnit(UnitType.Diplomat, 36, 25, g.PlayerNumber(them), false)!;
			Sim.ClearTasks();

			Enter(agent, ours);

			bool sabotage = Tasks().OfType<CivOne.Tasks.Show>().Any(s => s.Displayed is CivOne.Screens.Dialogs.DiplomatSabotage);
			Assert.Equal(!holds, sabotage);
			if (holds) Assert.DoesNotContain(agent, g.GetUnits());
			Sim.ClearTasks();
		}

		// Sabotage only: the same agent still steals an advance from a holder.
		[Fact]
		public void AnAIAgentStillStealsFromAHolder()
		{
			(Game g, Player us, Player them, City ours, City theirs) = Neighbours();
			ours.AddWonder(new CabinetNoir());
			foreach (IAdvance a in them.Advances) them.DeleteAdvance(a);
			us.AddAdvance(new Alphabet(), false);
			them.Gold = 0;
			var agent = (Diplomat)g.CreateUnit(UnitType.Diplomat, 36, 25, g.PlayerNumber(them), false)!;
			Sim.ClearTasks();

			Enter(agent, ours);

			Assert.Contains(Tasks().Cast<object>(), t => t is CivOne.Tasks.GetAdvance);
			Sim.ClearTasks();
		}

		// ── the AI holder and its Police Stations ──────────────────────────────────

		// FullGarrisonTests' city: a full garrison, Barracks up, and only Communism known, so
		// the cheapest building it can still raise is a Police Station.
		private static IProduction[] PlanWithOnlyAPoliceStationLeft(bool holds, bool democracy, int size)
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			Settings.Instance.Autopilot = true;
			Game g = Game.Instance;
			Player us = g.HumanPlayer;
			foreach (IAdvance a in us.Advances) us.DeleteAdvance(a);
			us.AddAdvance(new CivOne.Advances.Communism());
			if (democracy) us.Government = new CivOne.Governments.Democracy();
			us.LuxuriesRate = 0;
			ITile site = Map.Instance.AllTiles().First(t => t.Type == Terrain.Grassland1 && t.City is null && t.Y > 5 && t.Y < Map.HEIGHT - 5);
			us.Explore(site.X, site.Y, range: 3);
			City city = g.AddCity(us, 0, site.X, site.Y)!;
			city.AddBuilding(new Barracks());
			city.Size = (byte)size;
			city.ResetResourceTiles();
			ITile[] land = Map.Instance.AllTiles().Where(t => !t.IsOcean && t.Y > 5 && t.Y < Map.HEIGHT - 5).ToArray();
			var towns = new System.Collections.Generic.List<ITile> { site };
			for (int i = 0; i < 3; i++)
			{
				ITile t = land.First(x => towns.All(y => Common.DistanceToTile(x.X, x.Y, y.X, y.Y) > 8));
				towns.Add(t);
				Assert.NotNull(g.AddCity(us, 0, t.X, t.Y));
				g.CreateUnit(UnitType.Militia, t.X, t.Y, g.PlayerNumber(us), false);
			}
			if (holds) g.GetCities().First(c => c != city && c.Owner == g.PlayerNumber(us)).AddWonder(new CabinetNoir());
			g.CreateUnit(UnitType.Settlers, towns[1].X, towns[1].Y, g.PlayerNumber(us), false);
			for (int i = 0; i < 2; i++) g.CreateUnit(UnitType.Explorer, site.X, site.Y, g.PlayerNumber(us), false);
			for (int i = 0; i < 4; i++) g.CreateUnit(UnitType.Militia, site.X, site.Y, g.PlayerNumber(us), false);
			return AI.Instance(us).ProductionPlan(city);
		}

		[Theory]
		[InlineData(false, false, 1, true)]   // the control: no wonder, the station is built
		[InlineData(true,  false, 1, false)]  // the holder skips it
		[InlineData(true,  true,  6, true)]   // ...unless it stops the Democracy size leak
		public void AHolderSkipsThePoliceStationUnlessItStillPays(bool holds, bool democracy, int size, bool builds)
		{
			IProduction[] plan = PlanWithOnlyAPoliceStationLeft(holds, democracy, size);
			Assert.Equal(builds, plan.Any(p => p is PoliceStation));
		}

		// A new wonder id (47; Deacon's 46 is left empty) must come back from a save.
		[Fact]
		public void TheWonderSurvivesASaveAndLoad()
		{
			(Game g, Player us, Player them, City ours, City theirs) = Neighbours();
			ours.AddWonder(new CabinetNoir());
			string path = Path.Combine(Settings.Instance.SavesDirectory, "cabinetnoir.cos");

			g.SaveCos(path);
			Sim.ResetState();
			Assert.True(Game.LoadCos(path), "the save just written did not load");

			Assert.True(Game.Instance.HumanPlayer.HasWonder<CabinetNoir>());
			File.Delete(path);
		}

		// Never obsolete, and on Invention, as agreed.
		[Fact]
		public void ItComesWithInventionAndNeverGoesStale()
		{
			var w = new CabinetNoir();
			Assert.IsType<Invention>(w.RequiredTech);
			Assert.Null(w.ObsoleteTech);
		}
	}
}
