#!/usr/bin/env python3
"""
Generates ThreadModeler's icons (neutral, flat design) into ThreadModeler/resources/.

    python3 tools/make_icons.py            # requires Pillow (pip install pillow)
    python3 tools/make_icons.py --preview  # also writes tools/icons-preview.png

Every size is drawn separately (8x supersampled) so 16 px icons stay crisp; strokes have a
minimum width in final pixels. ICO frames are 32-bit BMP (what System.Drawing and Inventor expect).
"""
import math
import os
import struct
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "ThreadModeler", "resources")

BLUE = (31, 111, 209, 255)
BLUE_DARK = (20, 78, 150, 255)
GREEN = (46, 139, 87, 255)
GREEN_DARK = (30, 100, 62, 255)
STEEL = (154, 165, 177, 255)
STEEL_LIGHT = (196, 204, 212, 255)
STEEL_DARK = (74, 85, 97, 255)
WHITE = (255, 255, 255, 255)

SS = 8  # supersampling factor


def canvas(size):
    n = size * SS
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img), n


def finish(img, size):
    return img.resize((size, size), Image.LANCZOS)


def stroke(size, frac, min_px):
    """Stroke width in supersampled pixels: fraction of the icon, at least min_px final pixels."""
    return max(frac * size, min_px) * SS


def bolt(size):
    img, d, n = canvas(size)
    u = lambda v: v * n
    small = size <= 20
    w_out = stroke(size, 0.035, 1.0)

    # hex head (front view: three faces)
    hx0, hx1, hy0, hy1 = u(0.10), u(0.90), u(0.03), u(0.31)
    d.rounded_rectangle([hx0, hy0, hx1, hy1], radius=u(0.05), fill=STEEL, outline=STEEL_DARK, width=int(w_out))
    fx0, fx1 = u(0.37), u(0.63)
    d.rectangle([fx0, hy0 + w_out, fx1, hy1 - w_out], fill=STEEL_LIGHT)
    if not small:
        d.line([fx0, hy0, fx0, hy1], fill=STEEL_DARK, width=int(w_out * 0.8))
        d.line([fx1, hy0, fx1, hy1], fill=STEEL_DARK, width=int(w_out * 0.8))

    # shank
    sx0, sx1, sy0, sy1 = u(0.27), u(0.73), u(0.31), u(0.97)
    d.rectangle([sx0, sy0, sx1, sy1], fill=STEEL_LIGHT, outline=STEEL_DARK, width=int(w_out))

    # thread ridges: bold blue diagonals protruding on both sides
    pitch = 0.20 if small else 0.14
    w_thr = stroke(size, 0.085, 2.0)
    y = 0.38
    while y + 0.08 <= 0.97:
        d.line([u(0.19), u(y), u(0.81), u(y + 0.10)], fill=BLUE, width=int(w_thr))
        y += pitch
    # keep the outline of the shank visible between the ridges
    d.line([sx0, sy0, sx0, sy1], fill=STEEL_DARK, width=int(w_out))
    d.line([sx1, sy0, sx1, sy1], fill=STEEL_DARK, width=int(w_out))
    return finish(img, size)


def circle_badge(size, fill, edge):
    img, d, n = canvas(size)
    m = n * 0.03
    d.ellipse([m, m, n - m, n - m], fill=fill, outline=edge, width=int(stroke(size, 0.03, 0.75)))
    return img, d, n


def info(size):
    img, d, n = circle_badge(size, BLUE, BLUE_DARK)
    u = lambda v: v * n
    r = u(0.075)
    d.ellipse([u(0.5) - r, u(0.27) - r, u(0.5) + r, u(0.27) + r], fill=WHITE)
    bw = max(0.13 * size, 2.0) * SS / 2
    d.rounded_rectangle([u(0.5) - bw, u(0.42), u(0.5) + bw, u(0.78)], radius=bw * 0.5, fill=WHITE)
    return finish(img, size)


def _font(px):
    for name in ("DejaVuSans-Bold.ttf", "Arial Bold.ttf", "arialbd.ttf", "LiberationSans-Bold.ttf"):
        try:
            return ImageFont.truetype(name, int(px))
        except OSError:
            pass
    return ImageFont.load_default()


def question(size):
    img, d, n = circle_badge(size, BLUE, BLUE_DARK)
    font = _font(n * 0.70)
    l, t, r, b = d.textbbox((0, 0), "?", font=font)
    d.text(((n - (r - l)) / 2 - l, (n - (b - t)) / 2 - t), "?", font=font, fill=WHITE)
    return finish(img, size)


