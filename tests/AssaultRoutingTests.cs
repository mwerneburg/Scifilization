// CivOne tests
//
// The Franks' 13 Hover Tanks on the user's turn-394 Federation save (Oct 2026) died one
// at a time against Izibia, a Federation city held by veteran Mech Inf, while marching to
// stage on an EMPTY Thionville. Two rules met there:
//   - the planner treated every enemy city as passable, so the march went through Izibia
//     and each step into it was a lone assault;
//   - the defender chosen was the stack's best ATTACKER, not its best defender.

using System.Linq;
using CivOne;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class AssaultRoutingTests
	{
		private static (Player us, Player them, ITile centre) Field()
		{
			Sim.NewGame(width: 80, height: 50);
			Player us = Game.Instance.HumanPlayer;
			Player them = Game.Instance.Players.First(p => p is not null && p != us
			                                             && Game.Instance.PlayerNumber(p) != 0);
			ITile centre = Map.Instance.AllTiles().First(t => !t.IsOcean && t.Y > 8 && t.Y < Map.HEIGHT - 8);
			for (int dy = -5; dy <= 5; dy++)
			for (int dx = -5; dx <= 5; dx++)
				Map.Instance.ChangeTileType((centre.X + dx + Map.WIDTH) % Map.WIDTH, centre.Y + dy, Terrain.Grassland1);
			us.Explore(centre.X, centre.Y, range: 7);
			us.DeclareWar(them);
			return (us, them, centre);
		}

		// Tank one tile west of a garrisoned enemy city, goal one tile east of it. Mountains
		// flank the city, so the straight line through it is the cheapest route by terrain.
		[Fact]
		public void AMarchGoesAroundAGarrisonedEnemyCity()
		{
			(Player us, Player them, ITile c) = Field();
			int cx = c.X + 1, cy = c.Y;
			for (int dy = -5; dy <= 5; dy++)
				if (dy != 0) Map.Instance.ChangeTileType((cx + Map.WIDTH) % Map.WIDTH, cy + dy, Terrain.Mountains);
			Assert.NotNull(Game.Instance.AddCity(them, 0, cx, cy));
			Assert.NotNull(Game.Instance.CreateUnit(UnitType.Musketeers, cx, cy, Game.Instance.PlayerNumber(them)));

			IUnit tank = Game.Instance.CreateUnit(UnitType.Armor, c.X, c.Y, Game.Instance.PlayerNumber(us))!;
			ITile? step = Common.GotoStep(tank, c.X + 2, c.Y);

			Assert.NotNull(step);
			Assert.False(step!.X == cx && step.Y == cy, "the route assaulted a defended city it was only passing");
		}

		// The same city as the goal is still an attack the planner must allow.
		[Fact]
		public void AGarrisonedEnemyCityCanStillBeTheGoal()
		{
			(Player us, Player them, ITile c) = Field();
			int cx = c.X + 1, cy = c.Y;
			Assert.NotNull(Game.Instance.AddCity(them, 0, cx, cy));
			Assert.NotNull(Game.Instance.CreateUnit(UnitType.Musketeers, cx, cy, Game.Instance.PlayerNumber(them)));

			IUnit tank = Game.Instance.CreateUnit(UnitType.Armor, c.X, c.Y, Game.Instance.PlayerNumber(us))!;
			ITile? step = Common.GotoStep(tank, cx, cy);

			Assert.NotNull(step);
			Assert.True(step!.X == cx && step.Y == cy);
		}

		// Catapult (A6 D1) and Phalanx (A1 D2) share a city: the Phalanx defends.
		[Fact]
		public void TheBestDefenderDefends()
		{
			(Player us, Player them, ITile c) = Field();
			int cx = c.X + 1, cy = c.Y;
			Assert.NotNull(Game.Instance.AddCity(them, 0, cx, cy));
			byte owner = Game.Instance.PlayerNumber(them);
			Assert.NotNull(Game.Instance.CreateUnit(UnitType.Catapult, cx, cy, owner));
			Assert.NotNull(Game.Instance.CreateUnit(UnitType.Phalanx, cx, cy, owner));

			BaseUnit tank = (BaseUnit)Game.Instance.CreateUnit(UnitType.Armor, c.X, c.Y, Game.Instance.PlayerNumber(us))!;
			Assert.Equal(UnitType.Phalanx, tank.Defender(Map.Instance[cx, cy]).Type);
		}
	}
}
