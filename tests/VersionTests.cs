// CivOne tests
//
// Version 1.0 (Oct 2026). The version is written once, in shared/Properties/AssemblyInfo.cs; the
// title screen, --version and the Mac app's Info.plist all read it from there.

using System.Text.RegularExpressions;

namespace CivOne.Tests
{
	public class VersionTests
	{
		// A missing attribute reads as "?", which would ship silently on the title screen.
		[Fact]
		public void TheGameKnowsItsVersion()
		{
			Assert.Matches(new Regex(@"^\d+\.\d+\.\d+$"), Common.Version);
			System.Version v = typeof(Common).Assembly.GetName().Version!;
			Assert.Equal($"{v.Major}.{v.Minor}.{v.Build}", Common.Version);
		}
	}
}
