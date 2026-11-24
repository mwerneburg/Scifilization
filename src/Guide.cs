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
		Welcome, FirstCity, Grow, Research, Hut, Barbarians, Disorder, Treasury, Settlers, Monarchy,
		War, Wonder, Materials, Victory
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

		// `subject` is the note's one variable: a city, a wonder, a material.
		internal static string[] Text(GuideNote note, string subject = "") => note switch
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
				$"{subject}! Every story in my archive",
				"began with a city like this one.",
				"",
				"Click it to choose what it builds:",
				"Warriors keep it safe, Settlers",
				"found the next. Press Enter when",
				"you are ready to end the turn.",
			],
			GuideNote.Grow => [
				$"{subject} has reached size 3.",
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
				$"{subject} is in disorder: more",
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
			GuideNote.War => [
				"War. My archive holds many, and",
				"the peoples who reached the stars",
				"lived through theirs. Fortify a",
				"defender in every city: hills and",
				"City Walls make it stronger.",
				"For peace, TALK from the",
				"Intelligence Advisor (F3) once",
				"you hold an embassy.",
			],
			GuideNote.Wonder => [
				"We can now build a WONDER:",
				$"{subject}.",
				"Each stands once in all the world",
				"and gives its builder a lasting",
				"gift. If a rival finishes first,",
				"switch to another wonder: the",
				"shields carry over.",
			],
			GuideNote.Materials => [
				$"We hold no {subject.ToUpperInvariant()}, so this",
				"costs half again in shields.",
				"Deposits lie in special tiles:",
				"copper and coal in hills, iron in",
				"mountains, oil and salt in desert.",
				"Work one from a city, or walk",
				"Settlers there and press Y to",
				"make camp.",
			],
			GuideNote.Victory => [
				"Four cities! The peoples in my",
				"archive are remembered six ways:",
				"conquest, commerce, culture, the",
				"Dome, the stars, or leading the",
				"world in 2100. Temples, libraries",
				"and wonders build CULTURE. The",
				"Civilopedia's WINNING THE GAME",
				"tells the rest.",
			],
			_ => [],
		};

		// The note's screen, or null when it has been shown, the guide is off, or nobody is
		// watching (autopilot). Marks the note shown. For a caller that must put the note on
		// top of a screen it is opening itself; everyone else calls Note.
		internal static GuideMessage? Take(GuideNote note, string subject = "")
		{
			Game g = Game.Instance;
			if (g is null || !g.InstantAdvice || Settings.Instance.Autopilot) return null;
			if (!g.GuideNotesShown.Add(note)) return null;
			return new GuideMessage(Text(note, subject));
		}

		internal static void Note(GuideNote note, string subject = "")
		{
			if (Take(note, subject) is GuideMessage m) GameTask.Enqueue(Message.Guide(m));
		}
	}
}
