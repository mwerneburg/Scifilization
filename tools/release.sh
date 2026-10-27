#!/bin/bash
# Releases a new version of Scifilization:  tools/release.sh 1.1.1
#
#  1. Checks you are on master, everything is committed, and the version is new.
#  2. Writes the version into shared/Properties/AssemblyInfo.cs (the one place it lives).
#  3. Builds both projects and runs the version test.
#  4. Builds the DMG with tools/package_mac.sh and checks its version and signature.
#  5. Drafts release notes from the commit messages since the last release and opens them in
#     $EDITOR (nano if unset). Save and close to continue.
#  6. Asks before anything leaves this Mac. On yes: commits "Version X.Y.Z.", pushes, tags,
#     publishes the GitHub release with the DMG attached, and checks the upload's checksum.
#
# Answer no, or let any step fail, and the version change is undone; nothing is pushed.
# The full test suite (~30 min) is not run here: run it before you commit the last fix.
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH=$PATH:/opt/homebrew/bin/dotnet
export DOTNET_ROOT=${DOTNET_ROOT:-/opt/homebrew/Cellar/dotnet/10.0.300/libexec}

INFO=shared/Properties/AssemblyInfo.cs
V=${1:-}
[[ $V =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "usage: tools/release.sh X.Y.Z   (e.g. 1.1.1)"; exit 1; }
PREV=$(sed -n 's/.*AssemblyInformationalVersion("\(.*\)").*/\1/p' $INFO)
[ "$V" != "$PREV" ] && [ "$(printf '%s\n' "$PREV" "$V" | sort -V | tail -1)" = "$V" ] \
	|| { echo "$V is not newer than the current version, $PREV"; exit 1; }
[ "$(git branch --show-current)" = master ] || { echo "not on master"; exit 1; }
git diff --quiet HEAD || { echo "uncommitted changes; commit them first:"; git status --short --untracked-files=no; exit 1; }
git fetch -q origin
git merge-base --is-ancestor origin/master HEAD || { echo "origin/master has commits you don't; pull first"; exit 1; }
! git rev-parse -q --verify "refs/tags/v$V" >/dev/null || { echo "tag v$V already exists"; exit 1; }
LAST=$(git describe --tags --abbrev=0)

trap 'git checkout -q -- $INFO; echo "Stopped; version change undone."' EXIT
sed -i '' -e "s/AssemblyVersion(\"[0-9.]*\")/AssemblyVersion(\"$V.0\")/" \
          -e "s/AssemblyInformationalVersion(\"[0-9.]*\")/AssemblyInformationalVersion(\"$V\")/" $INFO

echo "== Building $V"
dotnet build CivOne.csproj -v q -nologo | grep -E "Warning\(s\)|Error\(s\)"
dotnet build runtime/sdl/CivOne.SDL.csproj -v q -nologo | grep -E "Error\(s\)"
dotnet test tests/CivOne.Tests.csproj --filter "FullyQualifiedName~VersionTests" -v q -nologo | grep -E "Passed!|Failed"

echo "== Packaging"
tools/package_mac.sh
DMG=bin/Scifilization-$V-macos-arm64.dmg
APP=bin/package/Scifilization.app
[ "$(/usr/libexec/PlistBuddy -c 'Print CFBundleShortVersionString' $APP/Contents/Info.plist)" = "$V" ] \
	|| { echo "the app's Info.plist does not say $V"; exit 1; }
codesign --verify --deep --strict $APP
SHA=$(shasum -a 256 "$DMG" | cut -d' ' -f1)
echo "DMG ok: $DMG  sha256 $SHA"

NOTES=bin/release-notes-$V.md
[ -f "$NOTES" ] || { printf "## What's new in %s\n\n" "$V"; git log --reverse --format='- %s' "$LAST"..HEAD | grep -v '^- Version '; } > "$NOTES"
${EDITOR:-nano} "$NOTES"
echo; cat "$NOTES"; echo

read -r -p "Publish v$V? This commits the version, pushes, tags and uploads the DMG to GitHub. [y/N] " ok
[ "$ok" = y ] || [ "$ok" = Y ] || exit 1

git commit -q -m "Version $V." -- $INFO
trap - EXIT
git push -q origin master
git tag "v$V"
git push -q origin "v$V"
gh release create "v$V" "$DMG" --title "Scifilization ${V%.0}" --notes-file "$NOTES"
[ "$(gh release view "v$V" --json assets --jq '.assets[0].digest')" = "sha256:$SHA" ] \
	&& echo "Released v$V; the uploaded DMG matches." \
	|| echo "WARNING: the uploaded DMG's checksum does not match $SHA; check the release page."
