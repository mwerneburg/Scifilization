// CivOne tests
//
// The science report, as the user saw it (Nov 2026): the "Researching" label sat inside the
// progress bar, where the bulbs drew over it; the tree's top row shared the scroll hint's row,
// so lines struck through the hint; and a node cut off at the left edge filled a box wider
// than itself, because Bytemap.Fill widened a fill that started left of the canvas.
//
// Then the lines themselves: a prerequisite several columns back ran straight across at its
// own row, behind every box in between, and every line into a column shared one vertical bar,
// so each source on it seemed to feed each target. Rows now sit on one grid, long lines run in
// the strips between rows, each target has its own lane, and joins carry a dot.

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

		private static (int x, int y)[][] Wires(ScienceReport s)
		{
			var wires = (System.Array)typeof(ScienceReport).GetField("_wires", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(s)!;
			FieldInfo pts = wires.GetType().GetElementType()!.GetField("Pts")!;
			return wires.Cast<object>().Select(w => ((int x, int y)[])pts.GetValue(w)!).ToArray();
		}

		private static (byte from, byte to)[] Ends(ScienceReport s)
		{
			var wires = (System.Array)typeof(ScienceReport).GetField("_wires", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(s)!;
			System.Type t = wires.GetType().GetElementType()!;
			return wires.Cast<object>().Select(w => ((byte)t.GetField("From")!.GetValue(w)!, (byte)t.GetField("To")!.GetValue(w)!)).ToArray();
		}

		private static ((int x, int y) p, (int x, int y) q)[] Segments((int x, int y)[] w) => w.Zip(w.Skip(1), (p, q) => (p, q)).ToArray();

		[Fact]
		public void NoLineRunsBehindABox()
		{
			ScienceReport s = Report();
			var boxes = ((System.Array)typeof(ScienceReport).GetField("_nodes", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(s)!)
				.Cast<object>().Select(n => { System.Type t = n.GetType(); int F(string f) => (int)t.GetField(f)!.GetValue(n)!; return (x: F("X"), y: F("Y"), w: F("W"), h: F("H")); }).ToArray();

			foreach (var wire in Wires(s))
				foreach (var (p, q) in Segments(wire))
					foreach (var b in boxes)
					{
						// Overlap of open intervals: touching a box's edge, as a line's two ends do, is fine.
						bool overX = System.Math.Max(System.Math.Min(p.x, q.x), b.x) < System.Math.Min(System.Math.Max(p.x, q.x), b.x + b.w)
							|| (p.x == q.x && p.x > b.x && p.x < b.x + b.w);
						bool overY = System.Math.Max(System.Math.Min(p.y, q.y), b.y) < System.Math.Min(System.Math.Max(p.y, q.y), b.y + b.h)
							|| (p.y == q.y && p.y > b.y && p.y < b.y + b.h);
						Assert.False(overX && overY, $"segment {p}-{q} runs through the box at ({b.x},{b.y})");
					}
		}

		// Two lines share a vertical only when they share an end: then the shared run is one
		// box's lines fanning out, or one box's lines gathering, and reads true.
		[Fact]
		public void LinesToDifferentTargetsNeverShareAVertical()
		{
			ScienceReport s = Report();
			var wires = Wires(s);
			var ends = Ends(s);
			for (int i = 0; i < wires.Length; i++)
				for (int j = i + 1; j < wires.Length; j++)
				{
					if (ends[i].from == ends[j].from || ends[i].to == ends[j].to) continue;
					foreach (var (p, q) in Segments(wires[i]).Where(sg => sg.p.x == sg.q.x))
						foreach (var (r, t) in Segments(wires[j]).Where(sg => sg.p.x == sg.q.x && sg.p.x == p.x))
							Assert.False(System.Math.Max(System.Math.Min(p.y, q.y), System.Math.Min(r.y, t.y))
								< System.Math.Min(System.Math.Max(p.y, q.y), System.Math.Max(r.y, t.y)),
								$"lines {ends[i]} and {ends[j]} share the vertical at x={p.x}");
				}
		}

		// Where lines join, a 3x3 dot: its corners are lit, which a plain crossing never lights.
		[Fact]
		public void AJoinShowsADot()
		{
			ScienceReport s = Report();
			var dots = (System.Collections.Generic.HashSet<(int x, int y)>)typeof(ScienceReport)
				.GetField("_dots", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(s)!;
			int top = Field<int>(s, "_nodesTop"), scroll = Field<int>(s, "_scrollX");
			var seen = dots.Select(d => (x: d.x - scroll, y: d.y + top))
				.Where(d => d.x > 0 && d.x < s.Bitmap.Width - 1 && d.y > top && d.y < s.Bitmap.Height - 1).ToArray();
			Assert.NotEmpty(seen);
			typeof(ScienceReport).GetMethod("Redraw", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(s, null);
			foreach (var (x, y) in seen)
				Assert.True(s.Bitmap[x - 1, y - 1] != CassetteTheme.BG0 && s.Bitmap[x + 1, y + 1] != CassetteTheme.BG0, $"no dot at ({x},{y})");
		}
	}
}
