// CivOne tests
//
// The hand-made tiles and palette were read only from the launch folder. build.sh runs the game
// from the repo, so they were always there in development; the DMG opened from Finder, and a
// Windows build on CivFanatics, started elsewhere and drew the fallback tiles instead (the user,
// Nov 2026). The runtime now installs them into the data folder, and Common.ArtFile falls back
// to that copy.

using System;
using System.IO;
using System.Linq;
using CivOne.Graphics;

namespace CivOne.Tests
{
	public class PackagedArtFilesTests
	{
		// A packaged build: nothing in the launch folder, the installed copy in data/.
		[Fact]
		public void TilesInstalledInTheDataFolderAreDrawn()
		{
			Sim.EnsureRuntime();
			Assert.False(File.Exists(Path.Combine(Environment.CurrentDirectory, "free_tiles.txt")),
				"scenario is broken: the test host's launch folder has its own free_tiles.txt");
			Directory.CreateDirectory(Settings.Instance.DataDirectory);
			string file = Path.Combine(Settings.Instance.DataDirectory, "free_tiles.txt");
			File.WriteAllText(file, "[salt_flat]\n" + string.Join("\n", Enumerable.Repeat(string.Join(" ", Enumerable.Repeat("7", 16)), 16)) + "\n");
			try
			{
				Free.Instance.ReloadTiles();
				Assert.All(Free.Instance.SaltFlat.ToByteArray(), b => Assert.Equal(7, b));
			}
			finally
			{
				File.Delete(file);
				Free.Instance.ReloadTiles();
			}
		}

		// The launch folder's copy still wins, so editing the repo's file and choosing
		// Debug > Reload free tiles shows the edit.
		[Fact]
		public void ALaunchFolderCopyWins()
		{
			Sim.EnsureRuntime();
			string dir = Path.Combine(Path.GetTempPath(), "civone-launch-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			File.WriteAllText(Path.Combine(dir, "palette.txt"), "");
			string was = Environment.CurrentDirectory;
			try
			{
				Environment.CurrentDirectory = dir;
				Assert.Equal(Path.Combine(Environment.CurrentDirectory, "palette.txt"), Common.ArtFile("palette.txt"));
				Assert.Equal(Path.Combine(Settings.Instance.DataDirectory, "shore_tiles.txt"), Common.ArtFile("shore_tiles.txt"));
			}
			finally
			{
				Environment.CurrentDirectory = was;
				Directory.Delete(dir, true);
			}
		}
	}
}
