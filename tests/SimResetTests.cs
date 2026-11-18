// CivOne tests
//
// A fresh test game starts with Autopilot off. It is a static setting that more than twenty
// tests switch on and never off, and it hands the human's cities to the AI, so the run order
// decided whether tests that need the human's own rules passed (Nov 2026: the lost-wonder
// banking failed only in the full suite).

namespace CivOne.Tests
{
	public class SimResetTests
	{
		[Fact]
		public void ANewTestGameStartsWithAutopilotOff()
		{
			Sim.EnsureRuntime();
			Settings.Instance.Autopilot = true;

			Sim.NewGame(width: 80, height: 50);

			Assert.False(Settings.Instance.Autopilot);
		}
	}
}
