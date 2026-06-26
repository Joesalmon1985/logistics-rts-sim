#!/usr/bin/env python3
"""Generate original placeholder sprites for Human-Playable Settler MVP (deterministic)."""
from __future__ import annotations

import json
import random
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "godot_project/client/assets/settler_mvp/generated"
REGISTRY = ROOT / "godot_project/client/assets/asset_registry.json"

RNG = random.Random(42)


def fill(size: int, rgb: tuple, alpha: int = 255) -> Image.Image:
    if len(rgb) == 4:
        return Image.new("RGBA", (size, size), rgb)
    return Image.new("RGBA", (size, size), (*rgb, alpha))


def draw_building(name: str, size: int, body: tuple, roof: tuple, accent: tuple) -> Image.Image:
    img = fill(size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = size // 8
    d.rectangle([m, size // 3, size - m, size - m], fill=(*body, 255))
    d.polygon(
        [(m, size // 3), (size // 2, m), (size - m, size // 3)],
        fill=(*roof, 255),
    )
    d.rectangle([size // 3, size // 2, size // 3 + size // 5, size - m], fill=(*accent, 255))
    d.ellipse([size - m * 3, m, size - m, m * 3], fill=(*accent, 200))
    return img


def draw_terrain(kind: str, size: int) -> Image.Image:
    palettes = {
        "grass": ((72, 140, 60), (58, 120, 48)),
        "forest": ((30, 90, 40), (18, 55, 28)),
        "rock": ((110, 105, 100), (80, 78, 74)),
        "water": ((50, 110, 180), (35, 85, 150)),
    }
    a, b = palettes[kind]
    img = fill(size, a)
    d = ImageDraw.Draw(img)
    for _ in range(12):
        x, y = RNG.randint(0, size - 8), RNG.randint(0, size - 8)
        d.ellipse([x, y, x + 8, y + 8], fill=(*b, 180))
    return img


def draw_ware(name: str, size: int, color: tuple) -> Image.Image:
    img = fill(size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([8, 12, size - 8, size - 8], radius=6, fill=(*color, 255))
    d.line([(12, 16), (size - 12, size - 12)], fill=(255, 255, 255, 120), width=2)
    return img


def draw_logistics(kind: str, size: int) -> Image.Image:
    img = fill(size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if kind == "road":
        d.rectangle([4, size // 2 - 6, size - 4, size // 2 + 6], fill=(90, 75, 55, 255))
        d.line([(8, size // 2), (size - 8, size // 2)], fill=(120, 100, 70, 255), width=2)
    elif kind == "road_node":
        d.ellipse([4, 4, size - 4, size - 4], fill=(200, 170, 60, 255))
        d.ellipse([10, 10, size - 10, size - 10], fill=(240, 210, 90, 255))
    elif kind == "carrier":
        d.ellipse([6, 10, size - 6, size - 6], fill=(230, 140, 50, 255))
        d.rectangle([size // 3, 4, 2 * size // 3, 14], fill=(180, 100, 40, 255))
    else:  # builder
        d.rectangle([size // 3, size // 4, 2 * size // 3, size - 8], fill=(70, 130, 200, 255))
        d.rectangle([size // 4, size // 3, size // 3, size // 2], fill=(200, 160, 90, 255))
    return img


def draw_ui(kind: str, size: int) -> Image.Image:
    img = fill(size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if kind == "selection_highlight":
        d.ellipse([2, 2, size - 2, size - 2], outline=(255, 240, 80, 255), width=4)
    elif kind == "placement_valid":
        d.rounded_rectangle([4, 4, size - 4, size - 4], radius=8, outline=(60, 220, 90, 220), width=4)
    else:
        d.rounded_rectangle([4, 4, size - 4, size - 4], radius=8, outline=(220, 60, 60, 220), width=4)
    return img


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    buildings = {
        "headquarters": ((180, 150, 50), (140, 90, 40), (240, 220, 120)),
        "woodcutter": ((120, 75, 45), (90, 55, 30), (200, 180, 100)),
        "forester": ((50, 120, 70), (35, 90, 55), (180, 230, 140)),
        "sawmill": ((150, 110, 70), (110, 80, 50), (220, 200, 160)),
        "quarry": ((130, 130, 135), (95, 95, 100), (200, 200, 210)),
        "construction_site": ((80, 80, 85), (60, 60, 65), (255, 200, 60)),
    }
    registry: dict = {"buildings": {}, "logistics": {}, "wares": {}, "terrain": {}, "ui": {}, "icons": {}}

    for name, colors in buildings.items():
        path = OUT / f"building_{name}.png"
        draw_building(name, 96, *colors).save(path)
        key = "construction_site" if name == "construction_site" else name
        registry["buildings"][key] = f"res://client/assets/settler_mvp/generated/building_{name}.png"

    for kind in ("road", "road_node", "carrier", "builder"):
        sz = 48 if kind in ("road_node", "carrier", "builder") else 64
        path = OUT / f"logistics_{kind}.png"
        draw_logistics(kind, sz).save(path)
        registry["logistics"][kind] = f"res://client/assets/settler_mvp/generated/logistics_{kind}.png"

    for ware, color in (
        ("timber", (140, 90, 45)),
        ("planks", (190, 150, 90)),
        ("stone", (150, 150, 155)),
        ("builder_work", (100, 160, 220)),
    ):
        path = OUT / f"ware_{ware}.png"
        draw_ware(ware, 64, color).save(path)
        registry["wares"][ware] = f"res://client/assets/settler_mvp/generated/ware_{ware}.png"

    for terrain in ("grass", "forest", "rock", "water"):
        path = OUT / f"terrain_{terrain}.png"
        draw_terrain(terrain, 64).save(path)
        registry["terrain"][terrain] = f"res://client/assets/settler_mvp/generated/terrain_{terrain}.png"

    for ui in ("selection_highlight", "placement_valid", "placement_invalid"):
        path = OUT / f"ui_{ui}.png"
        draw_ui(ui, 64).save(path)
        registry["ui"][ui] = f"res://client/assets/settler_mvp/generated/ui_{ui}.png"

    for btype in ("headquarters", "woodcutter", "forester", "sawmill", "quarry", "road", "cancel"):
        if btype == "cancel":
            img = fill(64, (60, 60, 65))
            d = ImageDraw.Draw(img)
            d.line([(16, 16), (48, 48)], fill=(220, 80, 80, 255), width=5)
            d.line([(48, 16), (16, 48)], fill=(220, 80, 80, 255), width=5)
            path = OUT / "icon_cancel.png"
            img.save(path)
            registry["icons"]["cancel"] = "res://client/assets/settler_mvp/generated/icon_cancel.png"
        elif btype == "road":
            img = draw_logistics("road", 64)
            path = OUT / "icon_road.png"
            img.save(path)
            registry["icons"]["road"] = "res://client/assets/settler_mvp/generated/icon_road.png"
        else:
            body = buildings.get(btype, ((100, 100, 100), (80, 80, 80), (200, 200, 200)))
            img = draw_building(btype, 64, *body)
            path = OUT / f"icon_{btype}.png"
            img.save(path)
            registry["icons"][btype] = f"res://client/assets/settler_mvp/generated/icon_{btype}.png"

    REGISTRY.parent.mkdir(parents=True, exist_ok=True)
    REGISTRY.write_text(json.dumps(registry, indent=2) + "\n", encoding="utf-8")
    print(f"Generated sprites in {OUT}")
    print(f"Wrote registry {REGISTRY}")


if __name__ == "__main__":
    main()
