#!/usr/bin/env python3
"""Generate original placeholder sprites for Military Spectator (deterministic)."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "godot_project/client/assets/military/generated"
REGISTRY = ROOT / "godot_project/client/assets/military/asset_registry.json"


def fill(size: int, rgb: tuple, alpha: int = 255) -> Image.Image:
    if len(rgb) == 4:
        return Image.new("RGBA", (size, size), rgb)
    return Image.new("RGBA", (size, size), (*rgb, alpha))


def draw_building(name: str, size: int, body: tuple, roof: tuple, accent: tuple) -> Image.Image:
    img = fill(size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = size // 8
    d.rectangle([m, size // 3, size - m, size - m], fill=(*body, 255))
    d.polygon([(m, size // 3), (size // 2, m), (size - m, size // 3)], fill=(*roof, 255))
    if name == "command_hub":
        d.rectangle([size // 2 - 6, m, size // 2 + 6, size // 3 + 4], fill=(220, 180, 40, 255))
        d.rectangle([m + 4, size - m - 12, size - m - 4, size - m - 4], fill=(*accent, 255))
    elif name == "power_generator":
        d.ellipse([size // 2 - 10, size // 2 - 8, size // 2 + 10, size // 2 + 8], fill=(*accent, 255))
        d.line([(size // 2 - 14, size // 2), (size // 2 + 14, size // 2)], fill=(240, 240, 250, 255), width=3)
    elif name == "barracks":
        d.rectangle([size // 3, size // 2, size // 3 + size // 5, size - m], fill=(*accent, 255))
        d.polygon([(size - m - 16, size // 2), (size - m - 4, size // 2 - 8), (size - m - 4, size // 2 + 8)], fill=(200, 60, 60, 255))
    elif name == "fabricator":
        d.rectangle([m + 6, size // 2 + 4, size - m - 6, size - m - 2], fill=(*accent, 255))
        d.rectangle([size // 3, size // 2 - 10, 2 * size // 3, size // 2 + 2], fill=(120, 120, 130, 255))
    elif name == "turret":
        d.rectangle([size // 2 - 8, size // 2, size // 2 + 8, size - m], fill=(*body, 255))
        d.rectangle([size // 2 - 14, size // 2 - 6, size // 2 + 14, size // 2 + 2], fill=(*accent, 255))
    else:
        d.ellipse([size - m * 3, m, size - m, m * 3], fill=(*accent, 200))
    return img


def draw_unit(name: str, size: int, body: tuple, accent: tuple) -> Image.Image:
    img = fill(size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if name == "rifle_squad":
        d.ellipse([size // 2 - 10, size // 2 - 6, size // 2 + 10, size // 2 + 10], fill=(*body, 255))
        d.line([(size // 2 + 8, size // 2), (size - 8, size // 2 - 6)], fill=(*accent, 255), width=4)
    elif name == "harvester":
        d.rounded_rectangle([10, size // 2 - 6, size - 10, size - 10], radius=6, fill=(*body, 255))
        d.rectangle([12, size // 2 - 14, size - 12, size // 2 - 4], fill=(*accent, 255))
        d.ellipse([size - 18, size - 16, size - 8, size - 6], fill=(40, 40, 45, 255))
    else:
        d.ellipse([12, 16, size - 12, size - 12], fill=(*body, 255))
    return img


def draw_effect(name: str, size: int) -> Image.Image:
    img = fill(size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if name == "attack_marker":
        d.line([(size // 2, 8), (size // 2, size - 8)], fill=(255, 80, 60, 230), width=4)
        d.line([(8, size // 2), (size - 8, size // 2)], fill=(255, 80, 60, 230), width=4)
        d.ellipse([8, 8, size - 8, size - 8], outline=(255, 120, 80, 200), width=3)
    elif name == "projectile":
        d.ellipse([size // 2 - 4, size // 2 - 4, size // 2 + 4, size // 2 + 4], fill=(255, 220, 80, 255))
    elif name == "construction_site":
        d.line([(8, size // 2), (size - 8, size // 2)], fill=(255, 200, 60, 255), width=2)
        d.line([(size // 2, 8), (size // 2, size - 8)], fill=(255, 200, 60, 255), width=2)
    elif name == "damage_flash":
        d.ellipse([6, 6, size - 6, size - 6], outline=(255, 60, 60, 220), width=4)
    else:
        d.rounded_rectangle([6, 6, size - 6, size - 6], radius=8, outline=(180, 180, 200, 200), width=3)
    return img


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    buildings = {
        "command_hub": ((70, 110, 170), (45, 75, 120), (240, 210, 90)),
        "power_generator": ((180, 140, 50), (140, 100, 35), (255, 220, 80)),
        "barracks": ((130, 85, 55), (95, 60, 40), (200, 170, 120)),
        "fabricator": ((90, 95, 105), (65, 70, 80), (180, 190, 210)),
        "turret": ((110, 110, 115), (80, 80, 85), (220, 70, 60)),
        "construction_site": ((80, 80, 85), (60, 60, 65), (255, 200, 60)),
    }
    units = {
        "rifle_squad": ((70, 130, 210), (40, 70, 120)),
        "harvester": ((200, 130, 50), (255, 210, 90)),
    }
    effects = {
        "attack_marker": (64,),
        "projectile": (32,),
        "damage_flash": (48,),
        "harvest_route": (48,),
    }
    registry: dict = {"buildings": {}, "units": {}, "effects": {}, "icons": {}}

    for name, colors in buildings.items():
        path = OUT / f"building_{name}.png"
        draw_building(name, 96, *colors).save(path)
        registry["buildings"][name] = f"res://client/assets/military/generated/building_{name}.png"

    for name, colors in units.items():
        path = OUT / f"unit_{name}.png"
        draw_unit(name, 64, *colors).save(path)
        registry["units"][name] = f"res://client/assets/military/generated/unit_{name}.png"

    for name, size_tuple in effects.items():
        sz = size_tuple[0]
        path = OUT / f"effect_{name}.png"
        draw_effect(name, sz).save(path)
        registry["effects"][name] = f"res://client/assets/military/generated/effect_{name}.png"

    registry["effects"]["construction_site"] = registry["buildings"]["construction_site"]

    for key in ("command_hub", "power_generator", "barracks", "fabricator", "turret", "rifle_squad", "harvester"):
        if key in buildings:
            img = draw_building(key, 64, *buildings[key])
            out = OUT / f"icon_{key}.png"
            img.save(out)
            registry["icons"][key] = f"res://client/assets/military/generated/icon_{key}.png"
        elif key in units:
            img = draw_unit(key, 64, *units[key])
            out = OUT / f"icon_{key}.png"
            img.save(out)
            registry["icons"][key] = f"res://client/assets/military/generated/icon_{key}.png"

    REGISTRY.parent.mkdir(parents=True, exist_ok=True)
    REGISTRY.write_text(json.dumps(registry, indent=2) + "\n", encoding="utf-8")
    print(f"Generated sprites in {OUT}")
    print(f"Wrote registry {REGISTRY}")


if __name__ == "__main__":
    main()
