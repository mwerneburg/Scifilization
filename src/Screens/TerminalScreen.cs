// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens
{
	// Base class for the MU/TH/R–style typewriter terminal screens.
	// Derived classes supply _lines and override ColorFor(lineIndex, text).
	[Expand, Modal]
	internal abstract class TerminalScreen : BaseScreen
	{
		protected const int FONT_ID = 0;
		protected const int PAD     = 10;

		// 2 chars per frame at ~15 frames/sec ≈ 30 chars/sec
		private const int CHARS_PER_TICK = 2;
		// Cursor blinks every N frames (half-period)
		private const int CURSOR_BLINK_HALF = 8;

		protected string[] _lines = null!;

		private int  _totalChars;   // sum of all (line.Length + 1) — +1 is the EOL pause token
		private int  _printedChars;
		private bool _complete;     // all characters typed
		private bool _dirty;        // needs a re-render while complete (e.g. after scroll)
		private int  _scrollY;
		private uint _curTick;

		// ── subclass contract ─────────────────────────────────────────────────

		protected abstract byte ColorFor(int lineIndex, string text);

		// Called just before the screen destroys itself (typing complete + dismiss).
		// Override in derived classes that need to launch the next screen.
		protected virtual void OnDismiss() { }

		// A full-screen picture before a line, as the original game's intro had between its
		// passages: the art key (event_art/<key>.png) and its caption, or null. A picture whose
		// file is missing is passed over, so the log reads as before until the art is in.
		protected virtual (string key, string caption)? ArtBefore(int lineIndex) => null;

		private int  _artLine = -1;   // the last line whose picture was considered
		private bool _artShowing;

		// ── typewriter state ──────────────────────────────────────────────────

		// Returns the line index and how many chars of that line have been printed.
		private void GetPosition(int printed, out int lineIdx, out int charInLine)
		{
			for (int i = 0; i < _lines.Length; i++)
			{
				int token = _lines[i].Length + 1; // EOL pause counts as 1
				if (printed <= _lines[i].Length)
				{
					lineIdx    = i;
					charInLine = printed;
					return;
				}
				if (printed < token)
				{
					// In the EOL pause — cursor at end of this line
					lineIdx    = i;
					charInLine = _lines[i].Length;
					return;
				}
				printed -= token;
			}
			lineIdx    = _lines.Length;
			charInLine = 0;
		}

		// Stops at the start of the next line with a picture, and shows it; typing goes on
		// when it is dismissed. The two-character step can carry past a line's start, so the
		// log is wound back to it.
		private bool ShowArtFromHere()
		{
			GetPosition(_printedChars, out int reach, out _);
			for (int i = _artLine + 1; i <= reach && i < _lines.Length; i++)
			{
				_artLine = i;
				if (ArtBefore(i) is not { } scene || EventArtScreen.FindPath(scene.key) is not string path) continue;
				_printedChars = LineStart(i);
				_artShowing = true;
				var art = new EventArtScreen(path, scene.caption, scene.key);
				art.Closed += (s, a) => _artShowing = false;
				Common.AddScreen(art);
				return true;
			}
			return false;
		}

		private int LineStart(int lineIndex)
		{
			int start = 0;
			for (int i = 0; i < lineIndex; i++) start += _lines[i].Length + 1;
			return start;
		}

		private void SkipToEnd()
		{
			_printedChars = _totalChars;
			_complete     = true;
			_dirty        = true;
		}

		// ── drawing ───────────────────────────────────────────────────────────

		private void Redraw()
		{
			int fh    = Resources.GetFontHeight(FONT_ID);
			int bodyH = Height - PAD * 2;

			GetPosition(_printedChars, out int curLine, out int curCol);

			// Auto-scroll during typing: keep the current line visible
			if (!_complete)
			{
				int curLineY = curLine * fh - _scrollY;
				if (curLineY < 0)
					_scrollY = curLine * fh;
				else if (curLineY + fh > bodyH)
					_scrollY = curLine * fh - bodyH + fh;
			}
			else
			{
				int maxScroll = Math.Max(0, _lines.Length * fh - bodyH);
				_scrollY = Math.Max(0, Math.Min(_scrollY, maxScroll));
			}

			this.FillRectangle(0, 0, Width, Height, CassetteTheme.BG0);
			this.DrawRectangle(2, 2, Width - 4, Height - 4, CassetteTheme.BORDER);

			bool cursorVisible = !_complete && (_curTick / CURSOR_BLINK_HALF) % 2 == 0;

			int y = PAD - _scrollY;
			for (int i = 0; i < _lines.Length; i++, y += fh)
			{
				if (y + fh < PAD || y >= Height - PAD) continue;

				bool lineFullyPrinted = (i < curLine) || (i == curLine && curCol == _lines[i].Length && _complete);
				bool linePrinting     = (i == curLine && !_complete);
				bool lineUnprinted    = (i > curLine) || (i == curLine && curCol == 0 && i > 0);

				if (i > curLine) continue; // not reached yet

				string text = _lines[i];

				if (lineFullyPrinted || (i < curLine))
				{
					this.DrawText(text, FONT_ID, ColorFor(i, text), PAD + 4, y);
				}
				else if (linePrinting)
				{
					// Draw the portion that has been typed
					string printed = text.Substring(0, curCol);
					this.DrawText(printed, FONT_ID, ColorFor(i, text), PAD + 4, y);

					// Blinking block cursor after last printed char
					if (cursorVisible)
					{
						int cursorX = PAD + 4 + Resources.GetTextSize(FONT_ID, printed).Width;
						this.FillRectangle(cursorX, y, Resources.GetTextSize(FONT_ID, "_").Width + 1, fh - 1,
						                   CassetteTheme.PHOS_GLOW);
					}
				}
				// i > curLine: not reached, draw nothing
			}

			// Footer
			if (_complete)
			{
				int maxScroll = Math.Max(0, _lines.Length * fh - bodyH);
				if (maxScroll > 0)
				{
					int pct = (int)(100.0 * _scrollY / maxScroll);
					this.DrawText($"[ ↑↓ TO SCROLL  {pct}%  ANY KEY DISMISSES ]",
					              FONT_ID, CassetteTheme.INK_LOW, Width / 2, Height - PAD + 1, TextAlign.Center);
				}
				else
				{
					this.DrawText("[ ANY KEY OR CLICK TO DISMISS ]",
					              FONT_ID, CassetteTheme.INK_LOW, Width / 2, Height - PAD + 1, TextAlign.Center);
				}
			}
			else
			{
				this.DrawText("[ ANY KEY TO SKIP ]",
				              FONT_ID, CassetteTheme.INK_LOW, Width / 2, Height - PAD + 1, TextAlign.Center);
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			_curTick = gameTick;

			if (_complete)
			{
				if (!_dirty) return false;
				_dirty = false;
				Redraw();
				return true;
			}

			if (_artShowing) return false;
			if (ShowArtFromHere()) return false;

			_printedChars = Math.Min(_totalChars, _printedChars + CHARS_PER_TICK);
			if (_printedChars >= _totalChars)
			{
				_complete = true;
				Redraw();
				return true;
			}

			Redraw();
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (!_complete)
			{
				SkipToEnd();
				return true;
			}

			int fh    = Resources.GetFontHeight(FONT_ID);
			int bodyH = Height - PAD * 2;
			int maxScroll = Math.Max(0, _lines.Length * fh - bodyH);

			if (maxScroll > 0 && (args.Key == Key.Up || args.Key == Key.NumPad8))
			{
				_scrollY = Math.Max(0, _scrollY - fh * 3);
				_dirty = true;
				return true;
			}
			if (maxScroll > 0 && (args.Key == Key.Down || args.Key == Key.NumPad2))
			{
				_scrollY = Math.Min(maxScroll, _scrollY + fh * 3);
				_dirty = true;
				return true;
			}

			OnDismiss();
			Destroy();
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (!_complete)
			{
				SkipToEnd();
				return true;
			}
			OnDismiss();
			Destroy();
			return true;
		}

		// ── constructor ───────────────────────────────────────────────────────

		protected TerminalScreen() : base(MouseCursor.Pointer)
		{
			using Palette p = Common.DefaultPalette;
			using (Palette cassette = CassetteTheme.CreatePalette())
				p.MergePalette(cassette, 1, 17);
			Palette = p;
		}

		// Call after _lines is populated in the derived constructor.
		protected void InitTypewriter()
		{
			_totalChars = 0;
			foreach (string line in _lines)
				_totalChars += line.Length + 1;
		}
	}
}
