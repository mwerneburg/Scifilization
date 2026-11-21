// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.IO;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Tiles;

namespace CivOne
{
	public partial class Map
	{
		// Row -> latitude edges for a loaded Earth map (see LoadEarthBin). Null when
		// the map was generated, or loaded from a file without the table.
		public static float[]? LatitudeEdges { get; private set; }

		// Tile row containing this latitude. Uses the loaded edge table when there
		// is one, otherwise the flat -90..90 projection that generated maps assume.
		public static int RowForLatitude(double lat, int height)
		{
			float[]? edges = LatitudeEdges;
			if (edges is null || edges.Length != height + 1)
			{
				int flat = (int)(((90.0 - lat) / 180.0) * height);
				return flat < 0 ? 0 : (flat >= height ? height - 1 : flat);
			}
			if (lat >= edges[0]) return 0;
			for (int y = 0; y < height; y++)
				if (lat >= edges[y + 1]) return y;
			return height - 1;
		}

		// Load a procedurally-built Earth from a simple binary file. Format:
		//   bytes  0..3   ASCII magic "CIVE"
		//   byte   4      version (currently 1)
		//   bytes  5..7   reserved (zero)
		//   bytes  8..11  width  (uint32 little-endian)
		//   bytes 12..15  height (uint32 little-endian)
		//   bytes 16..    width*height terrain codes (0=Ocean, 2=Forest, 3=Swamp,
		//                 6=Plains, 7=Tundra, 9=River, 10=Grassland, 11=Jungle,
		//                 12=Hills, 13=Mountains, 14=Desert, 15=Arctic — same
		//                 byte values as MAP.PIC for consistency with LoadMap).
		//   then          (optional) height+1 float32 latitude edges, north to
		//                 south — the row/latitude mapping, since the generated
		//                 Earth clips Antarctica and gives the mid-latitudes extra
		//                 rows. Read into LatitudeEdges; absent on older files.
		// Path defaults to ~/Library/Application Support/CivOne/earth_epic.bin so
		// the offline build_earth_map.py script can land its output where the
		// MapPreview screen looks for it. Sets _width/_height *before* tile
		// construction so the static dimensions match the loaded grid.
		internal bool LoadEarthBin(string path)
		{
			if (!File.Exists(path))
			{
				Log("LoadEarthBin: file not found {0}", path);
				return false;
			}
			byte[] data;
			try { data = File.ReadAllBytes(path); }
			catch (System.Exception e) { Log("LoadEarthBin: read failed {0}", e.Message); return false; }

			if (data.Length < 16 || data[0] != (byte)'C' || data[1] != (byte)'I' || data[2] != (byte)'V' || data[3] != (byte)'E')
			{
				Log("LoadEarthBin: bad magic header");
				return false;
			}
			int w = data[8] | (data[9] << 8) | (data[10] << 16) | (data[11] << 24);
			int h = data[12] | (data[13] << 8) | (data[14] << 16) | (data[15] << 24);
			if (w <= 0 || h <= 0 || data.Length < 16 + w * h)
			{
				Log("LoadEarthBin: dimensions ({0}x{1}) inconsistent with payload {2}", w, h, data.Length);
				return false;
			}

			_width = w;
			_height = h;
			_tiles = new ITile[w, h];

			// Optional trailing table: h+1 float32 latitude edges, north to south,
			// written by design/build_earth_map.py. The generated Earth no longer
			// maps rows linearly onto latitude (Antarctica is clipped and the
			// mid-latitudes are given extra rows), so anything converting a real
			// lat/lon to a tile — civ start positions above all — must go through
			// this table. Absent on older files, in which case LatitudeEdges stays
			// null and callers fall back to the flat projection.
			LatitudeEdges = null;
			int edgeBytes = 4 * (h + 1);
			if (data.Length >= 16 + w * h + edgeBytes)
			{
				float[] edges = new float[h + 1];
				System.Buffer.BlockCopy(data, 16 + w * h, edges, 0, edgeBytes);
				// Sanity: must descend from north to south and stay in range.
				bool sane = edges[0] <= 90.1f && edges[h] >= -90.1f;
				for (int i = 0; sane && i < h; i++)
					if (edges[i] <= edges[i + 1]) sane = false;
				if (sane) LatitudeEdges = edges;
				else Log("LoadEarthBin: latitude table present but not monotonic; ignoring");
			}

			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				bool special = TileIsSpecial(x, y);
				ITile tile;
				switch (data[16 + y * w + x])
				{
					case 2:  tile = new Forest(x, y, special); break;
					case 3:  tile = new Swamp(x, y, special); break;
					case 6:  tile = new Plains(x, y, special); break;
					case 7:  tile = new Tundra(x, y, special); break;
					case 9:  tile = new River(x, y); break;
					case 10: tile = new Grassland(x, y); break;
					case 11: tile = new Jungle(x, y, special); break;
					case 12: tile = new Hills(x, y, special); break;
					case 13: tile = new Mountains(x, y, special); break;
					case 14: tile = new Desert(x, y, special); break;
					case 15: tile = new Arctic(x, y, special); break;
					default: tile = new Ocean(x, y, special); break;
				}
				_tiles[x, y] = tile;
			}

