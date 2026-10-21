// CivOne tests
//
// Fourteen save slots, up from eight (Oct 2026): a long run of experiments filled all eight.
// The list must still fit on a 200-line screen in the taller fallback font (16 did not), and the AutoSave row must stay last on the load list.

using System;
using System.IO;
using System.Reflection;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class SaveSlotTests
	{
		private static object? Get(object o, string name)
		{
			const BindingFlags f = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
			Type t = o.GetType();
			return t.GetField(name, f)?.GetValue(o) ?? t.GetProperty(name, f)!.GetValue(o);
		}

		private static void Press(BaseScreen s, Key k, int times = 1)
		{
			for (int i = 0; i < times; i++) s.KeyDown(new KeyboardEventArgs(k, KeyModifier.None));
		}

		private static void AssertFits(BaseScreen s)
		{
			int y = (int)Get(s, "PanelY")!, h = (int)Get(s, "PanelH")!, H = (int)Get(s, "Height")!;
			Assert.True(y >= 0 && y + h <= H, $"panel {y}..{y + h} on a {H}-high screen");
		}

		[Fact]
		public void TheLastSlotSavesAndLists()
		{
			Sim.NewGame(width: 80, height: 50);
			SaveGame.SelectedGame = 0;
			string file = Path.Combine(Settings.Instance.SavesDirectory, "c", "CIVIL13.cos");
			File.Delete(file);

			var save = new SaveGame();
			AssertFits(save);
			Press(save, Key.Down, 20);   // runs off the end: stops at the last slot
			Press(save, Key.Enter);
			Assert.True(File.Exists(file));

			var load = new LoadGame(null!);
			AssertFits(load);
			var slots = (Array)Get(load, "_slots")!;
			Assert.Equal(15, slots.Length);
			Assert.True((bool)Get(slots.GetValue(13)!, "Exists")!);
			Assert.True((bool)Get(slots.GetValue(14)!, "IsAuto")!);
			File.Delete(file);
		}
	}
}
