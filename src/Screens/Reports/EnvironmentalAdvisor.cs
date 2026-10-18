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

namespace CivOne.Screens.Reports
{
	// The Sentiment Survey's layout, for smoke. One row per city: its pollution against the
	// 20 units a city absorbs for free, the polluted tiles in its radius, the citizens the smog
	// is making unhappy, and which of the cures it has. The footer is the world's side of it,
	// read from Game.WarmingThreshold — the number HandleGlobalWarming itself uses.
	internal class EnvironmentalAdvisor : BaseReport
	{
		private const byte FONT_ID = 0;
		private const int Tolerance = 20;   // City.SmokeStacks: the first 20 units are free
		private const int BarWidth = 60;

		private readonly City[] _cities;
		private bool _update = true;
		private int _page = 0;

		private int PageSize => (Height - 32 - 40) / 10;

		private static readonly (string code, Func<City, bool> has)[] _cures =
		{
			("R", c => c.HasBuilding<RecyclingCenter>()),
			("P", c => c.HasBuilding<HydroPlant>() || c.HasBuilding<NuclearPlant>() || c.HooverDamActive),
			("M", c => c.HasBuilding<MassTransit>()),
			("C", c => c.HasBuilding<CarbonCapture>()),
		};

		private void DrawRow(City city, int y)
		{
			int pressure = city.PollutionPressure;
			byte colour = pressure > Tolerance ? CassetteTheme.ALERT
			            : pressure >= Tolerance * 3 / 4 ? CassetteTheme.PHOS
			            : CassetteTheme.OK;
			this.DrawText(city.Name.Length > 13 ? city.Name.Substring(0, 13) : city.Name, FONT_ID, CassetteTheme.INK_HIGH, OX + 8, y);

			int bx = OX + 92;
			int fill = Math.Min(BarWidth, pressure * BarWidth / (Tolerance * 2));
			this.FillRectangle(bx, y + 1, BarWidth, 5, CassetteTheme.BG3)
				.FillRectangle(bx + BarWidth / 2, y, 1, 7, CassetteTheme.INK_LOW);   // the tolerance mark
			if (fill > 0) this.FillRectangle(bx, y + 1, fill, 5, colour);
			this.DrawText($"{pressure}", FONT_ID, colour, bx + BarWidth + 4, y);

			int tiles = city.CityTiles.Count(t => t.Pollution);
			this.DrawText(tiles > 0 ? $"{tiles} tiles" : "-", FONT_ID, tiles > 0 ? CassetteTheme.ALERT : CassetteTheme.INK_LOW, OX + 184, y);
			int smog = city.SmokeStacks / 10;
			this.DrawText(smog > 0 ? $"{smog} sad" : "-", FONT_ID, smog > 0 ? CassetteTheme.ALERT : CassetteTheme.INK_LOW, OX + 228, y);

			int x = OX + 272;
			foreach (var (code, has) in _cures)
			{
				this.DrawText(code, FONT_ID, has(city) ? CassetteTheme.PHOS_GLOW : CassetteTheme.INK_LOW, x, y);
				x += 8;
			}
		}

		private void DrawFooter(int y)
		{
			int polluted = Map.AllTiles().Count(t => t.Pollution);
			int threshold = Game.WarmingThreshold;
			this.DrawText($"Polluted tiles: {polluted} of {threshold} before warming",
				FONT_ID, polluted * 4 >= threshold * 3 ? CassetteTheme.ALERT : CassetteTheme.INK_HIGH, OX + 8, y);

			int world = Game.Players.Where(p => Game.PlayerNumber(p) != 0 && !p.IsDestroyed()).Sum(p => p.Pollution);
			string share = world == 0 ? "-" : $"{Human.Pollution * 100 / world}%";
			this.DrawText($"Warming events: {Game.GlobalWarmingCount}   Your share of smoke: {share}",
				FONT_ID, CassetteTheme.INK_MID, OX + 8, y + 9);

			if (Game.ReprocessorActive)
				this.DrawText("THE REPROCESSOR IS RUNNING", FONT_ID, CassetteTheme.ALERT, OX + 8, y + 18);
			else if (Game.WonderBuilt<Wonders.KyotoAccord>())
				this.DrawText("The Kyoto Accord is in force", FONT_ID, CassetteTheme.OK, OX + 8, y + 18);

			this.DrawText("R", FONT_ID, CassetteTheme.PHOS, OX + 8, y + 27);  this.DrawText("ecycling", FONT_ID, CassetteTheme.INK_MID, OX + 14, y + 27);
			this.DrawText("P", FONT_ID, CassetteTheme.PHOS, OX + 64, y + 27); this.DrawText("ower", FONT_ID, CassetteTheme.INK_MID, OX + 70, y + 27);
			this.DrawText("M", FONT_ID, CassetteTheme.PHOS, OX + 100, y + 27); this.DrawText("ass Transit", FONT_ID, CassetteTheme.INK_MID, OX + 106, y + 27);
			this.DrawText("C", FONT_ID, CassetteTheme.PHOS, OX + 176, y + 27); this.DrawText("arbon Capture", FONT_ID, CassetteTheme.INK_MID, OX + 182, y + 27);
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;

			this.FillRectangle(0, 28, Width, Height - 28, CassetteTheme.BG0);
			int y = 32;
			for (int i = _page++ * PageSize; i < _cities.Length && i < _page * PageSize; i++, y += 10)
				DrawRow(_cities[i], y);
			DrawFooter(Height - 40);

			_update = false;
			return true;
		}

		private bool NextPage()
		{
			if (_page * PageSize < _cities.Length) _update = true;
			else Destroy();
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args) => NextPage();

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (args.Y >= 32)
			{
				int cityIdx = (_page - 1) * PageSize + (args.Y - 32) / 10;
				if (cityIdx >= 0 && cityIdx < _cities.Length && args.Y < 32 + PageSize * 10)
				{
					Destroy();
					Common.AddScreen(new CityManager(_cities[cityIdx]));
					return true;
				}
			}
			return NextPage();
		}

		public EnvironmentalAdvisor() : base("ENVIRONMENTAL ADVISOR", CassetteTheme.BG0, MouseCursor.Pointer)
		{
			// Dirtiest first: the page is for deciding what to build where.
			_cities = Game.GetCities().Where(c => Human == c.Owner && c.Size > 0)
				.OrderByDescending(c => c.PollutionPressure).ToArray();
		}
	}
}
