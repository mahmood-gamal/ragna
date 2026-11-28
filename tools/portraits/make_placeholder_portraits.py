#!/usr/bin/env python3
"""Draws the placeholder dialogue bust portraits.

Usage: python3 tools/portraits/make_placeholder_portraits.py   (needs Pillow)

Writes assets/portraits/<character>/<expression>.png for every character below and
every expression (neutral, happy, angry, sad, surprised). Each portrait is
300x440 px with a transparent background, faces right, and stands on its bottom
pixel row (the bottom-centre anchor). See docs/dialogue.md.
"""
from pathlib import Path

from PIL import Image, ImageDraw

WIDTH, HEIGHT = 300, 440
SCALE = 2  # draw big, then shrink, for smooth edges
OUT = Path(__file__).resolve().parents[2] / "assets/portraits"
EXPRESSIONS = ["neutral", "happy", "angry", "sad", "surprised"]

INK = (40, 28, 24, 255)
SKIN = (240, 205, 170, 255)
SKIN_SHADE = (215, 175, 140, 255)
WHITE = (250, 250, 250, 255)

CHARACTERS = {
    # The player character; colours match the placeholder sprite sheet.
    "hero": {
        "hair": (120, 70, 40, 255),
        "body": (200, 60, 50, 255),
        "trim": (150, 40, 35, 255),
        "beard": None,
        "brows": (90, 50, 30, 255),
    },
    # The village elder: grey hair and beard, brown robe.
    "elder": {
        "hair": (200, 200, 205, 255),
        "body": (120, 85, 55, 255),
        "trim": (200, 165, 80, 255),
        "beard": (225, 225, 230, 255),
        "brows": (170, 170, 175, 255),
    },
}


def s(*values):
    """Scales design coordinates (300x440 space) to the drawing canvas."""
    return [v * SCALE for v in values]


def draw_bust(d, look, expression):
    cx = WIDTH / 2

    # Shoulders and body: a rounded trapezoid down to the bottom edge.
    d.polygon(s(cx - 140, HEIGHT, cx - 120, 330, cx - 60, 300, cx + 60, 300, cx + 120, 330, cx + 140, HEIGHT),
              fill=look["body"], outline=INK, width=3 * SCALE)
    # Collar trim.
    d.polygon(s(cx - 45, 300, cx, 360, cx + 45, 300), fill=look["trim"], outline=INK, width=3 * SCALE)

    # Neck.
    d.rectangle(s(cx - 22, 250, cx + 22, 310), fill=SKIN_SHADE, outline=INK, width=3 * SCALE)

    # Hair behind the head.
    d.ellipse(s(cx - 78, 95, cx + 74, 250), fill=look["hair"], outline=INK, width=3 * SCALE)

    # Head (a little forward: the portrait faces right).
    hx, hy = cx + 6, 185
    d.ellipse(s(hx - 66, hy - 80, hx + 66, hy + 80), fill=SKIN, outline=INK, width=3 * SCALE)
    # Nose, pointing right.
    d.polygon(s(hx + 40, hy - 8, hx + 66, hy + 22, hx + 40, hy + 26), fill=SKIN_SHADE, outline=INK, width=2 * SCALE)
    # Ear on the far side.
    d.ellipse(s(hx - 72, hy - 14, hx - 50, hy + 20), fill=SKIN_SHADE, outline=INK, width=2 * SCALE)
    # Fringe.
    d.pieslice(s(hx - 70, hy - 86, hx + 70, hy + 10), 180, 360, fill=look["hair"], outline=INK, width=3 * SCALE)

    if look["beard"]:
        d.chord(s(hx - 52, hy - 10, hx + 62, hy + 110), 0, 180, fill=look["beard"], outline=INK, width=3 * SCALE)

    draw_face(d, look, expression, hx, hy)


def draw_face(d, look, expression, hx, hy):
    eyes = [(hx - 16, hy - 4), (hx + 20, hy - 4)]
    mouth_x, mouth_y = hx + 10, hy + 42
    if look["beard"]:
        mouth_y = hy + 38

    # Eyes.
    for ex, ey in eyes:
        if expression == "happy":
            d.arc(s(ex - 9, ey - 6, ex + 9, ey + 10), 200, 340, fill=INK, width=4 * SCALE)
            continue
        r = 9 if expression == "surprised" else 7
        d.ellipse(s(ex - r, ey - r - 2, ex + r, ey + r + 2), fill=WHITE, outline=INK, width=2 * SCALE)
        pr = 4 if expression == "surprised" else 4.5
        d.ellipse(s(ex + 1 - pr, ey - pr, ex + 1 + pr, ey + pr), fill=INK)

    # Brows.
    brow = look["brows"]
    for i, (ex, ey) in enumerate(eyes):
        inner = 1 if i == 0 else -1  # towards the middle of the face
        if expression == "angry":
            d.line(s(ex - 12 * inner, ey - 22, ex + 10 * inner, ey - 12), fill=brow, width=5 * SCALE)
        elif expression == "sad":
            d.line(s(ex - 12 * inner, ey - 12, ex + 10 * inner, ey - 22), fill=brow, width=5 * SCALE)
        elif expression == "surprised":
            d.arc(s(ex - 12, ey - 32, ex + 12, ey - 14), 200, 340, fill=brow, width=5 * SCALE)
        else:
            d.line(s(ex - 11, ey - 18, ex + 11, ey - 18), fill=brow, width=5 * SCALE)

    # Mouth.
    if expression == "happy":
        d.chord(s(mouth_x - 18, mouth_y - 14, mouth_x + 18, mouth_y + 14), 0, 180, fill=(150, 50, 50, 255), outline=INK, width=3 * SCALE)
    elif expression == "sad":
        d.arc(s(mouth_x - 15, mouth_y, mouth_x + 15, mouth_y + 22), 200, 340, fill=INK, width=4 * SCALE)
    elif expression == "angry":
        d.line(s(mouth_x - 16, mouth_y + 6, mouth_x + 16, mouth_y + 2), fill=INK, width=4 * SCALE)
    elif expression == "surprised":
        d.ellipse(s(mouth_x - 9, mouth_y - 6, mouth_x + 9, mouth_y + 16), fill=(120, 40, 40, 255), outline=INK, width=3 * SCALE)
    else:
        d.line(s(mouth_x - 14, mouth_y + 4, mouth_x + 14, mouth_y + 4), fill=INK, width=4 * SCALE)


def main():
    for character, look in CHARACTERS.items():
        folder = OUT / character
        folder.mkdir(parents=True, exist_ok=True)
        for expression in EXPRESSIONS:
            image = Image.new("RGBA", (WIDTH * SCALE, HEIGHT * SCALE), (0, 0, 0, 0))
            draw_bust(ImageDraw.Draw(image), look, expression)
            image = image.resize((WIDTH, HEIGHT), Image.LANCZOS)
            path = folder / f"{expression}.png"
            image.save(path, optimize=True)
            print(f"Wrote {path.relative_to(OUT.parents[1])}")


if __name__ == "__main__":
    main()
