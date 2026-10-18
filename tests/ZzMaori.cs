using System.Linq;
using Xunit;
using Xunit.Abstractions;
namespace CivOne.Tests
{
	public class ZzMaori
	{
		private readonly ITestOutputHelper _o;
		public ZzMaori(ITestOutputHelper o) { _o = o; }
		[Fact]
		public void Dump()
		{
			Sim.EnsureRuntime(); Sim.ResetState();
			Assert.True(Game.LoadCos(System.Environment.GetEnvironmentVariable("SAVE")!));
			Game g = Game.Instance;
			foreach (Player p in g.Players.Where(p => p.Civilization is Civilizations.Maori))
			{
				byte n = g.PlayerNumber(p);
				_o.WriteLine($"M slot={n} start={p.Civilization.StartX},{p.Civilization.StartY} gold={p.Gold} advances={p.Advances.Count()} units={g.GetUnits().Count(u => u.Owner == n)}");
				foreach (City c in g.GetCities().Where(c => c.Owner == n))
					_o.WriteLine($"M  slot={n} {c.Name} at {c.X},{c.Y} size={c.Size} origOwner={c.OriginalOwner}");
				foreach (City c in g.GetCities().Where(c => c.OriginalOwner == n && c.Owner != n))
					_o.WriteLine($"M  slot={n} LOST {c.Name} at {c.X},{c.Y} now owner={c.Owner}");
			}
		}
	}
}
