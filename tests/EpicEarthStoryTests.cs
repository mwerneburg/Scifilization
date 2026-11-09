// CivOne tests
//
// The Auriga's story played only after Customize World. Picking EARTH (EPIC) now asks
// whether to watch it first (the user, Nov 2026); No goes on as before.

using System.Linq;
using System.Reflection;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class EpicEarthStoryTests
	{
		private static object? NextAfter(int choice)
		{
			Sim.EnsureRuntime();
			Sim.ResetState();   // an unloaded map: LoadEarthEpic refuses a loaded one
			typeof(Sim).GetMethod("StageEarthBin", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { "earth_epic.bin" });
			var credits = new Credits();
			typeof(Credits).GetMethod("EarthEpic", BindingFlags.NonPublic | BindingFlags.Instance)!
				.Invoke(credits, new object?[] { null, System.EventArgs.Empty });
			FieldInfo next = typeof(Credits).GetField("_nextScreen", BindingFlags.NonPublic | BindingFlags.Instance)!;
			Assert.Null(next.GetValue(credits));   // it asks first
			var menus = (System.Collections.IList)typeof(BaseScreen)
				.GetField("_menus", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(credits)!;
			Menu<int> story = menus.OfType<Menu<int>>().Single();
			Assert.Equal(new[] { "Yes", "No" }, story.Items.Select(i => i.Text));
			story.Items[choice].Select();
			return next.GetValue(credits);
		}

		[Fact]
		public void YesPlaysTheStory() => Assert.IsType<AurigaIntroScreen>(NextAfter(0));

		[Fact]
		public void NoGoesOnAsBefore() => Assert.IsNotType<AurigaIntroScreen>(NextAfter(1));
	}
}