def update(size):
    img, d, n = circle_badge(size, GREEN, GREEN_DARK)
    u = lambda v: v * n
    w = stroke(size, 0.10, 1.7)
    box = [u(0.25), u(0.25), u(0.75), u(0.75)]
    d.arc(box, start=-50, end=230, fill=WHITE, width=int(w))
    # arrow head at the end of the arc (angle -50 deg, pointing clockwise)
    cx, cy, rr = u(0.5), u(0.5), u(0.25) - w / 2
    a = math.radians(-50)
    tip_x, tip_y = cx + rr * math.cos(a), cy + rr * math.sin(a)
    h = max(0.20 * size, 4.0) * SS
    # tangent direction (clockwise) at angle a is (-sin a, cos a); arrow points along it
    tx, ty = -math.sin(a), math.cos(a)
    nx, ny = math.cos(a), math.sin(a)
    p1 = (tip_x + tx * h * 0.55, tip_y + ty * h * 0.55)
    p2 = (tip_x - tx * h * 0.25 + nx * h * 0.55, tip_y - ty * h * 0.25 + ny * h * 0.55)
    p3 = (tip_x - tx * h * 0.25 - nx * h * 0.55, tip_y - ty * h * 0.25 - ny * h * 0.55)
    d.polygon([p1, p2, p3], fill=WHITE)
    return finish(img, size)


def gear_points(cx, cy, r_out, r_in, teeth):
    """Outline of a gear: each tooth rises at 15 %, is flat to 45 % and falls at 60 % of its pitch angle."""
    pts = []
    step = 2 * math.pi / teeth
    for i in range(teeth):
        for frac, r in ((0.0, r_in), (0.15, r_out), (0.45, r_out), (0.60, r_in)):
            a = (i + frac) * step - math.pi / 2
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def settings(size):
    img, d, n = circle_badge(size, BLUE, BLUE_DARK)
    teeth = 8 if size >= 24 else 6
    d.polygon(gear_points(n / 2, n / 2, 0.37 * n, 0.27 * n, teeth), fill=WHITE)
    r = max(0.11 * size, 1.5) * SS
    d.ellipse([n / 2 - r, n / 2 - r, n / 2 + r, n / 2 + r], fill=BLUE)
    return finish(img, size)


def write_ico(path, frames):
    """
    Writes a standard ICO: one 32-bit BGRA DIB per frame (bottom-up) followed by its 1-bit AND mask
    (set where fully transparent). Pillow's ICO writer omits the AND mask, which strict loaders reject.
    """
    blobs = []
    for im in frames:
        w, h = im.size
        px = im.load()
        header = struct.pack("<IiiHHIIiiII", 40, w, h * 2, 1, 32, 0, 0, 0, 0, 0, 0)
        xor = bytearray()
        for y in range(h - 1, -1, -1):
            for x in range(w):
                r, g, b, a = px[x, y]
                xor += bytes((b, g, r, a))
        row_bytes = ((w + 31) // 32) * 4
        mask = bytearray()
        for y in range(h - 1, -1, -1):
            row = bytearray(row_bytes)
            for x in range(w):
                if px[x, y][3] == 0:
                    row[x // 8] |= 0x80 >> (x % 8)
            mask += row
        blobs.append((w, h, header + bytes(xor) + bytes(mask)))

    out = struct.pack("<HHH", 0, 1, len(blobs))
    offset = 6 + 16 * len(blobs)
    for w, h, data in blobs:
        out += struct.pack("<BBBBHHII", w % 256, h % 256, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
    for _, _, data in blobs:
        out += data
    with open(path, "wb") as f:
        f.write(out)


ICONS = {
    "threadModeler.ico": (bolt, [16, 20, 24, 32, 40, 48, 64, 256]),
    "about.ico": (info, [16, 20, 24, 32, 40, 48]),
    "Help.ico": (question, [16, 20, 24, 32, 40, 48]),
    "update.ico": (update, [16, 20, 24, 32, 40, 48]),
    "settings.ico": (settings, [16, 20, 24, 32, 40, 48]),
}


def main():
    previews = []
    for name, (draw, sizes) in ICONS.items():
        frames = [draw(s) for s in sizes]
        write_ico(os.path.join(OUT, name), frames)
        previews.append(frames)
        print("wrote", os.path.join("ThreadModeler", "resources", name), sizes)

    if "--preview" in sys.argv:
        # each row: 16, 24, 32, 48 at 1:1 and 4x (nearest) on light and dark backgrounds
        show = [16, 24, 32, 48]
        cell = 4 * 48 + 8
        sheet = Image.new("RGBA", (len(show) * cell * 2, len(previews) * (cell)), (255, 255, 255, 255))
        for row, (name, (draw, sizes)) in enumerate(ICONS.items()):
            for col, s in enumerate(show):
                im = draw(s)
                for k, bg in enumerate([(245, 245, 245, 255), (45, 48, 52, 255)]):
                    x = (col * 2 + k) * cell
                    y = row * cell
                    tile = Image.new("RGBA", (cell, cell), bg)
                    tile.alpha_composite(im.resize((s * 4, s * 4), Image.NEAREST), (4, 4))
                    tile.alpha_composite(im, (cell - s - 4, cell - s - 4))
                    sheet.alpha_composite(tile, (x, y))
        path = os.path.join(ROOT, "tools", "icons-preview.png")
        sheet.convert("RGB").save(path)
        print("wrote", path)


if __name__ == "__main__":
    main()
