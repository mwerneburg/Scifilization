// CivOne tests
//
// Gandhi renounced the player (Game.CheckGoodwillOverflow: "He will not treat with us again.
// Not ever.") and still met them with "Greetings! The Indians welcome you.": the greeting was
// picked from his Friendly disposition, which the overflow exists to overturn. And he still
// took a city or an advance as a gift, which bought nothing, since Player.AddAttitudeBonus
// ignores an implacable civ (the user's Federation game, Nov 2026). He still receives the
// ambassador, coldly, and refuses gifts.

using System;
using System.Linq;
using System.Reflection;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class RenouncedDiplomacyTests
	{
		private static (King king, Player me) AtTheTable()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Game g = Game.Instance;
			Player me = g.HumanPlayer;
			Player them = g.Players.First(p => p is not null && p != me && g.PlayerNumber(p) != 0);
			them.SetImplacable(me);
			// Something to give, so the old code would have opened the gift menu: cities past the
			// capital, and an advance he lacks.
			for (int x = 10; x <= 40; x++)
				Map.Instance.ChangeTileType(x, 20, Enums.Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			for (int i = 0; i < 3; i++) g.AddCity(me, i, 12 + i * 8, 20);
			var gift = Common.Advances.First(a => !them.HasAdvance(a));
			me.AddAdvance(gift, false);
			Sim.ClearTasks();
			return (new King(them), me);
		}

		private static string Speech(King k) => string.Join(" ",
			(string[])typeof(King).GetField("_speechLines", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(k)!);

		[Fact]
		public void ARenouncedCivDoesNotWelcomeYou()
		{
			var (king, me) = AtTheTable();

			Assert.Equal($"We have nothing to say to the {me.TribeNamePlural}.", Speech(king));
		}

		[Theory]
		[InlineData("OfferCity")]
		[InlineData("OfferTechnology")]
		public void ARenouncedCivRefusesGifts(string offer)
		{
			var (king, me) = AtTheTable();

			typeof(King).GetMethod(offer, BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(king, new object?[] { null, EventArgs.Empty });

			Assert.Equal($"We want nothing from the {me.TribeNamePlural}.", Speech(king));
			Assert.False((bool)typeof(King).GetProperty("HasMenu", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)!.GetValue(king)!,
				"a gift menu was opened");
		}
	}
}
