#!/bin/bash
# Checks a packaged game carries the hand-made tiles and palette (runtime/sdl/Resources/defaults/data)
# and the Earth maps (resources/), which it reads from data/:  tools/check_shipped_data.sh <binary>
# A build without one still runs, just plainly: 1.8.1 and earlier drew the fallback tiles
# and could not start EARTH (EPIC). Used by package_mac.sh and package_windows.sh.
set -euo pipefail
for f in free_tiles.txt shore_tiles.txt lake_shores.txt river_overlays.txt improvement_tiles.txt \
         difficulty_tiles.txt advisor_badges.txt palette.txt earth_epic.bin earth_standard.bin; do
	LC_ALL=C grep -aq "CivOne.Resources.defaults.data.$f" "$1" || { echo "$1 does not carry $f"; exit 1; }
done
