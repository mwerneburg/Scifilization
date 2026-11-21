// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Graphics.Sprites;

namespace CivOne.Screens
{
	public abstract partial class BaseScreen : IDefaultTextSettings
	{
		public TextSettings? DefaultTextSettings { get; set; }

		protected int Width => Bitmap.Width;
		protected int Height => Bitmap.Height;

		private Bytemap _bitmap = null!;
		public Bytemap Bitmap
		{
			get
			{
				return _bitmap;
			}
			protected set
			{
				_bitmap?.Dispose();
				_bitmap = value;
			}
		}
		private Palette _palette = null!, _originalColours = null!;
		public Palette Palette
		{
			get
			{
				return _palette;
			}
			set
			{
				// Dispose the previous palette before overwriting. Palette wraps an
				// unmanaged SDL handle; without this, every screen leaks one Palette per
				// assignment (the InitDefaultPalette copy → subclass's first assignment,
				// plus any later mutations). ReferenceEquals guards the no-op self-assign
				// case so we don't dispose our own backing field mid-setter.
				if (!ReferenceEquals(_palette, value)) _palette?.Dispose();
				_palette = value.Copy();
				if (_originalColours is null)
					_originalColours = value.Copy();
			}
		}
		public Palette OriginalColours => _originalColours;
		public void SetOriginalColours() => _originalColours.MergePalette(_palette);
		
		protected void DrawPanel(int x, int y, int width, int height, bool border = true)
		{
			int xx = x, yy = y, ww = width, hh = height;
			if (border)
			{
				xx++;
				yy++;
				ww -= 2;
				hh -= 2;
				this.DrawRectangle(x, y, width, height);
			}
			this.AddLayer(new Picture(ww, hh)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D(), xx, yy, dispose: true);
		}

		protected void DrawBorder(int border)
		{
			// Procedural cassette frame: bright top/left edge, dark bottom/right.
			const byte hi = 7, lo = 5;   // INK_MID / BORDER
			this.FillRectangle(0, 0, Width, 1, hi)
				.FillRectangle(0, 0, 1, Height, hi)
				.FillRectangle(0, Height - 1, Width, 1, lo)
				.FillRectangle(Width - 1, 0, 1, Height, lo);
		}

		protected void DrawButton(string text, byte fontId, byte colour, byte colourDark, int x, int y, int width, int height)
		{
			this.FillRectangle(x, y, width, 1, 7)
				.FillRectangle(x, y + 1, 1, (height - 1), 7)
				.FillRectangle(x + 1, y + (height - 1), width - 1, 1, colourDark)
				.FillRectangle(x + width - 1, y, 1, (height - 1), colourDark)
				.FillRectangle(x + 1, y + 1, width - 2, (height - 2), colour)
				.DrawText(text, fontId, colourDark, x + (int)Math.Ceiling((double)width / 2), y + 2, TextAlign.Center);
		}

		protected void DrawButton(string text, byte colour, byte colourDark, int x, int y, int width) => DrawButton(text, 1, colour, colourDark, x, y, width, Resources.GetFontHeight(1) + 3);

		public void ResetPalette()
		{
			for (int i = 0; i < Palette.Length && i < OriginalColours.Length && i < 256; i++)
				Palette[i] = OriginalColours[i];
		}

		public void SetPalette(Palette palette)
		{
			for (int i = 1; i < palette.Length && i < 256; i++)
				Palette[i] = palette[i];
		}

		public virtual void Dispose()
		{
			Bitmap?.Dispose();
			Palette?.Dispose();
			OriginalColours?.Dispose();
		}
	}
}