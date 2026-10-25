// CivOne tests
//
// Exports the Civilopedia for the website (tools/build_site.py). Not a test: it does nothing
// unless CIVONE_SITE_EXPORT names an output directory, the same opt-in HangRepro uses.
//
//   CIVONE_SITE_EXPORT=/tmp/site-export dotnet test tests/CivOne.Tests.csproj --filter SiteExport
//
// The text comes from the Civilopedia screen's own GetBlurb and terrain table, so the site
// carries exactly what the book shows — including the asset-free fallback text. No icons:
// without the DOS files ICivilopedia.Icon is a placeholder fill; the site uses
// data/improvement_art instead.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Concepts;
using CivOne.Screens;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Tests
{
	public class SiteExport
	{
		private static object? Ref(ICivilopedia? x) => x is null ? null : new { name = x.Name, kind = Kind(x) };

		private static string Kind(ICivilopedia x) => x switch
		{
			ITile or Lakes => "terrain",
			IWonder => "wonder",
			IBuilding => "improvement",
			IUnit => "unit",
			IAdvance => "advance",
			_ => "concept",
		};

		[Fact]
		public void Run()
		{
			string? dir = Environment.GetEnvironmentVariable("CIVONE_SITE_EXPORT");
			if (string.IsNullOrEmpty(dir)) return;
			Directory.CreateDirectory(dir);
			Sim.NewGame(width: 80, height: 50);

			MethodInfo blurb = typeof(Civilopedia).GetMethod("GetBlurb", BindingFlags.NonPublic | BindingFlags.Instance)!;
			MethodInfo category = typeof(Civilopedia).GetMethod("GetCategory", BindingFlags.NonPublic | BindingFlags.Instance)!;
			var lists = new (string key, ICivilopedia[] items)[]
			{
				("advances", Civilopedia.Advances), ("improvements", Civilopedia.Improvements),
				("wonders", Civilopedia.Wonders), ("units", Civilopedia.Units),
				("terrain", Civilopedia.TerrainType), ("concepts", Civilopedia.Misc),
			};

			var entries = new List<object>();
			foreach ((string key, ICivilopedia[] items) in lists)
			foreach (ICivilopedia x in items)
			{
				var screen = new Civilopedia(x);
				var pages = Enumerable.Range(1, x.PageCount).Select(p => (string[])blurb.Invoke(screen, new object[] { (byte)p })!).ToArray();

				object? stats = x switch
				{
					IWonder w => new { requires = Ref(w.RequiredTech as ICivilopedia), cost = w.Price * 10, obsolete = Ref(w.ObsoleteTech as ICivilopedia) },
					IBuilding b => new { requires = Ref(b.RequiredTech as ICivilopedia), cost = b.Price * 10, maintenance = b.Maintenance },
					IUnit u => (object)new { requires = Ref(u.RequiredTech as ICivilopedia), cost = u.Price * 10, attack = u.Attack, defense = u.Defense, move = u.Move, obsolete = Ref(u.ObsoleteTech as ICivilopedia) },
					IAdvance a => new
					{
						requires = a.RequiredTechs.Select(r => Ref(r as ICivilopedia)).ToArray(),
						allows = Common.Advances.Where(t => t.Requires(a.Id))
							.Select(t => new { name = t.Name, kind = "advance", with = t.RequiredTechs.Where(o => o.Id != a.Id).Select(o => o.Name).ToArray() })
							.Concat(Reflect.GetUnits().Where(u2 => u2.RequiredTech?.Id == a.Id).Select(u2 => new { name = u2.Name, kind = "unit", with = new string[0] }))
							.Concat(Reflect.GetBuildings().Where(b2 => b2.RequiredTech?.Id == a.Id).Select(b2 => new { name = b2.Name, kind = "improvement", with = new string[0] }))
							.Concat(Reflect.GetWonders().Where(w2 => w2.RequiredTech?.Id == a.Id).Select(w2 => new { name = w2.Name, kind = "wonder", with = new string[0] }))
							.ToArray(),
					},
					ITile t => new
					{
						rows = Civilopedia.TerrainTable(t, out int move, out int defense),
						move, defense,
						notes = new[] { Civilopedia.TerrainNoteDespotism, Civilopedia.TerrainNoteRepublic },
					},
					_ => null,
				};

				entries.Add(new { list = key, kind = Kind(x), category = (string)category.Invoke(screen, null)!, name = x.Name, pages, stats });
			}

			File.WriteAllText(Path.Combine(dir, "civilopedia.json"),
				JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
			Assert.Equal(lists.Sum(l => l.items.Length), entries.Count);
		}
	}
}
