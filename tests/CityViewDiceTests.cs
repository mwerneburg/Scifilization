// CivOne tests
//
// The city screen re-seeded the GAME's generator from the city's name to draw its houses,
// and drew its fireworks and noise from it too. Every city screen, including the one each
// finished building shows, reset the world's dice to a sequence fixed by that city: 23
// times in 121 turns of one headless game, and in play, whenever the player looked.

using CivOne.Buildings;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class CityViewDiceTests
	{
		[Fact]
		public void ACityScreenLeavesTheGamesDiceAlone()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			City city = g.AddCity(g.HumanPlayer, 0, 40, 25)!;
			city.Size = 5;

			Common.SetRandomSeed(4242);
			_ = new CityView(city);
			_ = new CityView(city, production: new Temple());   // the finished-building screen

			Assert.Equal(new Random(4242).Next(30000), Common.Random.Next(30000));
		}
	}
}
