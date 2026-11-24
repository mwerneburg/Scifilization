// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Screens.Dialogs;
using CivOne.Tasks;

namespace CivOne
{
	// The notes the guide shows, once a game each. Saved by NAME (Game.Cos), so reordering
	// or inserting here never replays an old note on a loaded game.
	internal enum GuideNote
	{
		Welcome, FirstCity, Grow, Research, Hut, Barbarians, Disorder, Treasury, Settlers, Monarchy
	}

	// Vesta, the newcomer's advisor (Nov 2026). Civ 1's Instant Advice showed HELP.TXT notes on
	// fixed dates; that text went with the DOS files, and Chieftain games were popping empty
	// boxes. These replace them: one note per MOMENT a newcomer meets, shown the first time it
	// happens, never twice. Same switch as before (Instant Advice: on by default at Chieftain,
	// any level from Options), and N on any note turns it off.
	//
	// Lines are kept to about 34 characters; GuideTests measures every note on screen.
	internal static class Guide
	{
		internal const string Name = "Vesta";

		internal static string[] Text(GuideNote note, string city = "") => note switch
		{
			GuideNote.Welcome => [
				"Hello, Leader. I am VESTA.",
				"Your far descendants built me and",
				"sent me back, to keep this first",
				"fire burning.",
				"",
				"Your Settlers are ready. Press B",
				"to found a city here, or step",
				"toward a river or coast first.",
			],
			GuideNote.FirstCity => [
				$"{city}! Every story in my archive",
				"began with a city like this one.",
				"",
				"Click it to choose what it builds:",
				"Warriors keep it safe, Settlers",
				"found the next. Press Enter when",
				"you are ready to end the turn.",
			],
			GuideNote.Grow => [
				$"{city} has reached size 3.",
				"The peoples who reach the stars",
				"all spread early. Each Settlers",
				"founds a new city, and costs its",
				"home one citizen to train.",
			],
			GuideNote.Research => [
				"Your scholars await a direction.",
				"Each advance opens new units,",
				"buildings or wonders. There is",
				"no wrong first choice, and the",
				"Civilopedia shows where each",
				"one leads.",
			],
			GuideNote.Hut => [
				"The village of an older people.",
				"Huts may hold gold, knowledge,",
				"new followers, or an ambush.",
				"Explore while the world is young:",
				"it rewards the curious.",
			],
			GuideNote.Barbarians => [
				"Raiders. My archive has them in",
				"every age. Keep a defender in",
				"each city and press F to fortify",
				"it; City Walls make it far harder",
				"to take.",
			],
			GuideNote.Disorder => [
				$"{city} is in disorder: more",
				"unhappy citizens than happy ones,",
				"and no work gets done. A Temple",
				"calms them. In the city screen,",
				"click a citizen to make them an",
				"entertainer.",
			],
			GuideNote.Treasury => [
				"Our treasury runs low. Each",
				"building costs upkeep, and when",
				"the gold runs out, buildings are",
				"sold. Set a higher tax with",
				"Science Rate in the GAME menu,",
				"or build a Marketplace.",
			],
			GuideNote.Settlers => [
				"Settlers shape the land, too:",
				"R builds a road, I irrigates,",
				"M mines, O lays a road to a far",
				"tile. Under Despotism irrigation",
				"and mines add nothing, so roads",
				"come first.",
			],
			GuideNote.Monarchy => [
				"MONARCHY is ours! Despotism lets",
				"irrigation, mines and rich seas",
				"yield nothing extra. Choose",
				"Revolution! in the ADVISORS menu:",
				"a few turns of anarchy, then a",
				"better age.",
			],
			_ => [],
		};

		// The note's screen, or null when it has been shown, the guide is off, or nobody is
		// watching (autopilot). Marks the note shown. For a caller that must put the note on
		// top of a screen it is opening itself; everyone else calls Note.
		internal static GuideMessage? Take(GuideNote note, string city = "")
		{
			Game g = Game.Instance;
			if (g is null || !g.InstantAdvice || Settings.Instance.Autopilot) return null;
			if (!g.GuideNotesShown.Add(note)) return null;
			return new GuideMessage(Text(note, city));
		}

		internal static void Note(GuideNote note, string city = "")
		{
			if (Take(note, city) is GuideMessage m) GameTask.Enqueue(Message.Guide(m));
		}
	}
}