			// Mirror LoadMapThread's post-pass so a loaded Earth behaves like an
			// engine-generated one: poles enforced, freshwater lakes detected,
			// continent labels computed (used by AI strategy), huts placed.
			CreatePoles();
			// Wooded slopes, which the file format cannot carry. See WoodExistingHills.
			WoodExistingHills();
			ComputeFreshwaterLakes();
			EnsureFreshwaterReachability();
			EnsureMaritimeFreshwater();
			CalculateContinentSize();
			PlaceHuts();
			CalculateLandValue();

			Ready = true;
			Log("LoadEarthBin: ready ({0}x{1})", w, h);
			return true;
		}

		// Resolved path for the procedural-Earth binary. Search order:
		//   1. User data directory (~/Library/Application Support/CivOne/data/ on macOS)
		//      — where the runtime installs the shipped copy (CivOne.SDL.csproj), and where
		//      design/build_earth_map.py writes by default.
		//   2. <executable_dir>/resources/earth_epic.bin — for installed builds that
		//      ship the resource alongside the binary.
		//   3. <executable_dir>/../../../../../resources/earth_epic.bin — for source
		//      builds running from runtime/sdl/bin/{Debug,Release}/net10.0/ (5 levels
		//      up to the repo root, then into resources/).
		// If nothing exists, returns the user-dir path so the missing-file error message
		// points the user at the location they can write to.
		public static string EarthEpicPath => ResolveEarthBin("earth_epic.bin");

		// Same search, for the 80x50 board that replaces the original MAP.PIC.
		public static string EarthStandardPath => ResolveEarthBin("earth_standard.bin");

		private static string ResolveEarthBin(string fileName)
		{
			{
				string userPath = Path.Combine(Settings.Instance.DataDirectory, fileName);
				if (File.Exists(userPath)) return userPath;
				string execDir = System.AppContext.BaseDirectory;
				string[] candidates = {
					Path.Combine(execDir, "resources", fileName),
					Path.GetFullPath(Path.Combine(execDir, "..", "..", "..", "..", "..", "resources", fileName)),
				};
				foreach (string c in candidates)
					if (File.Exists(c)) return c;
				return userPath;
			}
		}

		// EARTH (EPIC) load path used by the new-game menu. Unlike LoadMap (which loads
		// the original 80×50 MAP.PIC with fixed civ starting coordinates), this loads
		// a 320×200 procedural Earth and lets the engine's normal new-game placement
		// pick start positions — the civilization records don't have Epic-scale start
		// coordinates anyway. Synchronous; the file is tiny (~64KB) and the load
		// finishes well within a frame.
		public bool LoadEarthEpic()
		{
			if (Ready || _tiles is not null)
			{
				Log("ERROR: Map is already load{0}/generat{0}", (Ready ? "ed" : "ing"));
				return false;
			}

			_landMass = -1;
			_temperature = -1;
			_climate = -1;
			_age = -1;
			// Honour the per-civ StartX/StartY records. They're calibrated for the
			// 80×50 MAP.PIC classic Earth, but Game.NewGame.cs:AddStartingUnits scales
			// them by Map.WIDTH/80 (so 4× on Epic) and falls back to a spiral search
			// for habitable land when the scaled tile lands in ocean or mountain. Net
			// effect on Epic Earth: civs spawn approximately where the historical 80×50
			// map placed them — Russia in Russia, Mali in West Africa, etc.
			FixedStartPositions = true;

			return LoadEarthBin(EarthEpicPath);
		}

		public void LoadMap()
		{
			if (Ready || _tiles is not null)
			{
				Log("ERROR: Map is already load{0}/generat{0}", (Ready ? "ed" : "ing"));
				return;
			}

			_landMass = -1;
			_temperature = -1;
			_climate = -1;
			_age = -1;
			FixedStartPositions = true;

			// The generated 80x50 Earth (design/build_earth_map.py). It replaced the original
			// MAP.PIC, with the same dimensions and terrain codes, so the per-civ StartX/StartY
			// records land in roughly the right places. The MAP.PIC fallback went with the DOS
			// assets (Nov 2026); without either, it built an all-ocean world in silence.
			string standard = EarthStandardPath;
			if (!File.Exists(standard))
				throw new FileNotFoundException("The Earth map is missing; reinstall the game.", standard);
			LoadEarthBin(standard);
		}
	}
}