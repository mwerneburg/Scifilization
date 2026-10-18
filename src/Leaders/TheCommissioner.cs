// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;

namespace CivOne.Leaders
{
	// The Federation sends a point man, not a head of state. He has read the file on
	// Earth, and he is not impressed by it.
	internal class TheCommissioner : BaseLeader
	{
		protected override Leader Leader => Leader.TheCommissioner;

		public TheCommissioner() : base("The Commissioner")
		{
			Aggression  = AggressionLevel.Aggressive;
			Development = DevelopmentLevel.Perfectionist;
			Militarism  = MilitarismLevel.Militaristic;
		}
	}
}
