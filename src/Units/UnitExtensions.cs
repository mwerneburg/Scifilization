// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Drawing;
// using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;

namespace CivOne.Units
{
	public static class UnitExtensions
	{
		private static Player Human => Game.Instance.HumanPlayer;

		// The letter a working Settlers wears, on the map and in the side panel's stack: the
		// key that gave the order. Civ 1 lettered only road, irrigation, mine and fortress, so
		// every later job (a camp, a terrace, an aquafarm) looked idle. Camp is C, not its
		// key: lowercase y prints the same as Terrace's Y.
		internal static char? JobLetter(this Settlers s) =>
			s.BuildingRoad > 0 ? 'R' :
			s.BuildingIrrigation > 0 ? 'I' :
			s.BuildingMine > 0 ? 'M' :
			s.BuildingFortress > 0 ? 'F' :
			s.BuildingCamp > 0 ? 'C' :
			s.BuildingCleanPollution > 0 ? 'P' :
			s.BuildingCanopyArray > 0 ? 'Q' :
			s.BuildingAquafarm > 0 ? 'A' :
			s.BuildingLowerTerrain > 0 ? 'L' :
			s.BuildingRaiseTerrain > 0 ? 'H' :
			s.BuildingPlantForest > 0 ? 'V' :
			s.BuildingPlantJungle > 0 ? 'J' :
			s.BuildingThawTundra > 0 ? 'K' :
			s.BuildingAddRiver > 0 ? 'N' :
			s.BuildingTerrace > 0 ? 'Y' :
			s.BuildingMoistureFarm > 0 ? 'I' :
			null;

		public static Bytemap ToBitmap(this IUnit unit, bool showState = true) => ToBitmap(unit, unit.Owner, showState);
		public static Bytemap ToBitmap(this IUnit unit, byte colour, bool showState = true)
		{
			if (showState)
			{
				if (unit is Settlers settlers && settlers.JobLetter() is char job)
					return Unit.Letter(settlers.Type, job, colour).Bitmap;
				if (unit.Sentry)
				{
					return Unit.Sentry(unit.Type, colour).Bitmap;
				}
				else if (unit.FortifyActive)
				{
					return Unit.Letter(unit.Type, 'F', colour).Bitmap;
				}
				else if (unit.Fortify)
				{
					return Unit.Fortify(unit.Type, colour).Bitmap;
				}
				else if (Human == unit.Owner && unit.Goto != Point.Empty)
				{
					return Unit.Letter(unit.Type, 'G', colour).Bitmap;
				}
			}
			return Unit.Base(unit.Type, colour).Bitmap;
		}
	}
}