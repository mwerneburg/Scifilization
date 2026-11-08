// CivOne tests
//
// A screen that draws from the game's generator makes the world's dice depend on what the
// player looks at. The city screen re-seeded it from the city's name to draw its houses, and
// drew its fireworks and noise from it too, so every city screen — including the one each
// finished building shows — reset the dice to a sequence fixed by that city: 23 times in 121
// turns of one headless game, and in play whenever the player looked. The unit-death, conquest
// and credits screens filled their noise textures from it, so turning animations off changed
// the dice. Screens now roll their own.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CivOne.Buildings;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class ScreenDiceTests
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

		// The screens whose rolls ARE game decisions: the throne room's diplomacy and the new
		// game's setup. Every other screen is decoration and keeps its own generator.
		private static readonly string[] Deciders = { "King.cs", "NewGame.cs" };

		[Fact]
		public void OnlyDecidingScreensRollTheGamesDice()
		{
			string screens = Path.Combine(Sim.RepoRoot(), "src", "Screens");
			string[] rollers = Directory.GetFiles(screens, "*.cs", SearchOption.AllDirectories)
				.Where(f => Regex.IsMatch(File.ReadAllText(f), @"Common\.Random\b|SetRandomSeed"))
				.Select(Path.GetFileName)
				.Where(f => !Deciders.Contains(f))
				.ToArray()!;
			Assert.Empty(rollers);
		}
	}
}
