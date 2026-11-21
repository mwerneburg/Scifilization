#!/usr/bin/env python3
"""Build the game's fonts from Pixel Operator: resources/pixel_operator_8.txt, the text face,
and resources/pixel_operator_16.txt, the headline face (font ids 4 and 5), from the Bold cut:
the regular one's 1-pixel strokes read as wireframe at 16 pixels.

Pixel Operator is by Jayvee Enaguas and released under CC0 1.0 (the LICENSE.txt in its
download says so). Get it from https://www.dafont.com/pixel-operator.font and pass the
TTF inside and the file to write:

    python design/build_font.py ~/Downloads/pixel_operator/PixelOperator8.ttf pixel_operator_8.txt
    python design/build_font.py ~/Downloads/pixel_operator/PixelOperator-Bold.ttf pixel_operator_16.txt

Needs Pillow and fontTools (a venv: python3 -m venv v && v/bin/pip install Pillow fonttools).

The fonts draw on a 100-unit grid (8 or 16 pixels to the em), so rendering at the em with
antialiasing off reproduces its pixels exactly. Each glyph's 1-pixel left bearing is then
dropped, so letters sit one pixel apart (the renderer adds that pixel), as the game's text
always has; at the font's own two-pixel spacing, labels ran into their values.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont

ttf = sys.argv[1]
out = Path(__file__).resolve().parent.parent / 'resources' / sys.argv[2]

tt = TTFont(ttf)
unit = 100
em = tt['head'].unitsPerEm // unit
asc, desc = tt['hhea'].ascent // unit, -tt['hhea'].descent // unit
height = asc + desc
cmap, hmtx = tt.getBestCmap(), tt['hmtx']
font = ImageFont.truetype(ttf, em)

lines = [
    f'# {tt["name"].getDebugName(4)} by Jayvee Enaguas, CC0 1.0. Built by design/build_font.py; edit freely.',
    '# One section per character: [code char], then one row of . and # per pixel row. Every',
    '# glyph is as tall as "height"; its width is its own. The renderer adds one blank column',
    '# between letters.',
    f'height {height}',
    '',
]
for code in range(32, 127):
    if code not in cmap:
        continue
    advance = hmtx[cmap[code]][0] // unit
    img = Image.new('1', (advance + 4, height), 0)
    draw = ImageDraw.Draw(img)
    draw.fontmode = '1'
    draw.text((0, asc), chr(code), font=font, fill=1, anchor='ls')
    px = img.load()
    inked = [x for x in range(img.width) if any(px[x, y] for y in range(height))]
    width = max(advance - 1, inked[-1] + 1 if inked else 0)     # the advance includes the gap
    rows = [''.join('#' if px[x, y] else '.' for x in range(width)) for y in range(height)]
    if code != 32 and width > 1 and all(r[0] == '.' for r in rows):
        rows = [r[1:] for r in rows]                            # drop the left bearing
    lines.append(f'[{code} {chr(code)}]')
    lines.extend(rows)
    lines.append('')

out.write_text('\n'.join(lines))
print(f'{out}: {height} rows')
