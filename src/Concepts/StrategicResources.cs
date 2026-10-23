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
	// Iron / Coal / Oil shipped without a reference entry. A player who builds
	// a Factory for half again the shields deserves to be able to find out why.
	//
	// Kept exact against Game.ResourceAt, Game.RequiredResource and
	// Game.HasResource: deposits are derived from existing map specials (no new
	// map state), possession is a worked tile OR a camp, and the penalty is
	// +50% shields — a tax, never a wall. Since Oct 2026 copper reaches back to
	// the bronze age (the user's call): a start with none in reach pays half again
	// for its spearmen until a settler camps some.
	internal class StrategicResources : BaseConcept
	{
		private static readonly string[] _page1 =
		{
			"IRON, COAL, COPPER, OIL and SALT",
			"lie in the land's special tiles.",
			"",
			"IRON in the mountains.",
			"COAL or COPPER in the hills.",
			"OIL or SALT in the desert.",
			"OIL in the wetland.",
			"",
			"You hold a deposit when one of",
			"your cities works its tile, or",
			"when you keep a camp on it.",
			"",
			"WITHOUT THE MATERIAL, the work",
			"costs half again as much. It is a",
			"tax, not a wall.",
			"",
			"SALT is a luxury too: holding it",
			"calms one citizen in every city.",
		};

		private static readonly string[] _page2 =
		{
			"Exactly what wants what:",
			"",
			"COPPER: Phalanx, Chariot, Catapult,",
			"Trireme, Mech Inf, Cruise Missile,",
			"Reaper Drone, Nuclear, Hydro Plant,",
			"Observatory, SAM Battery, Neural",
			"Lab, Mission Control.",
			"SALT: Granary.",
			"IRON: Cannon, Artillery, Ironclad.",
			"COAL: Factory, Power Plant.",
			"OIL: Armor, Fighter, Bomber, Cruiser,",
			"Battleship, Submarine, Carrier,",
			"Transport.",
			"",
			"Mine the tile you work, camp the",
			"tile you don't. Stand a unit on a",
			"rival's camp and it is yours.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public StrategicResources()
		{
			Name = "Strategic Resources";
		}
	}
}
