// CivOne tests
//
// Exit Game → clicking "Y / ENTER - QUIT" kept playing: every click on the dialog dismissed
// it, so the mouse could never quit (reported from the 1.1.0 DMG, Oct 2026). A click on the
// QUIT line now quits; anywhere else still keeps playing.

using CivOne.Events;
using CivOne.Screens.Dialogs;

namespace CivOne.Tests
{
	public class ConfirmQuitMouseTests
	{
		private static int Quits()
		{
			Sim.EnsureRuntime();
			return ((TestRuntime)RuntimeHandler.Runtime).Quits;
		}

		private static int QuitsAfterClickAt(int x, int y)
		{
			int before = Quits();
			new ConfirmQuit().MouseDown(new ScreenEventArgs(x, y));
			return Quits() - before;
		}

		[Fact]
		public void ClickingTheQuitLineQuits() =>
			Assert.Equal(1, QuitsAfterClickAt(80, ConfirmQuit.QuitY + 2));

		[Fact]
		public void ClickingKeepPlayingDoesNotQuit() =>
			Assert.Equal(0, QuitsAfterClickAt(80, ConfirmQuit.KeepY + 2));

		[Fact]
		public void ClickingOutsideTheDialogDoesNotQuit() =>
			Assert.Equal(0, QuitsAfterClickAt(5, 5));
	}
}
