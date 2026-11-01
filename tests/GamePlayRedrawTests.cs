// CivOne tests
//
// The map is redrawn at most once per presented frame. The same-tick branch of GamePlay knew
// that; the normal per-tick path drew without saying so, and the same-tick branch then drew
// the map again before anything reached the screen. Under Autopilot every draw is a full
// viewport repaint, 11-23 ms, and in a replayed late turn a third of them were these repeats
// (Nov 2026).

using CivOne.Screens;

namespace CivOne.Tests
{
	public class GamePlayRedrawTests
	{
		[Fact]
		public void ADrawOnANewTickCountsForTheFrame()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			var gp = new GamePlay();
			TurnMetrics.AddRenderFrame(TurnMetrics.Now);   // a frame presented since the screen last drew
			int frames = TurnMetrics.Frames;

			Assert.True(gp.Update(5), "scenario is broken: the new tick did not draw");
			bool again = gp.Update(5);

			// The frame counter is shared, and a parallel test's turn resets it; only a run it
			// left alone says anything.
			if (TurnMetrics.Frames == frames)
				Assert.False(again, "drew the map a second time before a frame was presented");
		}
	}
}
