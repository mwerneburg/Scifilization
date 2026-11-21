// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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

			// Column x-positions (within the 320-wide centred content area)
			int colName    = OX + 8;    // city name
			int colStats   = OX + 82;   // size / food / shield / trade
			int colProd    = OX + 172;  // production name (clipped to colProgress - gap)
			int colProgress = OX + 310; // shields progress, right-aligned

			for (int i = (_page++ * pageSize); i < _cities.Length && i < (_page * pageSize); i++)
			{
				City city = _cities[i];

				bool   invalid    = ProductionInvalid(city);
				string? production = (city.CurrentProduction as ICivilopedia)?.Name;
				string progress   = $"{city.Shields}/{city.ProductionCost(city.CurrentProduction)}";

				// A wonder in near-white, so it stands out from the ordinary builds (the user, Nov 2026).
				byte prodColor = invalid ? CassetteTheme.ALERT
				               : city.CurrentProduction is IWonder ? CassetteTheme.WHITE
				               : CassetteTheme.INK_MID;
				byte progColor = invalid ? CassetteTheme.ALERT : CassetteTheme.INK_LOW;

				this.DrawText(city.Name, FONT_ID, CassetteTheme.PHOS, colName, yy)
				    .DrawText($"{city.Size}-{city.FoodTotal}{FOOD} {city.ShieldTotal}{SHIELD} {city.TradeTotal}{TRADE}", FONT_ID, CassetteTheme.INK_HIGH, colStats, yy)
				    .DrawText(production, FONT_ID, prodColor, colProd, yy)
				    .DrawText(progress, FONT_ID, progColor, colProgress, yy, TextAlign.Right);

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
