#!/bin/bash
# Builds Scifilization.app and a DMG for Apple silicon:  tools/package_mac.sh
# Output: bin/Scifilization-<version>-macos-arm64.dmg (bin/ is git-ignored).
#
# Self-contained (no .NET install needed) with libSDL2 bundled beside the executable, where
# Program.RegisterNativeResolver looks first. Ad-hoc signed, not notarized: the first launch
# is refused until the player clicks Open Anyway in System Settings > Privacy & Security.
# ponytail: arm64 only. An Intel build needs an x86_64 libSDL2 (Homebrew here is arm64) and a
# second publish with -r osx-x64; add it when someone asks.
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH=$PATH:/opt/homebrew/bin/dotnet
export DOTNET_ROOT=${DOTNET_ROOT:-/opt/homebrew/Cellar/dotnet/10.0.300/libexec}

SDL=${SDL2_DYLIB:-/opt/homebrew/lib/libSDL2.dylib}
VERSION=$(sed -n 's/.*AssemblyInformationalVersion("\(.*\)").*/\1/p' shared/Properties/AssemblyInfo.cs)
[ -n "$VERSION" ] || { echo "no version in shared/Properties/AssemblyInfo.cs"; exit 1; }
[ -f "$SDL" ] || { echo "libSDL2 not found at $SDL (brew install sdl2, or set SDL2_DYLIB)"; exit 1; }

OUT=bin/package
APP=$OUT/Scifilization.app
rm -rf "$OUT"
dotnet publish runtime/sdl/CivOne.SDL.csproj -c ReleaseMacOS -r osx-arm64 --self-contained true \
	-p:PublishSingleFile=true -o "$OUT/publish" -v q

mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$OUT/publish/CivOne.SDL" "$APP/Contents/MacOS/Scifilization"
cp -L "$SDL" "$APP/Contents/MacOS/libSDL2.dylib"
# Homebrew builds .NET from source and links the apphost against its own brotli, which a
# player's Mac doesn't have: bundle it and point the executable at the copies.
BROTLI=$(otool -L "$APP/Contents/MacOS/Scifilization" | awk '/libbrotlidec/{print $1}' | xargs dirname)
for lib in common dec enc; do
	cp -L "$BROTLI/libbrotli$lib.1.dylib" "$APP/Contents/MacOS/"
	install_name_tool -change "$BROTLI/libbrotli$lib.1.dylib" "@executable_path/libbrotli$lib.1.dylib" \
		"$APP/Contents/MacOS/Scifilization" 2>/dev/null
	install_name_tool -change @rpath/libbrotlicommon.1.dylib @loader_path/libbrotlicommon.1.dylib \
		"$APP/Contents/MacOS/libbrotli$lib.1.dylib" 2>/dev/null
done
for lib in "$APP"/Contents/MacOS/*.dylib; do install_name_tool -id "@executable_path/$(basename "$lib")" "$lib" 2>/dev/null; done
if otool -L "$APP"/Contents/MacOS/* | grep -E '/opt/homebrew|/usr/local|@rpath'; then
	echo "the app still links the libraries above, which a player's Mac won't have"; exit 1
fi
cp resources/macos/CivOne.icns "$APP/Contents/Resources/"
# The oldest macOS the .NET apphost itself was built for.
MINOS=$(otool -l "$APP/Contents/MacOS/Scifilization" | awk '/LC_BUILD_VERSION/{f=1} f&&/minos/{print $2; exit}')
sed -e "s/__VERSION__/$VERSION/g" -e "s/__MINOS__/$MINOS/" resources/macos/Info.plist > "$APP/Contents/Info.plist"
codesign --force --deep --sign - "$APP"

DMG=bin/Scifilization-$VERSION-macos-arm64.dmg
mkdir -p "$OUT/dmg"
cp -R "$APP" "$OUT/dmg/"
ln -s /Applications "$OUT/dmg/Applications"
rm -f "$DMG"
hdiutil create -volname "Scifilization $VERSION" -srcfolder "$OUT/dmg" -ov -format UDZO "$DMG" >/dev/null
echo "$DMG ($(du -h "$DMG" | cut -f1)), macOS $MINOS or later"
