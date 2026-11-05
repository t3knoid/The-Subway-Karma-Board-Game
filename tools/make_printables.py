"""Generate the README image and the printable board PDF from the original board artwork.

Outputs (repo root):
    subway_karma.png          README picture of the board
    Subway Karma Game.pdf     US Letter, print at 100%:
                              page 1 = game board, page 2 = cut-out spinner

The board is drawn by tools/make_board.py (same artwork as the in-game board); the spinner
uses the game's own spinner images from SubwayKarmaGame/Assets/Resources.

Usage (from the repo root, Windows; needs Pillow and the fonts used by make_board.py):
    python -I tools/make_printables.py
"""
import importlib.util
import sys

sys.dont_write_bytecode = True   # importing make_board.py should not leave __pycache__ behind
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
RES = "SubwayKarmaGame/Assets/Resources/"
DPI = 300
PAGE = (int(8.5 * DPI), int(11 * DPI))     # 2550 x 3300
INK = (0x33, 0x33, 0x33)
FONT = "C:/Windows/Fonts/bahnschrift.ttf"

# Spinner arrow pivot (from SpinnerController.cs): pixel (85, 230) in the 948 x 452 arrow image.
ARROW_PIVOT = (85, 230)
WHEEL_PIVOT = (1062, 1062)


def load_board_module():
    spec = importlib.util.spec_from_file_location("make_board", os.path.join(HERE, "make_board.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def font(size, weight="Regular"):
    f = ImageFont.truetype(FONT, size)
    try:
        f.set_variation_by_name(weight)
    except Exception:
        pass
    return f


def centred(d, y, text, f, width=PAGE[0]):
    b = d.textbbox((0, 0), text, font=f)
    d.text(((width - (b[2] - b[0])) / 2 - b[0], y - b[1]), text, font=f, fill=INK)
    return b[3] - b[1]


def board_page(board):
    page = Image.new("RGB", PAGE, "white")
    w = PAGE[0] - 2 * int(0.25 * DPI)                       # 0.25" side margins
    h = round(board.height * w / board.width)
    page.paste(board.resize((w, h), Image.LANCZOS), ((PAGE[0] - w) // 2, (PAGE[1] - h) // 2))
    d = ImageDraw.Draw(page)
    centred(d, PAGE[1] - 150, "The Subway Karma Game  ·  print at 100% (actual size), ideally in colour on card stock", font(34))
    return page


def spinner_page():
    page = Image.new("RGB", PAGE, "white")
    d = ImageDraw.Draw(page)
    centred(d, 140, "Spinner", font(90, "SemiBold"))

    wheel = Image.open(RES + "spinner.png").convert("RGBA")
    arrow = Image.open(RES + "spinner_arrow.png").convert("RGBA")
    # The source arrow has a stray green blob above its tail; erase the blob's area
    # (padded, to catch its soft edge) rather than editing the game's art.
    px = arrow.load()
    green = [(x, y) for y in range(arrow.height) for x in range(arrow.width)
             if px[x, y][3] and px[x, y][1] > px[x, y][0] + 25 and px[x, y][1] > px[x, y][2] + 25]
    if green:
        pad = 8
        x0, x1 = min(p[0] for p in green) - pad, max(p[0] for p in green) + pad
        y0, y1 = min(p[1] for p in green) - pad, max(p[1] for p in green) + pad
        ImageDraw.Draw(arrow).rectangle([x0, y0, x1, y1], fill=(0, 0, 0, 0))
    scale = (6.0 * DPI) / wheel.width                          # 6" wheel
    wheel = wheel.resize((round(wheel.width * scale), round(wheel.height * scale)), Image.LANCZOS)
    arrow = arrow.resize((round(arrow.width * scale), round(arrow.height * scale)), Image.LANCZOS)

    wx, wy = (PAGE[0] - wheel.width) // 2, 320
    page.paste(wheel, (wx, wy), wheel)
    ax, ay = (PAGE[0] - arrow.width) // 2, wy + wheel.height + 140
    page.paste(arrow, (ax, ay), arrow)

    # Mark both pivot points so the fastener goes in the right place.
    for px, py in ((wx + WHEEL_PIVOT[0] * scale, wy + WHEEL_PIVOT[1] * scale),
                   (ax + ARROW_PIVOT[0] * scale, ay + ARROW_PIVOT[1] * scale)):
        r = 14
        d.ellipse([px - r, py - r, px + r, py + r], outline=(200, 0, 0), width=5)

    steps = [
        "1. Cut out the wheel and the arrow. Gluing them to card stock first makes them sturdier.",
        "2. Pierce the red circles: the centre of the wheel and the round end of the arrow.",
        "3. Join them with a brass paper fastener, loose enough for the arrow to spin freely.",
        "4. Flick the arrow and move the number of squares it stops on.",
    ]
    y = ay + arrow.height + 120
    f = font(44)
    for s in steps:
        y += centred(d, y, s, f) + 34
    return page


def main():
    mb = load_board_module()
    board = mb.build()

    readme = board.resize((970, round(board.height * 970 / board.width)), Image.LANCZOS)
    readme.save("subway_karma.png", optimize=True)

    pages = [board_page(board), spinner_page()]
    pages[0].save("Subway Karma Game.pdf", save_all=True, append_images=pages[1:], resolution=DPI)
    print("wrote subway_karma.png", readme.size, "and Subway Karma Game.pdf (2 pages)")


if __name__ == "__main__":
    main()
