// CivOne tests
//
// Every start made saves/a through saves/z, one folder per DOS drive letter, and Save and Load
// only ever use c. The user found 25 empty folders beside their saves (Nov 2026).

using System.IO;
using System.Reflection;

namespace CivOne.Tests
{
	public class SavesFolderTests
	{
		[Fact]
		public void StartupMakesTheSaveFolderAndNoOthers()
		{
			Sim.EnsureRuntime();
			string saves = Settings.Instance.SavesDirectory;
			foreach (string d in new[] { "a", "z" })
				if (Directory.Exists(Path.Combine(saves, d)) && Directory.GetFileSystemEntries(Path.Combine(saves, d)).Length == 0)
					Directory.Delete(Path.Combine(saves, d));
			if (Directory.Exists(Path.Combine(saves, "c")) && Directory.GetFileSystemEntries(Path.Combine(saves, "c")).Length == 0)
				Directory.Delete(Path.Combine(saves, "c"));

			typeof(Settings).GetMethod("CreateDirectories", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Settings.Instance, null);

			Assert.True(Directory.Exists(Path.Combine(saves, "c")), "no saves/c, where Save and Load look");
			Assert.False(Directory.Exists(Path.Combine(saves, "a")), "made saves/a");
			Assert.False(Directory.Exists(Path.Combine(saves, "z")), "made saves/z");
		}
	}
}
