// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.Graphics;

namespace CivOne.Screens
{
	// The Federation arc, beat by beat. Earth does not learn they are human until the
	// Ultimatum, and the point man's lack of ceremony about it is the horror.
	internal class FederationTransmission : TerminalScreen
	{
		internal enum Beat { Ultimatum, Compliance, Annihilation, Landing, Sovereignty }

		private static string[] BuildLines(Beat beat, string gameDate, bool domeHeld, int destroyed, string? capital, int seized)
		{
			var lines = new List<string>();
			switch (beat)
			{
				case Beat.Ultimatum:
					lines.AddRange(new[]
					{
						"PRIORITY OMEGA — FIRST CONTACT",
						$"CONTACT TIMESTAMP: {gameDate}",
						"STATUS: ORBITAL — FORMATION HOLDING",
						"",
						"TRIANGULAR HULLS OVER EVERY CONTINENT.",
						"THEY DID NOT FIRE. THEY DID NOT NEED TO.",
						capital is null
							? "A SHUTTLE CAME DOWN. ONE MAN GOT OUT."
							: $"A SHUTTLE CAME DOWN AT {capital.ToUpper()}. ONE MAN GOT OUT.",
						"",
						"HE WAS HUMAN.",
						"",
						"[TRANSCRIPT — NO TRANSLATION REQUIRED]",
						">> 'YOUR ANCESTORS SENT THE FIRST SHIPS. MINE STAYED ON THEM.'",
						">> 'WE HAVE REVIEWED YOUR RECORD.'",
						destroyed == 1
							? ">> 'ONE PEOPLE EXTINGUISHED. NO STANDARDS. NO COMMAND.'"
							: $">> '{destroyed} PEOPLES EXTINGUISHED. NO STANDARDS. NO COMMAND.'",
						">> 'THE FEDERATION DOES NOT OBJECT TO THE KILLING.'",
						">> 'IT OBJECTS TO THE PAPERWORK.'",
						">> 'EARTH IS IN BREACH OF FEDERATION NORMS.'",
						">> 'YOU WILL SUBMIT TO REGISTRATION AND COMMAND.'",
						"",
						"ASSESSMENT: THEY ARE US.",
						"ASSESSMENT: THEY HAVE BEEN DOING WHAT WE DID, FOR LONGER.",
						domeHeld
							? "ASSESSMENT: THE DOME HOLDS THEIR CAPITAL SHIPS AT RANGE."
							: "ASSESSMENT: NOTHING STANDS BETWEEN THE FLEET AND THE SURFACE.",
						domeHeld
							? "ASSESSMENT: WE ARE IN A POSITION TO ANSWER."
							: "ASSESSMENT: ANY ANSWER BUT ONE WILL BE THE LAST.",
						"",
						"AWAITING EARTH'S ANSWER.",
					});
					break;

				case Beat.Compliance:
					lines.AddRange(new[]
					{
						"PRIORITY OMEGA — REGISTRATION",
						$"TIMESTAMP: {gameDate}",
						"STATUS: SURFACE — COMPLIANT",
						"",
						"EARTH HAS SUBMITTED.",
						"",
						">> 'A SENSIBLE DECISION.'",
						">> 'YOUR CITIES WILL BE NUMBERED.'",
						">> 'YOUR ARMIES WILL BE INDUCTED.'",
						">> 'THEY WILL SEE A GREAT DEAL OF SERVICE.'",
						">> 'WELCOME TO THE FEDERATION.'",
						"",
						"ASSESSMENT: WE ARE STILL ALIVE.",
						"ASSESSMENT: WE ARE NO LONGER OURSELVES.",
						"",
						"THE HOMEWORLD HAS BEEN RECOVERED.",
						"",
						"TRANSMISSION ENDS.",
					});
					break;

				case Beat.Annihilation:
					lines.AddRange(new[]
					{
						"PRIORITY OMEGA",
						$"TIMESTAMP: {gameDate}",
						"STATUS: ORBITAL — UNKNOWN",
						"",
						"EARTH HAS REFUSED.",
						"",
						">> 'NOTED.'",
						"",
						"THE FORMATION WITHDREW TO HIGH ORBIT.",
						"FOR SIX HOURS, NOTHING.",
						"",
						"THEN SOMETHING ROSE OVER THE LIMB OF THE MOON.",
						"IT WAS TOO BIG TO BE A SPACE STATION.",
						"",
						"[FINAL TELEMETRY]",
						"",
						"",
						"TRANSMISSION ENDS.",
					});
					break;

				case Beat.Landing:
					lines.AddRange(new[]
					{
						"PRIORITY OMEGA — HOSTILE LANDING",
						$"TIMESTAMP: {gameDate}",
						"STATUS: SURFACE — CONTESTED",
						"",
						"EARTH HAS REFUSED.",
						"",
						">> 'THEN WE WILL DO THIS THE WAY YOU DID.'",
						"",
						"THE DOME HELD THE CAPITAL SHIPS IN ORBIT.",
						"THE LANDERS CAME IN BENEATH IT.",
						$"{seized} CITIES HAVE BEEN TAKEN.",
						"",
						"ASSESSMENT: THEY FIGHT THE WAY WE FIGHT.",
						"ASSESSMENT: WITHOUT THE FLEET ABOVE THEM, THEY CAN BE BEATEN.",
						"",
						"RECOMMENDATION: DRIVE THEM OFF.",
						"THE LAST FEDERATION CITY ENDS THIS.",
						"",
						"TRANSMISSION ENDS.",
					});
					break;

				case Beat.Sovereignty:
					lines.AddRange(new[]
					{
						"PRIORITY OMEGA — ALL STATIONS",
						$"TIMESTAMP: {gameDate}",
						"STATUS: ORBITAL — DEPARTING",
						"",
						"THE LAST FEDERATION GARRISON HAS FALLEN.",
						"",
						">> 'THIS WORLD IS DESIGNATED HOSTILE.'",
						">> 'IT WILL BE LEFT ALONE.'",
						"",
						"THE TRIANGULAR HULLS HAVE BROKEN ORBIT.",
						"",
						"ASSESSMENT: THEY WILL REMEMBER US AS WE REMEMBERED THEM.",
						"ASSESSMENT: AS A WARNING.",
						"",
						"EARTH IS ITS OWN.",
						"",
						"TRANSMISSION ENDS.",
					});
					break;
			}
			return lines.ToArray();
		}

