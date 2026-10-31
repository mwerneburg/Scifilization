// CivOne tests
//
// An Earth start is a heartland's real coordinates, and several land far below the ground a
// few tiles away. On Epic Earth the Ethiopian start scored 35 (Game.StartSiteScore) against
// 97 six tiles off; the Aztecs, Persians, Ottomans and Greeks started at 44-57, and those five
// came last game after game. A start now moves to the best site within reach when it is clearly
// better, every civ and the human included (the user, Oct 2026).

using System.Linq;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class EarthStartSiteTests
	{
		[Fact]
		public void NoEarthStartIsLeftOnPoorGround()
		{
			Sim.NewGame(map: "earth-epic", competition: 16);
			Game g = Game.Instance;
			var starts = g.GetUnits().OfType<Settlers>()
				.Select(s => (civ: g.GetPlayer(s.Owner).TribeNamePlural, s.X, s.Y, score: Game.StartSiteScore(s.X, s.Y)))
				.ToArray();

			var poor = starts.Where(s => s.score < 60).ToArray();
			Assert.True(poor.Length == 0, "poor starts: " + string.Join(", ", poor.Select(s => $"{s.civ} ({s.X},{s.Y}) {s.score}")));
		}

		// Moved, but onto ground a capital can work from its centre. The best-scoring tile near
		// the Persian heartland is a swamp: the score is the radius, and a swamp centre feeds
		// nobody.
		[Fact]
		public void AMovedStartStandsOnFeedableGround()
		{
			Sim.NewGame(map: "earth-epic", competition: 16);
			Game g = Game.Instance;
			IUnit per = g.GetUnits().OfType<Settlers>().First(s => g.GetPlayer(s.Owner).Civilization is Civilizations.Persian);
			ITile t = Map.Instance[per.X, per.Y];

			Assert.False(per.X == 207 && per.Y == 79, "the Persians still start at their heartland point");
			Assert.True(t is Grassland || t is Plains || t is River || t is Hills, $"moved onto {t.GetType().Name}");
		}
	}
}
