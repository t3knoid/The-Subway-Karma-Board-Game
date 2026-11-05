"""Draw an original Subway Karma game board (no MTA map, route symbols or line names).

Keeps the exact geometry of the original board.jpg (2411 x 2835 px, 22 squares) so the
tap zones in BoardSetup.cs and the token positions still line up.

Usage (from the repo root, Windows; needs Pillow and the Bahnschrift / Century Gothic fonts):
    python -I tools/make_board.py SubwayKarmaGame/Assets/Resources/board.jpg [preview.png]

Line names, colours and markers are invented. Place names are real New York
neighbourhoods but deliberately not real station names.
"""
import math
import sys
from PIL import Image, ImageDraw, ImageFont

W, H = 2411, 2835

# Cell boundaries in pixels, derived from BoardSetup.SquareCenters (centre = 1205.5 + 100x, 1417.5 - 100y).
XS = [4, 395, 800.5, 1206, 1611.5, 2017, 2408]
YS = [9.5, 401.5, 807.7, 1213.9, 1620.1, 2026.3, 2432.5, 2824.5]
GAP = 5  # half-width of the dark grid line between squares

# -- Palette ---------------------------------------------------------------------------
FRAME = (0x33, 0x33, 0x33)
GREEN = (0x8e, 0xb6, 0x10)
BLACK = (0x14, 0x14, 0x14)
WHITE = (0xff, 0xff, 0xff)
RED_TEXT = (0xe0, 0x12, 0x12)
MAP_BG = (0xf6, 0xf5, 0xf1)
WATER = (0xd6, 0xe6, 0xef)
STREET = (0xe7, 0xe5, 0xdf)

# Invented lines: colour families like a real subway, but our own shades, names and letters.
LINES = {
    "8": ("Riverside Local",    (0xe0, 0x3a, 0x2f), WHITE),
    "9": ("Riverside Express",  (0xe0, 0x3a, 0x2f), WHITE),
    "K": ("Americas Express",   (0xf5, 0x8a, 0x1f), WHITE),
    "P": ("Americas Local",     (0xf5, 0x8a, 0x1f), WHITE),
    "U": ("Hudson Express",     (0x1f, 0x5f, 0xbf), WHITE),
    "Y": ("Hudson Local",       (0x1f, 0x5f, 0xbf), WHITE),
    "X": ("Queensboro Express", (0xf2, 0xc0, 0x1e), BLACK),
    "H": ("Queensboro Local",   (0xf2, 0xc0, 0x1e), BLACK),
}

# -- Fonts -----------------------------------------------------------------------------
FONTS = "C:/Windows/Fonts/"


def font(name, size):
    return ImageFont.truetype(FONTS + name, size)


def sans(size, weight="Regular"):
    """Bahnschrift (DIN-like, close to transit signage) at a named weight."""
    f = font("bahnschrift.ttf", size)
    try:
        f.set_variation_by_name(weight)
    except Exception:
        pass
    return f


def gothic(size, bold=False):
    return font("GOTHICB.TTF" if bold else "GOTHIC.TTF", size)


# -- Squares (index → content), matching StandardGame.cs -------------------------------
STATIONS = {0: ("Upper West Side", "8"), 5: ("Midtown", "P"), 11: ("Hell's Kitchen", "Y"), 16: ("Astoria", "H")}
EXPRESS = {4: "K", 10: "U", 15: "X", 21: "9"}
EARLY = {1, 8, 12, 19}
LATE = {3, 9, 14, 20}
SICK = {6, 17}


def cell_rect(i):
    """Pixel rect (x0, y0, x1, y1) of square i, and the rotation that makes its text face outward."""
    if i <= 5:                       # top row, left → right; text faces the top edge
        return (XS[i], YS[0], XS[i + 1], YS[1]), 180 if i != 0 else -90
    if i <= 10:                      # right column, top → bottom
        r = i - 5
        return (XS[5], YS[r], XS[6], YS[r + 1]), 90
    if i <= 16:                      # bottom row, right → left
        c = 16 - i
        return (XS[c], YS[6], XS[c + 1], YS[7]), 0 if i != 11 else 90
    r = 22 - i                       # left column, bottom → top
    return (XS[0], YS[r], XS[1], YS[r + 1]), -90


