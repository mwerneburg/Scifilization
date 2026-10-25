// CivOne tests
//
// A city taken by a diplomat's bribe changed hands with no replay entry: both incite sites
// (the AI branch in Diplomat.Confront and the human's DiplomatIncite dialog) flip the owner
// by hand, and only conquest by arms went through BaseUnit's CityCaptured line.

using System.Linq;
using System.Reflection;
using CivOne.Enums;
using CivOne.Screens.Dialogs;
using CivOne.Units;

namespace CivOne.Tests
{
	public class InciteReplayTests
	{
		// A size-4 rival city at 40,25 with a diplomat of the briber's standing beside it at war.
		private static (City city, Diplomat spy) ABribe(bool humanBribes)
		{
			Sim.NewGame(width: 80, height: 50);
			Settings.Instance.Autopilot = false;
			Game g = Game.Instance;
			for (int x = 38; x <= 42; x++) Map.Instance.ChangeTileType(x, 25, Terrain.Grassland1);
			Player owner = g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer);
			Player briber = humanBribes ? g.HumanPlayer
				: g.Players.First(p => p is not null && g.PlayerNumber(p) != 0 && p != g.HumanPlayer && p != owner);
			owner.Explore(40, 25, range: 3);
			briber.Explore(40, 25, range: 3);
			Map.Instance.ChangeTileType(20, 25, Terrain.Grassland1);
			g.AddCity(owner, 64, 20, 25);   // the capital, so the Palace is not in the target
			Map.Instance.RecalculateContinentsIfDirty();
			City city = g.AddCity(owner, 65, 40, 25)!;
			city.Size = 4;
			briber.DeclareWar(owner);
			briber.Gold = 30000;
			Diplomat spy = (Diplomat)g.CreateUnit(UnitType.Diplomat, 39, 25, g.PlayerNumber(briber))!;
			Sim.ClearTasks();
			return (city, spy);
		}

		private static ReplayData.CityCaptured? Logged(City city) =>
			Game.Instance.GetReplayData<ReplayData.CityCaptured>().SingleOrDefault(r => r.CityNameId == city.NameId);

		[Fact]
		public void AnAIBribeIsInTheReplay()
		{
			(City city, Diplomat spy) = ABribe(humanBribes: false);

			typeof(Diplomat).GetMethod("Confront", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(spy, new object[] { 1, 0 });

			Assert.Equal(spy.Owner, city.Owner);   // the bribe went through
			Assert.Equal(spy.Owner, Logged(city)?.NewOwnerId);
		}

		[Fact]
		public void TheHumansBribeIsInTheReplay()
		{
			(City city, Diplomat spy) = ABribe(humanBribes: true);

			typeof(DiplomatIncite).GetMethod("Incite", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(new DiplomatIncite(city, spy), new object?[] { null, null });
			// The flip runs when the rebellion art finishes: end that screen the way a click would.
			var queue = (System.Collections.Generic.List<GameTask>)typeof(GameTask)
				.GetField("_tasks", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
			GameTask art = queue.First(t => t is Tasks.Show);
			typeof(GameTask).GetMethod("EndTask", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(art, null);

			Assert.Equal(spy.Owner, city.Owner);
			Assert.Equal(spy.Owner, Logged(city)?.NewOwnerId);
		}
	}
}
