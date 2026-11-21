// CivOne tests
//
// The start menu and the other pre-game menus kept the DOS palette's colour numbers: text in
// slot 5, the highlight in slot 11. In our palette slot 5 is the dark border brown, so every
// unselected item was dark brown on black (reported Nov 2026). They now use the dialogs'
// colours: light text on a faint amber highlight.

using System.Linq;
using System.Reflection;
using CivOne.Graphics;
using CivOne.Screens;

namespace CivOne.Tests
{
	public class MainMenuColourTests
	{
		private static double Luma(Colour c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;

		[Fact]
		public void TheStartMenuTextIsLightAndStandsOffItsHighlight()
		{
			Sim.EnsureRuntime();
			var credits = new Credits();
			typeof(Credits).GetMethod("CreateMenu", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(credits, null);
			var menus = (System.Collections.Generic.List<IMenu>)typeof(BaseScreen)
				.GetField("_menus", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(credits)!;
			Menu menu = (Menu)menus.Single();

			double text = Luma(menu.Palette[menu.TextColour]);
			double bar  = Luma(menu.Palette[menu.ActiveColour]);

			Assert.True(text >= 150, $"menu text luma {text:0}");
			Assert.True(text - bar >= 100, $"text {text:0} vs highlight {bar:0}");
			menu.Close();
		}
	}
}
