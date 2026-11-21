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
using CivOne.Screens.Dialogs;

namespace CivOne.IO
{
	public class FileSystem
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		public static bool CopyPlugins(string folder)
		{
			Queue<OverwritePlugin> dialogs = new();

			Log("Copying plugins...");
			foreach (string filepath in Directory.GetFiles(folder))
			{
				if (!Plugin.Validate(filepath))
				{
					Log($"- Invalid plugin file: {filepath}");
					continue;
				}

				string filename = Path.GetFileName(filepath);
				string destinationFile = Path.Combine(Settings.Instance.PluginsDirectory, Path.GetFileName(filename));
				if (File.Exists(destinationFile))
				{
					Log($"- Plugin already exists: {filepath}");
					dialogs.Enqueue(new OverwritePlugin(filepath, destinationFile));
					continue;
				}

				Log($"- Plugin copied: {filepath}");
				File.Copy(filepath, destinationFile);
				Reflect.LoadPlugin(destinationFile);
			}

			Action nextDialog = null!;
			nextDialog = () =>
			{
				if (dialogs.Count == 0) return;
				OverwritePlugin dialog = dialogs.Dequeue();
				dialog.Closed += (s, a) => nextDialog();
				Common.AddScreen(dialog);
			};
			nextDialog();

			return true;
		}
	} 
}