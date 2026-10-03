"""
Builds the ProCargo logo files in this folder.

    python brand/build_logo.py

The wordmark is drawn from Poppins Bold and saved as vector outlines,
so the logo looks identical on every computer, with no font needed.

Colours
    Navy    #0A1A3A   main brand colour
    Orange  #FF7A1A   accent (the "P" bowl and "Cargo")
    White   #FFFFFF   on dark backgrounds
"""

from pathlib import Path

from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont

HERE = Path(__file__).parent
FONT_FILE = "/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf"

NAVY = "#0A1A3A"
ORANGE = "#FF7A1A"
WHITE = "#FFFFFF"


# ---------------------------------------------------------------------------
# The mark: a rounded navy square holding a "P".
# The stem is white; the bowl is an orange arrow pointing forward,
# with a small white cargo box inside it.
# Drawn on a 64 x 64 grid.
# ---------------------------------------------------------------------------
def mark_shapes(background: str, stem: str, bowl: str, box: str) -> str:
    return f"""
    <rect width="64" height="64" rx="15" fill="{background}"/>
    <rect x="15" y="13" width="10" height="38" rx="2.5" fill="{stem}"/>
    <path d="M25 13 H37 L50 25.5 L37 38 H25 V30 H33.5 L38 25.5 L33.5 21 H25 Z" fill="{bowl}"/>
    <rect x="27.5" y="22.5" width="6" height="6" rx="1" fill="{box}"/>
    """


def wordmark_paths(text_parts, font_size: float, x: float, baseline: float):
    """Converts text to SVG paths. text_parts = [(text, colour), ...]."""
    font = TTFont(FONT_FILE)
    glyph_set = font.getGlyphSet()
    cmap = font.getBestCmap()
    scale = font_size / font["head"].unitsPerEm

    paths = []
    cursor = x
    for text, colour in text_parts:
        for character in text:
            glyph_name = cmap[ord(character)]
            pen = SVGPathPen(glyph_set)
            # Font units point up; SVG points down, so flip the y axis.
            transform = (scale, 0, 0, -scale, cursor, baseline)
            glyph_set[glyph_name].draw(TransformPen(pen, transform))
            outline = pen.getCommands()
            if outline:
                paths.append(f'<path d="{outline}" fill="{colour}"/>')
            cursor += glyph_set[glyph_name].width * scale
    return "\n    ".join(paths), cursor


def svg(width: float, height: float, body: str, title: str) -> str:
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width:.0f} {height:.0f}" '
        f'width="{width:.0f}" height="{height:.0f}" role="img" aria-label="{title}">\n'
        f"  <title>{title}</title>\n"
        f"  {body.strip()}\n"
        f"</svg>\n"
    )


def build():
    # 1. Mark only (app icon, favicon, social avatar)
    mark = mark_shapes(NAVY, WHITE, ORANGE, WHITE)
    (HERE / "procargo-mark.svg").write_text(svg(64, 64, mark, "ProCargo"))

    # 2. Horizontal logo for light backgrounds: mark + "Pro" navy + "Cargo" orange
    words, end_x = wordmark_paths([("Pro", NAVY), ("Cargo", ORANGE)], 42, 78, 47)
    body = f"<g>{mark}</g>\n    {words}"
    (HERE / "procargo-logo.svg").write_text(svg(end_x + 4, 64, body, "ProCargo"))

    # 3. Horizontal logo for dark backgrounds: white "Pro"
    words_dark, end_x = wordmark_paths([("Pro", WHITE), ("Cargo", ORANGE)], 42, 78, 47)
    mark_dark = mark_shapes(ORANGE, NAVY, WHITE, ORANGE)
    body_dark = f"<g>{mark_dark}</g>\n    {words_dark}"
    (HERE / "procargo-logo-white.svg").write_text(svg(end_x + 4, 64, body_dark, "ProCargo"))

    print("Logo files written to", HERE)


if __name__ == "__main__":
    build()
