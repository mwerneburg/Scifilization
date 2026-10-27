#!/usr/bin/env python3
"""Build the Scifilization website section into html/scifilization/.

    python3 tools/build_site.py

The Civilopedia is exported from the game itself first (tests/SiteExport.cs, run through
`dotnet test`), so the site always carries what the current build's book says. To reuse an
export, pass its folder: python3 tools/build_site.py /path/to/export

Static HTML, one stylesheet, no JavaScript (etherlabs.net's CSP allows 'self' only). Every
link is relative, so the folder can be served from any path. Needs ImageMagick (`magick`)
and `cwebp`. The hand-written pages live in tools/site/pages.py.
"""
import html, json, os, re, shutil, subprocess, sys, tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "html" / "scifilization"
ART_SRC = ROOT / "runtime/sdl/Resources/defaults/data/event_art"
SITE = Path(__file__).resolve().parent / "site"
sys.path.insert(0, str(SITE))
sys.dont_write_bytecode = True   # no __pycache__ beside the sources
import pages  # noqa: E402  (hand-written content)

MAX_EDGE = 1200
HERE = ' class="here"'
# Most of the generated art carries a Gemini sparkle within 80px of the picture's right edge
# (inside any black bars), at positions that vary image to image. Cropping that strip from
# EVERY image, after trimming the bars, removes all of them without a hand-kept list that would
# rot; found by contact sheets of every trimmed corner, Oct 2026. The in-game copies still
# carry the mark.
WATERMARK_STRIP = 80
IMPROVEMENT_ART = ROOT / "runtime/sdl/Resources/defaults/data/improvement_art"

LISTS = [("advances", "Civilization Advances"), ("improvements", "City Improvements"),
         ("wonders", "Wonders of the World"), ("units", "Military Units"),
         ("terrain", "Terrain Types"), ("concepts", "Game Concepts")]

NAV = [("index.html", "Home"), ("signal.html", "The Signal"), ("visitors/index.html", "Visitors"),
       ("features/index.html", "What's New"), ("civipedia/index.html", "Civipedia"), ("install.html", "Install")]


def slug(s):
    return re.sub(r"[^a-z0-9]+", "-", s.lower()).strip("-")


def esc(s):
    return html.escape(s, quote=True)


# ── art ──────────────────────────────────────────────────────────────────────

def make_image(src, rel, edge):
    """src PNG -> OUT/rel (.webp): bars trimmed, watermark strip cropped, long edge <= edge."""
    dest = OUT / rel
    if dest.exists() and dest.stat().st_mtime > src.stat().st_mtime:
        return dest
    dest.parent.mkdir(parents=True, exist_ok=True)
    tmp = dest.with_suffix(".png")
    # Most 1280x720 images are a 1080-wide picture between 100px black bars. Trim clear bars
    # only: at least 40px gone and 80% of the width kept, so a dark starfield is not eaten.
    box = subprocess.run(["magick", str(src), "-fuzz", "4%", "-format", "%@", "info:"],
                         check=True, capture_output=True, text=True).stdout
    width = int(subprocess.run(["magick", "identify", "-format", "%w", str(src)],
                               check=True, capture_output=True, text=True).stdout)
    trimmed = int(box.split("x")[0])
    trim = ["-crop", box, "+repage"] if width - trimmed >= 40 and trimmed * 5 >= width * 4 else []
    crop = ["-gravity", "West", "-crop", f"%[fx:w-{WATERMARK_STRIP}]x%[fx:h]+0+0", "+repage"]
    subprocess.run(["magick", str(src), *trim, *crop, "-resize", f"{edge}x{edge}>", "-strip", str(tmp)], check=True)
    subprocess.run(["cwebp", "-quiet", "-q", "80", str(tmp), "-o", str(dest)], check=True)
    tmp.unlink()
    return dest


def make_art(name):
    src = ART_SRC / f"{name}.png"
    if not src.exists():
        sys.exit(f"missing art: {src}")
    return make_image(src, f"art/{slug(name)}.webp", MAX_EDGE)


