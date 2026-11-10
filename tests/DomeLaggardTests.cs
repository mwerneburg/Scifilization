// CivOne tests
//
// A player last in the tech race was assigned a Dome part whose advance it did not have. The
// part stays off the build list until the advance is learned, and the science brief named the
// part without saying so, so it read as a part the game refused (the user, Nov 2026). A human
// that can build no part now gets the one its research is nearest, and the brief names the
// advance still needed.

using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class DomeLaggardTests
	{
		// The human knows only the road to Computers, so the Command Hub is one advance off and
		// every other part at least two. The one clean fixture: every other part's road passes
		// through, or alongside, another part's advance (Fusion Power needs Superconductors, the
		// Rocketry road holds all Computers needs), and a tie goes to the part more civs know.
		[Fact]
		public void AHumanThatCanBuildNoPartGetsTheNearest()
		{
			const Wonder target = Wonder.DomeCommandHub;
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player human = g.HumanPlayer;
			foreach (IAdvance a in Common.Advances) human.DeleteAdvance(a);
			IAdvance needs = Game.DomeFiveComponents.First(c => (Wonder)c.Id == target).RequiredTech!;
			void Learn(IAdvance a) { foreach (IAdvance before in a.RequiredTechs) { Learn(before); human.AddAdvance(before, false); } }
			Learn(needs);

			Assert.Equal(1, human.AdvancesShortOf(needs));
			foreach (IWonder other in Game.DomeFiveComponents.Where(c => (Wonder)c.Id != target))
				Assert.True(human.AdvancesShortOf(other.RequiredTech) >= 2, $"fixture: {other.Name} is as near as the target");

			g.AssignDomeComponents();

			Assert.Equal(new[] { target }, g.GetDomeAssignments(human));
		}

		[Fact]
		public void TheBriefNamesTheAdvanceStillNeeded()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player human = g.HumanPlayer;
			g.DomeAssignments[g.PlayerNumber(human)] = new List<Wonder> { Wonder.DomeCommandHub };
			string need = $"First we need {new Computers().Name.ToUpper()}.";

			human.DeleteAdvance(new Computers());
			Assert.Contains(need, g.DomeBriefLines()!);

			human.AddAdvance(new Computers(), false);
			Assert.DoesNotContain(g.DomeBriefLines()!, l => l.StartsWith("First we need"));
		}
	}
}
