// CivOne tests
//
// Any tribe, in a game of any size (the user, Nov 2026). The picker offered only the tribes
// whose own seat fit the game, so a three-civ game had none from Asia or North America. A
// tribe whose seat is beyond the game now takes the last seat; the human is seated before
// the AI draws, so no AI is dealt the human's tribe.

using System.Linq;
using CivOne.Civilizations;

namespace CivOne.Tests
{
	public class TribeChoiceTests
	{
		private static ICivilization Tribe(string name)
		{
			Sim.EnsureRuntime();
			return Common.Civilizations.First(c => c.Name == name);
		}

		private static int[] Seated(Game g) =>
			Enumerable.Range(1, g.Players.Count() - 1).Select(i => g.Players.ElementAt(i).Civilization.Id).ToArray();

		[Theory]
		[InlineData("Lakota")]          // seat 10
		[InlineData("Japanese")]        // seat 8
		[InlineData("Haida")]           // seat 5
		[InlineData("Haudenosaunee")]   // seat 17
		public void AnyTribeCanBePlayedInAThreeCivGame(string name)
		{
			Sim.NewGame(competition: 3, human: Tribe(name));
			Game g = Game.Instance;

			Assert.Equal(name, g.HumanPlayer.Civilization.Name);
			Assert.Equal(3, g.PlayerNumber(g.HumanPlayer));
			Assert.Equal(3, Seated(g).Distinct().Count());
		}

		// Its own seat still holds when the game is big enough.
		[Fact]
		public void ATribeThatFitsKeepsItsOwnSeat()
		{
			Sim.NewGame(competition: 7, human: Tribe("Haida"));
			Assert.Equal(5, Game.Instance.PlayerNumber(Game.Instance.HumanPlayer));
		}

		// On Earth the Arabs' seat 11 is refilled from any unseated tribe that never respawns.
		// In a 16-civ game the Haudenosaunee, seat 17, are the one later tribe it can deal, so
		// a human seated in turn (seat 16, after seat 11) could be dealt to an AI as well. The
		// region preference makes it rare: of these thirty seeds only 26 did it, so this test
		// guards by one seed and a change to the draw could leave it guarding none.
		[Fact]
		public void NoAIIsDealtTheHumansTribe()
		{
			for (short seed = 1; seed <= 30; seed++)
			{
				Sim.NewGame(competition: 16, map: "earth-epic", seed: seed, human: Tribe("Haudenosaunee"));
				int[] seated = Seated(Game.Instance);
				Assert.True(seated.Length == seated.Distinct().Count(), $"seed {seed}: a tribe was dealt twice");
			}
		}
	}
}
