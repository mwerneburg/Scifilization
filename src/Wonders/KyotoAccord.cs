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

namespace CivOne.Wonders
{
	// The one wonder that works for everybody. While it stands, the world can carry half again
	// as much smoke before the climate turns (Game.WarmingThreshold) — the builder's rivals
	// included, because the atmosphere is not divisible. What the builder gets that nobody else
	// does is the culture and the score.
	//
	// The machines never signed it: while the Reprocessor runs, the Accord does nothing.
	internal class KyotoAccord : BaseWonder
	{
		private static readonly string[] _page1 =
		{
			"The KYOTO ACCORD binds every",
			"nation to limits on what it",
			"puts into the sky.",
			"",
			"While it stands, the world can",
			"carry HALF AGAIN as much",
			"pollution before GLOBAL WARMING.",
		};

		private static readonly string[] _page2 =
		{
			"Requires PLANETARY STEWARDSHIP.",
			"",
			"It helps every civilization,",
			"not just the one that builds it.",
			"",
			"The machines never signed it.",
			"Against THE REPROCESSOR it does",
			"nothing.",
		};

		public override string[] GetPageText(byte pageNumber) => pageNumber == 1 ? _page1 : _page2;

		public KyotoAccord() : base(60)
		{
			Name = "Kyoto Accord";
			RequiredTech = new PlanetaryStewardship();
			ObsoleteTech = null;
			SetSmallIcon(1, 5);
			Type = Wonder.KyotoAccord;
		}
	}
}
