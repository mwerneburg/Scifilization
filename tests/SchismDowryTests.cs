// CivOne tests
//
// The Internet's schism takes half the treasury with it. The share was held in 16 bits, so past
// 65,535 gold the new civ's share wrapped negative and the parent came out richer than it went in.
// AI hoards reach that: the Aztecs banked 94,445 in one game and Japan about 90,000 in another.

using System.Linq;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class SchismDowryTests
	{
		[Fact]
		public void AGreatFortuneIsSplitInHalf()
		{
			Sim.NewGame(width: 80, height: 50, competition: 4);
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player builder = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			City capital = g.AddCity(builder, 0, 25, 25)!;
			capital.AddBuilding(new CivOne.Buildings.Palace());
			City wonder = g.AddCity(builder, 1, 30, 25)!;
			g.AddCity(builder, 2, 35, 25);
			g.AddCity(builder, 3, 40, 25);
			builder.Gold = 100000;

			Player? splinter = g.ExecuteSocialMediaSchism(builder, wonder);
			Sim.ClearTasks();

			Assert.NotNull(splinter);
			Assert.Equal(50000, splinter!.Gold);
			Assert.Equal(50000, builder.Gold);
		}
	}
}
