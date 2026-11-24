// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.Screens.Dialogs
{
	// A note from Vesta (Guide.cs): the cabinet's AdvisorMessage layout, badge on the left,
	// with a footer saying how to stop the notes. Its own class, not an AdvisorMessage,
	// because Message.Run drops an AdvisorMessage while another is on screen, and a note
	// that is dropped has already been marked shown.
	internal class GuideMessage : BaseDialog
	{
		internal const string Footer = "N: no more notes";
		private const int Margin = 4;
		private const int Top = 36;

		private static int TextLeft => Margin + Free.BadgeSize + Margin;

		private static Picture[] Lines(string[] message) =>
			message.Select(l => Resources.GetText(l.Length == 0 ? " " : l, 0, 15)).ToArray();

		internal static int WidthFor(string[] message) =>
			Lines(message.Append(Footer).Append($"{Guide.Name}:").ToArray()).Max(b => b.Width) + 8 + TextLeft;

		internal static int HeightFor(string[] message)
		{
			int lineHeight = Resources.GetText(" ", 0, 15).Height;
			int text = 4 + 9 + 4 + (message.Length + 1) * lineHeight + 6;
			return System.Math.Max(text, Margin + Free.BadgeSize + Margin);
		}

		// Centred across the 320-wide screen; GuideTests holds every note inside it.
		internal static int LeftFor(string[] message) => (320 - WidthFor(message)) / 2;
		internal static int TopFor => Top;

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (char.ToUpperInvariant(args.KeyChar) == 'N') Game.Instance.InstantAdvice = false;
			return base.KeyDown(args);
		}

		public GuideMessage(string[] message)
			: base(LeftFor(message), Top, WidthFor(message), HeightFor(message))
		{
			if (Free.Instance.AdvisorBadge("guide") is Bytemap badge)
				DialogBox.AddLayer(badge, Margin, Margin);

			string title = $"{Guide.Name}:";
			DialogBox.DrawText(title, 0, 15, TextLeft, 4);
			DialogBox.FillRectangle(TextLeft, 11, Resources.GetText(title, 0, 15).Width + 1, 1, 11);

			Picture[] lines = Lines(message);
			int y = 13;
			foreach (Picture line in lines)
			{
				DialogBox.AddLayer(line, TextLeft, y);
				y += line.Height;
			}
			DialogBox.DrawText(Footer, 0, CassetteTheme.INK_LOW, TextLeft, y + 2);
		}
	}
}
