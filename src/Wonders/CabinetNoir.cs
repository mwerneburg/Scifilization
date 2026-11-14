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
	// A Police Station's guard against sabotage, in every city of the builder's, from Invention
	// on and never obsolete (the user, Nov 2026). Sabotage only: foreign agents still steal
	// advances and incite revolts. See Diplomat.SabotageProof.
	internal class CabinetNoir : BaseWonder
	{
		private static readonly string[] _page1 =
		{
			"The CABINET NOIR reads every",
			"letter that crosses the border.",
			"",
			"No foreign agent can SABOTAGE",
			"any of your cities: each one is",
			"known before it arrives, and held.",
			"They may still steal advances",
			"and incite revolts.",
		};

		private static readonly string[] _page2 =
		{
			"Requires INVENTION. Never obsolete.",
			"",
			"Richelieu's black chamber opened,",
			"copied and resealed the post of",
			"half of Europe. Its clerks could",
			"lift a wax seal and set it back",
			"so that no ambassador knew his",
			"dispatches had been read.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public CabinetNoir() : base(30)
		{
			Name = "Cabinet Noir";
			RequiredTech = new Invention();
			ObsoleteTech = null;
			SetSmallIcon(1, 5);
			Type = Wonder.CabinetNoir;
		}
	}
}
