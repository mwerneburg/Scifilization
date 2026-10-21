// CivOne tests
//
// The score traces carry the civ's banner pair, as the political map does (Common.CivPattern):
// six primaries for 17 civs put Russia, Mali and the Mongols in one orange line.

using System.Linq;
using System.Reflection;
using CivOne.Screens.Reports;

namespace CivOne.Tests
{
	public class ScoreTraceColourTests
	{
		private static CivilizationScore AScoreScreen()
		{
			Sim.NewGame(width: 80, height: 50);
			return new CivilizationScore();
		}

		private static void Invoke(object o, string name, params object?[] args) =>
			o.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(o, args);

		// A third of the line in the accent, and the dashes keep their rhythm across a joint:
		// the pattern is by screen position, so two segments read as one line.
		[Fact]
		public void ATraceDashesInItsAccent()
		{
			CivilizationScore s = AScoreScreen();
			const int y = 100, x0 = 60;
			Invoke(s, "DrawLine", x0, y, x0 + 29, y, (byte)13, (byte?)14);
			Invoke(s, "DrawLine", x0 + 29, y, x0 + 59, y, (byte)13, (byte?)14);

			byte[] row = Enumerable.Range(x0, 60).Select(x => s.Bitmap[x, y]).ToArray();
			Assert.Equal(20, row.Count(c => c == 14));
			Assert.Equal(40, row.Count(c => c == 13));
			for (int i = 6; i < row.Length; i++) Assert.Equal(row[i - 6], row[i]);
		}

		// Each legend row leads with a swatch of its civ's pair; the text alone is one colour.
		[Fact]
		public void TheLegendShowsThePair()
		{
			CivilizationScore s = AScoreScreen();
			Invoke(s, "Draw");

			byte pri = s.Bitmap[56, 35], acc = s.Bitmap[60, 35];
			Assert.NotEqual(pri, acc);
			Assert.Contains(CivilizationScore.Plotted(culturePage: false), p =>
				Common.ColourLight[Game.Instance.PlayerNumber(p)] == pri
				&& Common.BannerSecondary[Game.Instance.PlayerNumber(p)] == acc);
		}
	}
}
