// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Concepts
{
	// Spawning is Civilizations/Barbarian.cs and the cap in Game.cs; the combat
	// modifiers are in BaseUnit.AttackOutcome.
	internal class Barbarians : BaseConcept
	{
		private static readonly string[] _page1 =
		{
			"Barbarians belong to no nation, are",
			"at war with everyone, and cannot be",
			"bargained with.",
			"",
			"Every 4 turns a raiding party may",
			"appear on land you have never seen:",
			"one unit at first, two from turn",
			"150, three from turn 300. They arm",
			"as the world does: MILITIA, then",
			"LEGIONS once anyone knows BRONZE",
			"WORKING, KNIGHTS once anyone knows",
			"GUNPOWDER.",
			"",
			"Raids begin on turn 80 at Chieftain",
			"and 16 turns sooner per level.",
		};

		private static readonly string[] _page2 =
		{
			"Every 8 turns a ship may land a raid",
			"near the nearest city: LEGIONS or",
			"KNIGHTS and a DIPLOMAT. Sea raids",
			"end for good once anyone learns",
			"COMBUSTION. No new raid appears",
			"while 30 barbarian units are about.",
			"",
			"Barbarians take cities. Against",
			"your units they fight at a quarter",
			"of their strength at Chieftain, more",
			"at each level. A raider must win",
			"twice to break into a city, a",
			"PALACE halves its attack, and a",
			"civ's last city cannot fall to them.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public Barbarians()
		{
			Name = "Barbarians";
		}
	}
}
