// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Reflection;

// The one place the version is written. Common.Version reads it, the title screen and
// --version show it, and tools/package_mac.sh copies it into the app's Info.plist.
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0")]
// The compiled assembly carries the licence of the work as distributed, which is
// the fork's. The CC0 header above is upstream's and is accurate for this file's
// history — see NOTICE for why both are true at once.
[assembly: AssemblyCopyright("Copyright (C) 2026 Michael Werneburg - GPL-3.0-or-later")]