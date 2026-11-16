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
	// The rules are Screens/King.cs (the talks), Player.DeclareWar (pacts) and
	// Player.EstablishTribute.
	internal class Diplomacy : BaseConcept
	{
		private static readonly string[] _page1 =
		{
			"To talk you need a channel. A",
			"DIPLOMAT at a foreign city can ask",
			"for an audience; an EMBASSY lets",
			"you TALK to that leader at any time",
			"from the Intelligence report (F3).",
			"",
			"At peace you may trade knowledge or",
			"maps, give a city or an advance,",
			"propose a DEFENSE PACT, demand",
			"tribute, or declare war.",
			"At war you may propose peace, or",
			"demand tribute for it.",
			"",
			"Gifts buy GOODWILL: turns in which",
			"that leader thinks well of you.",
		};

		private static readonly string[] _page2 =
		{
			"A city buys 30 turns of goodwill",
			"plus 10 per size, 100 at most; an",
			"advance up to 75, more for one they",
			"want. Arm a civ and its enemies",
			"resent you. A leader who will not",
			"forgive you takes no gifts.",
			"",
			"A DEFENSE PACT lasts 50 turns: an",
			"attack on either partner brings the",
			"other into the war, you included.",
			"",
			"A weak civ losing a war to you may",
			"offer yearly TRIBUTE. Accept, and",
			"peace holds as long as it pays.",
			"",
			"Rivals make demands too. Give in",
			"for goodwill, or refuse and take",
			"your chances.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public Diplomacy()
		{
			Name = "Diplomacy";
		}
	}
}
