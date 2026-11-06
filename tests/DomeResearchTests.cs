// CivOne tests
//
// A Dome part goes to a civ whether or not it can build it, and nothing steered research toward
// the advance the part needs, so one late part lost the race for everyone. In game 99ea98b8 the
// Sensor Net went to the human slot, which under Autopilot researched whatever the research
// screen listed first: it learned Superconductor 35 turns after the Owners arrived, with 14 civs
// ahead of it.

using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Tasks;

namespace CivOne.Tests
{
	public class DomeResearchTests : System.IDisposable
	{
		private readonly bool _auto;
		public DomeResearchTests() { Sim.EnsureRuntime(); _auto = Settings.Instance.Autopilot; }
		public void Dispose() => Settings.Instance.Autopilot = _auto;

		// Everything known but the advances named; the civ holds the Sensor Net (Superconductor).
		private static Player AHolder(bool human, bool assigned, params System.Type[] unknown)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			Player p = human ? g.HumanPlayer : g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			foreach (IAdvance a in Common.Advances.Where(a => !unknown.Contains(a.GetType()) && a is not FutureTech))
				p.AddAdvance(a, false);
			p.Government = new CivOne.Governments.Monarchy();
			g.AddCity(p, 0, 40, 25);
			if (assigned) g.DomeAssignments[g.PlayerNumber(p)] = new System.Collections.Generic.List<Wonder> { Wonder.DomeSensorNet };
			Sim.ClearTasks();
			return p;
		}

		// The advances a fresh civ could study alongside, so the draw is a real choice.
		private static readonly System.Type[] Elsewhere = { typeof(SuperConductor), typeof(GeneticEngineering), typeof(LaborUnion), typeof(Recycling) };

		private static double Share<T>(Player p, System.Action choose) where T : IAdvance
		{
			const int rolls = 300;
			int hits = 0;
			for (int i = 0; i < rolls; i++)
			{
				p.CurrentResearch = null;
				choose();
				if (p.CurrentResearch is T) hits++;
			}
			return (double)hits / rolls;
		}

		[Fact]
		public void TheHolderStudiesThePartsAdvance()
		{
			Player p = AHolder(false, true, Elsewhere);
			Assert.Contains(p.AvailableResearch, a => a is SuperConductor);
			Assert.True(p.AvailableResearch.Count() >= 3, "the fixture leaves no choice");

			double share = Share<SuperConductor>(p, () => AI.Instance(p).ChooseResearch());
			Assert.True(share > 0.6, $"Superconductor took {share:P0}");
		}

		// The control: without the part, Superconductor is one choice among several.
		[Fact]
		public void WithoutThePartItIsOneChoiceAmongMany()
		{
			Player p = AHolder(false, false, Elsewhere);

			double share = Share<SuperConductor>(p, () => AI.Instance(p).ChooseResearch());
			Assert.True(share < 0.6, $"Superconductor took {share:P0}");
		}

		// The road, not only the last step: a missing prerequisite comes first.
		[Fact]
		public void APrerequisiteOnTheRoadIsFavoured()
		{
			System.Type before = new SuperConductor().RequiredTechs.First().GetType();
			Player p = AHolder(false, true, Elsewhere.Append(before).ToArray());
			IAdvance? first = p.AvailableResearch.FirstOrDefault(a => a.GetType() == before);
			Assert.NotNull(first);

			int hits = 0;
			for (int i = 0; i < 300; i++)
			{
				p.CurrentResearch = null;
				AI.Instance(p).ChooseResearch();
				if (p.CurrentResearch?.GetType() == before) hits++;
			}
			Assert.True(hits > 180, $"{before.Name} took {hits} of 300");
		}

		// Once the part stands there is nothing to hurry.
		[Fact]
		public void ABuiltPartIsNoLongerChased()
		{
			Player p = AHolder(false, true, Elsewhere);
			Game.Instance.Players.First(x => x == p).Cities[0].AddWonder(new CivOne.Wonders.DomeSensorNet());

			double share = Share<SuperConductor>(p, () => AI.Instance(p).ChooseResearch());
			Assert.True(share < 0.6, $"Superconductor took {share:P0}");
		}

		// Under Autopilot the human slot's research is the AI's choice, not the first in the list.
		[Fact]
		public void AutopilotResearchesLikeACivilization()
		{
			Player p = AHolder(true, true, Elsewhere);
			Settings.Instance.Autopilot = true;
			p.Science = 1;

			double share = Share<SuperConductor>(p, () => new TechSelect(p).Run());
			Assert.True(share > 0.6, $"Superconductor took {share:P0}");
		}
	}
}
