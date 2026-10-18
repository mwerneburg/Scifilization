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

namespace CivOne.Buildings
{
	// Everything else in the pollution chain stops new smoke; only Settlers took it off the
	// ground. This cleans one polluted tile in the city's radius each turn (City.NewTurn),
	// so a city that makes less than one a turn is clean for good without an engineer.
	internal class CarbonCapture : BaseBuilding
	{
		private static readonly string[] _page1 =
		{
			"CARBON CAPTURE draws the smoke",
			"out of the air and buries it.",
			"",
			"Each turn it cleans ONE polluted",
			"tile in the city's radius.",
		};

		private static readonly string[] _page2 =
		{
			"Requires RECYCLING.",
			"",
			"It does not stop pollution; it",
			"cleans up after it, without",
			"tying up SETTLERS.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public CarbonCapture() : base(12, 2)
		{
			Name = "Carbon Capture";
			RequiredTech = new Recycling();
			Type = Building.CarbonCapture;
		}
	}
}
