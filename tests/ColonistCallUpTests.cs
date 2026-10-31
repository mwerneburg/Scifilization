// CivOne tests
//
// A recent epic game left the entire New World unoccupied. The cause was not
// motivation, it was reachability: AI.cs only looked for a boat when
// BestSettleSite AND BestImproveSite both came back empty, and BestImproveSite
// wants any unimproved tile within 6 of a city. Games finish at 42-45% improved
// land, so it was never empty and no settler ever boarded anything.
//
// Colonists are now designated ahead of local gardening, the hull rule no longer
// closes permanently once the map is charted, and the port sends a defender along.

using System.Linq;
using CivOne;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Tests
{
	public class ColonistCallUpTests
	{
		// A civ with cities, a settler, and a transport in port — plus plenty of
		// unimproved ground, which is exactly what used to keep the settler at home.
		private static (Player p, IUnit settler, IUnit hull, ITile berth) Port(int cities = 5)
		{
			Sim.NewGame(width: 80, height: 50);
			for (int y = 20; y <= 30; y++)
			for (int x = 30; x <= 44; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			// Open water east of the coast...
			for (int y = 18; y <= 32; y++)
			for (int x = 45; x <= 51; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Ocean);
			// ...and a New World on the far side of it, which is the whole point.
			for (int y = 22; y <= 28; y++)
			for (int x = 52; x <= 58; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();

			Player p = Game.Instance.Players.First(x => Game.Instance.PlayerNumber(x) != 0);
			byte id = Game.Instance.PlayerNumber(p);
			p.Explore(42, 25, range: 10);
			p.Explore(55, 25, range: 6);   // charted, so the register has somewhere to record
			for (int i = 0; i < cities; i++)
				Game.Instance.AddCity(p, (byte)i, 44 - i * 2, 25 - (i % 2));

			// A Sail, not a Trireme: an AI Trireme hugs the coast (Common.GotoStepInner) and cannot
			// cross this strait. The player is the human, whose own Trireme is exempt, but not under
			// the Autopilot an earlier test may leave on, and the call-up now asks whether the hull
			// can reach a site.
			IUnit hull = Game.Instance.CreateUnit(UnitType.Sail, 45, 25, id)!;
			IUnit settler = Game.Instance.CreateUnit(UnitType.Settlers, 44, 25, id)!;
			Sim.ClearTasks();
			return (p, settler, hull, Map.Instance[45, 25]);
		}

		// Without a charted site there is nowhere to go, and nothing should change.
		[Fact]
		public void WithNowhereCharted_NoColonistIsCalledUp()
		{
			var (p, _, _, _) = Port();
			Assert.False(AI.Instance(p).WantsColonist());
		}

		// A young civ does not ship out its citizens.
		[Fact]
		public void AYoungCiv_DoesNotColonise()
		{
			var (p, _, _, _) = Port(cities: 2);
			Assert.False(AI.Instance(p).WantsColonist());
		}

		// The finding: with a hull, a charted site and unimproved land everywhere, the
		// settler heads for the boat instead of staying home to irrigate.
		[Fact]
		public void ADesignatedColonist_WalksToTheBoat()
		{
			var (p, settler, hull, berth) = Port();
			AI ai = AI.Instance(p);
			// Survey: this is what fills the colony register in a real game.
			ai.BestOverseasSite(hull);
			Assert.True(ai.KnownColonySites() > 0, "scenario is broken: no overseas site was surveyed");

			ai.Move(settler);

			Assert.False(settler.Goto.IsEmpty, "a designated colonist should be routed somewhere");
			Assert.True(settler.Goto.X == berth.X && settler.Goto.Y == berth.Y,
				$"expected the berth at ({berth.X},{berth.Y}); got ({settler.Goto.X},{settler.Goto.Y})");
		}

		// The port's gift, and the two things that must bound it.
		[Fact]
		public void TheEscortSailsWithTheColonist_AndCostsTheColonyNothing()
		{
			var (p, settler, hull, berth) = Port();
			AI ai = AI.Instance(p);
			ai.BestOverseasSite(hull);
			Assert.True(ai.KnownColonySites() > 0, "scenario is broken: no overseas site was surveyed");

			ai.Move(settler);

			IUnit[] escorts = berth.Units.Where(u => u.Class == UnitClass.Land).ToArray();
			Assert.True(escorts.Length >= 1, "the port should have put a defender aboard");
			Assert.All(escorts, e => Assert.Null(e.Home));
			Assert.All(escorts, e => Assert.True(e.Sentry, "an escort aboard must be sentried as cargo"));
		}

		// The cap counts settlers still WALKING to the port, not only those aboard.
		// Without that, an epic-map civ marches half its settlers at one hull and stops
		// improving its land — this change, inverted.
		[Fact]
		public void SettlersAlreadyWalkingToPort_CountAgainstTheCap()
		{
			var (p, settler, hull, berth) = Port();
			AI ai = AI.Instance(p);
			ai.BestOverseasSite(hull);
			Assert.True(ai.WantsColonist());

			byte id = Game.Instance.PlayerNumber(p);
			for (int i = 0; i < 2; i++)
			{
				IUnit walker = Game.Instance.CreateUnit(UnitType.Settlers, 36 + i, 22, id)!;
				walker.Goto = new System.Drawing.Point(berth.X, berth.Y);
			}

			Assert.False(ai.WantsColonist(),
				"two settlers already marching at the boat is a full quota");
		}

		// Already in the hull's own city, the colonist boards: in port a hull sails with what is
		// asleep. It used to be routed to its own tile, find no step and stand there awake; eleven
		// Mongol settlers in Erdenet on the t570 save of game c8d7d2e8.
		[Fact]
		public void AColonistInTheHullsCity_BoardsIt()
		{
			var (p, settler, _, _) = Port();
			// Docked in the coastal city, where the settler stands.
			IUnit hull = Game.Instance.CreateUnit(UnitType.Sail, 44, 25, Game.Instance.PlayerNumber(p))!;
			AI ai = AI.Instance(p);
			ai.BestOverseasSite(hull);
			Assert.True(ai.WantsColonist(), "scenario is broken: no colonist wanted");

			ai.Move(settler);

			Assert.True(settler.Sentry, $"not aboard; Goto ({settler.Goto.X},{settler.Goto.Y})");
		}

		// A hull that can reach none of the charted sites calls nobody. Erdenet's Trireme could
		// reach none of the Mongols' 48, so a colonist would only sleep aboard for good.
		[Fact]
		public void AHullWithNowhereToSail_CallsNoColonist()
		{
			var (p, settler, _, _) = Port();
			IUnit hull = Game.Instance.CreateUnit(UnitType.Sail, 44, 25, Game.Instance.PlayerNumber(p))!;
			AI ai = AI.Instance(p);
			ai.BestOverseasSite(hull);
			// Silt up the strait: the site stays charted, the hull is left in a one-tile lake.
			for (int y = 18; y <= 32; y++)
			for (int x = 45; x <= 51; x++)
				if (x != 45 || y != 25) Map.Instance.ChangeTileType(x, y, Terrain.Grassland1);
			Map.Instance.RecalculateContinentsIfDirty();
			Assert.True(ai.WantsColonist(), "scenario is broken: no colonist wanted");

			ai.Move(settler);

			Assert.False(settler.Sentry, "boarded a hull that cannot sail anywhere");
		}

		// Set beside the New World, the colonist goes ashore to found, even when its civ is past
		// the point where MayFoundCities says yes at home. The call-up never asks that, and the
		// settler used to look only for work near its own cities and drift for home across water:
		// Zulu, Khmer and Malian settlers stood aboard for good on the t570 save of game c8d7d2e8.
		[Fact]
		public void AColonistAtSea_GoesAshore_EvenWhenHomeIsFull()
		{
			var (p, _, _, _) = Port();
			byte id = Game.Instance.PlayerNumber(p);
			byte n = 5;
			foreach ((int x, int y) in new[] { (31, 20), (34, 20), (31, 23), (34, 23), (31, 26), (34, 26), (31, 29), (34, 29), (38, 20) })
				Game.Instance.AddCity(p, n++, x, y);   // fourteen size-1 cities: over target, far below its size bar
			AI ai = AI.Instance(p);
			Assert.False(ai.MayFoundCities(), "scenario is broken: home still wants cities");
			Game.Instance.CreateUnit(UnitType.Sail, 51, 25, id);
			IUnit settler = Game.Instance.CreateUnit(UnitType.Settlers, 51, 25, id)!;
			Sim.ClearTasks();

			ai.Move(settler);
			Sim.Settle();

			Assert.True(settler.X >= 52 || settler.Goto.X >= 52,
				$"stayed at sea: at ({settler.X},{settler.Y}), Goto ({settler.Goto.X},{settler.Goto.Y})");
		}

		// Only one escort per crossing, however many times the settler re-decides.
		[Fact]
		public void TheEscort_IsNotDonatedTwice()
		{
			var (p, settler, hull, berth) = Port();
			AI ai = AI.Instance(p);
			ai.BestOverseasSite(hull);
			Assert.True(ai.KnownColonySites() > 0, "scenario is broken: no overseas site was surveyed");

			ai.Move(settler);
			int after = berth.Units.Count(u => u.Class == UnitClass.Land);
			ai.Move(settler);

			Assert.Equal(after, berth.Units.Count(u => u.Class == UnitClass.Land));
		}
	}
}
