// CivOne tests
//
// The research bank was 16 bits. A city adds its science before any of the turn's research is
// paid for, so a civ earning more than 32,767 a turn wrapped negative and threw away 65,536
// beakers: the Franks of a turn-546 Epic save earned 117,668 a turn and held -10,631.

using System.Linq;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class ScienceBankTests
	{
		public ScienceBankTests() => Sim.EnsureRuntime();

		[Fact]
		public void ACitysScienceTakesTheBankPast16Bits()
		{
			Sim.NewGame(width: 320, height: 200);
			Game g = Game.Instance;
			Player us = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			for (int y = 95; y <= 105; y++)
			for (int x = 95; x <= 105; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.River);
			Map.Instance.RecalculateContinentsIfDirty();
			City city = g.AddCity(us, 0, 100, 100)!;
			city.Size = 16;
			us.Gold = 10000;
			city.InvalidateCache();
			Sim.ClearTasks();
			Assert.True(city.Science > 0, "the fixture city makes no science");

			us.Science = short.MaxValue;
			city.NewTurn();

			Assert.True(us.Science > short.MaxValue, $"the bank wrapped to {us.Science}");
		}

		[Fact]
		public void ABigBankSurvivesASave()
		{
			Sim.NewGame(width: 320, height: 200);
			Game g = Game.Instance;
			Player us = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			byte id = g.PlayerNumber(us);
			us.Science = 40000;

			string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"civone-bank-{System.Guid.NewGuid():N}.cos");
			try
			{
				g.SaveCos(path);
				Sim.ResetState();
				Assert.True(Game.LoadCos(path), "the fixture could not read its own save");
				Assert.Equal(40000, Game.Instance.GetPlayer(id).Science);
			}
			finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
		}
	}
}
