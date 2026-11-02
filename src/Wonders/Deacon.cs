// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Advances;
using CivOne.Enums;

namespace CivOne.Wonders
{
	// Cursed wonder #13 (docs/cursed_wonders.md), the user's brief: a war computer in a
	// mountain, a minder and a play on DEFCON. Its owner's units defend with advantage
	// (BaseUnit.AttackOutcome) — but one time in four it decides the war game is real
	// (Game.ArmDeacon): five turns of DEFCON, then five strikes at the builder's largest
	// rival, unless the builder is at peace with everyone when the clock runs out.
	internal class Deacon : BaseWonder
	{
		private static readonly string[] _page1 =
		{
			"DEACON plays out every war",
			"before it is fought. Its",
			"owner's units DEFEND WITH",
			"ADVANTAGE: a lost defence is",
			"fought again, and the second",
			"result stands.",
			"",
			"Deacon has never lost a war",
			"it was allowed to finish.",
		};

		public override string[] GetPageText(byte pageNumber) => _page1;

		public Deacon() : base(40)
		{
			Name = "Deacon";
			RequiredTech = new Computers();
			ObsoleteTech = null;
			SetSmallIcon(1, 5);
			Type = Wonder.Deacon;
		}
	}
}
