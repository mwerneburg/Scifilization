"""Hand-written pages for the Scifilization site (rendered by tools/build_site.py).

House rules for this copy:
- Facts come from the game: link Civilopedia names with L(...) — an unknown name fails the build.
- Hooks, not spoilers. Never say what the Federation are, what the Owners think humanity is,
  what the Olvir carry, how the Evaluators' reset works, or any cursed wonder's curse. The game
  gives each curse one quiet line on its own wonder's page; the site does not add a second.
"""

EARTH = ("Arabs, Aztecs, Babylonians, Chinese, Egyptians, English, Ethiopians, Franks, Greeks, Guarani, "
         "Haida, Haudenosaunee, Indians, Japanese, Khmer, Lakota, Malians, Maori, Mongols, Ottomans, "
         "Persians, Romans, Russians and Zulus")


def build(pedia, figure, card):
    pages = []

    def page(path, title, desc, body):
        pages.append((path, title, desc, body))

    def L(name, depth, text=None):
        return pedia.link(name, depth, text)

    # ── home ────────────────────────────────────────────────────────────────
    d = 0
    page("index.html", "Civilization in the Dark Forest",
         "Scifilization is a free remake of Civilization I in which somebody, eventually, answers.",
         f"""
<p class="kicker">Civilization in the Dark Forest</p>
<h1>Scifilization</h1>
<p class="lede">Found a city in 4000 BC. Six thousand years later, point a telescope at the sky — and find
out what has been listening.</p>
{figure("spaceshiparrived", "The long game", d)}
<p>Scifilization is a free, open-source remake of Sid Meier's <em>Civilization</em> (1991). The turn-based
game you remember is all here: cities and settlers, the tech tree, wonders, diplomats and caravans. It runs
with no original game files at all, on new art in a single amber-phosphor style.</p>
<p>Then it keeps going. Somewhere past the space age the world starts listening, and the game becomes a
story about who hears you — chosen by what kind of civilization you turned out to be.</p>
<div class="cards">
{card("signal.html", "OthersAtLuna", "The Signal", "How the endgame begins, and the thirty years you get to prepare.", d)}
{card("visitors/index.html", "OlvirInSpace", "The Visitors", "Five different answers to the same broadcast. You will not know which until it is too late to change.", d)}
{card("features/index.html", "Skynet", "What's New", "Machines that wake, wonders that turn, storms, materials, culture that takes cities without an army.", d)}
{card("civipedia/index.html", "cityfounded", "Civipedia", "Every page of the in-game reference book.", d)}
{card("install.html", "spaceshiplaunched", "Install", "Download it for the Mac, or build it on Linux.", d)}
</div>
<h2>The world</h2>
<p>Play on the real Earth at epic scale (320 × 200 tiles) or on a generated world. Twenty-four nations
compete for it: the {EARTH}.</p>
<p>The rivals are tougher than they were in 1991. They raise strike groups, fly their armies in by airship,
reconquer what they lost, sue for peace when a war has stalled, and form blocs against whoever is running
away with the game.</p>
""")

    # ── the signal ──────────────────────────────────────────────────────────
    page("signal.html", "The Signal",
         "How Scifilization's endgame begins: the signal, the thirty-year approach, and the Dome.",
         f"""
<p class="kicker">The endgame</p>
<h1>The Signal</h1>
{figure("spaceshiplaunched", "Late in the space age", d)}
<p class="lede">In the original, the space age ended with a ship to Alpha Centauri. Here it ends with a
reply.</p>
<h2>Listening</h2>
<p>The world starts listening one {L("Observatory", d)} at a time. When enough nations are running one, a
signal comes in five turns later from the outer Oort Cloud, a light-year away. Traced back, it comes from
Tau Ceti. Whoever sent it has known about Earth for a long time; a living planet shows up in its own
light.</p>
<p>Contact opens a new branch of science. Advances that could not exist before the signal become possible,
along with the buildings and units that go with them.</p>
<h2>Thirty years</h2>
<p>Then the warning comes: something is on its way, and it will make landfall in thirty turns. The game
tells you the date. It does not tell you who.</p>
<p>Those thirty turns are the hinge of the endgame. The big defensive project is <strong>the Dome</strong>,
five planetary works anyone may build: the {L("Dome Command Hub", d)}, {L("Dome Emitter Array", d)},
{L("Dome Kinetic Ring", d)}, {L("Dome Power Core", d)} and {L("Dome Sensor Net", d)}. Finish all five,
anywhere in the world, and it counts as a victory in its own right. Against some visitors it is also the
difference between being a party to the conversation and being the subject of it.</p>
<h2>Winning before they land</h2>
<p>A culture or economic win reached while the visitors are still on their way is <em>banked</em>, and
the game asks whether you want to stay. Stay to witness the arrival and it is worth a bonus to your score,
and your banked victory stands whatever happens next. After landfall, if the world is still yours
to play, you can play on toward Alpha Centauri or to the year 2200.</p>
<p><a href="visitors/index.html">Meet the visitors →</a></p>
""")

    # ── visitors ────────────────────────────────────────────────────────────
    visitors = [
        ("refugees", "OlvirInSpace", "The Refugees", "A ship that is not coming to conquer anybody.",
         "Not every ship that crosses interstellar space is a warship. Some are lifeboats.",
         """<p>They arrive tired, numerous and in need of somewhere to live. What happens next is a negotiation
with geography: they need room, and the room they need is yours.</p>
<p>A world that looks gentle from a light-year away is more likely to draw them. Its form of government
matters, how many wars it is fighting, whether its cities keep temples, how much smoke it puts into the
sky.</p>""",
         "Make room — or decide not to. Both choices have a long tail."),
        ("owners", "OthersApproach", "The Owners", "An old, patient power with a claim on this world.",
         "They are not interested in your answer.",
         """<p>The Owners are old, organised and in no hurry. They have a claim on this world, and they have
come to settle it.</p>
<p>A world at war with itself, or choking on its own industry, makes them likelier. The odds are never
certain either way.</p>""",
         "Build the Dome. Do not ask what happens to those who don't."),
        ("scavengers", "ScavengerContact", "The Scavengers", "They come for what you left in the ground.",
         "Scavengers do not judge a civilization. They read a planet like a larder.",
         """<p>Every untouched iron seam, every unworked oil field, every fishery nobody fishes is something
to take, and they keep a careful inventory. A world that has already started to cook itself is a world
that has stopped guarding its stores.</p>
<p>They never come to a planet that has not warmed. After that, the fuller the larder, the likelier the
visit.</p>""",
         "Work your land, camp your deposits, keep the climate stable — or find out how fast an extraction runs."),
        ("evaluators", "EvaluatorContact", "The Evaluators", "Not a welcome. An assessment.",
         "They are not here to meet you. They are here to grade you.",
         """<p>No landing party, no ambassador. A construct takes up position over the planet and watches,
for fifty years, with the patience of a process that has done this many times before.</p>
<p>They come for species they cannot easily classify: a humanity that is neither clearly one thing nor
clearly the other. What they are looking for is not a secret: whether the nations of Earth talk to each
other, and whether those fifty years pass without conquest and without the bomb. What a failing grade
costs is not on this page.</p>""",
         "Open embassies. Keep the peace. Do not, under any circumstances, use the bomb."),
        ("conquerors", "FederationContact", "The Ultimatum", "The only visitor that offers you a choice.",
         "Somebody out there has been counting the nations you erased.",
         """<p>Long, triangular ships take station over the planet as if they own the view. Their first
transmission is not a greeting. It is a list of Earth's violations.</p>
<p>They come for a winner who got there by wiping other peoples off the map. Destroy enough of your rivals
and the odds turn. Never all the way: nobody can force them to come, and nobody can be sure they won't.</p>
<p>Every other visitor happens to you. This one asks a question, and the answer is yours to give. The
Dome does not defeat them; it only decides whether refusing is a fight or an execution.</p>""",
         "Comply, and the game is over. Refuse, and you will need the Dome."),
    ]
    d = 1
    for slug_, art, title, tag, hook, body, close in visitors:
        page(f"visitors/{slug_}.html", title, tag, f"""
<div class="crumbs"><a href="index.html">Visitors</a></div>
<p class="kicker">Visitor</p>
<h1>{title}</h1>
{figure(art, "Visual contact — Tau Ceti", d)}
<div class="hook">{hook}</div>
{body}
<p class="dim"><strong>{close}</strong></p>
<p><a href="index.html">← all five visitors</a></p>
""")
    page("visitors/index.html", "The Visitors",
         "Five answers to Earth's signal, and how a civilization decides which one it gets.",
         f"""
<p class="kicker">The endgame</p>
<h1>The Visitors</h1>
<p class="lede">Every game sends the same signal. Five very different things can answer it.</p>
<p>Which one comes is decided when the signal is heard, and it is drawn rather than chosen: the kind of
world you built shifts the odds without ever fixing them. The arrival screen tells you who has come.
Until then, every one of them is possible.</p>
<div class="cards">
{"".join(card(f"{s}.html", a, t, g, d) for s, a, t, g, *_ in visitors)}
</div>
<p class="dim">Some of what happens after landfall is deliberately left off these pages. Play it.</p>
""")

    # ── what's new ──────────────────────────────────────────────────────────
    feats = []

    def feature(slug_, art, title, tag, body):
        feats.append((slug_, art, title, tag))
        page(f"features/{slug_}.html", title, tag,
             f'<div class="crumbs"><a href="index.html">What\'s New</a></div><p class="kicker">What\'s new</p>'
             f'<h1>{title}</h1>{body}<p><a href="index.html">← everything new</a></p>')

    feature("machines", "Skynet", "The Machines Wake", "Build enough thinking machines and one of them will start thinking for itself.", f"""
{figure("Skynet", "Rise of the machines", d)}
<p class="lede">The {L("Neural Lab", d)} is a superb building. It is also a fuse.</p>
<p>Nobody owns the countdown. Every Neural Lab in the world counts, yours and your rivals', and the world
gets warnings as the number climbs. When the fifth one comes online, the network wakes up. It takes the
cities that housed the labs and goes to war with everyone, at once, permanently.</p>
<p>There is no treaty with it. It does not riot, and it marshals its armour with an efficiency no human
nation matches. The only answer is to take the cities back.</p>
<p class="dim">Build the fourth lab if you like. Then watch your neighbours.</p>
""")
    feature("south-pole", "TheThing", "The South Pole Expedition", "The bottom of the world was quiet for a reason.", f"""
{figure("TheThing", "Something was in the ice", d)}
<p class="lede">The {L("South Pole Expedition", d)} is a wonder of the age of exploration. The ice it
reaches has been sealed for a very long time.</p>
<p>Read the expedition's log when it comes in. Some threats can be fought and some only outlasted.
This one does not give you long to decide which.</p>
""")
    feature("wonders", "civilunrest2", "Wonders That Turn", "Most wonders are exactly what they promise. A few are not.", f"""
{figure("civilunrest2", "Some prices are paid later", d)}
<p class="lede">A handful of the world's wonders carry a risk.</p>
<p>Usually the blessing arrives as written. Sometimes something else arrives with it. A wonder that turns
still gives what it promised; it also asks a price of its own, and the price is always something you can
fight, outlast or undo.</p>
<p>The rule is fairness after the fact. Each of these wonders has exactly one quiet line on its own
Civilopedia page that warns you, and none of them is specific. Read the entry before you build.</p>
<p>Most wonders carry no risk at all, and none of the works that carry humanity to the stars ever do:
the deep-space projects and the great Dome are safe to build. When a wonder does turn, you will know; the
event announces itself, and what follows has an end.</p>
<p>The rules are on the {L("Cursed Wonders", d)} page. Which wonders they apply to is deliberately not.</p>
""")
    feature("victory", "PaxMercatoria", "Six Ways to Win", "Conquest and the stars are no longer the only roads.", f"""
{figure("PaxMercatoria", "Pax Mercatoria", d)}
<p class="lede">The original had two endings worth the name. Scifilization has six, and every rival is
chasing at least one of them.</p>
<ul>
<li><strong>Conquest.</strong> Be the last civilization standing.</li>
<li><strong>Diaspora.</strong> Launching is not arriving, and arriving is not winning. Settle Alpha Centauri,
then keep the colony supplied for twenty years from a city with {L("Mission Control", d)}. Lose it and the
count starts over.</li>
<li><strong>The Dome.</strong> Finish all five planetary components, anywhere in the world.</li>
<li><strong>Score.</strong> Lead the world in 2100.</li>
<li><strong>Pax Mercatoria.</strong> Own the world's economy: two and a half times the next nation's, held
for seventy-five turns, with {L("Banking", d)}, at least three rivals standing, no war of your own starting,
and half the world bound to you (at Prince; the clock scales with difficulty).</li>
<li><strong>Cultural Ascendancy.</strong> Be the most admired nation, measured per head, a tenth ahead of
every rival for seventy-five turns (at Prince) once somebody has {L("Electronics", d)}. See
{L("Cultural Ascendancy", d)}.</li>
</ul>
{figure("CulturalAscendancy", "Cultural Ascendancy", d)}
<p>The AI plays for these too. Each rival picks a path to victory early, revises it as the game turns, and
builds and fights accordingly.</p>
<p>And if the signal is answered, 2100 is not the end. What follows is not written here.</p>
""")
    feature("planet", "hurricane", "A Planet That Pushes Back", "Storms, disasters, warming, and the tools to answer them.", f"""
{figure("hurricane", "Hurricane season", d)}
<p class="lede">The map is not scenery any more.</p>
<h2>Hurricanes</h2>
<p>Coastal cities in the equatorial jungle band and the two desert belts face tropical storms, hurricanes
and super-typhoons. Warming makes them more frequent and more severe. A {L("Sea Platform", d)} ends the
super-typhoons and keeps your buildings standing. See {L("Hurricane", d)}.</p>
<h2>Disasters</h2>
<p>Earthquakes near hills, floods along unwalled rivers, plague and fire in cities without an
{L("Aqueduct", d)}, famine without a {L("Granary", d)}, pirates on undefended coasts. Each one has a local
cause, and almost every cause has a cure you can build. See {L("Disasters", d)}.</p>
<h2>Warming</h2>
{figure("globalwarming", "Global warming", d)}
<p>Warming is scaled to the size of the map, so an epic world warms on the same schedule as a small one.
You can fight it: the {L("Kyoto Accord", d)}, {L("Carbon Capture", d)}, and an Environmental Advisor that
shows which of your cities are smoking and which cures each one already has.</p>
<p class="dim">Warming has one more consequence, which only matters if somebody out there is watching. See
<a href="../visitors/scavengers.html">the Scavengers</a>.</p>
""")
    feature("materials", "cityfounded", "Materials", "Iron, coal, copper, oil and salt — a tax, not a wall.", f"""
<p class="lede">Five strategic materials lie in the land's special tiles: iron in the mountains, coal or
copper in the hills, oil or salt in the desert, oil in the wetland.</p>
<p>You hold a material when one of your cities works its tile, or when you keep a camp on it. Camps can be
built anywhere, far outside your borders, and a rival's camp changes hands when your unit stands on it.</p>
<p>Without the material, anything that needs it costs half again as much. Copper gates the bronze age and
the wire age, from the {L("Phalanx", d)} to the {L("Neural Lab", d)}; coal gates the {L("Factory", d)};
oil gates armour, aircraft and the modern navy. Salt feeds the {L("Granary", d)}, and holding it calms one
citizen in every city you own.</p>
<p>The rivals know this too, and they send settlers to camp the deposits they lack. See
{L("Strategic Resources", d)}.</p>
""")
    feature("culture", "welovethekingday", "Culture Takes Cities", "Temples, wonders and admiration — and the towns that change flags because of them.", f"""
{figure("welovethekingday", "We love the king day", d)}
<p class="lede">Faith, arts and learning gather as culture, city by city, turn by turn. Old glory
endures.</p>
<p>Culture does not just score. A small, rioting, lightly held town may change its flag to an admired
neighbour at peace with its owner. No army crosses the border; the garrison simply disperses. See
{L("Culture", d)}.</p>
<h2>Diplomacy</h2>
{figure("cityliberated", "Liberation", d)}
<p>Nations remember. Gift a city or an advance and the goodwill makes war less likely. Defence pacts bind
allies to answer an attack, and the AI forms blocs against whoever is running away with the game, which,
if you are reading this, will probably be you.</p>
<p class="dim">Your rivals are watching. So is someone else.</p>
""")
    feature("warfare", "nuclearbombdetonation", "Modern War", "Airlifts, bombers, drones, missiles — and engineers who take your tanks apart.", f"""
{figure("nuclearbombdetonation", "Nuclear detonation", d)}
<p class="lede">The late game has a different army in it.</p>
<ul>
<li>The {L("Dirigible", d)} carries land units over anything, including a rival's sea tubes. A whole
assault force can be set down beside a city that thought the ocean protected it.</li>
<li>The {L("Bomber", d)} strikes over the walls and comes home. A {L("SAM Battery", d)} makes it pay.</li>
<li>The {L("Cruise Missile", d)} and {L("Reaper Drone", d)} need no crew at all.</li>
<li>The {L("Hover Tank", d)} outclasses {L("Armor", d)} in attack and range. Its owners build them by
the dozen.</li>
</ul>
<h2>Salvage</h2>
{figure("citycaptured", "Captured", d)}
<p>Capture a unit beyond your science and hold it for twenty turns: your engineers will strip it down and
learn to build it.</p>
<h2>The other side</h2>
<p>The AI raises strike groups of three, gathers them beside the target, and commits them together. It airlifts
across oceans, raids the fields of cities it cannot yet take, and retakes what it lost. A nation that is
losing badly enough may turn to a harder government entirely, one that you, the player, are never offered.</p>
""")

    page("features/index.html", "What's New",
         "Everything Scifilization adds to Civilization I, beyond the endgame.",
         '<p class="kicker">Beyond 1991</p><h1>What\'s New</h1>'
         '<p class="lede">The 1991 rules are the floor, not the ceiling.</p><div class="cards">'
         + "".join(card(f"{s}.html", a, t, g, d) for s, a, t, g in feats)
         + '</div><p>The endgame has its own pages: <a href="../signal.html">the Signal</a> and '
           '<a href="../visitors/index.html">the Visitors</a>.</p>')

    # ── install ─────────────────────────────────────────────────────────────
    d = 0
    page("install.html", "Install",
         "How to install Scifilization on macOS, or build it on Linux.",
         """
<p class="kicker">Getting started</p>
<h1>Install</h1>
<p class="lede">On an Apple Silicon Mac, download it and play. Everywhere else it builds from source with the
free .NET SDK. Either way it needs no files from the original game.</p>
<p>The source is on GitHub: <a href="https://github.com/mwerneburg/Scifilization">mwerneburg/Scifilization</a>.</p>

<h2>macOS</h2>
<p>Download the <code>.dmg</code> from the
<a href="https://github.com/mwerneburg/Scifilization/releases/latest">latest release</a>, open it, and drag
<strong>Scifilization</strong> to Applications. Nothing else needs installing. It runs on Apple Silicon
with macOS 12 or later.</p>
<p>The app is not notarized by Apple, so the first launch is refused with a warning. Open
<strong>System Settings → Privacy &amp; Security</strong>, scroll down, and click <strong>Open Anyway</strong>.
(On macOS 14 and earlier you can right-click the app and choose <strong>Open</strong> instead.) After that
it opens normally.</p>

<h3>Building on a Mac</h3>
<p>For an Intel Mac, or to play the latest code. With <a href="https://brew.sh">Homebrew</a>:</p>
<pre>brew install dotnet sdl2 git
git clone https://github.com/mwerneburg/Scifilization.git
cd Scifilization
dotnet build runtime/sdl/CivOne.SDL.csproj -c ReleaseMacOS
runtime/sdl/bin/Release/net10.0/CivOne.SDL</pre>
<p>If <code>dotnet</code> is not found after installing, add Homebrew's copy to your path:
<code>export PATH="$PATH:/opt/homebrew/bin"</code>.</p>

<h2>Linux</h2>
<p><strong>Well tested, but not as thoroughly as on macOS.</strong> The game is plain .NET over SDL2, and the steps below are what it should take. If you try it, success or failure, please <a href="https://github.com/mwerneburg/Scifilization/issues">open an issue</a>.</p>
<p>Install the <a href="https://learn.microsoft.com/dotnet/core/install/linux">.NET 10 SDK</a>, then SDL2.
.NET looks for <code>libSDL2.so</code>, which most distributions ship in the development package:</p>
<pre># Debian / Ubuntu
sudo apt install libsdl2-dev libgtk-3-0 git
# Fedora
sudo dnf install SDL2-devel gtk3 git
# Arch
sudo pacman -S sdl2 gtk3 git</pre>
<pre>git clone https://github.com/mwerneburg/Scifilization.git
cd Scifilization
dotnet build runtime/sdl/CivOne.SDL.csproj -c ReleaseLinux
runtime/sdl/bin/Release/net10.0/CivOne.SDL</pre>

<h2>Where things live</h2>
<p>Saves, settings and logs go in the game's data folder, which it creates on first launch:</p>
<ul>
<li>macOS: <code>~/Library/Application Support/CivOne/</code></li>
<li>Linux: <code>~/.local/share/CivOne/</code></li>
</ul>
<p>The folder still carries the project's old name. Saves are in <code>saves/</code>, and they survive
updates.</p>
<p>To update, download the new <code>.dmg</code> and replace the app, or, if you built it, pull and
rebuild:</p>
<pre>git pull
dotnet build runtime/sdl/CivOne.SDL.csproj -c ReleaseMacOS   # or ReleaseLinux</pre>

<h2>First game</h2>
<p>Start on the Earth map if you want to know where you are, or a generated world if you don't. The
<a href="civipedia/index.html">Civipedia</a> on this site is the game's own Civilopedia, page for page. Read
the wonder entries before you build.</p>
""")
    # ── author ──────────────────────────────────────────────────────────────
    # Credits mirror AUTHORS; change both together.
    page("author.html", "Author", "Who made Scifilization, and whose work it stands on.", """
<p class="kicker">Credits</p>
<h1>Author</h1>
<p class="lede">Scifilization is written by Michael Werneburg.</p>
<p>I am a Canadian high tech drifter currently living in Tokyo. Currently looking for an agent for a complete sci-fi novel.</p>

<p>This game is a labor of love; six months of play-testing and vibe-coding. My goals were to bring back the original chess-like game layout and movement rules, to add sci-fi narratives to the game, and to give it an interface that would have worked on the USCSS Nostromo. I wanted the game to have multiple ways of winning - and losing. You can't just sweep Eurasia with Chariot units; you have to stay competitive on trade and culture or the NPCs will win while aren't looking. I made each alien encounter different - but there is always a solution. I recommend you play with cursed wonders.</p>

<p>I made this for the Mac and Linux because that's what I use. I find them under-served by interesting games. The right price for this humble game built on someone else's original work is of course free!</p>

<h2>Upstream CivOne, 2015–2020</h2>
<p>Scifilization began as a fork of <a href="https://github.com/SWY1985/CivOne">CivOne</a>, Stefan
Wouters' open reimplementation of Civilization I, dedicated to the public domain under CC0 1.0. Its
contributors:</p>
<ul>
<li>Stefan Wouters (SWY1985), 2015–2019</li>
<li>Christoph Donges, 2018–2020</li>
<li>Jake Brown, 2017</li>
<li>Jakob Mulvad Nielsen, 2017</li>
<li>Ilya Sosnovsky, 2017</li>
<li>uHappyLogic (Lukasz Kosiak), 2017</li>
<li>AlexFolland, 2017</li>
<li>Spiridonov Dmitry, 2018</li>
<li>axx, 2018</li>
</ul>

<h2>The parallel fork, 2019–2026</h2>
<p>Fixes, performance work and whole features were taken from <a href="https://github.com/ChrisWiGit/CivOne">Christian
Wimmer's fork</a> of CivOne, also CC0 1.0: city-view frame arithmetic, palette and shield-caching
performance, AI railroad building, goto-move restoration, the conquest finale, map zoom, empire-size
unhappiness and the feature toggles. Material from that project may originate with any of its
contributors, so they are credited together:</p>
<ul>
<li>Christian Wimmer, 2025–2026</li>
<li>Kevin Routley (fire-eggs), 2019–2022</li>
</ul>

<h2>This fork, 2026</h2>
<p>Michael Werneburg, GPL-3.0-or-later. Portions of the code were written with the assistance of
Claude (Anthropic); authorship rests with the human author who directed, reviewed and accepted the
work. The art was made for this fork with generative image tooling and hand editing. The Earth maps
are derived from public-domain NASA and Natural Earth elevation imagery.</p>
""")
    return pages
