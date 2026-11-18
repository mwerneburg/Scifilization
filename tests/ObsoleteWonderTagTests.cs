// CivOne tests
//
// An obsolete wonder still builds, for its score and a little culture, but its effect is off
// for good, and the production list offered it as if it worked (the user, Nov 2026). The
// row now says OBSOLETE.

using System.Linq;
using CivOne.Advances;
using CivOne.Screens;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class ObsoleteWonderTagTests
	{
		private static City ACity()
		{
			Sim.NewGame(width: 80, height: 50);
			Player human = Game.Instance.HumanPlayer;
			human.Explore(40, 25, range: 4);
			City c = Game.Instance.AddCity(human, 0, 40, 25)!;
			Sim.ClearTasks();
			return c;
		}

		[Fact]
		public void AWonderThatStillWorksHasNoTag()
		{
			City c = ACity();

			Assert.Null(CityChooseProduction.RowTag(c, new GreatWall()));
		}

		[Fact]
		public void AnObsoleteWonderSaysSo()
		{
			City c = ACity();
			// Anyone's Gunpowder ends the Great Wall, not only the builder's.
			Player rival = Game.Instance.Players.First(p => p is not null && Game.Instance.PlayerNumber(p) != 0 && p != c.Player);
			rival.AddAdvance(new Gunpowder(), false);

			Assert.Equal("OBSOLETE", CityChooseProduction.RowTag(c, new GreatWall()));
		}
	}
}
