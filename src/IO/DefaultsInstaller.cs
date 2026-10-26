// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CivOne.IO
{
	// Copies the art and data shipped inside the executable into the storage directory, where
	// the game reads them and a player may swap in their own. It used to skip every file that
	// already existed, so a redrawn image in an update never reached anyone who had installed
	// an earlier version (1.0.0 shipped 25 Oct 2026).
	//
	// defaults.manifest records the build and a hash of every file as installed. A file whose
	// hash still matches is untouched and is replaced when the shipped copy changes; one that
	// no longer matches was edited by the player and is left alone. A file with no record (an
	// install from before the manifest) counts as untouched.
	public static class DefaultsInstaller
	{
		public const string ManifestName = "defaults.manifest";

		// files: path relative to storageDir with '/' separators, and how to open the shipped copy.
		public static void Install(string storageDir, string build, IEnumerable<KeyValuePair<string, Func<Stream>>> files)
		{
			string manifestPath = Path.Combine(storageDir, ManifestName);
			string[] lines = File.Exists(manifestPath) ? File.ReadAllLines(manifestPath) : new string[0];
			// Same build as last launch: nothing shipped has changed, so only replace what is
			// missing. Keeps an ordinary launch from hashing a few hundred MB of art.
			bool sameBuild = lines.Length > 0 && lines[0] == build;
			var installed = new Dictionary<string, string>();
			foreach (string line in lines.Skip(1))
			{
				int tab = line.IndexOf('\t');
				if (tab > 0) installed[line.Substring(0, tab)] = line.Substring(tab + 1);
			}

			foreach (KeyValuePair<string, Func<Stream>> file in files)
			{
				string targetPath = Path.Combine(new[] { storageDir }.Concat(file.Key.Split('/')).ToArray());
				bool exists = File.Exists(targetPath);
				if (exists && sameBuild) continue;

				byte[] shipped;
				using (Stream src = file.Value())
				{
					if (src == null) continue;
					using (var ms = new MemoryStream()) { src.CopyTo(ms); shipped = ms.ToArray(); }
				}
				string hash = Hash(shipped);
				if (exists)
				{
					string onDisk = Hash(File.ReadAllBytes(targetPath));
					if (onDisk == hash) { installed[file.Key] = hash; continue; }
					// The player's edit: theirs wins, and the old record stays so it keeps winning.
					if (installed.TryGetValue(file.Key, out string was) && was != onDisk) continue;
				}
				Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
				File.WriteAllBytes(targetPath, shipped);
				installed[file.Key] = hash;
			}

			if (!sameBuild)
				File.WriteAllLines(manifestPath, new[] { build }.Concat(installed.Select(kv => kv.Key + "\t" + kv.Value)));
		}

		private static string Hash(byte[] data)
		{
			using (SHA256 sha = SHA256.Create())
				return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
		}
	}
}
