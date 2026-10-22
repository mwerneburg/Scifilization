// CivOne tests
//
// While the machines, the Federation, the Registry or the organism hold a city, Earth's
// nations stop fighting one another. Run 3ee31bef (Oct 2026): the Federation took 104
// cities while the Franks opened four wars on Earth and the Ethiopians joined one by pact.

using System.Linq;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Units;

namespace CivOne.Tests
{
	public class OutsiderTruceTests
	{
		// An aggressive Monarchy with twelve cities, a grudge and an army, beside a weak
		// neighbour of three: every reason for war (TradeDeterrentTests' fixture, no trade).
		private static (Game g, Player us, Player them) AWarmongerAndAVictim(bool outsiders)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 10; y <= 40; y++)
			for (int x = 10; x <= 70; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();

			Player[] ais = g.Players.Where(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer).ToArray();
			Player us = ais.First(p => p.Civilization.Leader.Aggression == AggressionLevel.Aggressive);
			Player them = ais.First(p => p != us);
			us.Government = new Monarchy();
			them.Government = new Monarchy();

			int id = 0;
			for (int i = 0; i < 12; i++) g.AddCity(us, id++, 14 + (i % 6) * 4, 14 + (i / 6) * 4)!.Size = 10;
			for (int i = 0; i < 3; i++) g.AddCity(them, id++, 18 + i * 6, 30)!.Size = 10;
			for (int i = 0; i < 4; i++) g.CreateUnit(UnitType.Armor, 14, 14, g.PlayerNumber(us));
			us.AddGrudge(them, 100);

			if (outsiders)
			{
				var fed = new Player(Common.Civilizations.First(c => c is Civilizations.Federation), "Faction");
				g.AddPlayer(fed);
				g.AddCity(fed, id++, 66, 38);
			}
			Sim.ClearTasks();
			return (g, us, them);
		}

		private static void Turns(Player us, int n, System.Func<bool> until)
		{
			AI ai = AI.Instance(us)!;
			for (int i = 0; i < n && !until(); i++) ai.ConsiderWar();
		}

		// The control: without it the next test passes against a warmonger that never fights.
		[Fact]
		public void AWarmongerGoesToWarWithNoOutsiders()
		{
			(Game _, Player us, Player them) = AWarmongerAndAVictim(outsiders: false);
			Turns(us, 60, () => us.IsAtWar(them));
			Assert.True(us.IsAtWar(them));
		}

		[Fact]
		public void NotWhileTheFederationHoldsACity()
		{
			(Game _, Player us, Player them) = AWarmongerAndAVictim(outsiders: true);
			Turns(us, 60, () => us.IsAtWar(them));
			Assert.False(us.IsAtWar(them));
		}

		// Thirty turns: the stalemate exit needs more than thirty at war, so only the
		// truce can end it. 25% a turn leaves it standing once in ~5,000 runs.
		[Fact]
		public void AWarAlreadyRunningEndsWhileTheFederationHoldsACity()
		{
			(Game _, Player us, Player them) = AWarmongerAndAVictim(outsiders: true);
			us.DeclareWar(them);
			Sim.ClearTasks();
			Turns(us, 30, () => !us.IsAtWar(them));
			Assert.False(us.IsAtWar(them));
		}

		[Fact]
		public void AWarAlreadyRunningGoesOnWithNoOutsiders()
		{
			(Game _, Player us, Player them) = AWarmongerAndAVictim(outsiders: false);
			us.DeclareWar(them);
			Sim.ClearTasks();
			Turns(us, 30, () => !us.IsAtWar(them));
			Assert.True(us.IsAtWar(them));
		}

		// The human's peace offer is an audience screen, which cannot run headless.
		[Fact]
		public void YourPeaceOfferIsLikelierWhileOutsidersHoldCities()
		{
			string src = System.IO.File.ReadAllText(
				System.IO.Path.Combine(Sim.RepoRoot(), "src", "Screens", "King.cs"));
			Assert.Contains("AIAccepts(Game.OutsidersAbroad ? 80 : 50)", src);
		}
	}
}
