// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyProduct("Scifilization")]
// The one place the version is written. Common.Version reads it, the title screen and
// --version show it, and tools/package_mac.sh copies it into the app's Info.plist.
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]

[assembly:InternalsVisibleTo("CivOne.UnitTests")]