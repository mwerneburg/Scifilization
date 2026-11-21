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
using System.Drawing;
using System.IO;
using CivOne.Enums;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Tiles;

namespace CivOne.Graphics
{
	public class Resources
	{
		private static Settings Settings => Settings.Instance;

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private readonly Dictionary<string, Bytemap> _textCache = new();
		private readonly IFont _defaultFont = new DefaultFont();
		private readonly GlyphFont? _font = GlyphFont.Load();
		private readonly GlyphFont? _headline = GlyphFont.Load(GlyphFont.HeadlineResourceName);
		private readonly Dictionary<Direction, IBitmap> _fog = new();
		
		internal void ClearTextCache() => _textCache.Clear();
		
		public bool ValidCharacter(int fontId, char c)
		{
			byte asciiChar = (byte)c;
			return (asciiChar >= Font(fontId).FirstChar && asciiChar <= Font(fontId).LastChar);
		}
		
		public Size GetTextSize(int font, string text)
		{
			int width = 0, height = 0;
			foreach (char c in text)
			{
				Size size = GetLetterSize(font, c);
				width += size.Width + 1;
				if (height < size.Height) height = size.Height;
			}
			return new Size(width, height);
		}
		
		public Picture GetText(string text, int font, byte colour)
		{
			return GetText(text, font, colour, colour);
		}
		
		public Picture GetText(string text, int font, byte colourFirstLetter, byte colour)
		{
			if (text is null) text = "[MISSING STRING]";

			List<Bytemap> letters = new();
			bool isFirstLetter = true;
			foreach (char c in text)
			{
				letters.Add(GetLetter(isFirstLetter ? colourFirstLetter : colour, font, c));
				isFirstLetter = false;
			}
			
			int width = 0, height = 0;
			foreach (Bytemap letter in letters)
			{
				width += letter.Width + 1;
				if (height < letter.Height) height = letter.Height;
			}
			
			Picture output = new Picture(width, height);
			
			int xx = 0;
			foreach (Bytemap letter in letters)
			{
				output.AddLayer(letter, xx, 0);
				xx += letter.Width + 1;
			}
			
			return output;
		}
		
		internal Size GetLetterSize(int font, char letter) => GetLetter(5, font, letter).Size;

		// The font ids callers pass are the DOS FONTS.CV's eight faces, which no longer load
		// (Nov 2026). Ids 4 (the title) and 5 (the city, discovery and conquest headlines,
		// whose callers space lines 15 pixels apart) get the 16-pixel headline face; every
		// other id is the text face.
		private GlyphFont? Glyphs(int font) => font is 4 or 5 ? _headline ?? _font : _font;
		private IFont Font(int font) => (IFont?)Glyphs(font) ?? _defaultFont;
		
		public int GetFontHeight(int font)
		{
			return Font(font).FontHeight;
		}
		
		private Bytemap GetLetter(byte colour, int font, char letter)
		{
			string key = $"letter{colour}|{font}|{letter}";
			if (!_textCache.ContainsKey(key))
			{
				// Icons first, then Pixel Operator for ASCII, then DefaultFont's symbols (★, ♥).
				IFont face = Glyphs(font) is GlyphFont g && !g.Has(letter) ? _defaultFont : Font(font);
				_textCache.Add(key, IconGlyphs.Get(letter, colour, Font(font).FontHeight) ?? face.GetLetter(letter, colour));
			}
			return _textCache[key];
		}

		
		// Runtime-injected images (set by the SDL/API layer at startup)
		public static IBitmap SpacedockImage { get; set; } = null!;
		public static SplashData SplashRawImage { get; set; } = null!;

		private static Resources _instance = null!;
		public static Resources Instance
		{
			get
			{
				if (_instance is null)
				{
					_instance = new Resources();
				}
				return _instance;
			}
		}

		public static void ClearInstance()
		{
			_instance = null!;
			TextFile.ClearInstance();
			Sprites.Cursor.ClearCache();
		}
		
		private Resources()
		{
		}
	}
}