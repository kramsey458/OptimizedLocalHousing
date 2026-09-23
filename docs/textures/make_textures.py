"""Makes the Optimized Local Housing site's surfaces: the planner's linen board (light and lamplit dark) and the card
stock the plan and the place cards are cut from. Procedural (numpy and Pillow, fixed seeds); no source images and no
generative model. Every texture tiles. Run from this folder: python make_textures.py"""
import numpy as np
from PIL import Image

N = 384

def wrap_noise(rng, n, cell):
    """Smooth noise that wraps: a coarse random grid upsampled with wrap-around interpolation."""
    g = rng.normal(0, 1, (n // cell, n // cell))
    g = (g - g.min()) / (g.max() - g.min())
    big = np.tile(g, (3, 3))
    up = np.asarray(Image.fromarray((big * 255).astype(np.uint8)).resize((n * 3, n * 3), Image.BICUBIC), float) / 255
    return up[n:2 * n, n:2 * n] - .5

def linen(rng, n):
    """A plain weave: threads across and down, each with its own slight thickness, plus slubs along some threads."""
    y, x = np.mgrid[0:n, 0:n].astype(float)
    period = 4.0  # one thread every 4 px; 384 is a whole number of threads, so it tiles
    warp_w = rng.uniform(.7, 1.3, int(n / period))
    weft_w = rng.uniform(.7, 1.3, int(n / period))
    wi = (x // period).astype(int) % len(warp_w); fi = (y // period).astype(int) % len(weft_w)
    warp = np.cos(np.pi * ((x % period) / period - .5)) ** 2 * warp_w[wi]
    weft = np.cos(np.pi * ((y % period) / period - .5)) ** 2 * weft_w[fi]
    over = ((x // period + y // period) % 2).astype(bool)          # which thread is on top at each crossing
    v = np.where(over, warp, weft) - .5
    slub = np.clip(wrap_noise(rng, n, 24) * 4, 0, 1)
    return v, slub

def save(name, rgb):
    Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)).save(name, quality=90, method=6)
    print("made", name, rgb.mean(axis=(0, 1)).round(1))

# the board, light: pale grey-beige linen
rng = np.random.default_rng(3101)
v, slub = linen(rng, N)
mottle = wrap_noise(rng, N, 96)
base = np.array([230, 225, 214], float)
rgb = base + (v * 14 + slub * 6 + mottle * 10)[..., None] * np.array([1, 1, .96]) + rng.normal(0, 1.6, (N, N, 1))
save("board-light.webp", rgb)

# the board, dark: the same weave by lamplight
rng = np.random.default_rng(3102)
v, slub = linen(rng, N)
mottle = wrap_noise(rng, N, 96)
base = np.array([27, 29, 33], float)
rgb = base + (v * 7 + slub * 3 + mottle * 5)[..., None] * np.array([1, 1, 1.06]) + rng.normal(0, 1.2, (N, N, 1))
save("board-dark.webp", rgb)

# card stock: faint fibres and a little tooth, cream by day
rng = np.random.default_rng(3103)
fib = np.zeros((N, N))
for _ in range(900):
    cx, cy, ang, ln = rng.uniform(0, N), rng.uniform(0, N), rng.uniform(0, np.pi), rng.uniform(6, 22)
    t = np.linspace(-ln / 2, ln / 2, int(ln * 2))
    xs = ((cx + t * np.cos(ang)) % N).astype(int); ys = ((cy + t * np.sin(ang)) % N).astype(int)
    fib[ys, xs] += rng.uniform(-1, 1)
tooth = wrap_noise(rng, N, 8) + .5 * wrap_noise(rng, N, 32)
base = np.array([251, 248, 241], float)
rgb = base + (fib * 5 + tooth * 7)[..., None] * np.array([1, 1, .95]) + rng.normal(0, 1.2, (N, N, 1))
save("card-light.webp", rgb)

rng = np.random.default_rng(3104)
tooth = wrap_noise(rng, N, 8) + .5 * wrap_noise(rng, N, 32)
base = np.array([38, 41, 46], float)
rgb = base + (tooth * 5)[..., None] + rng.normal(0, 1, (N, N, 1))
save("card-dark.webp", rgb)