# -- Drawing helpers -------------------------------------------------------------------

def text_size(d, text, f):
    b = d.textbbox((0, 0), text, font=f)
    return b[2] - b[0], b[3] - b[1], b


def centred_lines(img, lines, box, gap=12):
    """lines = [(text, font, colour)], vertically and horizontally centred in box."""
    d = ImageDraw.Draw(img)
    sizes = [text_size(d, t, f) for t, f, _ in lines]
    total = sum(s[1] for s in sizes) + gap * (len(lines) - 1)
    x0, y0, x1, y1 = box
    y = y0 + (y1 - y0 - total) / 2
    for (t, f, c), (w, h, b) in zip(lines, sizes):
        d.text((x0 + (x1 - x0 - w) / 2 - b[0], y - b[1]), t, font=f, fill=c)
        y += h + gap


def wrap(d, text, f, max_w):
    words, out, line = text.split(), [], ""
    for w in words:
        trial = (line + " " + w).strip()
        if text_size(d, trial, f)[0] <= max_w:
            line = trial
        else:
            out.append(line)
            line = w
    out.append(line)
    return out


def marker(size, key):
    """Rounded diamond with a letter/number: deliberately not a circular route 'bullet'."""
    _, colour, ink = LINES[key]
    s = size * 4                      # supersample for smooth edges
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    sq = Image.new("RGBA", (int(s * 0.70), int(s * 0.70)), (0, 0, 0, 0))
    ImageDraw.Draw(sq).rounded_rectangle([0, 0, sq.width - 1, sq.height - 1], radius=int(s * 0.12), fill=colour)
    sq = sq.rotate(45, resample=Image.BICUBIC, expand=True)
    im.alpha_composite(sq, ((s - sq.width) // 2, (s - sq.height) // 2))
    d = ImageDraw.Draw(im)
    f = sans(int(s * 0.42), "Bold")
    w, h, b = text_size(d, key, f)
    d.text(((s - w) / 2 - b[0], (s - h) / 2 - b[1]), key, font=f, fill=ink)
    return im.resize((size, size), Image.LANCZOS)


# -- Square content (drawn upright, then rotated to face its edge) ----------------------

def content_station(L, D, name, key):
    im = Image.new("RGBA", (L, D), BLACK + (255,))
    band = int(D * 0.34)
    m = marker(int(band * 0.82), key)
    im.alpha_composite(m, (18, (band - m.height) // 2 + 4))
    d = ImageDraw.Draw(im)
    f = sans(int(band * 0.27), "Bold")
    lines = wrap(d, LINES[key][0], f, L - m.width - 46)
    centred_lines(im, [(t, f, WHITE) for t in lines], (m.width + 22, 6, L - 10, band), gap=6)
    d.rectangle([12, band + 8, L - 13, D - 13], fill=WHITE)
    nf = sans(int(D * 0.15), "SemiBold")
    nl = wrap(d, name, nf, L - 60)
    centred_lines(im, [(t, nf, BLACK) for t in nl], (12, band + 8, L - 13, D - 13), gap=8)
    return im


def content_express(L, D, key):
    im = Image.new("RGBA", (L, D), GREEN + (255,))
    d = ImageDraw.Draw(im)
    band = int(D * 0.36)
    d.rectangle([0, 0, L, band], fill=BLACK)
    m = marker(int(band * 0.80), key)
    im.alpha_composite(m, (14, (band - m.height) // 2))
    f = sans(int(band * 0.25), "Bold")
    lines = wrap(d, LINES[key][0], f, L - m.width - 40)
    centred_lines(im, [(t, f, WHITE) for t in lines], (m.width + 18, 4, L - 8, band), gap=6)
    bf = gothic(int(D * 0.083))
    body = wrap(d, "Caught Express Train! Spin again and move that many extra spaces.", bf, L - 50)
    centred_lines(im, [(t, bf, WHITE) for t in body], (20, band + 6, L - 20, D - 6), gap=8)
    return im


def content_effect(L, D, lines, colour):
    im = Image.new("RGBA", (L, D), GREEN + (255,))
    d = ImageDraw.Draw(im)
    size = int(D * 0.15)
    while size > 20:                                   # shrink until every line fits
        f = sans(size, "SemiBold")
        if max(text_size(d, t, f)[0] for t in lines) <= L - 40:
            break
        size -= 2
    gap = int(D * (0.06 if len(lines) <= 3 else 0.035))
    centred_lines(im, [(t, f, colour) for t in lines], (10, 10, L - 10, D - 10), gap=gap)
    return im


def square_content(i, L, D):
    if i in STATIONS:
        return content_station(L, D, *STATIONS[i])
    if i in EXPRESS:
        return content_express(L, D, EXPRESS[i])
    if i in EARLY:
        return content_effect(L, D, ["Early Train", "Move 1 Space", "Ahead"], WHITE)
    if i in LATE:
        return content_effect(L, D, ["Late Train", "Lose a Turn"], RED_TEXT)
    if i in SICK:
        return content_effect(L, D, ["Sick", "Passenger", "Go Back 1", "Space"], RED_TEXT)
    return Image.new("RGBA", (L, D), GREEN + (255,))   # empty square


# -- Centre: original stylised transit map, title card, instructions -------------------

def draw_route(d, pts, colour, width):
    d.line(pts, fill=colour, width=width, joint="curve")
    r = width // 2
    for x, y in (pts[0], pts[-1]):
        d.ellipse([x - r, y - r, x + r, y + r], fill=colour)


def centre_panel(w, h):
    s = 2                                  # supersample
    im = Image.new("RGBA", (w * s, h * s), MAP_BG + (255,))
    d = ImageDraw.Draw(im)
    S = lambda *p: [(x * s, y * s) for x, y in p]

    # Rivers on both sides and a faint street grid: suggests Manhattan without copying any map.
    d.polygon(S((0, 0), (150, 0), (120, 700), (170, 1400), (110, h), (0, h)), fill=WATER)
    d.polygon(S((w, 0), (w - 120, 0), (w - 210, 650), (w - 150, 1300), (w - 230, h), (w, h)), fill=WATER)
    for gx in range(260, w - 200, 120):
        d.line(S((gx, 0), (gx - 60, h)), fill=STREET, width=3 * s)
    for gy in range(80, h, 95):
        d.line(S((150, gy), (w - 150, gy - 30)), fill=STREET, width=2 * s)

    lw = 30 * s
    red, orange, blue, yellow = (LINES[k][1] for k in ("8", "K", "U", "X"))
    green = (0x2e, 0x9e, 0x4f)                     # decorative east-side line
    routes = [
        (red,    S((330, -40), (330, 520), (470, 700), (470, 1500), (390, 1650), (390, h + 40))),
        (blue,   S((250, -40), (250, 900), (300, 1000), (300, h + 40))),
        (orange, S((610, -40), (610, 640), (700, 760), (700, 1300), (620, 1420), (620, h + 40))),
        (yellow, S((w + 40, 560), (1180, 560), (1060, 680), (1060, 1150), (900, 1320), (900, h + 40))),
        (green,  S((1360, -40), (1360, 1000), (1460, 1100), (1460, h + 40))),
    ]
    for colour, pts in routes:
        draw_route(d, pts, colour, lw)
        d.line(pts, fill=WHITE + (255,), width=4 * s, joint="curve")   # thin centre stripe

    labels = [   # (point on a route, name, side); kept clear of the title card and instructions
        ((330, 180), "Harlem", "r"), ((330, 420), "Upper West Side", "r"),
        ((1360, 300), "Upper East Side", "l"), ((1300, 560), "Astoria", "a"),
        ((1060, 1100), "Murray Hill", "r"), ((300, 1180), "Hell's Kitchen", "l"),
        ((300, 1500), "Chelsea", "l"), ((620, 1750), "SoHo", "r"), ((390, 1880), "Tribeca", "r"),
    ]
    lf = sans(36 * s, "SemiBold")
    for (x, y), name, side in labels:
        X, Y = x * s, y * s
        r = 15 * s
        d.ellipse([X - r, Y - r, X + r, Y + r], fill=BLACK, outline=WHITE, width=4 * s)
        tw, th, b = text_size(d, name, lf)
        if side == "r":
            pos = (X + r + 12 * s, Y - th / 2 - b[1])
        elif side == "l":
            pos = (X - r - 12 * s - tw, Y - th / 2 - b[1])
        else:
            pos = (X - tw / 2, Y - r - 10 * s - th - b[1])
        d.text(pos, name, font=lf, fill=(0x22, 0x22, 0x22))

    im = im.resize((w, h), Image.LANCZOS)

    # Title card, as on the original board.
    card = title_card(980)
    card = card.rotate(32, resample=Image.BICUBIC, expand=True)
    im.alpha_composite(card, (int(w * 0.50 - card.width / 2), int(h * 0.43 - card.height / 2)))

    # Instructions box (bottom right).
    box = instructions(760, 720)
    im.alpha_composite(box, (w - box.width - 30, h - box.height - 24))
    return im


def title_card(width):
    h = int(width * 0.50)
    im = Image.new("RGBA", (width, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, width - 1, h - 1], fill=GREEN, outline=FRAME, width=10)
    centred_lines(im, [("The Subway", gothic(int(width * 0.09)), FRAME),
                       ("KARMA", gothic(int(width * 0.22), bold=True), FRAME),
                       ("Game", gothic(int(width * 0.09)), FRAME)], (0, 0, width, h), gap=int(width * 0.025))
    return im


def instructions(w, h):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, w - 1, h - 1], fill=FRAME + (245,))
    tf = gothic(36, bold=True)
    tw, th, b = text_size(d, "Instructions", tf)
    d.text(((w - tw) / 2 - b[0], 26 - b[1]), "Instructions", font=tf, fill=WHITE)
    d.line([((w - tw) / 2, 26 + th + 8), ((w + tw) / 2, 26 + th + 8)], fill=WHITE, width=3)
    steps = [
        "Start on any station square. Youngest player goes first. Direction of play is clockwise.",
        "Use the spinner to decide how many squares to move.",
        "Follow the instructions on the square you land on.",
        "Pick up a karma card whenever you land on or pass a station. Keep the karma card.",
        "The game ends when all karma cards are used.",
        "Add up your karma card points. The player with the most points wins.",
    ]
    bf = gothic(27)
    y = 26 + th + 34
    for n, step in enumerate(steps, 1):
        lines = wrap(d, step, bf, w - 110)
        d.text((40, y), f"{n}.", font=bf, fill=WHITE)
        for line in lines:
            d.text((82, y), line, font=bf, fill=WHITE)
            y += 34
        y += 14
    return im


# -- Assemble --------------------------------------------------------------------------

def build():
    board = Image.new("RGBA", (W, H), FRAME + (255,))
    for i in range(22):
        (x0, y0, x1, y1), rot = cell_rect(i)
        x0, y0, x1, y1 = round(x0 + GAP), round(y0 + GAP), round(x1 - GAP), round(y1 - GAP)
        cw, ch = x1 - x0, y1 - y0
        L, D = (ch, cw) if rot in (90, -90) else (cw, ch)   # length along the edge, depth inward
        art = square_content(i, L, D)
        if rot:
            art = art.rotate(rot, resample=Image.BICUBIC, expand=True)
        board.alpha_composite(art.resize((cw, ch)), (x0, y0))

    cx0, cy0, cx1, cy1 = round(XS[1] + GAP), round(YS[1] + GAP), round(XS[5] - GAP), round(YS[6] - GAP)
    board.alpha_composite(centre_panel(cx1 - cx0, cy1 - cy0), (cx0, cy0))
    return board.convert("RGB")


if __name__ == "__main__":
    out = sys.argv[1]
    img = build()
    img.save(out, quality=92, subsampling=0)
    if len(sys.argv) > 2:
        img.resize((W // 3, H // 3), Image.LANCZOS).save(sys.argv[2])
    print("wrote", out, img.size)
