// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;

namespace CivOne.Concepts
{
	// The veto is in BaseUnit.Confront and King.BuildMenu; the hearings are
	// SenateGrievanceResponse and IncitedCityResponse; the threshold is
	// Game.ProvocationThreshold.
	internal class Senate : BaseConcept
	{
		private static readonly string[] _page1 =
		{
			"Under DEMOCRACY the Senate will not",
			"let you start a war. A unit ordered",
			"to attack a civ you are at peace",
			"with is stopped, and talks offer no",
			"declaration of war.",
			"",
			"Barbarians are always fair game,",
			"and a war declared on you is yours",
			"to fight.",
			"",
			"A civ whose agents strike you three",
			"times, by sabotage or an incited",
			"revolt, loses the Senate's",
			"protection: you may attack it.",
		};

		private static readonly string[] _page2 =
		{
			"Under REPUBLIC or DEMOCRACY the",
			"third act of sabotage calls a",
			"hearing: take no action, declare",
			"war, or, with an EMBASSY, demand",
			$"reparations of up to 200{IconGlyphs.Gold}.",
			"",
			"An incited city calls a hearing at",
			"once. With an embassy you may",
			"convene talks to win it back, with",
			"50 turns of peace.",
			"",
			"Either demand succeeds only if your",
			"score is at least double theirs.",
			"Otherwise they deny everything.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public Senate()
		{
			Name = "The Senate";
		}
	}
}
