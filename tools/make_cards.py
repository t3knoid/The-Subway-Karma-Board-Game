"""Generate the printable Subway Karma karma cards from the game's own deck.

The deck is read from SubwayKarmaGame/Assets/Scripts/Rules/StandardGame.cs, the same
data the game uses, so printed cards always match the digital game.

Output (US Letter, 300 dpi, Avery 5371 business-card layout: 2 x 5 cards of 3.5" x 2"):
    assets/karma1.jpg    positive cards
    assets/karma2.jpg    negative cards
    assets/karma3.jpg    special cards
    Karma Cards.pdf      all sheets, ready to print at 100% scale

Usage (from the repo root, Windows; needs Pillow and the Franklin Gothic / Arial Black fonts):
    python -I tools/make_cards.py [preview_dir]
"""
import math
import os
import re
import sys
from PIL import Image, ImageDraw, ImageFont

DECK_SOURCE = "SubwayKarmaGame/Assets/Scripts/Rules/StandardGame.cs"

DPI = 300
PAGE = (int(8.5 * DPI), int(11 * DPI))            # 2550 x 3300
CARD = (int(3.5 * DPI), int(2.0 * DPI))           # 1050 x 600
COLS, ROWS = 2, 5
MARGIN = (int(0.75 * DPI), int(0.5 * DPI))        # Avery 5371 left / top margins

INK = (0x33, 0x33, 0x33)
POSITIVE = (0x4f, 0x6e, 0x00)                     # darker board green, readable in print
NEGATIVE = (0xc8, 0x10, 0x10)
GUIDE = (0xd0, 0xd0, 0xd0)

FONTS = "C:/Windows/Fonts/"
TITLE_FONT = FONTS + "FRABK.TTF"                  # Franklin Gothic Book (as on the original cards)
POINTS_FONT = FONTS + "ariblk.ttf"                # Arial Black

# Rule text printed under the special cards' titles (matches docs/GAME_RULES.md).
SUBTITLES = {
    "InstantKarma": "Take a random card from every other player",
    "NoOp": "Sorry, no points for being human",
}


# -- Deck -------------------------------------------------------------------------------

def read_deck(path):
    """Parse CreateDeck() in StandardGame.cs into [(title, points, type)], expanding loops."""
    src = open(path, encoding="utf-8").read()
    body = src[src.index("CreateDeck()"):]
    card = r'deck\.Add\(new KarmaCard\("((?:[^"\\]|\\.)*)",\s*([+-]?\d+)(?:,\s*KarmaCardType\.(\w+))?\)\);'
    deck = []
    for m in re.finditer(r'for \(int i = 0; i < (\d+); i\+\+\)\s*' + card + '|' + card, body):
        if m.group(1):
            n, title, pts, kind = int(m.group(1)), m.group(2), m.group(3), m.group(4)
        else:
            n, title, pts, kind = 1, m.group(5), m.group(6), m.group(7)
        deck += [(title.replace('\\"', '"'), int(pts), kind or "Standard")] * n
    if not deck:
        sys.exit("No cards found in " + path)
    return deck


# -- Drawing ----------------------------------------------------------------------------

def text_size(d, text, f):
    b = d.textbbox((0, 0), text, font=f)
    return b[2] - b[0], b[3] - b[1], b


def wrap(d, text, f, max_w):
    out, line = [], ""
    for w in text.split():
        trial = (line + " " + w).strip()
        if text_size(d, trial, f)[0] <= max_w or not line:
            line = trial
        else:
            out.append(line)
            line = w
    out.append(line)
    return out


