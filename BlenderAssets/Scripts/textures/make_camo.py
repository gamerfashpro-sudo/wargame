"""Процедурный бесшовный камуфляж WARGAME (собственная текстура, без сторонних лицензий).

python3 BlenderAssets/Scripts/textures/make_camo.py  -> Exports/Textures/Shared/T_Camo_Woodland_A.png
"""
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "BlenderAssets" / "Exports" / "Textures" / "Shared"


def periodic_noise(size, scale, seed):
    """Бесшовный шум: белый шум, сглаженный в частотной области (FFT периодичен)."""
    rng = np.random.default_rng(seed)
    f = np.fft.fft2(rng.standard_normal((size, size)))
    k = np.fft.fftfreq(size)[:, None] ** 2 + np.fft.fftfreq(size)[None, :] ** 2
    f *= np.exp(-k * (size / scale) ** 2)
    n = np.real(np.fft.ifft2(f))
    return (n - n.mean()) / n.std()


def camo(size=1024, seed=7):
    # sRGB цвета: олив, тёмно-зелёный, коричневый, песок
    olive = np.array([110, 115, 80])
    dark = np.array([52, 60, 38])
    brown = np.array([88, 70, 48])
    sand = np.array([150, 138, 102])
    a = periodic_noise(size, 9, seed) + 0.35 * periodic_noise(size, 30, seed + 1)
    b = periodic_noise(size, 11, seed + 2) + 0.35 * periodic_noise(size, 30, seed + 3)
    img = np.empty((size, size, 3), dtype=np.float32)
    img[:] = olive
    img[a > 0.55] = dark
    img[(b > 0.8) & (a <= 0.55)] = brown
    img[(b < -1.1)] = sand
    # мелкая грязь/потёртости
    grime = periodic_noise(size, 120, seed + 4) * 6 + periodic_noise(size, 400, seed + 5) * 4
    img += grime[..., None]
    return np.clip(img, 0, 255).astype(np.uint8)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    p = OUT / "T_Camo_Woodland_A.png"
    Image.fromarray(camo()).save(p, optimize=True)
    print("saved", p)
