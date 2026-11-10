// CivOne tests
//
// A foreign city's worked tiles are closed to our soldiers unless we are at war with its
// owner. "At war" read Player.IsAtWar, which only records declared wars, and nobody declares
// war on the Barbarians: they are hostile without one. So a city the Barbarians took kept
// its fields closed, as if a neutral farmed them: the user could not move an attacker onto
// the one tile a size-1 barbarian-held city worked (Nov 2026). The move rule and both GoTo
// planner checks now ask Player.IsHostileTo.

using System.Linq;
using CivOne;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class BarbarianFieldsTests
	{
		// A size-1 barbarian city two tiles east of centre, working the tile between.
		private static (Player human, ITile centre) Field()
		{
			Sim.NewGame(width: 80, height: 50);
			Player human = Game.Instance.HumanPlayer;
			Player barbarians = Game.Instance.GetPlayer(0);

			ITile centre = Map.Instance.AllTiles().First(t => !t.IsOcean && t.Y > 6 && t.Y < Map.HEIGHT - 6);
			for (int dy = -4; dy <= 4; dy++)
			for (int dx = -4; dx <= 4; dx++)
				Map.Instance.ChangeTileType((centre.X + dx + Map.WIDTH) % Map.WIDTH, centre.Y + dy, Terrain.Grassland1);
			human.Explore(centre.X, centre.Y, range: 6);
			barbarians.Explore(centre.X, centre.Y, range: 6);

			City city = Game.Instance.AddCity(barbarians, 0, centre.X + 2, centre.Y)!;
			Assert.NotNull(city);
			city.Size = 1;
			city.ResetResourceTiles();
			int fx = centre.X + 1, fy = centre.Y;
			if (!city.ResourceTiles.Any(t => t.X == fx && t.Y == fy))
			{
				// SetResourceTile toggles: free the one citizen, then put it on the tile between.
				city.SetResourceTile(city.ResourceTiles.First(t => !(t.X == city.X && t.Y == city.Y)));
				city.SetResourceTile(Map.Instance[fx, fy]);
			}
			Assert.True(Game.Instance.IsWorkedByOther(fx, fy, Game.Instance.PlayerNumber(human)),
				"precondition: the barbarian city works the tile east of centre");
			Assert.False(human.IsAtWar(barbarians), "precondition: no declared war, as in the live game");
			return (human, centre);
		}

		[Fact]
		public void ASoldierMayStepIntoABarbarianCitysFields()
		{
			(Player human, ITile centre) = Field();
			var legion = (BaseUnit)Game.Instance.CreateUnit(UnitType.Legion, centre.X, centre.Y, Game.Instance.PlayerNumber(human))!;
			Assert.Empty(Map.Instance[centre.X + 1, centre.Y].Units);

			Assert.Contains(legion.MoveTargets, t => t.X == centre.X + 1 && t.Y == centre.Y);
		}

		// The GoTo planner keeps its own copy of the rule.
		[Fact]
		public void AGotoIsPlannedThroughThem()
		{
			(Player human, ITile centre) = Field();
			IUnit legion = Game.Instance.CreateUnit(UnitType.Legion, centre.X - 1, centre.Y, Game.Instance.PlayerNumber(human))!;

			Assert.NotNull(Common.GotoStep(legion, centre.X + 1, centre.Y));
		}
	}
}
