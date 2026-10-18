// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Leaders;

namespace CivOne.Civilizations
{
	// Humanity's earlier exodus, militarised and come home. Lands only when an Earth
	// holding the Dome refuses the ultimatum (Game.ExecuteFederationLanding), and only
	// ever holds the cities it seizes; these names are fallbacks for one it founds.
	internal class Federation : BaseCivilization<TheCommissioner>
	{
		public Federation() : base(Civilization.Federation, "Federal", "Federation")
		{
			CityNames = new string[]
			{
				"Compliance",
				"Directive",
				"Mandate",
				"Bastion",
				"Muster",
				"Bulwark",
				"Standard",
				"Citadel",
				"Rampart",
				"Ordinance",
				"Sentinel",
				"Tribunal",
			};
		}
	}
}
