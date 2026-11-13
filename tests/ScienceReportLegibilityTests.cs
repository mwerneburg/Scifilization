// CivOne tests
//
// The science report, as the user saw it (Nov 2026): the "Researching" label sat inside the
// progress bar, where the bulbs drew over it; the tree's top row shared the scroll hint's row,
// so lines struck through the hint; and a node cut off at the left edge filled a box wider
// than itself, because Bytemap.Fill widened a fill that started left of the canvas.

using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Screens.Reports;

namespace CivOne.Tests
{
	public class ScienceReportLegibilityTests
	{
		[Fact]
		public void AFillOffTheTopLeftIsClippedNotWidened()
		{
			var map = new Bytemap(20, 20);
			map.Fill(-5, -3, 10, 6, 7);

			int painted = Enumerable.Range(0, 20).Sum(y => Enumerable.Range(0, 20).Count(x => map[x, y] == 7));
			Assert.Equal(5 * 3, painted);
		}

		private static ScienceReport Report()
		{
			Sim.NewGame(width: 80, height: 50);
			Player h = Game.Instance.HumanPlayer;
			IAdvance target = new TheCorporation();
			void Learn(IAdvance a) { foreach (IAdvance b in a.RequiredTechs) { Learn(b); h.AddAdvance(b, false); } }
			Learn(target);
			h.CurrentResearch = target;
			h.Science = h.ScienceCost * 9 / 10;   // a bar nearly full of bulbs
			return new ScienceReport();
		}

		private static T Field<T>(ScienceReport s, string name) =>
			(T)typeof(ScienceReport).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(s)!;

		private static bool RowsHold(ScienceReport s, int top, int bottom, byte colour) =>
			Enumerable.Range(top, bottom - top).Any(y => Enumerable.Range(0, s.Bitmap.Width).Any(x => s.Bitmap[x, y] == colour));

		// The bar is rows 28-37; the label is drawn in PHOS_GLOW, which nothing else up there uses.
		[Fact]
		public void TheResearchLabelIsClearOfTheBar()
		{
			ScienceReport s = Report();

			Assert.False(RowsHold(s, 28, 38, CassetteTheme.PHOS_GLOW), "label drawn inside the progress bar");
			Assert.True(RowsHold(s, 38, Field<int>(s, "_treeTop"), CassetteTheme.PHOS_GLOW), "no label under the bar");
		}

		// At every scroll position, the hint's row holds only the background and the hint.
		[Fact]
		public void TheTreeNeverDrawsOverTheScrollHint()
		{
			ScienceReport s = Report();
			MethodInfo redraw = typeof(ScienceReport).GetMethod("Redraw", BindingFlags.NonPublic | BindingFlags.Instance)!;
			FieldInfo scroll = typeof(ScienceReport).GetField("_scrollX", BindingFlags.NonPublic | BindingFlags.Instance)!;
			int top = Field<int>(s, "_treeTop"), bottom = top + Field<int>(s, "_fontH") + 3;
			int treeW = Field<int>(s, "_treeW"), colW = Field<int>(s, "_colW");
			Assert.True(treeW > s.Bitmap.Width, "precondition: the tree scrolls, so the hint shows");

			for (int x = 0; x <= treeW - s.Bitmap.Width; x += colW)
			{
				scroll.SetValue(s, x);
				redraw.Invoke(s, null);
				for (int y = top; y < bottom; y++)
					for (int px = 0; px < s.Bitmap.Width; px++)
						Assert.True(s.Bitmap[px, y] is CassetteTheme.BG0 or CassetteTheme.INK_LOW,
							$"scrolled {x}: colour {s.Bitmap[px, y]} at ({px},{y}) in the hint's row");
			}
		}
	}
}
