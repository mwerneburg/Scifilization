// CivOne tests
//
// The City Status table was a fixed 320-wide band with the stats as one string: a three-digit
// trade ran into the production name, and "Infrastructure Bond" into its progress (the user,
// Nov 2026). It is now measured, the way TradeReport is: no column may reach into the next,
// whatever the canvas width, and a wider canvas gives the columns more room.

using CivOne.Graphics;
using CivOne.Screens.Reports;

namespace CivOne.Tests
{
	public class CityStatusLayoutTests
	{
		private static int W(string s) => Resources.Instance.GetTextSize(0, s).Width;

		// The Tabriz row from the report, with the longest city name and progress we expect.
		private static readonly string Name = "Constantinople", Food = $"13-44{IconGlyphs.Food}",
			Shield = $"17{IconGlyphs.Shield}", Trade = $"159{IconGlyphs.Trade}",
			Prod = "Infrastructure Bond", Progress = "120/600";

		private static CityStatus.Columns Lay(int width)
			=> CityStatus.Layout(width, W(Name), W(Food), W(Shield), W(Trade), W(Prod), W(Progress));

		[Theory]
		[InlineData(320)]
		[InlineData(640)]
		public void NoColumnReachesIntoTheNext(int width)
		{
			Sim.EnsureRuntime();
			CityStatus.Columns c = Lay(width);
			string prod = TradeReport.Fit(Prod, c.ProdRoom);

			Assert.True(c.Name >= 0);
			Assert.True(c.Name + W(Name) < c.FoodRight - W(Food), "name into food");
			Assert.True(c.FoodRight < c.ShieldRight - W(Shield), "food into shields");
			Assert.True(c.ShieldRight < c.TradeRight - W(Trade), "shields into trade");
			Assert.True(c.TradeRight < c.Prod, "trade into production");
			Assert.True(c.Prod + W(prod) < c.ProgressRight - W(Progress), "production into progress");
			Assert.True(c.ProgressRight <= width, "progress off the screen");
		}

		// Room to spare goes to the gaps and to the production name, which is not cut.
		[Fact]
		public void AWideCanvasPrintsTheWholeProductionName()
		{
			Sim.EnsureRuntime();
			CityStatus.Columns c = Lay(640);

			Assert.True(c.ProdRoom >= W(Prod));
			Assert.True(c.Prod - c.TradeRight > 8, $"gap {c.Prod - c.TradeRight}");
		}
	}
}
