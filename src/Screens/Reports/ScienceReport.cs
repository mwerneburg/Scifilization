// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens.Reports
{
	internal class ScienceReport : BaseReport
	{
		// ── node data ────────────────────────────────────────────────────────────────

		private struct TechNode
		{
			public IAdvance Advance;
			public int X, Y, W, H;
		}

		// One prerequisite line as a run of horizontal and vertical segments.
		private struct Wire
		{
			public byte From, To;
			public (int x, int y)[] Pts;
		}

		private TechNode[]              _nodes = null!;
		private Wire[]                  _wires = null!;
		private HashSet<(int x, int y)> _dots  = null!;   // where wires join: a dot, as on a circuit diagram
		private Dictionary<byte, int>   _nodeIdx = null!;   // advance ID → index into _nodes
		private int _treeW, _treeTop, _scrollX;
		private int _nodesTop;   // below the scroll hint: the tree never draws over it
		private bool _dirty;
		private IAdvance? _selected;      // node clicked for path highlight
		private HashSet<byte>? _pathIds; // ancestor IDs of _selected (including itself)

		// layout constants (computed from font)
		private int _fontH, _nodeH, _rowH, _colW, _nodeW;
		private const int PAD_X = 4, PAD_Y = 2, CONNECTOR = 18, ROW_GAP = 14, LANE_MARGIN = 6, LANE_STEP = 4;

		// ── state helpers ─────────────────────────────────────────────────────────

		private bool PlayerHas(IAdvance a)    => Human.Advances.Any(h => h.Id == a.Id);
		private bool IsResearching(IAdvance a) => Human.CurrentResearch?.Id == a.Id;
		private bool CanResearch(IAdvance a)   => !PlayerHas(a) && a.RequiredTechs.All(r => PlayerHas(r));

		// All ancestors of target (prerequisites, recursively), including target itself.
		private HashSet<byte> ComputePath(IAdvance target)
		{
			var ids = new HashSet<byte>();
			void Recurse(IAdvance a)
			{
				if (!ids.Add(a.Id)) return;
				foreach (var req in a.RequiredTechs) Recurse(req);
			}
			Recurse(target);
			return ids;
		}

		// ── layout ───────────────────────────────────────────────────────────────────

		private void BuildLayout()
		{
			_fontH = Resources.GetFontHeight(0);
			_nodeH = _fontH + PAD_Y * 2;
			_rowH  = _nodeH + ROW_GAP;

			var all = Common.Advances.Where(a => !(a is FutureTech)).ToArray();

			// Topological depth: longest prerequisite chain to this node.
			var depth = all.ToDictionary(a => a.Id, _ => 0);
			for (bool changed = true; changed;)
			{
				changed = false;
				foreach (var a in all)
				{
					if (a.RequiredTechs.Length == 0) continue;
					int d = a.RequiredTechs
					         .Where(r => depth.ContainsKey(r.Id))
					         .Select(r => depth[r.Id] + 1)
					         .DefaultIfEmpty(1).Max();
					if (d == depth[a.Id]) continue;
					depth[a.Id] = d;
					changed = true;
				}
			}

			// Uniform node width: fit the widest tech name.
			_nodeW = all.Max(a => Resources.GetTextSize(0, a.Name).Width) + PAD_X * 2;
			_colW  = _nodeW + CONNECTOR;

			// Group by depth, sort within each column by tech ID for stability.
			var cols = all.GroupBy(a => depth[a.Id])
			              .OrderBy(g => g.Key)
			              .Select(g => g.OrderBy(a => a.Id).ToArray())
			              .ToArray();

			int maxRows = cols.Max(g => g.Length);

			// Rows sit on one grid across all columns, so the strip between two rows is empty
			// the whole width of the tree: long wires run along those strips, never behind a box.
			var row = new Dictionary<byte, int>(all.Length);
			var colOf = new Dictionary<byte, int>(all.Length);
			for (int c = 0; c < cols.Length; c++)
				for (int r = 0; r < cols[c].Length; r++)
				{
					row[cols[c][r].Id] = (maxRows - cols[c].Length) / 2 + r;
					colOf[cols[c][r].Id] = c;
				}

			var edges = all.SelectMany(a => a.RequiredTechs.Where(q => depth.ContainsKey(q.Id)).Select(q => (from: q.Id, to: a.Id))).ToList();

			// Each gap holds a lane per source leaving it on a long wire, then a lane per target
			// entering the next column, so wires to different targets never share a vertical.
			var lane = new Dictionary<(int gap, byte id), int>();
			int[] gapW = new int[cols.Length];
			for (int c = 1; c < cols.Length; c++)
			{
				int i = 0;
				foreach (byte id in edges.Where(e => colOf[e.from] == c - 1 && colOf[e.to] > c).Select(e => e.from).Distinct().OrderBy(id => row[id]))
					lane[(c, id)] = i++;
				foreach (byte id in edges.Where(e => colOf[e.to] == c).Select(e => e.to).Distinct().OrderBy(id => row[id]))
					lane[(c, id)] = i++;
				gapW[c] = Math.Max(CONNECTOR, LANE_MARGIN * 2 + LANE_STEP * (i - 1));
			}
			int[] colX = new int[cols.Length];
			for (int c = 1; c < cols.Length; c++) colX[c] = colX[c - 1] + _nodeW + gapW[c];
			int LaneX(int gap, byte id) => colX[gap - 1] + _nodeW + LANE_MARGIN + LANE_STEP * lane[(gap, id)];
			int NodeY(byte id) => ROW_GAP + row[id] * _rowH;
			// A box's wires leave a little low and arrive a little high, so a wire out of one box
			// never runs on into the box beside it in the next column.
			int OutY(byte id) => NodeY(id) + _nodeH / 2 + 1;
			int InY(byte id)  => NodeY(id) + _nodeH / 2 - 2;

			var nodes   = new List<TechNode>(all.Length);
			_nodeIdx    = new Dictionary<byte, int>(all.Length);
			foreach (var a in all.OrderBy(a => colOf[a.Id]).ThenBy(a => row[a.Id]))
			{
				_nodeIdx[a.Id] = nodes.Count;
				nodes.Add(new TechNode { Advance = a, X = colX[colOf[a.Id]], Y = NodeY(a.Id), W = _nodeW, H = _nodeH });
			}
			_nodes = nodes.ToArray();
			_treeW = colX[cols.Length - 1] + _nodeW;

			// Long wires: out to the source's lane, along the strip between two rows, into the
			// target's lane. Strip k lies above row k; each has three tracks, given out first-come
			// so two wires never overlap along one.
			var taken = new Dictionary<(int strip, int track), List<(int x0, int x1)>>();
			int[] trackDy = { 0, -4, 4 };
			var wires = new List<Wire>(edges.Count);
			foreach (var e in edges.OrderBy(e => colOf[e.from]).ThenBy(e => row[e.from]).ThenBy(e => row[e.to]))
			{
				int a = colOf[e.from], b = colOf[e.to];
				int x0 = colX[a] + _nodeW, inX = LaneX(b, e.to);
				(int x, int y)[] pts;
				if (b == a + 1)
					pts = new[] { (x0, OutY(e.from)), (inX, OutY(e.from)), (inX, InY(e.to)), (colX[b], InY(e.to)) };
				else
				{
					int outX = LaneX(a + 1, e.from);
					double ra = row[e.from] + 0.5, rb = row[e.to] + 0.5;
					int stripY = 0;
					bool placed = false;
					foreach (int k in Enumerable.Range(0, maxRows + 1).OrderBy(k => Math.Abs(k - ra) + Math.Abs(k - rb)).ThenBy(k => Math.Abs(k - rb)))
					{
						foreach (int t in Enumerable.Range(0, trackDy.Length))
						{
							var used = taken.TryGetValue((k, t), out var u) ? u : taken[(k, t)] = new List<(int, int)>();
							if (used.Any(iv => iv.x0 <= inX + LANE_STEP && outX <= iv.x1 + LANE_STEP)) continue;
							used.Add((outX, inX));
							stripY = ROW_GAP + k * _rowH - ROW_GAP / 2 + trackDy[t];
							placed = true;
							break;
						}
						if (placed) break;
					}
					// ponytail: every track full, share the nearest strip's middle; none was in the shipped tree.
					if (!placed) stripY = ROW_GAP + (int)Math.Round(rb) * _rowH - ROW_GAP / 2;
					pts = new[] { (x0, OutY(e.from)), (outX, OutY(e.from)), (outX, stripY), (inX, stripY), (inX, InY(e.to)), (colX[b], InY(e.to)) };
				}
				wires.Add(new Wire { From = e.from, To = e.to, Pts = pts });
			}
			_wires = wires.ToArray();

			// Where three or more wire ends meet, the wires join, and a dot says so; two wires
			// that only cross get none.
			var segs = _wires.SelectMany(w => w.Pts.Zip(w.Pts.Skip(1), (p, q) => (p, q))).Distinct().ToArray();
			_dots = new HashSet<(int x, int y)>();
			foreach (var pt in _wires.SelectMany(w => w.Pts).Distinct())
			{
				var ways = new HashSet<(int, int)>();
				foreach (var (p, q) in segs)
				{
					bool onH = p.y == q.y && pt.y == p.y && pt.x >= Math.Min(p.x, q.x) && pt.x <= Math.Max(p.x, q.x);
					bool onV = p.x == q.x && pt.x == p.x && pt.y >= Math.Min(p.y, q.y) && pt.y <= Math.Max(p.y, q.y);
					if (!onH && !onV) continue;
					foreach (var end in new[] { p, q })
						if (end != pt) ways.Add((Math.Sign(end.x - pt.x), Math.Sign(end.y - pt.y)));
				}
				if (ways.Count >= 3) _dots.Add(pt);
			}
		}

		// ── line helpers (clipped to tree area) ───────────────────────────────────

		private void HLine(int x1, int y, int x2, byte col)
		{
			if (x1 > x2) { int t = x1; x1 = x2; x2 = t; }
			x1 = Math.Max(0, x1);
			x2 = Math.Min(Width - 1, x2);
			if (y < _nodesTop || y >= Height || x1 > x2) return;
			this.FillRectangle(x1, y, x2 - x1 + 1, 1, col);
		}

		private void VLine(int x, int y1, int y2, byte col)
		{
			if (y1 > y2) { int t = y1; y1 = y2; y2 = t; }
			y1 = Math.Max(_nodesTop, y1);
			y2 = Math.Min(Height - 1, y2);
			if (x < 0 || x >= Width || y1 > y2) return;
			this.FillRectangle(x, y1, 1, y2 - y1 + 1, col);
		}

		// ── edge drawing ──────────────────────────────────────────────────────────

		private byte WireColour(Wire w)
		{
			if (_pathIds != null)
				return _pathIds.Contains(w.From) && _pathIds.Contains(w.To) ? CassetteTheme.PHOS_GLOW : CassetteTheme.BG2;
			bool from = Human.Advances.Any(h => h.Id == w.From), to = Human.Advances.Any(h => h.Id == w.To);
			return from && to ? CassetteTheme.PHOS_DIM : from ? CassetteTheme.INK_MID : CassetteTheme.BG3;
		}

		// Verticals, then horizontals, then the junction dots; a highlighted
		// path is drawn after the rest so nothing dims it.
		private void DrawWires()
		{
			var order = _wires.OrderBy(w => _pathIds != null && _pathIds.Contains(w.From) && _pathIds.Contains(w.To)).ToArray();
			foreach (bool vertical in new[] { true, false })
			{
				foreach (Wire w in order)
				{
					byte col = WireColour(w);
					for (int i = 1; i < w.Pts.Length; i++)
					{
						var (p, q) = (w.Pts[i - 1], w.Pts[i]);
						if (vertical && p.x == q.x) VLine(p.x - _scrollX, p.y + _nodesTop, q.y + _nodesTop, col);
						if (!vertical && p.y == q.y) HLine(p.x - _scrollX, p.y + _nodesTop, q.x - _scrollX, col);
					}
				}
			}
			foreach (Wire w in order)
				foreach (var pt in w.Pts.Where(_dots.Contains))
					for (int d = -1; d <= 1; d++)
						HLine(pt.x - _scrollX - 1, pt.y + _nodesTop + d, pt.x - _scrollX + 1, WireColour(w));
		}

		// ── node drawing ──────────────────────────────────────────────────────────

		private void DrawNode(TechNode node)
		{
			int sx = node.X - _scrollX;
			int sy = node.Y + _nodesTop;
			if (sx + node.W < 0 || sx >= Width)         return;
			if (sy + node.H < _nodesTop || sy >= Height) return;

			bool onPath   = _pathIds != null && _pathIds.Contains(node.Advance.Id);
			bool isTarget = _selected != null && node.Advance.Id == _selected.Id;

			byte bg, fg, border;
			if (isTarget)
			{
				bg     = CassetteTheme.PHOS;
				fg     = CassetteTheme.BG0;
				border = CassetteTheme.PHOS_GLOW;
			}
			else if (_pathIds != null && onPath && !PlayerHas(node.Advance))
			{
				bg     = CassetteTheme.BG1;
				fg     = CassetteTheme.PHOS_GLOW;
				border = CassetteTheme.PHOS;
			}
			else if (_pathIds != null && onPath && PlayerHas(node.Advance))
			{
				bg     = CassetteTheme.BG2;
				fg     = CassetteTheme.PHOS_DIM;
				border = CassetteTheme.BORDER;
			}
			else if (_pathIds != null)
			{
				// Off-path: dim everything
				bg     = CassetteTheme.BG0;
				fg     = CassetteTheme.BG3;
				border = CassetteTheme.BG1;
			}
			else if (IsResearching(node.Advance))
			{
				bg     = CassetteTheme.PHOS_DIM;
				fg     = CassetteTheme.PHOS_GLOW;
				border = CassetteTheme.BORDER;
			}
			else if (PlayerHas(node.Advance))
			{
				bg     = CassetteTheme.BG2;
				fg     = Game.GetAdvanceOrigin(node.Advance, Human)
				       ? CassetteTheme.PHOS : CassetteTheme.INK_HIGH;
				border = CassetteTheme.BORDER;
			}
			else if (CanResearch(node.Advance))
			{
				bg     = CassetteTheme.BG1;
				fg     = CassetteTheme.INK_MID;
				border = CassetteTheme.BORDER;
			}
			else
			{
				bg     = CassetteTheme.BG0;
				fg     = CassetteTheme.INK_LOW;
				border = CassetteTheme.BORDER;
			}

			this.FillRectangle(sx, sy, node.W, node.H, bg);
			this.DrawRectangle(sx, sy, node.W, node.H, border);
			this.DrawText(node.Advance.Name, 0, fg, sx + PAD_X, sy + PAD_Y);
		}

		// ── scroll hint ───────────────────────────────────────────────────────────

		private void DrawScrollHint()
		{
			if (_treeW <= Width) return;
			int maxScroll = _treeW - Width;
			int pct = maxScroll > 0 ? (int)(100.0 * _scrollX / maxScroll) : 0;
			string hint = _selected != null
				? $"[ < > scroll  {pct}%  click tech to clear  any key closes ]"
				: $"[ < > scroll  {pct}%  click a tech to show its path ]";
			this.DrawText(hint, 0, CassetteTheme.INK_LOW, Width / 2, _treeTop + 1, TextAlign.Center);
		}

		// ── full redraw ───────────────────────────────────────────────────────────

		private void Redraw()
		{
			this.FillRectangle(0, _treeTop, Width, Height - _treeTop, CassetteTheme.BG0);

			DrawScrollHint();

			// Draw legend at bottom-right
			int ly = Height - _fontH - 3;
			int lx = Width - 4;
			this.DrawText("known (yours)", 0, CassetteTheme.PHOS,     lx, ly, TextAlign.Right); ly -= _fontH + 1;
			this.DrawText("known (traded)", 0, CassetteTheme.INK_HIGH, lx, ly, TextAlign.Right); ly -= _fontH + 1;
			this.DrawText("available",      0, CassetteTheme.INK_MID,  lx, ly, TextAlign.Right); ly -= _fontH + 1;
			this.DrawText("locked",         0, CassetteTheme.INK_LOW,  lx, ly, TextAlign.Right);

			// Wires behind nodes
			DrawWires();

			// Nodes on top
			foreach (var node in _nodes)
				DrawNode(node);

			_dirty = false;
		}

		// ── update gate ───────────────────────────────────────────────────────────

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_dirty) return false;
			Redraw();
			return true;
		}

		// ── input ─────────────────────────────────────────────────────────────────

		public override bool KeyDown(KeyboardEventArgs args)
		{
			int maxScroll = Math.Max(0, _treeW - Width);
			if (args.Key == Key.Left || args.Key == Key.NumPad4)
			{
				_scrollX = Math.Max(0, _scrollX - _colW);
				_dirty = true;
				return true;
			}
			if (args.Key == Key.Right || args.Key == Key.NumPad6)
			{
				_scrollX = Math.Min(maxScroll, _scrollX + _colW);
				_dirty = true;
				return true;
			}
			if (_selected != null)
			{
				_selected = null;
				_pathIds  = null;
				_dirty    = true;
				return true;
			}
			Destroy();
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			foreach (var node in _nodes)
			{
				int sx = node.X - _scrollX;
				int sy = node.Y + _nodesTop;
				if (args.X >= sx && args.X < sx + node.W &&
				    args.Y >= sy && args.Y < sy + node.H)
				{
					if (_selected?.Id == node.Advance.Id)
					{
						_selected = null;
						_pathIds  = null;
					}
					else
					{
						_selected = node.Advance;
						_pathIds  = ComputePath(node.Advance);
					}
					_dirty = true;
					return true;
				}
			}
			// Click on empty space: clear selection, or close if nothing was selected.
			if (_selected != null)
			{
				_selected = null;
				_pathIds  = null;
				_dirty    = true;
				return true;
			}
			Destroy();
			return true;
		}

		// ── constructor ───────────────────────────────────────────────────────────

		public ScienceReport() : base("SCIENCE REPORT", 1, MouseCursor.Pointer)
		{
			// Research progress bar (positioned just below the header)
			if (Human.CurrentResearch is not null)
			{
				double w = 8.0;
				while (w * Human.ScienceCost > Width - 40 && w > 0.1) w -= 0.1;
				int barW = (int)Math.Ceiling(w * Human.ScienceCost);
				int barX = (Width - barW) / 2;

				this.FillRectangle(barX, 28, barW, 10, 9);
				// The label goes under the bar: inside it, the bulbs drew over it as research grew.
				const int labelY = 40;
				this.DrawText($"Researching: {Human.CurrentResearch.Name}", 0, CassetteTheme.PHOS_GHOST, Width / 2, labelY, TextAlign.Center)
				    .DrawText($"Researching: {Human.CurrentResearch.Name}", 0, CassetteTheme.PHOS_GLOW,  Width / 2 - 1, labelY, TextAlign.Center);

				int plotX = -1;
				for (int i = 0; i < Human.Science; i++)
				{
					int nx = (int)Math.Floor(w * i) + barX;
					if (nx == plotX) continue;
					plotX = nx;
					this.AddLayer(Icons.Science, nx, 30);
				}
				_treeTop = labelY + Resources.GetFontHeight(0) + 2;
			}
			else
			{
				_treeTop = 30;
			}

			BuildLayout();
			_nodesTop = _treeTop + _fontH + 3;

			// Start scroll so the current research (or earliest locked tech) is in view.
			if (Human.CurrentResearch is not null &&
			    _nodeIdx.TryGetValue(Human.CurrentResearch.Id, out int idx))
			{
				_scrollX = Math.Max(0, _nodes[idx].X - Width / 3);
			}

			_dirty = true;
		}
	}
}
