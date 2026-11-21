// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Wonders;

namespace CivOne.Screens.Reports
{
	internal class CityStatus : BaseReport
	{
		private const char FOOD = IconGlyphs.Food;
		private const char SHIELD = IconGlyphs.Shield;
		private const char TRADE = IconGlyphs.Trade;
		private const byte FONT_ID = 0;

		// A blank pixel row between data rows, so a row's icons do not touch the next row's
		// letters (the user, Nov 2026).
		internal int RowHeight => Resources.GetFontHeight(FONT_ID) + 1;

		private readonly City[] _cities;

		// ── layout ───────────────────────────────────────────────────────────
		//
		// Measured, across the real canvas, the way TradeReport is laid out. It was a fixed
		// 320-wide band with the stats as one string, so the numbers did not line up from row
		// to row, a three-digit trade ran into the production name and a long production name
		// ran into its progress ("Infrastructure Bond54/70"; the user, Nov 2026).
		//
		// Food, shields and trade are columns of their own, each right-aligned so its digits
		// line up. The gaps between the four groups widen with the canvas, up to MAX_GAP; on a
		// canvas too narrow for everything the production name is cut (TradeReport.Fit).
		private const int MARGIN = 8, MIN_GAP = 8, MAX_GAP = 24, STAT_GAP = 6;

		internal readonly struct Columns
		{
			public readonly int Name, FoodRight, ShieldRight, TradeRight, Prod, ProdRoom, ProgressRight;
			public Columns(int name, int foodRight, int shieldRight, int tradeRight, int prod, int prodRoom, int progressRight)
			{
				Name = name; FoodRight = foodRight; ShieldRight = shieldRight; TradeRight = tradeRight;
				Prod = prod; ProdRoom = prodRoom; ProgressRight = progressRight;
			}
		}

		// The widths are the widest entry in each column.
		internal static Columns Layout(int width, int name, int food, int shield, int trade, int prod, int progress)
		{
			int content = name + food + STAT_GAP + shield + STAT_GAP + trade + prod + progress;
			int gap     = Math.Max(MIN_GAP, Math.Min(MAX_GAP, (width - 2 * MARGIN - content) / 3));
			int left    = Math.Max(MARGIN, (width - content - 3 * gap) / 2);

			int foodRight   = left + name + gap + food;
			int shieldRight = foodRight + STAT_GAP + shield;
			int tradeRight  = shieldRight + STAT_GAP + trade;
			int prodX       = tradeRight + gap;
			int progRight   = Math.Min(width - MARGIN, prodX + prod + gap + progress);
			int prodRoom    = Math.Max(0, progRight - progress - gap - prodX);
			return new Columns(left, foodRight, shieldRight, tradeRight, prodX, prodRoom, progRight);
		}

		private static int TextWidth(string text) => Resources.GetTextSize(FONT_ID, text).Width;
		private static string FoodText(City c)     => $"{c.Size}-{c.FoodTotal}{FOOD}";
		private static string ShieldText(City c)   => $"{c.ShieldTotal}{SHIELD}";
		private static string TradeText(City c)    => $"{c.TradeTotal}{TRADE}";
		private static string ProdText(City c)     => (c.CurrentProduction as ICivilopedia)?.Name ?? string.Empty;
		private static string ProgressText(City c) => $"{c.Shields}/{c.ProductionCost(c.CurrentProduction)}";

		private int Widest(Func<City, string> text) => _cities.Select(c => TextWidth(text(c))).DefaultIfEmpty(0).Max();

		internal Columns Layout() => Layout(Width, Widest(c => c.Name), Widest(FoodText), Widest(ShieldText),
			Widest(TradeText), Widest(ProdText), Widest(ProgressText));

		private bool _update = true;
		private int _page = 0;

		private static bool ProductionInvalid(City city)
		{
			if (city.CurrentProduction is IBuilding b) return city.HasBuilding(b);
			if (city.CurrentProduction is IWonder   w) return Game.Instance.WonderBuilt(w);
			return false;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;

			this.FillRectangle(0, 32, Width, Height - 32, CassetteTheme.BG1);

			int pageSize   = (Height - 32) / RowHeight;
			int yy = 32;

			Columns col = Layout();

			for (int i = (_page++ * pageSize); i < _cities.Length && i < (_page * pageSize); i++)
			{
				City city = _cities[i];

				bool   invalid    = ProductionInvalid(city);
				string production = TradeReport.Fit(ProdText(city), col.ProdRoom);

				// A wonder in near-white, so it stands out from the ordinary builds (the user, Nov 2026).
				byte prodColor = invalid ? CassetteTheme.ALERT
				               : city.CurrentProduction is IWonder ? CassetteTheme.WHITE
				               : CassetteTheme.INK_MID;
				byte progColor = invalid ? CassetteTheme.ALERT : CassetteTheme.INK_LOW;

				this.DrawText(city.Name, FONT_ID, CassetteTheme.PHOS, col.Name, yy)
				    .DrawText(FoodText(city), FONT_ID, CassetteTheme.INK_HIGH, col.FoodRight, yy, TextAlign.Right)
				    .DrawText(ShieldText(city), FONT_ID, CassetteTheme.INK_HIGH, col.ShieldRight, yy, TextAlign.Right)
				    .DrawText(TradeText(city), FONT_ID, CassetteTheme.INK_HIGH, col.TradeRight, yy, TextAlign.Right)
				    .DrawText(production, FONT_ID, prodColor, col.Prod, yy)
				    .DrawText(ProgressText(city), FONT_ID, progColor, col.ProgressRight, yy, TextAlign.Right);

				yy += RowHeight;
			}

			_update = false;
			return true;
		}

		private bool NextPage()
		{
			int pageSize = (Height - 32) / RowHeight;
			if ((_page * pageSize) < _cities.Length)
			{
				_update = true;
			}
			else
			{
				Destroy();
			}
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			return NextPage();
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			int pageSize = (Height - 32) / RowHeight;
			if (args.Y >= 32)
			{
				int rowIdx  = (args.Y - 32) / RowHeight;
				int cityIdx = (_page - 1) * pageSize + rowIdx;
				if (cityIdx >= 0 && cityIdx < _cities.Length)
				{
					Destroy();
					Common.AddScreen(new CityManager(_cities[cityIdx]));
					return true;
				}
			}
			return NextPage();
		}

		public CityStatus() : base("CITY STATUS", 8, MouseCursor.Pointer)
		{
			_cities = Game.GetCities().Where(c => Human == c.Owner && c.Size > 0).ToArray();
		}
	}
}
