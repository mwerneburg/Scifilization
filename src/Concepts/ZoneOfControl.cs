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
	// The rule is in BaseUnit.MoveTo; the pathfinder mirrors it (Common, InZoc).
	internal class ZoneOfControl : BaseConcept
	{
		private static readonly string[] _page1 =
		{
			"A land unit next to a foreign unit",
			"may not step straight to another",
			"tile that is also next to a foreign",
			"unit. Any foreign unit counts, at",
			"war or at peace.",
			"",
			"It may still attack, enter or leave",
			"a city, or move onto a tile one of",
			"your own units already holds.",
			"",
			"Units inside a city exert no zone",
			"of control; only units in the field",
			"do.",
			"",
			"DIPLOMATS, CARAVANS and EXPLORERS",
			"ignore zones of control.",
		};

		private static readonly string[] _page2 =
		{
			"This is how a thin line holds: two",
			"units a tile apart stop an enemy",
			"slipping between them.",
			"",
			"To get past, attack the screen, go",
			"around it, or advance along a chain",
			"of your own units.",
			"",
			"Ships and aircraft are never held,",
			"nor are units moving to or from a",
			"ship.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public ZoneOfControl()
		{
			Name = "Zone of Control";
		}
	}
}
