// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.IO;
using CivOne.IO;

namespace CivOne.Graphics
{
	// The game's text face: Pixel Operator 8 by Jayvee Enaguas (CC0 1.0), one pixel between
	// letters, stored as editable glyph grids (resources/pixel_operator_8.txt, built by
	// design/build_font.py) and compiled into the assembly, so the game and the tests draw
	// with the same font. It covers printable ASCII; DefaultFont keeps the few symbols above it.
	internal class GlyphFont : IFont
	{
		internal const string ResourceName = "CivOne.pixel_operator_8.txt";

		private readonly Dictionary<char, bool[,]> _glyphs = new();

		public int FontHeight { get; }
		public byte FirstChar => 32;
		public byte LastChar => 126;

		internal bool Has(char c) => _glyphs.ContainsKey(c);

		internal static GlyphFont? Load()
		{
			using Stream? stream = typeof(GlyphFont).Assembly.GetManifestResourceStream(ResourceName);
			if (stream is null) return null;
			using StreamReader reader = new StreamReader(stream);
			return new GlyphFont(reader.ReadToEnd().Replace("\r", "").Split('\n'));
		}

		internal GlyphFont(string[] lines)
		{
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				if (line.StartsWith("height ")) { FontHeight = int.Parse(line.Substring(7)); continue; }
				if (!line.StartsWith("[") || FontHeight == 0) continue;

				int end = line.IndexOfAny(new[] { ' ', ']' });
				char c = (char)int.Parse(line.Substring(1, end - 1));
				int width = lines[i + 1].Length;
				bool[,] glyph = new bool[width, FontHeight];
				for (int y = 0; y < FontHeight; y++)
				for (int x = 0; x < width; x++)
					glyph[x, y] = lines[i + 1 + y][x] == '#';
				_glyphs[c] = glyph;
				i += FontHeight;
			}
		}

		public Bytemap GetLetter(char character, byte colour)
		{
			bool[,] glyph = _glyphs.TryGetValue(character, out bool[,] g) ? g : _glyphs['?'];
			Bytemap output = new Bytemap(glyph.GetLength(0), FontHeight);
			for (int y = 0; y < FontHeight; y++)
			for (int x = 0; x < glyph.GetLength(0); x++)
				if (glyph[x, y]) output[x, y] = colour;
			return output;
		}
	}

	// Characters that draw as the game's own icons inside a line of text. The DOS font kept its
	// icons in punctuation (# a person, $ gold, { food, | shield, } trade, \ luxuries, ~ science),
	// and the reports still used those; but the game's text also means them literally ("Future
	// Tech #3", "$120", the Hall of Fame's # column), so the icons get private-use characters
	// of their own instead.
	internal static class IconGlyphs
	{
		public const char Food = '\uE000';
		public const char Shield = '\uE001';
		public const char Trade = '\uE002';
		public const char Gold = '\uE003';
		public const char Science = '\uE004';
		public const char Luxuries = '\uE005';
		public const char Person = '\uE006';

		// A citizen, drawn in the text's own colour so it reads as part of the number.
		private static readonly string[] PersonRows = { ".###.", ".###.", "..#..", "#####", ".###.", ".#.#.", ".#.#.", "....." };

		internal static Bytemap? Get(char c, byte colour, int height)
		{
			if (c == Person)
			{
				Bytemap person = new Bytemap(5, height);
				int top = height - PersonRows.Length;
				for (int y = 0; y < PersonRows.Length; y++)
				for (int x = 0; x < 5; x++)
					if (PersonRows[y][x] == '#') person[x, top + y] = colour;
				return person;
			}
			IBitmap? icon = c switch
			{
				Food => Icons.Food, Shield => Icons.Shield, Trade => Icons.Trade, Gold => Icons.Taxes,
				Science => Icons.Science, Luxuries => Icons.Luxuries, _ => null
			};
			if (icon is null) return null;
			// A copy, bottom-aligned on the line: the text cache owns what it is handed.
			Bytemap source = icon.Bitmap;
			Bytemap output = new Bytemap(source.Width, System.Math.Max(height, source.Height));
			int oy = output.Height - source.Height;
			for (int y = 0; y < source.Height; y++)
			for (int x = 0; x < source.Width; x++)
				output[x, oy + y] = source[x, y];
			return output;
		}
	}
}
