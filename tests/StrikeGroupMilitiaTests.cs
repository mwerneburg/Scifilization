// CivOne tests
//
// A civ at war with no attacking advance got a Militia queued in its near cities every turn.
// MarshalStrikeGroup builds BestAttacker() where the group is short, and that falls back to a
// Militia: a defender, which the group never counts and the `raising` check never sees. The
// Babylonians of game abb9ba24 had a city queuing nine, 17 Militia standing in five cities,
// and 187 of their production picks from t150 to t460 were Militia.

using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tests
{
	public class StrikeGroupMilitiaTests
	{
		[Fact]
		public void NoMilitiaIsQueuedForAStrikeGroup()
		{
			Sim.NewGame(width: 80, height: 50, difficulty: 2);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int y = 20; y <= 30; y++)
			for (int x = 20; x <= 60; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Player p = g.Players.First(x => x is not null && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			p.Government = new Governments.Monarchy();
			foreach (IAdvance a in new IAdvance[] { new Automobile(), new Metallurgy(), new Chivalry(), new TheWheel(), new HorsebackRiding(), new IronWorking() })
				p.DeleteAdvance(a);
			p.Explore(40, 25, range: 30);
			City home = g.AddCity(p, 0, 25, 25)!;
			home.Size = 4;
			Player enemy = g.Players.First(x => x is not null && x != p && x != g.HumanPlayer && g.PlayerNumber(x) != 0);
			g.AddCity(enemy, 1, 45, 25)!.Size = 4;
			enemy.DeclareWar(p);
			Sim.ClearTasks();
			var flags = BindingFlags.NonPublic | BindingFlags.Instance;
			Assert.Equal("Militarize", typeof(AI).GetMethod("GetStance", flags)!.Invoke(AI.Instance(p), null)!.ToString());
			Assert.IsType<Militia>(typeof(AI).GetMethod("BestAttacker", flags)!.Invoke(AI.Instance(p), null));

			for (int turn = 0; turn < 3; turn++) AI.Instance(p).MarshalStrikeGroup();

			Assert.DoesNotContain(home.ProductionQueue, x => x is Militia);
		}
	}
}
