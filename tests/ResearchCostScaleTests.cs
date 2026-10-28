// CivOne tests
//
// Civ 1's research price was set for its 80-wide map, while the AI's city target scales with
// the map's width. On the 320-wide Epic map civs fielded three to four times Civ 1's cities at
// Civ 1's prices, and the world reached Electronics around 650 AD (game aa0eb1a5). The price
// now scales with the map like the target.

using System.Linq;
using CivOne.Advances;

namespace CivOne.Tests
{
	public class ResearchCostScaleTests
	{
		// The price of the next advance, per advance already known, for an AI civ that
		// knows a few: above the 12-beaker floor, so the floor cannot hide the scale.
		private static double PerAdvance(int width)
		{
			Sim.NewGame(width: width, height: width * 5 / 8);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			foreach (IAdvance a in new IAdvance[] { new Alphabet(), new Pottery(), new Masonry(), new BronzeWorking(), new HorsebackRiding() })
				p.AddAdvance(a, false);
			return (double)p.ScienceCost / (p.Advances.Length + 1);
		}

		[Fact]
		public void ADoubleWidthWorldPaysDoubleForAnAdvance()
		{
			Assert.Equal(2.0, PerAdvance(160) / PerAdvance(80), 3);
		}
	}
}
