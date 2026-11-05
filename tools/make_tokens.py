"""Generate the player tokens: top-down illustrated subway cars, one per player colour.

Output (SubwayKarmaGame/Assets/Resources/Tokens/):
    car_0.png .. car_3.png   car pointing right (+X), 512 x 256 (power of two, so Unity
                             imports it without resizing); the car spans CAR_PX of the width
    car_shadow.png           soft shadow of the same silhouette

PlayerToken.cs loads these and rotates them to face along the track.

Usage (from the repo root; needs Pillow):
    python -I tools/make_tokens.py [preview.png]
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFilter

OUT = "SubwayKarmaGame/Assets/Resources/Tokens"
W, H = 512, 256                 # texture size
CAR_PX = 480                    # car length in pixels (PlayerToken.CarPixels)
CAR_W = 168                     # car width in pixels
S = 4                           # supersampling factor

# Player colours (slot 0..3), matching the score panel order.
SLOTS = [
    (0x0b, 0x6c, 0xc4),          # blue
    (0xc8, 0x26, 0x3c),          # red
    (0x2a, 0xa6, 0x4a),          # green
    (0xe0, 0xb0, 0x1a),          # gold
]

OUTLINE = (0x1c, 0x1f, 0x24)
GLASS = (0x1e, 0x2b, 0x38)
STEEL = (0xd9, 0xdd, 0xe2)
STEEL_DARK = (0x9a, 0xa1, 0xaa)
HEADLIGHT = (0xff, 0xf1, 0xb8)
TAILLIGHT = (0xff, 0x3b, 0x30)


def shade(c, k):
    """k < 1 darkens, k > 1 lightens toward white."""
    if k <= 1:
        return tuple(int(v * k) for v in c)
    t = k - 1
    return tuple(int(v + (255 - v) * t) for v in c)


def box(x0, y0, x1, y1):
    """Canvas-space box in supersampled pixels, car centred on the canvas."""
    ox, oy = (W - CAR_PX) / 2, (H - CAR_W) / 2
    return [int((ox + x0) * S), int((oy + y0) * S), int((ox + x1) * S), int((oy + y1) * S)]


def silhouette():
    m = Image.new("L", (W * S, H * S), 0)
    ImageDraw.Draw(m).rounded_rectangle(box(0, 0, CAR_PX, CAR_W), radius=30 * S, fill=255)
    return m


def car(colour):
    im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    L, Wd = CAR_PX, CAR_W

    # Body: outline, then the car's sides (darker), seen as bands along both long edges.
    d.rounded_rectangle(box(0, 0, L, Wd), radius=30 * S, fill=OUTLINE)
    d.rounded_rectangle(box(4, 4, L - 4, Wd - 4), radius=27 * S, fill=shade(colour, 0.72))

    # Side windows along both edges.
    for y0, y1 in ((8, 16), (Wd - 16, Wd - 8)):
        for x in range(46, L - 70, 38):
            d.rounded_rectangle(box(x, y0, x + 26, y1), radius=2 * S, fill=GLASS)

    # Roof: curved, so lighter along the centre line (drawn as horizontal bands).
    rx0, ry0, rx1, ry1 = 14, 22, L - 18, Wd - 22
    roof = Image.new("RGBA", im.size, (0, 0, 0, 0))
    rd = ImageDraw.Draw(roof)
    bands = 40
    for k in range(bands):
        y0 = ry0 + (ry1 - ry0) * k / bands
        y1 = ry0 + (ry1 - ry0) * (k + 1) / bands
        centre = 1 - abs((k + 0.5) / bands - 0.5) * 2          # 0 at edges, 1 in the middle
        rd.rectangle(box(rx0, y0, rx1, y1 + 0.5), fill=shade(colour, 0.92 + 0.38 * centre ** 1.5))
    mask = Image.new("L", im.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(box(rx0, ry0, rx1, ry1), radius=20 * S, fill=255)
    im.paste(roof, (0, 0), mask)
    d.rounded_rectangle(box(rx0, ry0, rx1, ry1), radius=20 * S, outline=shade(colour, 0.6), width=2 * S)

    # Roof equipment: three air-conditioning units with grilles.
    cy = Wd / 2
    for x in (70, 200, 330):
        d.rounded_rectangle(box(x, cy - 30, x + 78, cy + 30), radius=8 * S, fill=STEEL,
                            outline=STEEL_DARK, width=2 * S)
        for gx in range(x + 12, x + 70, 11):
            d.line([tuple(box(gx, cy - 20, gx, cy + 20)[:2]), tuple(box(gx, cy + 20, gx, cy + 20)[:2])],
                   fill=STEEL_DARK, width=2 * S)

    # Front (right): windscreen with a highlight, headlights; rear (left): tail lights.
    d.rounded_rectangle(box(L - 52, 26, L - 18, Wd - 26), radius=10 * S, fill=GLASS)
    d.line([tuple(box(L - 44, 36, 0, 0)[:2]), tuple(box(L - 30, Wd - 60, 0, 0)[:2])],
           fill=(0x6f, 0x8b, 0xa3), width=3 * S)
    for y in (44, Wd - 44):                     # on the flat front face, clear of the rounded corners
        d.ellipse(box(L - 15, y - 9, L - 3, y + 9), fill=HEADLIGHT, outline=OUTLINE, width=2 * S)
    for y in (44, Wd - 44):
        d.rounded_rectangle(box(3, y - 10, 12, y + 10), radius=3 * S, fill=TAILLIGHT, outline=OUTLINE, width=2 * S)

    # Crisp outline on top so the car reads clearly on the green board.
    d.rounded_rectangle(box(0, 0, L, Wd), radius=30 * S, outline=OUTLINE, width=5 * S)
    return im.resize((W, H), Image.LANCZOS)


def shadow():
    a = silhouette().filter(ImageFilter.GaussianBlur(10 * S)).resize((W, H), Image.LANCZOS)
    sh = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sh.putalpha(a.point(lambda v: int(v * 0.5)))
    return sh


def main():
    os.makedirs(OUT, exist_ok=True)
    cars = []
    for i, colour in enumerate(SLOTS):
        img = car(colour)
        img.save(os.path.join(OUT, f"car_{i}.png"), optimize=True)
        cars.append(img)
    sh = shadow()
    sh.save(os.path.join(OUT, "car_shadow.png"), optimize=True)
    print(f"wrote {len(cars)} cars + shadow to {OUT}")

    if len(sys.argv) > 1:                       # preview on a board-green background
        prev = Image.new("RGBA", (W * 2 + 60, H * 2 + 60), (0x8e, 0xb6, 0x10, 255))
        for i, img in enumerate(cars):
            x, y = 20 + (i % 2) * (W + 20), 20 + (i // 2) * (H + 20)
            prev.alpha_composite(sh, (x + 10, y + 14))
            prev.alpha_composite(img, (x, y))
        small = Image.new("RGBA", (300, 120), (0x8e, 0xb6, 0x10, 255))   # roughly in-game size
        for i, img in enumerate(cars[:2]):
            c = img.resize((100, 50), Image.LANCZOS)
            s2 = sh.resize((100, 50), Image.LANCZOS)
            small.alpha_composite(s2, (40 + 3, 8 + i * 52 + 4))
            small.alpha_composite(c, (40, 8 + i * 52))
        prev.alpha_composite(small, (prev.width - 310, prev.height - 130))
        prev.convert("RGB").save(sys.argv[1])


if __name__ == "__main__":
    main()
