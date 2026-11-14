// CivOne tests
//
// On the City Status page a wonder in production is written in near-white, so it stands out
// from the tan of ordinary builds (the user, Nov 2026).

using System.Linq;
using System.Reflection;
using CivOne.Graphics;
using CivOne.Screens.Reports;

namespace CivOne.Tests
{
	public class CityStatusWonderTests
	{
		// The barracks row is the control: nothing else on the page is near-white, so the
		// wonder row's white comes from the wonder.
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void AWonderInProductionIsWrittenInNearWhite(bool wonder)
		{
			Sim.NewGame(width: 80, height: 50);
			City city = Game.Instance.AddCity(Game.Instance.HumanPlayer, 0, 40, 25)!;
			city.Size = 3;
			city.SetProduction(wonder ? new CivOne.Wonders.Colossus() : new CivOne.Buildings.Barracks());
			Sim.ClearTasks();

			var screen = new CityStatus();
			typeof(CityStatus).GetMethod("HasUpdate", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(screen, new object[] { 0u });

			bool white = Enumerable.Range(32, screen.Bitmap.Height - 32)
				.Any(y => Enumerable.Range(0, screen.Bitmap.Width).Any(x => screen.Bitmap[x, y] == CassetteTheme.WHITE));
			Assert.Equal(wonder, white);
		}
	}
}