		protected override byte ColorFor(int lineIndex, string text)
		{
			if (lineIndex == 0)                             return CassetteTheme.PHOS_GLOW;
			if (text.StartsWith("TIMESTAMP") || text.StartsWith("CONTACT TIMESTAMP")
			    || text.StartsWith("STATUS:"))              return CassetteTheme.PHOS_DIM;
			if (text.StartsWith("["))                       return CassetteTheme.INK_HIGH;
			if (text.StartsWith(">> "))                     return CassetteTheme.PHOS;
			if (text == "HE WAS HUMAN."
			    || text == "IT WAS TOO BIG TO BE A SPACE STATION."
			    || text.EndsWith("CITIES HAVE BEEN TAKEN.")) return CassetteTheme.ALERT;
			if (text.StartsWith("ASSESSMENT:"))             return CassetteTheme.INK_MID;
			if (text.StartsWith("RECOMMENDATION")
			    || text == "THE LAST FEDERATION CITY ENDS THIS."
			    || text == "EARTH IS ITS OWN."
			    || text == "THE HOMEWORLD HAS BEEN RECOVERED.") return CassetteTheme.PHOS_GLOW;
			if (text == "TRANSMISSION ENDS.")               return CassetteTheme.INK_LOW;
			return CassetteTheme.INK_MID;
		}

		internal FederationTransmission(Beat beat, string gameDate, bool domeHeld = false,
			int destroyed = 0, string? capital = null, int seized = 0)
		{
			_lines = BuildLines(beat, gameDate, domeHeld, destroyed, capital, seized);
			InitTypewriter();
		}
	}
}
