// CivOne tests
//
// Haudenosaunee settlers raced back and forth on the rails around Tonawanda (game f37aa663,
// turn 472). Their clean-up crew was sent to smog that Carbon Capture cleared the same turn:
// units move before cities, so the crew arrived on clean ground, rode home, and was sent to
// the next tile. Smog a Carbon Capture city will clear is not the crew's to chase.

using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class CarbonCaptureCrewTests
	{
		private static (Player p, City c, Settlers s) ACrewBesideSmog(bool capture)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player p = g.Players.First(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer);
			for (int y = 20; y <= 30; y++)
			for (int x = 35; x <= 45; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			p.Explore(40, 25, range: 4);   // Carbon Capture works only the ground its owner can see (City.CityTiles)
			City c = g.AddCity(p, 0, 40, 25)!;
			if (capture) c.AddBuilding(new CarbonCapture());
			Map.Instance[41, 26].Pollution = true;
			var s = (Settlers)g.CreateUnit(UnitType.Settlers, 38, 25, g.PlayerNumber(p))!;
			s.AutoClean = true;
			Sim.ClearTasks();
			return (p, c, s);
		}

		[Fact]
		public void TheCrewLeavesCarbonCaptureSmogAlone()
		{
			(Player p, _, Settlers s) = ACrewBesideSmog(capture: true);
			s.NewTurn();
			Assert.True(s.Goto.IsEmpty, $"sent to {s.Goto}");
			Assert.Equal(0, AI.Instance(p).PollutionBacklog());
		}

		// Control: without the building the same smog is the crew's job.
		[Fact]
		public void WithoutCarbonCaptureTheCrewGoes()
		{
			(Player p, _, Settlers s) = ACrewBesideSmog(capture: false);
			s.NewTurn();
			Assert.Equal((41, 26), (s.Goto.X, s.Goto.Y));
			Assert.Equal(1, AI.Instance(p).PollutionBacklog());
		}
	}
}
