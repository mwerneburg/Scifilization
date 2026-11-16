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
	// The rule is BaseUnit.Salvage and BaseUnit.ReverseEngineer.
	internal class CapturingUnits : BaseConcept
	{
		private static readonly string[] _page1 =
		{
			"Beat a lone enemy LAND unit in the",
			"open and, one time in four, you take",
			"it intact instead of destroying it.",
			"",
			"Only hardware you cannot build yet",
			"is taken. A unit whose advance you",
			"already know is destroyed as usual,",
			"and so is any unit in a city, in a",
			"stack, or at sea.",
			"",
			"Hold the captured unit 20 turns and",
			"your engineers take it apart: you",
			"learn the advance that builds it.",
			"Lose it first and you learn nothing.",
		};

		private static readonly string[] _page2 =
		{
			"A captured unit arrives with no",
			"moves left and no home city, so no",
			"city pays its upkeep.",
			"",
			"The choice is what to do with it:",
			"throw a superior unit into the line",
			"now, or keep it safe in the rear",
			"until it pays out a whole advance.",
			"",
			"Learn the advance some other way",
			"first and the clock simply stops.",
			"Units nobody can build, such as",
			"monsters, are never taken.",
			"",
			"Your rivals capture too: a lone",
			"modern unit far from support is a",
			"gift to a backward enemy.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public CapturingUnits()
		{
			Name = "Capturing Units";
		}
	}
}
