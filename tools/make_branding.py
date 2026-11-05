"""Generate Subway Karma app icons and splash logo from the board's title-card style.

Usage (from the repo root, Windows; needs Pillow and the Century Gothic font):
    python -I tools/make_branding.py SubwayKarmaGame/Assets/Art/Branding

Then in Unity: Tools > Subway Karma > Apply App Icons and Splash Screen.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

GREEN = (0x8e, 0xb6, 0x10)
DARK = (0x33, 0x33, 0x33)
FONT = "C:/Windows/Fonts/GOTHIC.TTF"
FONT_B = "C:/Windows/Fonts/GOTHICB.TTF"
TILT = 32  # degrees counter-clockwise, like the board's title card

out = sys.argv[1]
os.makedirs(out, exist_ok=True)


def card(width, subtitle=True, game=False):
    """Green title card with dark outline: 'The Subway' / 'KARMA' / 'Game'."""
    h = int(width * (0.62 if game else 0.52))
    im = Image.new("RGBA", (width, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    border = max(4, width // 90)
    d.rectangle([0, 0, width - 1, h - 1], fill=GREEN + (255,), outline=DARK + (255,), width=border)

    def centred(text, font, y):
        bbox = d.textbbox((0, 0), text, font=font)
        d.text(((width - (bbox[2] - bbox[0])) / 2 - bbox[0], y - bbox[1]), text, font=font, fill=DARK)
        return bbox[3] - bbox[1]

    karma = ImageFont.truetype(FONT_B, int(width * 0.25))
    small = ImageFont.truetype(FONT, int(width * 0.105))
    kb = d.textbbox((0, 0), "KARMA", font=karma)
    kh = kb[3] - kb[1]
    sh = d.textbbox((0, 0), "The Subway", font=small)
    shh = sh[3] - sh[1]
    gap = int(width * 0.035)
    total = kh + (shh + gap if subtitle else 0) + (shh + gap if game else 0)
    y = (h - total) / 2
    if subtitle:
        centred("The Subway", small, y); y += shh + gap
    centred("KARMA", karma, y); y += kh + gap
    if game:
        centred("Game", small, y)
    return im


def place(canvas, art, max_extent, centre):
    """Scale `art` so its larger side is max_extent, paste centred."""
    s = max_extent / max(art.size)
    art = art.resize((int(art.width * s), int(art.height * s)), Image.LANCZOS)
    canvas.alpha_composite(art, (int(centre[0] - art.width / 2), int(centre[1] - art.height / 2)))


N = 1024
tilted = card(1600).rotate(TILT, resample=Image.BICUBIC, expand=True)

# 1. Default / legacy icon: opaque (iOS rejects alpha), card fills the square.
icon = Image.new("RGBA", (N, N), DARK + (255,))
place(icon, tilted, int(N * 1.02), (N / 2, N / 2))
icon.convert("RGB").save(os.path.join(out, "AppIcon.png"))

# 2. Android adaptive icon: background + foreground kept inside the 66% safe circle.
Image.new("RGB", (N, N), DARK).save(os.path.join(out, "AppIcon_AdaptiveBackground.png"))
# Launchers show the middle 72/108 of this layer and mask it (circle, squircle...).
# Keep every card corner inside the 66/108 safe circle: scale the card's diagonal to it.
fg = Image.new("RGBA", (N, N), (0, 0, 0, 0))
flat = card(1600)
diag = (flat.width ** 2 + flat.height ** 2) ** 0.5
s = (N * 66 / 108) * 0.96 / diag
flat = flat.resize((int(flat.width * s), int(flat.height * s)), Image.LANCZOS)
rot = flat.rotate(TILT, resample=Image.BICUBIC, expand=True)
fg.alpha_composite(rot, ((N - rot.width) // 2, (N - rot.height) // 2))
fg.save(os.path.join(out, "AppIcon_AdaptiveForeground.png"))

# 3. Splash logo: full title card on transparent background.
splash_card = card(1600, subtitle=True, game=True).rotate(TILT * 0.6, resample=Image.BICUBIC, expand=True)
splash = Image.new("RGBA", (1600, 1000), (0, 0, 0, 0))
place(splash, splash_card, 1000, (800, 500))
splash.save(os.path.join(out, "SplashLogo.png"))

# Preview sheet (not shipped): icon at store and launcher sizes, adaptive mask, splash.
prev = Image.new("RGBA", (1700, 620), (240, 240, 240, 255))
x = 20
for size in (512, 192, 96, 48):
    prev.alpha_composite(icon.resize((size, size), Image.LANCZOS), (x, 20)); x += size + 20
masked = Image.new("RGBA", (N, N), DARK + (255,)); masked.alpha_composite(fg)
v = int(N * 72 / 108); o = (N - v) // 2
masked = masked.crop((o, o, o + v, o + v)).resize((N, N), Image.LANCZOS)   # what a launcher shows
mask = Image.new("L", (N, N), 0); ImageDraw.Draw(mask).ellipse([0, 0, N, N], fill=255)
circ = Image.new("RGBA", (N, N), (0, 0, 0, 0)); circ.paste(masked, (0, 0), mask)
prev.alpha_composite(circ.resize((192, 192), Image.LANCZOS), (x, 20))
sp = Image.new("RGBA", splash.size, (31, 31, 32, 255)); sp.alpha_composite(splash)
prev.alpha_composite(sp.resize((560, 350), Image.LANCZOS), (x, 250))
prev.convert("RGB").save(os.path.join(os.environ.get("TEMP", "."), "branding_preview.png"))  # not committed
print("ok")
