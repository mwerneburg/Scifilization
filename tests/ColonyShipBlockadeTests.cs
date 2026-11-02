// CivOne tests
//
// The Owners' recovery fleet takes every colony ship while the Registry holds anything on
// Earth. Game cea9d583: fourteen civs launched full 51/16/12 hulls between t643 and t702 and
// every one was taken, while the Others were down to a single city. The AI now holds a
// finished ship on the pad, and the road reopens when the Others are eradicated.

using System.Linq;
using CivOne.Enums;

namespace CivOne.Tests
{
	public class ColonyShipBlockadeTests
	{
		private static (Game g, byte rival) AnOwnersWorld(bool othersHoldACity)
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			g.SETISignalReceived = true;
			g.VisitorType = VisitorArchetype.Owners;

			var others = new Player(Common.Civilizations.First(c => c is Civilizations.TheOthers), "The Registry");
			g.AddPlayer(others);
			if (othersHoldACity)
			{
				Map.Instance.ChangeTileType(40, 25, Terrain.Grassland1);
				Assert.NotNull(g.AddCity(others, 0, 40, 25));
			}

			Player rival = g.Players.First(p => p is not null && p != g.HumanPlayer
				&& g.PlayerNumber(p) != 0 && p != others);
			return (g, g.PlayerNumber(rival));
		}

		private static void AFinishedHull(Game g, byte p)
		{
			g.Progress(p).SpaceshipComponent  = 2;
			g.Progress(p).SpaceshipModule     = 3;
			g.Progress(p).SpaceshipStructural = Game.SpaceshipStructuresNeeded(2, 3);
		}

		// Same driver as SpaceRaceNotAVictoryTests: the launch and arrival checks run once
		// _currentPlayer wraps.
		private static void PlayARound(Game g)
		{
			for (int i = 0; i <= g.Players.Count(); i++)
			{
				Sim.ClearTasks();
				g.EndTurn();
			}
		}

		[Fact]
		public void AFinishedShipIsHeldWhileTheOthersHoldACity()
		{
			(Game g, byte rival) = AnOwnersWorld(othersHoldACity: true);
			AFinishedHull(g, rival);

			PlayARound(g);

			Assert.Equal(0, g.Progress(rival).SpaceshipLaunchTurn);
			Assert.Equal(2, g.Progress(rival).SpaceshipComponent);
		}

		[Fact]
		public void TheShipLaunchesOnceTheOthersAreGone()
		{
			(Game g, byte rival) = AnOwnersWorld(othersHoldACity: false);
			AFinishedHull(g, rival);

			PlayARound(g);

			Assert.NotEqual(0, g.Progress(rival).SpaceshipLaunchTurn);
		}

		[Fact]
		public void AShipInFlightIsTakenWhileTheOthersRemain()
		{
			(Game g, byte rival) = AnOwnersWorld(othersHoldACity: true);
			g.Progress(rival).SpaceshipArrivalTurn = (int)g.GameTurn + 1;

			PlayARound(g);

			Assert.False(g.Progress(rival).ColonyFounded);
		}

		[Fact]
		public void AShipInFlightLandsOnceTheOthersAreGone()
		{
			(Game g, byte rival) = AnOwnersWorld(othersHoldACity: false);
			g.Progress(rival).SpaceshipArrivalTurn = (int)g.GameTurn + 1;

			PlayARound(g);

			Assert.True(g.Progress(rival).ColonyFounded);
		}

		// Before the fleet lands there is no Registry on Earth to eradicate; the road is shut.
		[Fact]
		public void TheRoadIsShutBeforeTheOthersLand()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			g.SETISignalReceived = true;
			g.VisitorType = VisitorArchetype.Owners;

			Assert.True(g.ColonyShipsIntercepted);
		}
	}
}
