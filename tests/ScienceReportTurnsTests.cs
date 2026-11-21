// CivOne tests
//
// The turns per advance were only on the trade report; the science report now shows them
// under the research bar, with the turns left on the current one (the user, Nov 2026).

using CivOne.Screens.Reports;

namespace CivOne.Tests
{
	public class ScienceReportTurnsTests
	{
		// 25 of 40 bulbs in, 4 a turn: 15 to go is 4 turns; a whole advance is 10.
		[Fact]
		public void ItCountsWhatIsLeftAndAWholeAdvance()
			=> Assert.Equal("4 turns left, 10 per advance", ScienceReport.TurnsText(40, 25, 4));

		[Fact]
		public void OneTurnIsSingular()
			=> Assert.Equal("1 turn left, 10 per advance", ScienceReport.TurnsText(40, 38, 4));

		// At 0% science nothing is ever learned; no line rather than a division by zero.
		[Fact]
		public void NoScienceNoLine()
			=> Assert.Null(ScienceReport.TurnsText(40, 25, 0));
	}
}
