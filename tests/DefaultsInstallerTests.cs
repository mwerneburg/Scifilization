// CivOne tests
//
// The art and data shipped inside the executable are copied to the storage directory, and
// that copy used to skip any file already there: a redrawn image in an update never reached
// anyone who had installed an earlier version. Now a file is refreshed when the shipped copy
// changes, unless the player edited it.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CivOne.IO;

namespace CivOne.Tests
{
	public class DefaultsInstallerTests : IDisposable
	{
		private readonly string _dir = Path.Combine(Path.GetTempPath(), "civone-defaults-" + Guid.NewGuid().ToString("N"));
		private string ArtPath => Path.Combine(_dir, "data", "event_art", "Landing.png");

		public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

		private void Ship(string build, string content)
			=> DefaultsInstaller.Install(_dir, build, new[]
			{
				new KeyValuePair<string, Func<Stream>>("data/event_art/Landing.png",
					() => new MemoryStream(Encoding.UTF8.GetBytes(content)))
			});

		private string OnDisk => File.ReadAllText(ArtPath);

		[Fact]
		public void AFreshInstallWritesTheFile()
		{
			Ship("build-1", "v1 art");
			Assert.Equal("v1 art", OnDisk);
		}

		// The bug: 1.0.0 art stayed on disk after an update shipped a new picture.
		[Fact]
		public void AnUpdateRefreshesAFileThePlayerNeverTouched()
		{
			Ship("build-1", "v1 art");
			Ship("build-2", "v2 art");
			Assert.Equal("v2 art", OnDisk);
		}

		[Fact]
		public void APlayersEditSurvivesAnUpdate()
		{
			Ship("build-1", "v1 art");
			File.WriteAllText(ArtPath, "my own art");
			Ship("build-2", "v2 art");
			Assert.Equal("my own art", OnDisk);
			// And the one after: the edit is still theirs, not a new baseline.
			Ship("build-3", "v3 art");
			Assert.Equal("my own art", OnDisk);
		}

		// Installs from before the manifest (1.0.0, your brother's) have no record at all; their
		// copies are the old defaults, so they are refreshed once.
		[Fact]
		public void AnInstallFromBeforeTheManifestIsRefreshed()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(ArtPath)!);
			File.WriteAllText(ArtPath, "v1 art");
			Ship("build-2", "v2 art");
			Assert.Equal("v2 art", OnDisk);
		}

		// A deleted file comes back, even on the same build.
		[Fact]
		public void AMissingFileIsReinstalled()
		{
			Ship("build-1", "v1 art");
			File.Delete(ArtPath);
			Ship("build-1", "v1 art");
			Assert.Equal("v1 art", OnDisk);
		}
	}
}
