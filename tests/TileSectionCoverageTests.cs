// CivOne tests
//
// Every tile has a fallback drawn in code, so a section the code asks for and the files lack
// does not fail: the tile quietly reverts to the plain look. That is how the packaged builds
// went unnoticed (PackagedArtFilesTests). This pins the names the code asks for by literal to
// the sections the shipped files hold, so renaming or dropping one fails here instead.
// Names built at run time (shores by direction, rivers, road_/rail_ spokes) are not covered.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CivOne.Tests
{
	public class TileSectionCoverageTests
	{
		private static readonly string[] Files =
			{ "free_tiles.txt", "shore_tiles.txt", "lake_shores.txt", "river_overlays.txt",
			  "improvement_tiles.txt", "difficulty_tiles.txt", "advisor_badges.txt" };

		// Optional overrides: the code's own drawing is the art, and a section would replace it.
		private static readonly string[] Optional = { "forested_hills", "special_ocean" };

		private static string Code(string path) =>
			Regex.Replace(File.ReadAllText(Path.Combine(Sim.RepoRoot(), path)), @"//.*", "");   // not comments

		[Fact]
		public void EverySectionTheCodeNamesIsShipped()
		{
			var sections = Files.SelectMany(f => File.ReadLines(Path.Combine(Sim.RepoRoot(), Common.RepoArtDir, f)))
				.Select(l => Regex.Match(l, @"^\[([^\]]+)\]")).Where(m => m.Success)
				.Select(m => m.Groups[1].Value).ToHashSet();

			var named = Regex.Matches(Code("src/Graphics/Free.cs"), "\"([a-z][a-z0-9_]*[a-z0-9])\"")
				.Concat(new[] { "src/Graphics/Sprites/MapTile.cs", "src/Graphics/Icons.cs" }.SelectMany(f =>
					Regex.Matches(Code(f), "Free(?:\\.Instance)?\\.(?:Improvement|AdvisorBadge)\\(\"([a-z_]+)\"\\)")))
				.Concat(Regex.Matches(Regex.Match(Code("src/Screens/Dialogs/AdvisorMessage.cs"), @"names = \[(.*)\]").Value, "\"([a-z]+)\""))
				.Select(m => m.Groups[1].Value).Distinct().ToList();
			Assert.True(named.Count > 40, $"scenario is broken: only {named.Count} names found");

			Assert.Empty(named.Except(sections).Except(Optional));
		}
	}
}