def train_icon(size):
    """Original train-front pictogram in a rounded box (drawn, not copied)."""
    s = size * 4
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    lw = max(2, s // 40)
    d.rounded_rectangle([lw, lw, s - lw, s - lw], radius=s // 12, fill="white", outline="black", width=lw)
    bx0, by0, bx1, by1 = s * 0.30, s * 0.22, s * 0.70, s * 0.70        # body
    d.rounded_rectangle([bx0, by0, bx1, by1], radius=s * 0.09, fill="black")
    d.rounded_rectangle([s * 0.36, s * 0.32, s * 0.64, s * 0.46], radius=s * 0.03, fill="white")   # windscreen
    d.rectangle([s * 0.44, s * 0.24, s * 0.56, s * 0.29], fill="white")                           # sign
    for cx in (0.38, 0.62):                                                                       # headlights
        r = s * 0.035
        d.ellipse([s * cx - r, s * 0.58 - r, s * cx + r, s * 0.58 + r], fill="white")
    for cx in (0.45, 0.55):                                                                       # roof lights
        r = s * 0.025
        d.ellipse([s * cx - r, s * 0.15 - r, s * cx + r, s * 0.15 + r], fill="black")
    rail = s * 0.045                                                                              # rails
    d.line([(s * 0.38, s * 0.68), (s * 0.27, s * 0.85)], fill="black", width=int(rail))
    d.line([(s * 0.62, s * 0.68), (s * 0.73, s * 0.85)], fill="black", width=int(rail))
    return im.resize((size, size), Image.LANCZOS)


def draw_card(title, points, kind):
    w, h = CARD
    im = Image.new("RGB", (w, h), "white")
    d = ImageDraw.Draw(im)
    icon = train_icon(190)
    special = kind != "Standard"

    if special:
        im.paste(icon, ((w - icon.width) // 2, 40), icon)
        tf = ImageFont.truetype(TITLE_FONT, 72)
        sf = ImageFont.truetype(TITLE_FONT, 40)
        y = 40 + icon.height + 45
        for line in wrap(d, title, tf, w - 120):
            tw, th, b = text_size(d, line, tf)
            d.text(((w - tw) / 2 - b[0], y - b[1]), line, font=tf, fill=INK)
            y += th + 14
        sub = SUBTITLES.get(kind, "")
        for line in wrap(d, sub, sf, w - 120):
            tw, th, b = text_size(d, line, sf)
            d.text(((w - tw) / 2 - b[0], y + 8 - b[1]), line, font=sf, fill=INK)
            y += th + 10
        return im

    # Points card: icon centred-left, big value on the right, title below.
    im.paste(icon, (int(w * 0.36), 40), icon)
    pf = ImageFont.truetype(POINTS_FONT, 190)
    label = f"{points:+d}"
    pw, ph, b = text_size(d, label, pf)
    d.text((w - 80 - pw - b[0], 40 + (icon.height - ph) / 2 - b[1]), label, font=pf,
           fill=POSITIVE if points > 0 else NEGATIVE)

    tf = ImageFont.truetype(TITLE_FONT, 62)
    lines = wrap(d, title, tf, w - 140)
    sizes = [text_size(d, t, tf) for t in lines]
    total = sum(s[1] for s in sizes) + 16 * (len(lines) - 1)
    top, bottom = 40 + icon.height + 20, h - 40
    y = top + (bottom - top - total) / 2
    for line, (tw, th, b) in zip(lines, sizes):
        d.text(((w - tw) / 2 - b[0], y - b[1]), line, font=tf, fill=INK)
        y += th + 16
    return im


def dashed(d, p0, p1, dash=18, gap=14):
    (x0, y0), (x1, y1) = p0, p1
    length = math.hypot(x1 - x0, y1 - y0)
    n = int(length // (dash + gap)) + 1
    for k in range(n):
        a = k * (dash + gap) / length
        b = min(1, (k * (dash + gap) + dash) / length)
        d.line([(x0 + (x1 - x0) * a, y0 + (y1 - y0) * a), (x0 + (x1 - x0) * b, y0 + (y1 - y0) * b)],
               fill=GUIDE, width=3)


def sheet(cards):
    page = Image.new("RGB", PAGE, "white")
    for i, c in enumerate(cards):
        r, col = divmod(i, COLS)
        page.paste(draw_card(*c), (MARGIN[0] + col * CARD[0], MARGIN[1] + r * CARD[1]))
    d = ImageDraw.Draw(page)                      # light cutting guides on the card grid
    for col in range(COLS + 1):
        x = MARGIN[0] + col * CARD[0]
        dashed(d, (x, 0), (x, PAGE[1]))
    for r in range(ROWS + 1):
        y = MARGIN[1] + r * CARD[1]
        dashed(d, (0, y), (PAGE[0], y))
    return page


def main():
    deck = read_deck(DECK_SOURCE)
    groups = [
        [c for c in deck if c[2] == "Standard" and c[1] > 0],
        [c for c in deck if c[2] == "Standard" and c[1] < 0],
        [c for c in deck if c[2] != "Standard" or c[1] == 0],
    ]
    pages = []
    for g in groups:
        for k in range(0, len(g), COLS * ROWS):
            pages.append(sheet(g[k:k + COLS * ROWS]))

    os.makedirs("assets", exist_ok=True)
    for n, p in enumerate(pages, 1):
        p.save(f"assets/karma{n}.jpg", quality=92, dpi=(DPI, DPI))
    pages[0].save("Karma Cards.pdf", save_all=True, append_images=pages[1:], resolution=DPI)
    print(f"{len(deck)} cards -> {len(pages)} sheets: assets/karma1..{len(pages)}.jpg, Karma Cards.pdf")

    if len(sys.argv) > 1:                         # optional small previews for review
        os.makedirs(sys.argv[1], exist_ok=True)
        for n, p in enumerate(pages, 1):
            p.resize((PAGE[0] // 4, PAGE[1] // 4), Image.LANCZOS).save(os.path.join(sys.argv[1], f"sheet{n}.png"))
        draw_card(*groups[1][2]).save(os.path.join(sys.argv[1], "card_detail.png"))


if __name__ == "__main__":
    main()
