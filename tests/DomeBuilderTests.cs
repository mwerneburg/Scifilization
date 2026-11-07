// CivOne tests
//
// An assigned Dome part waited behind everything else a city wanted. In game 96367b22 the
// approach warning came at turn 432, all three civs were at war from turn 420, and no city chose
// a part until turn 479, seventeen turns after the Owners landed. The part now goes first in the
// top production city, which keeps it once started, and the civ's other cities send Caravans.

using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class DomeBuilderTests
	{
		// An AI civ that knows everything and holds the Sensor Net, at war with a rival, with a
		// big city at (36,25) and a small one at (44,25), both garrisoned, on plains.
		private static (Player p, City big, City small) TwoCities()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			for (int y = 18; y <= 32; y++)
			for (int x = 28; x <= 52; x++)
				Map.Instance.ChangeTileType(x, y, Terrain.Plains);
			Map.Instance.RecalculateContinentsIfDirty();
			Player[] ais = g.Players.Where(x => x is not null && g.PlayerNumber(x) != 0 && x != g.HumanPlayer).ToArray();
			Player p = ais[0];
			foreach (IAdvance a in Common.Advances.Where(a => a is not FutureTech)) p.AddAdvance(a, false);
			p.Government = new CivOne.Governments.Monarchy();
			p.DeclareWar(ais[1]);
			byte id = g.PlayerNumber(p);
			p.Explore(40, 25, range: 12);
			City big = g.AddCity(p, 0, 36, 25)!;
			City small = g.AddCity(p, 1, 44, 25)!;
			big.Size = 8;
			small.Size = 1;
			big.ResetResourceTiles();
			small.ResetResourceTiles();
			g.CreateUnit(UnitType.Musketeers, 36, 25, id);
			g.CreateUnit(UnitType.Musketeers, 44, 25, id);
			g.DomeAssignments[id] = new System.Collections.Generic.List<Wonder> { Wonder.DomeSensorNet };
			Sim.ClearTasks();
			Assert.True(big.ShieldIncome > small.ShieldIncome, "fixture: the big city should out-produce the small one");
			return (p, big, small);
		}

		[Fact]
		public void TheTopCityPutsThePartFirstAtWar()
		{
			var (p, big, small) = TwoCities();
			Assert.IsType<DomeSensorNet>(AI.Instance(p).ProductionPlan(big)[0]);
			Assert.DoesNotContain(AI.Instance(p).ProductionPlan(small), x => x is DomeSensorNet);
		}

		// Overtaken mid-build, the builder keeps the part, the new top city does not start a
		// second one, and sends a Caravan instead.
		[Fact]
		public void TheBuilderKeepsItAndTheOthersSendCaravans()
		{
			var (p, big, small) = TwoCities();
			big.SetProduction(new DomeSensorNet());
			big.Size = 1;
			small.Size = 8;
			big.ResetResourceTiles();
			small.ResetResourceTiles();
			Assert.True(small.ShieldIncome > big.ShieldIncome, "fixture: the builder should have been overtaken");

			Assert.IsType<DomeSensorNet>(AI.Instance(p).ProductionPlan(big)[0]);
			var other = AI.Instance(p).ProductionPlan(small);
			Assert.DoesNotContain(other, x => x is DomeSensorNet);
			Assert.IsType<Caravan>(other[0]);
		}

		// The warning replans the holders at once; otherwise the part waits for the current build
		// and the queue behind it.
		[Fact]
		public void TheWarningReplansAtOnce()
		{
			var (p, big, small) = TwoCities();
			Game g = Game.Instance;
			g.DomeAssignments.Clear();
			foreach (City c in new[] { big, small })
			{
				c.SetProduction(new Settlers());
				c.EnqueueProduction(new Settlers());
			}
			g.AssignDomeComponents();
			Assert.True(g.GetDomeAssignments(p).Any(), "fixture: this civ was handed no part");

			Assert.IsAssignableFrom<IDomeComponent>(big.CurrentProduction);
			Assert.IsType<Caravan>(small.CurrentProduction);
		}

		// Round-robin by advance count handed parts to civs that could not build them; a part
		// goes to a civ that knows its advance, however far down the table that civ is.
		[Fact]
		public void APartGoesToACivThatCanBuildIt()
		{
			Sim.NewGame(width: 80, height: 50);
			Game g = Game.Instance;
			Player[] nations = g.HumanityNations();
			Assert.True(nations.Length > 5, "fixture: with five or fewer nations everyone gets a part anyway");
			foreach (Player n in nations)
				foreach (IAdvance a in Common.Advances.Where(a => a is not FutureTech && a is not FusionPower)) n.AddAdvance(a, false);
			Player slow = nations.Last(n => n != g.HumanPlayer);
			foreach (IAdvance a in Common.Advances.Take(30)) slow.DeleteAdvance(a);
			slow.AddAdvance(new FusionPower(), false);
			Assert.True(nations.All(n => n == slow || n.Advances.Length > slow.Advances.Length), "fixture: the civ with Fusion Power should be the least advanced");

			g.AssignDomeComponents();

			Assert.Contains(Wonder.DomeEmitterArray, g.GetDomeAssignments(slow));
			Assert.True(g.GetDomeAssignments(g.HumanPlayer).Any(), "the human keeps its part");
		}

		[Fact]
		public void LearningThePartsAdvanceReplansAtOnce()
		{
			var (p, big, small) = TwoCities();
			p.DeleteAdvance(new SuperConductor());
			foreach (City c in new[] { big, small })
			{
				c.SetProduction(new Settlers());
				c.EnqueueProduction(new Settlers());
			}
			p.AddAdvance(new SuperConductor());

			Assert.IsType<DomeSensorNet>(big.CurrentProduction);
			Assert.IsType<Caravan>(small.CurrentProduction);
		}

		[Fact]
		public void ACaravanInTheBuilderAddsItsShields()
		{
			var (p, big, _) = TwoCities();
			big.SetProduction(new DomeSensorNet());
			int before = big.Shields;
			IUnit caravan = Game.Instance.CreateUnit(UnitType.Caravan, 36, 25, Game.Instance.PlayerNumber(p))!;
			AI.Instance(p).Move(caravan);
			Assert.Equal(before + CaravanActions.WonderShields, big.Shields);
			Assert.DoesNotContain(Game.Instance.GetUnits(), u => u == caravan);
		}

		[Fact]
		public void ACaravanElsewhereHeadsForTheBuilder()
		{
			var (p, big, small) = TwoCities();
			big.SetProduction(new DomeSensorNet());
			IUnit caravan = Game.Instance.CreateUnit(UnitType.Caravan, 44, 25, Game.Instance.PlayerNumber(p))!;
			caravan.SetHome(small);
			AI.Instance(p).Move(caravan);
			Assert.Equal(new Point(36, 25), caravan.Goto);
		}
	}
}
