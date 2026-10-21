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
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens
{
	// Modal: these are full-screen views, but without it RuntimeHandler.OnDraw
	// composites every screen's bitmap — the GamePlay screen underneath included —
	// while using a SINGLE palette taken from the top screen. The game map behind
	// was therefore drawn through this screen's palette, which is the "background
	// palette goes wrong" symptom. Modal draws this screen alone, matching
	// Civilopedia ([Modal, OwnPalette, Expand]).
	[Modal, Expand]
	internal class WorldMap : BaseScreen
	{
		private bool _update = true;

		// Tab flips to a political view, after the original's replay map: flat land, and
		// every known city a 3x3 block of its owner's colour.
		private bool _political;

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			_update = false;
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.Key == Key.Tab)
			{
				_political = !_political;
				Draw();
				_update = true;
				return true;
			}
			Destroy();
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		public WorldMap()
		{
			// Was WorldMapTiles.Palette — the SP299 placeholder's palette when the
			// original assets are absent. The standard base palette carries the
			// terrain colours MiniMap.TerrainColour indexes into.
			Palette = Common.DefaultPalette;
			using (Palette cassette = CassetteTheme.CreatePalette())
				Palette.MergePalette(cassette, 1, 18);
			Draw();
		}

		private void Draw()
		{
			this.Clear(CassetteTheme.BG0);

			int px = Math.Max(1, Math.Min(Width / Map.WIDTH, Height / Map.HEIGHT));
			int ox = (Width - Map.WIDTH * px) / 2;
			int oy = (Height - Map.HEIGHT * px) / 2;

			if (_political)
			{
				DrawPolitical(px, ox, oy);
				return;
			}

			for (int x = 0; x < Map.WIDTH; x++)
			for (int y = 0; y < Map.HEIGHT; y++)
			{
				if (!Settings.RevealWorld && !Human.Visible(x, y)) continue;

				ITile tile = Map[x, y];
				int dx = ox + x * px;
				int dy = oy + y * px;
				this.FillRectangle(dx, dy, px, px, MiniMap.TerrainColour(tile));

				City city = tile.City;
				if (city is not null && city.Size > 0)
				{
					this.FillRectangle(dx, dy, px, px, Common.ColourLight[city.Owner]);
				}
				else
				{
					IUnit[] units = tile.Units;
					if (units.Length > 0)
					{
						int iS = Math.Max(1, px - 1);
						this.FillRectangle(dx + 1, dy + 1, iS, iS, CassetteTheme.BORDER)
							.FillRectangle(dx, dy, iS, iS, Common.ColourLight[units[0].Owner]);
					}
				}
			}
			DrawHint("[ TAB political ]");
		}

		private void DrawPolitical(int px, int ox, int oy)
		{
			bool Known(int x, int y) => Settings.RevealWorld || Human.Visible(x, y);

			for (int x = 0; x < Map.WIDTH; x++)
			for (int y = 0; y < Map.HEIGHT; y++)
			{
				if (!Known(x, y)) continue;
				byte colour = Map[x, y].Type == Terrain.Ocean ? CassetteTheme.OCEAN : CassetteTheme.BG3;
				this.FillRectangle(ox + x * px, oy + y * px, px, px, colour);
			}

			// Clipped to known tiles, so a city's block doesn't reveal the fog around it.
			foreach (City city in Game.GetCities())
			{
				if (city.Size == 0 || !Known(city.X, city.Y)) continue;
				for (int dy = -1; dy <= 1; dy++)
				for (int dx = -1; dx <= 1; dx++)
				{
					int x = (city.X + dx + Map.WIDTH) % Map.WIDTH, y = city.Y + dy;
					if (y < 0 || y >= Map.HEIGHT || !Known(x, y)) continue;
					this.FillRectangle(ox + x * px, oy + y * px, px, px, Common.CivPattern(city.Owner, x, y));
				}
			}
			DrawHint("[ TAB terrain ]");
		}

		private void DrawHint(string text)
		{
			int fh = Resources.GetFontHeight(0);
			int w = Resources.GetTextSize(0, text).Width;
			this.FillRectangle((Width - w) / 2 - 2, Height - fh - 3, w + 4, fh + 3, CassetteTheme.BG0)
				.DrawText(text, 0, CassetteTheme.INK_LOW, Width / 2, Height - fh - 1, TextAlign.Center);
		}
	}
}
