#!/bin/bash
# Builds the Windows x64 zip on this Mac:  tools/package_windows.sh
# Output: bin/Scifilization-<version>-windows-x64.zip (bin/ is git-ignored), holding a
# Scifilization folder with Scifilization.exe and SDL2.dll.
#
# Self-contained single-file exe (no .NET install needed), cross-built here. SDL2.dll is the
# official libsdl-org build, pinned to the version the Mac app bundles and checked against
# its published sha256. Unsigned: SmartScreen warns on the first run until the player clicks
# More info > Run anyway.
# ponytail: x64 only. Official SDL2 has no ARM64 Windows build; ARM laptops run x64 emulated.
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH=$PATH:/opt/homebrew/bin/dotnet
export DOTNET_ROOT=${DOTNET_ROOT:-/opt/homebrew/Cellar/dotnet/10.0.300/libexec}

SDL_VERSION=2.32.10
SDL_SHA256=6cf9706eefd0a4a06dc764007934d428afaf029fabdd408a9e646048c91e18fb
VERSION=$(sed -n 's/.*AssemblyInformationalVersion("\(.*\)").*/\1/p' shared/Properties/AssemblyInfo.cs)
[ -n "$VERSION" ] || { echo "no version in shared/Properties/AssemblyInfo.cs"; exit 1; }

OUT=bin/package-windows
DIR=$OUT/Scifilization
rm -rf "$OUT"
mkdir -p "$DIR"
dotnet publish runtime/sdl/CivOne.SDL.csproj -c ReleaseWindows -r win-x64 --self-contained true \
	-p:PublishSingleFile=true -o "$OUT/publish" -v q
file "$OUT/publish/CivOne.SDL.exe" | grep -q "(GUI)" || { echo "the exe is not a GUI exe; a console window would open"; exit 1; }
cp "$OUT/publish/CivOne.SDL.exe" "$DIR/Scifilization.exe"
tools/check_shipped_data.sh "$DIR/Scifilization.exe"

SDLZIP=bin/SDL2-$SDL_VERSION-win32-x64.zip
[ -f "$SDLZIP" ] || curl -fsSL -o "$SDLZIP" \
	"https://github.com/libsdl-org/SDL/releases/download/release-$SDL_VERSION/SDL2-$SDL_VERSION-win32-x64.zip"
echo "$SDL_SHA256  $SDLZIP" | shasum -a 256 -c - >/dev/null \
	|| { rm -f "$SDLZIP"; echo "SDL2 download does not match its published checksum; removed it"; exit 1; }
unzip -q "$SDLZIP" SDL2.dll README-SDL.txt -d "$DIR"

ZIP=bin/Scifilization-$VERSION-windows-x64.zip
rm -f "$ZIP"
(cd "$OUT" && zip -qr9 "../../$ZIP" Scifilization)
echo "$ZIP ($(du -h "$ZIP" | cut -f1))"
