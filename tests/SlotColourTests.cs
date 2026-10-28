// CivOne tests
//
// A game at 1915 CE had 21 player slots, the Arabs in slot 20, and the colour tables held
// 20. The first Arab city on the minimap indexed ColourLight[20] and crashed the game
// (crash.log, 28 Oct 2026: IndexOutOfRangeException in SideBar.DrawMiniMap). Game.AddPlayer
// has no cap, so the tables wrap.

namespace CivOne.Tests
{
	public class SlotColourTests
	{
		public SlotColourTests() => Sim.EnsureRuntime();

		[Fact]
		public void EverySlotHasAColour()
		{
			for (int slot = 0; slot < 64; slot++)
			{
				_ = Common.ColourLight[slot];
				_ = Common.BannerSecondary[slot];
				_ = Common.ColourDark[slot];
			}
		}

		// The first twenty keep the colours they always had.
		[Fact]
		public void SlotTwentyWrapsToSlotZero()
		{
			Assert.Equal(Common.ColourLight[0], Common.ColourLight[Common.ColourLight.Length]);
		}
	}
}