def figure(name, caption, depth):
    make_art(name)
    return (f'<figure class="art"><img src="{"../" * depth}art/{slug(name)}.webp" alt="{esc(caption)}" loading="lazy">'
            f'<figcaption>{esc(caption)}</figcaption></figure>')


def card(href, art, title, desc, depth):
    make_art(art)
    return (f'<a class="card" href="{href}"><img src="{"../" * depth}art/{slug(art)}.webp" alt="" loading="lazy">'
            f'<span class="body"><span class="t">{esc(title)}</span><span class="d">{desc}</span></span></a>')


# ── page shell ───────────────────────────────────────────────────────────────

def shell(path, title, body, desc=""):
    depth = path.count("/")
    up = "../" * depth
    def here(href):
        return path == href or (href != "index.html" and "/" in href and path.startswith(href.split("/")[0] + "/"))
    nav = "".join(f'<a href="{up}{href}"{HERE if here(href) else ""}>{label}</a>' for href, label in NAV)
    doc = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{esc(title)} · Scifilization</title>
<meta name="description" content="{esc(desc or 'Scifilization: Civilization in the Dark Forest.')}">
<link rel="stylesheet" href="{up}style.css">
</head>
<body>
<header class="top"><div class="inner"><a class="brand" href="{up}index.html">SCIFILIZATION</a><nav class="nav">{nav}</nav></div></header>
<main>
{body}
</main>
<footer><div class="inner"><span>Scifilization · GPL-3.0-or-later</span><a href="https://github.com/mwerneburg/Scifilization">source on GitHub</a><a href="{up}author.html">author</a><a href="/">etherlabs.net</a></div></footer>
</body>
</html>
"""
    dest = OUT / path
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(doc, encoding="utf-8")


# ── Civipedia ──────────────────────────────────────────────────────────────

class Pedia:
    def __init__(self, export):
        self.export = Path(export)
        self.entries = json.loads((self.export / "civilopedia.json").read_text(encoding="utf-8"))
        self.by_name = {}
        for e in self.entries:
            e["href"] = f"civipedia/{e['list']}/{slug(e['name'])}.html"
            self.by_name.setdefault(e["name"], e)
        # The book refers to other entries in CAPITALS ("needs IRON", "the MANHATTAN PROJECT").
        names = sorted((n for n in self.by_name if len(n) >= 3), key=len, reverse=True)
        self._caps = re.compile(r"\b(" + "|".join(re.escape(n.upper()) for n in names) + r")\b")
        self._caps_map = {n.upper(): n for n in names}

    def href(self, name, depth):
        """Link target for a Civilopedia entry; fails the build on a name the game does not have."""
        if name not in self.by_name:
            sys.exit(f"no Civilopedia entry named {name!r}")
        return "../" * depth + self.by_name[name]["href"]

    def link(self, name, depth, text=None):
        return f'<a href="{self.href(name, depth)}">{esc(text or name)}</a>'

    def linkify(self, text, depth, own):
        out, last = [], 0
        for m in self._caps.finditer(text):
            name = self._caps_map[m.group(1)]
            out.append(esc(text[last:m.start()]))
            out.append(esc(m.group(1)) if name == own else self.link(name, depth, m.group(1)))
            last = m.end()
        out.append(esc(text[last:]))
        return "".join(out)

    def illustration(self, e):
        """The improvement-built art (data/improvement_art/<name>.png), as the game finds it."""
        if e["kind"] not in ("improvement", "wonder"):
            return None
        src = IMPROVEMENT_ART / (e["name"].lower().replace(" ", "_") + ".png")
        if not src.exists():
            return None
        make_image(src, f"art/pedia/{slug(e['name'])}.webp", 900)
        make_image(src, f"art/pedia/thumb/{slug(e['name'])}.webp", 192)
        return slug(e["name"])

    def thumb(self, e, depth):
        art = self.illustration(e)
        return f'<img src="{"../" * depth}art/pedia/thumb/{art}.webp" alt="" loading="lazy">' if art else ""

    # The book is set in 34-character lines. Reflow them into prose, but keep a break where
    # the book starts a labelled row ("COPPER: Phalanx, ...") or a list item, and keep an
    # indented block (the Disasters table) exactly as set.
    _ROW = re.compile(r"^([A-Z][A-Z0-9 .'&/-]*:|[-*\u2022] )")

    def prose(self, lines, depth, own):
        out, para = [], []
        for line in lines + [""]:
            if line.strip():
                para.append(line)
                continue
            if not para:
                continue
            if any(l.startswith(" ") for l in para):
                out.append(f'<div class="lines">{self.linkify(chr(10).join(para), depth, own)}</div>')
            else:
                rows = []
                for l in para:
                    if rows and not self._ROW.match(l):
                        rows[-1] += " " + l.strip()
                    else:
                        rows.append(l.strip())
                out.append("<p>" + "<br>".join(self.linkify(r, depth, own) for r in rows) + "</p>")
            para = []
        return "".join(out)

    def render_pages(self, e, depth):
        pages = [self.prose(lines, depth, e["name"]) for lines in e["pages"] if any(l.strip() for l in lines)]
        return '<hr class="page">'.join(pages)

    def render_stats(self, e, depth):
        s = e.get("stats")
        if not s:
            return ""
        rows = []
        def ref(r):
            return self.link(r["name"], depth) if r else '<span class="low">(none)</span>'
        if e["kind"] in ("improvement", "wonder", "unit"):
            rows.append(("Requires", ref(s.get("requires"))))
            rows.append(("Cost", f'{s["cost"]} shields'))
            if e["kind"] == "improvement":
                rows.append(("Maintenance", f'${s["maintenance"]}/turn'))
            if e["kind"] == "unit":
                rows.append(("Attack / Defense / Move", f'{s["attack"]} / {s["defense"]} / {s["move"]}'))
            if s.get("obsolete"):
                rows.append(("Obsolete with", ref(s["obsolete"])))
        elif e["kind"] == "advance":
            rows.append(("Requires", ", ".join(ref(r) for r in s["requires"]) or '<span class="low">(none)</span>'))
            allows = []
            for a in s["allows"]:
                suffix = {"unit": " unit", "improvement": " improvement", "wonder": " Wonder"}.get(a["kind"], "")
                w = f' <span class="low">(with {esc(", ".join(a["with"]))})</span>' if a["with"] else ""
                allows.append(self.link(a["name"], depth) + esc(suffix) + w)
            rows.append(("Allows", "<br>".join(allows) or '<span class="low">nothing further</span>'))
        elif e["kind"] == "terrain":
            blocks = []
            for row in s["rows"]:
                blocks.append(f'<h3>{esc(row[0])}</h3><ul>' + "".join(f"<li>{esc(l)}</li>" for l in row[1:]) + "</ul>")
            notes = "".join(f'<p class="low"><small>{esc(n)}</small></p>' for n in s["notes"])
            return (f'<h2>Yields</h2><div class="terrain">{"".join(blocks)}</div>{notes}'
                    f'<dl class="stats"><dt>Movement cost</dt><dd>{s["move"]} MP</dd><dt>Defense bonus</dt><dd>+{s["defense"]}%</dd></dl>')
        return '<h2>Data</h2><dl class="stats">' + "".join(f"<dt>{k}</dt><dd>{v}</dd>" for k, v in rows) + "</dl>"

    def build(self):
        for e in self.entries:
            depth = 2
            title = esc(e["name"])
            body = (f'<div class="crumbs"><a href="../index.html">Civipedia</a> / '
                    f'<a href="index.html#{e["list"]}">{esc(dict(LISTS)[e["list"]])}</a></div>'
                    f'<p class="kicker">{esc(e["category"])}</p><h1>{title}</h1>'
                    + (f'<figure class="art pedia"><img src="../../art/pedia/{art}.webp" alt="{title}"></figure>'
                       if (art := self.illustration(e)) else "")
                    + f'<div class="entry">{self.render_pages(e, depth)}</div>{self.render_stats(e, depth)}')
            shell(e["href"], e["name"], body, f'{e["name"]} — {e["category"]}, from the Scifilization Civipedia.')
            # each list folder gets a redirect-free index pointing back to the full contents
        sections = []
        for key, label in LISTS:
            items = [e for e in self.entries if e["list"] == key]
            links = "".join(f'<a href="{key}/{slug(e["name"])}.html">{self.thumb(e, 1)}<span>{esc(e["name"])}</span></a>' for e in items)
            sections.append(f'<h2 id="{key}">{esc(label)} <span class="low">· {len(items)}</span></h2><div class="pedia-grid">{links}</div>')
            shell(f"civipedia/{key}/index.html", label,
                  f'<p class="kicker">Civipedia</p><h1>{esc(label)}</h1><div class="pedia-grid">'
                  + "".join(f'<a href="{slug(e["name"])}.html">{self.thumb(e, 2)}<span>{esc(e["name"])}</span></a>' for e in items)
                  + '</div><p><a href="../index.html">All sections</a></p>')
        shell("civipedia/index.html", "Civipedia",
              '<p class="kicker">Encyclopedia of Civilization</p><h1>Civipedia</h1>'
              f'<p class="lede">The in-game reference book, every page of it — {len(self.entries)} entries, '
              'exported straight from the game so the two never disagree.</p>'
              '<p class="dim">Names in CAPITALS inside an entry link to their own pages, as they do in the game.</p>'
              + "".join(sections))


# ── checks ───────────────────────────────────────────────────────────────────

def check_links():
    bad = []
    for page in OUT.rglob("*.html"):
        text = page.read_text(encoding="utf-8")
        for ref in re.findall(r'(?:href|src)="([^"]+)"', text):
            if ref.startswith(("http://", "https://", "mailto:", "/")) or ref.startswith("#"):
                continue
            target = (page.parent / ref.split("#")[0]).resolve()
            if not target.exists():
                bad.append(f"{page.relative_to(OUT)} -> {ref}")
        if re.search(r"<script|\son[a-z]+=\"", text):
            bad.append(f"{page.relative_to(OUT)}: script or inline handler (the site CSP forbids both)")
    return bad


def export_civilopedia():
    """Run tests/SiteExport.cs into a temporary folder and return the folder."""
    dest = Path(tempfile.mkdtemp(prefix="scifilization-export-"))
    env = dict(os.environ, CIVONE_SITE_EXPORT=str(dest))
    # Homebrew's dotnet, as build.sh sets it up, for shells that do not.
    env["PATH"] = env.get("PATH", "") + ":/opt/homebrew/bin"
    env.setdefault("DOTNET_ROOT", "/opt/homebrew/Cellar/dotnet/10.0.300/libexec")
    print("exporting the Civilopedia from the game (dotnet test, about a minute)...")
    run = subprocess.run(["dotnet", "test", str(ROOT / "tests/CivOne.Tests.csproj"),
                          "--filter", "FullyQualifiedName~CivOne.Tests.SiteExport"],
                         env=env, capture_output=True, text=True)
    if run.returncode != 0 or not (dest / "civilopedia.json").exists():
        sys.exit("Civilopedia export failed:\n" + run.stdout[-3000:] + run.stderr[-2000:])
    return dest


def main():
    if len(sys.argv) > 2:
        sys.exit(__doc__)
    export = Path(sys.argv[1]) if len(sys.argv) == 2 else export_civilopedia()
    if not (export / "civilopedia.json").exists():
        sys.exit(f"{export} holds no civilopedia.json. Run with no argument to export one.\n\n{__doc__}")
    OUT.mkdir(parents=True, exist_ok=True)
    for stale in OUT.rglob("*.html"):
        stale.unlink()
    shutil.copy(SITE / "style.css", OUT / "style.css")
    pedia = Pedia(export)
    pedia.build()
    for path, title, desc, body in pages.build(pedia, figure, card):
        shell(path, title, body, desc)
    bad = check_links()
    n_html = len(list(OUT.rglob("*.html")))
    size = sum(f.stat().st_size for f in OUT.rglob("*") if f.is_file())
    print(f"{n_html} pages, {size / 1e6:.1f} MB in {OUT}")
    if bad:
        print("BROKEN:\n  " + "\n  ".join(bad))
        sys.exit(1)


if __name__ == "__main__":
    main()
