#!/usr/bin/env python3
"""Regenerate the 60 Eidolon: Seam Wars SVG map/UI assets.

Drop this file anywhere in the project and run:
    python regenerate_assets.py --root /path/to/project

The original filenames and directory layout are preserved. All artwork is
procedural SVG; no external packages are required. Existing files with the
same names are overwritten.
"""
from __future__ import annotations

import argparse
import math
from pathlib import Path
from xml.sax.saxutils import escape

# ---------------------------------------------------------------------------
# SVG primitives
# ---------------------------------------------------------------------------

def doc(w: int, h: int, body: str, defs: str = "") -> str:
    return f'''<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"
 width="{w}" height="{h}" viewBox="0 0 {w} {h}" role="img">
<defs>
 <linearGradient id="metal" x2="0" y2="1"><stop stop-color="#f3e6c2"/><stop offset=".22" stop-color="#9d927c"/><stop offset=".5" stop-color="#454b50"/><stop offset=".78" stop-color="#20272e"/><stop offset="1" stop-color="#090e14"/></linearGradient>
 <linearGradient id="gold" x2=".15" y2="1"><stop stop-color="#fff0a5"/><stop offset=".35" stop-color="#d6a846"/><stop offset=".72" stop-color="#81501e"/><stop offset="1" stop-color="#392516"/></linearGradient>
 <linearGradient id="stone" x2=".15" y2="1"><stop stop-color="#c4c0b2"/><stop offset=".45" stop-color="#777b78"/><stop offset="1" stop-color="#343b3e"/></linearGradient>
 <linearGradient id="wood" x2="0" y2="1"><stop stop-color="#bd8a50"/><stop offset=".45" stop-color="#76502f"/><stop offset="1" stop-color="#34251d"/></linearGradient>
 <radialGradient id="orb"><stop stop-color="#fff"/><stop offset=".18" stop-color="#d8f6ff"/><stop offset=".55" stop-color="#62b8d5"/><stop offset="1" stop-color="#24516e"/></radialGradient>
 <linearGradient id="panel" x2="0" y2="1"><stop stop-color="#29343c"/><stop offset=".5" stop-color="#151e26"/><stop offset="1" stop-color="#0a1017"/></linearGradient>
 <filter id="shadow" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur in="SourceAlpha" stdDeviation="2.2"/><feOffset dy="2"/><feComponentTransfer><feFuncA type="linear" slope=".55"/></feComponentTransfer><feMerge><feMergeNode/><feMergeNode in="SourceGraphic"/></feMerge></filter>
 <filter id="glow" x="-80%" y="-80%" width="260%" height="260%"><feGaussianBlur stdDeviation="2.8" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
 <pattern id="grain" width="17" height="13" patternUnits="userSpaceOnUse"><circle cx="2" cy="3" r=".65" fill="#fff" opacity=".13"/><circle cx="12" cy="9" r=".7" fill="#080d12" opacity=".17"/><path d="M5 12l2-1" stroke="#fff" stroke-width=".5" opacity=".1"/></pattern>
 {defs}
</defs>
{body}
</svg>\n'''


def write(out: Path, name: str, w: int, h: int, body: str, defs: str = "") -> None:
    out.mkdir(parents=True, exist_ok=True)
    (out / f"{name}.svg").write_text(doc(w, h, body, defs), encoding="utf-8")


def polygon(points: str, fill: str, stroke: str = "#171d20", sw: float = 1) -> str:
    return f'<polygon points="{points}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" stroke-linejoin="round"/>'


def hex_pts(cx: float, cy: float, r: float) -> str:
    return " ".join(f"{cx+r*math.cos(math.radians(60*i-90)):.2f},{cy+r*math.sin(math.radians(60*i-90)):.2f}" for i in range(6))

# ---------------------------------------------------------------------------
# 10 terrain tiles: different landforms, texture and lighting, not just recolors
# ---------------------------------------------------------------------------
BIOMES = {
"hex_grass": ("#587b3d", "#263e28", '''
 <path d="M4 39Q18 29 31 38T60 34V62H4Z" fill="#456a35"/><path d="M5 25Q17 18 29 26T59 21" fill="none" stroke="#8cae5b" stroke-width="2" opacity=".8"/>
 <g stroke="#b4c77a" stroke-width="1.5" stroke-linecap="round"><path d="M13 43l-2-8m2 8l4-7m13 10l-1-9m1 9l5-7m12 4l-2-8m2 8l5-6"/></g>
 <g fill="#d1c18a"><circle cx="20" cy="28" r="1.2"/><circle cx="42" cy="39" r="1"/><circle cx="51" cy="24" r="1"/></g>'''),
"hex_forest": ("#294b32", "#14281d", '''
 <path d="M5 50L16 23 27 50Z" fill="#173323"/><path d="M17 53L32 14 47 53Z" fill="#1c3d28"/><path d="M34 51L47 21 60 51Z" fill="#244a2e"/>
 <path d="M32 14L37 29H27Z" fill="#55784a" opacity=".8"/><path d="M16 23L20 36H12Z" fill="#527447"/><path d="M47 21L52 34H42Z" fill="#64834a"/>
 <g stroke="#8a7449" stroke-width="2"><path d="M16 43v8m16-8v10m15-8v8"/></g><path d="M5 55Q25 48 60 56" fill="none" stroke="#6d8b4c" stroke-width="2"/>'''),
"hex_mountain": ("#626b70", "#30383e", '''
 <path d="M4 52L22 17 31 33 40 11 61 53Z" fill="#899398"/><path d="M22 17L29 31 21 28 14 35Z" fill="#e0ded2"/><path d="M40 11L46 27 39 23 33 32Z" fill="#f0eee2"/>
 <path d="M4 52L22 36 31 44 40 31 61 53Z" fill="#48545a"/><path d="M9 48l9-5m20-2l9-5m-16 13l5-3" stroke="#b6b9b1" stroke-width="1.2" opacity=".7"/>'''),
"hex_swamp": ("#59634a", "#303c2d", '''
 <path d="M4 36Q15 28 26 36T60 33V60H4Z" fill="#394d3e"/><ellipse cx="22" cy="42" rx="13" ry="5" fill="#243f3e"/><ellipse cx="45" cy="49" rx="10" ry="4" fill="#294847"/>
 <g stroke="#a0a36a" stroke-width="1.5" fill="none"><path d="M12 43q-3-12 1-18m3 18q5-9 3-16m22 19q-2-12 2-18m4 18q5-8 4-14"/></g>
 <g fill="#b6a75e"><circle cx="14" cy="25" r="2"/><circle cx="42" cy="27" r="2"/><circle cx="48" cy="34" r="1.5"/></g><path d="M8 53q10-4 18 0t20 0" fill="none" stroke="#81926a"/>'''),
"hex_snow": ("#cbd8dd", "#8499a3", '''
 <path d="M4 39L19 28 29 34 43 21 60 37V60H4Z" fill="#e8eff0"/><path d="M4 48L21 40 32 45 48 34 60 43V60H4Z" fill="#9eb2ba"/>
 <path d="M19 28l5 8-7-2-6 5m32-18l5 10-8-3-5 7" fill="none" stroke="#fff" stroke-width="2"/><g fill="#fff" opacity=".9"><circle cx="13" cy="20" r="1"/><circle cx="34" cy="18" r="1.3"/><circle cx="52" cy="26" r=".9"/></g>'''),
"hex_desert": ("#c49b55", "#77562d", '''
 <path d="M4 36Q17 24 31 35T60 31V60H4Z" fill="#d9b66d"/><path d="M4 45Q18 33 32 44T60 40" fill="none" stroke="#f0d18b" stroke-width="3"/><path d="M7 54Q22 44 36 53T60 49" fill="none" stroke="#a77a3f" stroke-width="2"/>
 <path d="M17 36q-3-9 2-14 6 6 2 13m22 5q-2-7 3-12 5 7 1 13" fill="#6d7844"/><path d="M18 36v-8m24 13v-9" stroke="#53613b" stroke-width="2"/>'''),
"hex_water": ("#245d78", "#143747", '''
 <path d="M4 23Q13 17 22 23T40 23T60 22V60H4Z" fill="#286f8a"/><path d="M4 33Q13 27 22 33T40 33T60 32M4 45Q13 39 22 45T40 45T60 44" fill="none" stroke="#8bc4cf" stroke-width="2" opacity=".85"/>
 <path d="M10 52q8-4 15 0m13-15q7-4 13 0" fill="none" stroke="#c0e0df" stroke-width="1.2"/><path d="M7 17q9-5 18 0" stroke="#b2d8dc" stroke-width="1" opacity=".7"/>'''),
"hex_road": ("#77736a", "#403c36", '''
 <path d="M-4 43Q15 28 28 34T68 20" fill="none" stroke="#3d3a35" stroke-width="18"/><path d="M-4 40Q15 25 28 31T68 17" fill="none" stroke="#b9aa8c" stroke-width="13"/><path d="M-4 40Q15 25 28 31T68 17" fill="none" stroke="#e0d3b4" stroke-width="1.5" stroke-dasharray="4 4"/>
 <path d="M7 50l5-3m24-5l5-3m8-18l4-2" stroke="#514b42" stroke-width="2"/>'''),
"hex_seam": ("#171b2a", "#080c16", '''
 <path d="M4 15L24 25 17 40 34 59 60 42 47 28 57 10" fill="none" stroke="#4b477b" stroke-width="5" opacity=".8"/><path d="M4 15L24 25 17 40 34 59 60 42" fill="none" stroke="#9a79d0" stroke-width="1.5"/>
 <g fill="#b5a1ff" filter="url(#glow)"><circle cx="24" cy="25" r="2"/><circle cx="47" cy="28" r="1.5"/><circle cx="34" cy="48" r="2"/></g><path d="M32 13l3 8-4 5 7 7-7 7" fill="none" stroke="#6c5ab2" stroke-width="1.5"/>'''),
}

def generate_biomes(out: Path) -> int:
    for name, (base, edge, detail) in BIOMES.items():
        body = f'''<polygon points="{hex_pts(32,32,30)}" fill="{base}" stroke="{edge}" stroke-width="2.2" filter="url(#shadow)"/>
<clipPath id="clip"><polygon points="{hex_pts(32,32,28.8)}"/></clipPath><g clip-path="url(#clip)">
<path d="M0 0H64V64H0Z" fill="url(#grain)" opacity=".8"/>{detail}<path d="M3 5L61 5" stroke="#fff" opacity=".12" stroke-width="2"/></g>
<polygon points="{hex_pts(32,32,29)}" fill="none" stroke="#ffffff" stroke-opacity=".2" stroke-width="1"/>'''
        write(out, name, 64, 64, body)
    return len(BIOMES)

# ---------------------------------------------------------------------------
# Object tokens: unique silhouettes and material language per object
# ---------------------------------------------------------------------------

def mine(kind: str) -> str:
    shapes = {
      "gold": '<path d="M17 40l5-19 11-5 14 12-4 17-17 3Z" fill="url(#gold)" stroke="#4b321c" stroke-width="2"/><path d="M22 21l11 8 14-1M33 29l-7 19m7-19l10 15" fill="none" stroke="#fff0a8" stroke-width="2"/><path d="M15 45l33-4" stroke="#6c4b27" stroke-width="2"/>',
      "wood": '<path d="M15 25l32-7 4 8-32 8Z" fill="url(#wood)" stroke="#34251d" stroke-width="2"/><path d="M18 36l31-7 3 8-31 8Z" fill="#95643a" stroke="#34251d" stroke-width="2"/><ellipse cx="18" cy="29" rx="4" ry="5" fill="#d5a16a"/><ellipse cx="20" cy="40" rx="4" ry="5" fill="#c28a51"/><path d="M25 25l17-4m-16 16l18-4" stroke="#e0b67b"/>',
      "ore": '<path d="M14 39l5-19 13-7 18 12-4 21-19 4Z" fill="url(#stone)" stroke="#252d32" stroke-width="2"/><path d="M19 20l13 10 18-5M32 30l-5 20m5-20l14 16" fill="none" stroke="#e2e3d9" stroke-width="2"/><path d="M32 14l-1 15" stroke="#fff" stroke-width="1.5"/>',
      "crystal": '<path d="M31 9l10 15-2 24-10 7-9-25Z" fill="#3f9bc9" stroke="#123f62" stroke-width="2"/><path d="M31 9l1 46 8-31Z" fill="#b9f3ff" opacity=".8"/><path d="M20 30l11-21 3 24-4 22Z" fill="#75d5ed" opacity=".8"/><path d="M31 9l10 15-10 9-11-3Z" fill="#e1ffff" opacity=".65"/>',
      "sulfur": '<path d="M13 43l6-17 12-10 15 8 5 17-15 9-17-2Z" fill="#d8b92c" stroke="#5c4816" stroke-width="2"/><path d="M19 27l12-11 6 17-18 10Z" fill="#f4e36a"/><path d="M37 33l9-9 5 17-15 9Z" fill="#a76a22"/><circle cx="27" cy="36" r="3" fill="#fff2a0"/>',
      "mercury": '<path d="M19 23c4-9 20-10 27-1 8 10-2 24-12 27-11 3-23-7-15-17Z" fill="#aabdc5" stroke="#4b5960" stroke-width="2"/><path d="M24 22c5-5 14-4 18 1-7-1-11 3-14 8-5 7-8 5-10 2" fill="#f2ffff"/><circle cx="39" cy="37" r="5" fill="#d8f5fa"/><circle cx="27" cy="43" r="2" fill="#fff"/>',
      "essence": '<path d="M32 9l6 15 16 8-16 7-6 16-7-16-15-7 15-8Z" fill="#8569c7" stroke="#33275f" stroke-width="2"/><path d="M32 9v46l6-16 16-7-16-8Z" fill="#c8b7ff" opacity=".75"/><path d="M17 32l15-8 15 8-15 7Z" fill="#efeaff" opacity=".8"/><circle cx="32" cy="32" r="4" fill="#fff" filter="url(#glow)"/>',
    }
    return shapes[kind]


def building(kind: str, faction: str, dwelling: bool = False) -> str:
    accent = {"brass":"#c18a3b", "ash":"#9b8acb", "root":"#6e9b5b"}[faction]
    if kind == "settlement":
        if faction == "brass":
            return f'''<path d="M8 44L18 29 28 35 39 23 56 43Z" fill="#6b6254" stroke="#171d20" stroke-width="2"/><path d="M12 42V26l13-9 13 9v16Z" fill="url(#stone)" stroke="#252d32" stroke-width="2"/><path d="M9 27l16-12 16 12-4 4-12-9-12 9Z" fill="url(#gold)"/><path d="M42 43V22l7-7 7 7v21Z" fill="#6e7475" stroke="#20282d" stroke-width="2"/><path d="M45 24h8M45 30h8" stroke="{accent}" stroke-width="2"/><rect x="21" y="31" width="8" height="11" fill="#25323a"/><path d="M23 31v-5h4v5" fill="none" stroke="#e7c57b"/>'''
        if faction == "ash":
            return '''<path d="M7 46L19 30 30 37 44 22 58 46Z" fill="#49474f"/><path d="M15 44V25l17-12 16 12v19Z" fill="#b5b0aa" stroke="#393b43" stroke-width="2"/><path d="M11 26l21-16 21 16-5 4-16-12-16 12Z" fill="#655c79"/><path d="M24 44V31q8-9 15 0v13Z" fill="#34313b"/><path d="M28 32h7" stroke="#c9b5ee" stroke-width="2"/><path d="M32 10V4m-4 4h8" stroke="#b6a5d9" stroke-width="2"/>'''
        return '''<path d="M5 46L17 32 29 38 43 27 59 47Z" fill="#344b37"/><path d="M13 44V26l18-13 19 13v18Z" fill="#8c9976" stroke="#253b2a" stroke-width="2"/><path d="M9 27l22-17 23 17-5 4-18-13-18 13Z" fill="#5f8350"/><path d="M23 44V31q8-8 16 0v13Z" fill="#273d2c"/><path d="M31 13v-7m-5 7q5-8 10 0" stroke="#c1d08b" stroke-width="2"/>'''
    # Dwelling: intentionally compact, domestic silhouette distinct from settlement.
    if faction == "brass":
        return '''<path d="M12 31L32 15 52 31v17H12Z" fill="#a9a394" stroke="#33383a" stroke-width="2"/><path d="M8 31L32 11 56 31l-4 4-20-16-20 16Z" fill="#a85d3c" stroke="#4a3027" stroke-width="2"/><rect x="27" y="35" width="10" height="13" fill="#39434a"/><rect x="16" y="34" width="7" height="7" fill="#7bc0c5" stroke="#41484b"/><path d="M19 34v7m-3-3h7" stroke="#e4e1d3"/>'''
    if faction == "ash":
        return '''<path d="M13 29L32 17 51 29v19H13Z" fill="#c5c0b4" stroke="#45434b" stroke-width="2"/><path d="M10 29L32 12 54 29l-4 4-18-14-18 14Z" fill="#76688e"/><rect x="27" y="34" width="10" height="14" fill="#34313d"/><rect x="17" y="33" width="7" height="7" fill="#a5d5dc"/><path d="M20 33v7m-3-3h7" stroke="#eee"/>'''
    return '''<path d="M12 31Q32 4 52 31v17H12Z" fill="#718b5b" stroke="#2b402e" stroke-width="2"/><path d="M8 32Q32 1 56 32" fill="none" stroke="#b3c48b" stroke-width="5"/><path d="M20 48V35q12-12 24 0v13Z" fill="#344c34"/><path d="M32 17v-8m-7 12q7-12 14 0" stroke="#c7d89c" stroke-width="2"/>'''


def hero(faction: str) -> str:
    a = {"brass":"#c79a50", "ash":"#b8a9d8", "root":"#84a96a"}[faction]
    return f'''<path d="M18 48l4-21 10-8 10 8 4 21-14 5Z" fill="#343d45" stroke="#111820" stroke-width="2"/><path d="M22 29l10-13 10 13-4 5H26Z" fill="{a}" stroke="#24252a" stroke-width="2"/><path d="M27 31h10l-2 6h-6Z" fill="#1b2730"/><path d="M32 17V8m-5 5l5-6 5 6" stroke="{a}" stroke-width="2.5"/><path d="M19 35l-8 12m34-12l8 12" stroke="#a6a49b" stroke-width="3"/><path d="M12 47l-3 5m42-5l3 5" stroke="{a}" stroke-width="2"/><path d="M27 41h10" stroke="#f2d69a" stroke-width="2"/>'''


def token_symbol(key: str) -> str:
    if key.startswith("mine_"):
        return mine(key[5:])
    if key.startswith("settlement_"):
        return building("settlement", key.split("_")[1])
    if key.startswith("dwelling_"):
        return building("dwelling", key.split("_")[1], True)
    if key == "echo_cache":
        return '''<path d="M13 25L32 14 51 25v22L32 56 13 45Z" fill="#55446e" stroke="#211b31" stroke-width="2"/><path d="M13 25l19 11 19-11M32 36v20" fill="none" stroke="#c6a8ee" stroke-width="2"/><path d="M22 26l10-6 10 6-10 6Z" fill="#e4d8ff"/><circle cx="32" cy="37" r="5" fill="#f1d77b" filter="url(#glow)"/>'''
    if key.startswith("patrol_"):
        faction = key.split("_")[1]; a = {"brass":"#c79a50","ash":"#b8a9d8","root":"#84a96a"}[faction]
        return f'''<path d="M32 10l18 8v13q-1 15-18 23Q15 46 14 31V18Z" fill="#39434a" stroke="#111820" stroke-width="2"/><path d="M32 15l12 6v10q-1 10-12 16Q21 41 20 31V21Z" fill="{a}"/><path d="M32 19v22m-7-11h14" stroke="#252e35" stroke-width="3"/><path d="M22 20l10-5 10 5" fill="none" stroke="#fff" opacity=".5"/>'''
    if key.startswith("hero_"):
        return hero(key.split("_")[1])
    if key == "army_neutral":
        return '''<path d="M32 9l18 8v15q-2 14-18 22Q16 46 14 32V17Z" fill="#6c7779" stroke="#1b262c" stroke-width="2"/><path d="M32 15l11 5v11q-2 9-11 14-9-5-11-14V20Z" fill="#b8c0ba"/><path d="M32 20l-6 11 6 8 6-8Z" fill="#47565b"/><path d="M32 22v15" stroke="#f4e7bd" stroke-width="2"/>'''
    if key.startswith("flag_"):
        faction = key.split("_")[1]
        flag_color = {"brass":"#c79a50", "ash":"#9b8acb", "root":"#6e9b5b"}[faction]
        return '''<path d="M21 51L25 10h4l-4 41Z" fill="url(#wood)"/><path d="M28 13q12-5 25 2l-6 12q-9-5-19 0Z" fill="{flag_color}" stroke="#492d2a" stroke-width="2"/><path d="M30 16q8-3 16 0" stroke="#f0c28c" stroke-width="2"/><path d="M15 52h22" stroke="#c6aa75" stroke-width="3"/>'''
    raise KeyError(key)

TOKENS = [
 ("token_mine_gold","#252d35","#d4a94f","mine_gold"),("token_mine_wood","#252d35","#a9784b","mine_wood"),("token_mine_ore","#252d35","#aeb8ba","mine_ore"),("token_mine_crystal","#202b38","#70c9e9","mine_crystal"),("token_mine_sulfur","#302d20","#dfc84a","mine_sulfur"),("token_mine_mercury","#26343c","#c5e2e6","mine_mercury"),("token_mine_essence","#29243c","#a990e8","mine_essence"),
 ("token_settlement_brass","#252d35","#c79a50","settlement_brass"),("token_settlement_ash","#292733","#b8a9d8","settlement_ash"),("token_settlement_root","#202d24","#84a96a","settlement_root"),
 ("token_dwelling_brass","#252d35","#c79a50","dwelling_brass"),("token_dwelling_ash","#292733","#b8a9d8","dwelling_ash"),("token_dwelling_root","#202d24","#84a96a","dwelling_root"),
 ("token_echo_cache","#29243c","#c6a8ee","echo_cache"),
 ("token_patrol_brass","#252d35","#c79a50","patrol_brass"),("token_patrol_ash","#292733","#b8a9d8","patrol_ash"),("token_patrol_root","#202d24","#84a96a","patrol_root"),
 ("token_hero_brass","#252d35","#c79a50","hero_brass"),("token_hero_ash","#292733","#b8a9d8","hero_ash"),("token_hero_root","#202d24","#84a96a","hero_root"),
 ("token_army_neutral","#283239","#c2c9c4","army_neutral"),
 ("token_flag_brass","#252d35","#c79a50","flag_brass"),("token_flag_ash","#292733","#b8a9d8","flag_ash"),("token_flag_root","#202d24","#84a96a","flag_root"),
]

def generate_tokens(out: Path) -> int:
    for name, bg, accent, key in TOKENS:
        body = f'''<circle cx="32" cy="32" r="29" fill="#080d12" opacity=".75"/>
<circle cx="32" cy="30" r="28" fill="{bg}" stroke="{accent}" stroke-width="2.5" filter="url(#shadow)"/>
<circle cx="32" cy="30" r="24.5" fill="none" stroke="#fff" stroke-opacity=".22" stroke-width=".8"/>
<path d="M10 25Q32 5 54 25" fill="none" stroke="#fff" stroke-opacity=".13" stroke-width="2"/>
<g transform="translate(0,-1)" filter="url(#shadow)">{token_symbol(key)}</g>
<path d="M13 48Q32 59 51 48" fill="none" stroke="{accent}" stroke-opacity=".6" stroke-width="1.2"/>'''
        write(out, name, 64, 64, body)
    return len(TOKENS)

# ---------------------------------------------------------------------------
# Resource badges
# ---------------------------------------------------------------------------
RESOURCES = [("icon_gold","#d2a443","gold"),("icon_wood","#95633d","wood"),("icon_ore","#87969b","ore"),("icon_crystal","#55b7df","crystal"),("icon_sulfur","#d7bd37","sulfur"),("icon_mercury","#b9d1d7","mercury"),("icon_essence","#9278d2","essence")]
def generate_resources(out: Path) -> int:
    for name, color, key in RESOURCES:
        body = f'''<circle cx="16" cy="16" r="15" fill="#0b1118"/><circle cx="16" cy="15" r="13.5" fill="{color}" stroke="#202a32" stroke-width="1.5"/>
<g transform="translate(-16,-16) scale(.5)">{mine(key)}</g><path d="M5 9Q15 1 26 9" fill="none" stroke="#fff" stroke-opacity=".45" stroke-width="1.5"/>'''
        write(out, name, 32, 32, body)
    return len(RESOURCES)

# ---------------------------------------------------------------------------
# Map-state indicators: recognizable pictograms with consistent dark enamel
# ---------------------------------------------------------------------------
INDICATORS = {
"icon_movement_cost": '<path d="M16 5l9 17H7Z" fill="#d6ad59" stroke="#704b24" stroke-width="1.5"/><path d="M16 10v7m0 3v.5" stroke="#252a2e" stroke-width="2.2" stroke-linecap="round"/>',
"icon_visibility": '<path d="M3 16Q16 3 29 16 16 29 3 16Z" fill="#79c5df" stroke="#24536a" stroke-width="1.5"/><circle cx="16" cy="16" r="5.5" fill="#1a3546"/><circle cx="14" cy="14" r="2" fill="#e8fbff"/>',
"icon_explored": '<path d="M6 8l10-4 10 4v15l-10 5-10-5Z" fill="#5d9665" stroke="#23472e" stroke-width="1.5"/><path d="M16 4v24M6 8l10 5 10-5" fill="none" stroke="#d0e3a9" stroke-width="1.3"/>',
"icon_hidden": '<path d="M4 16q5-8 12-8t12 8q-5 8-12 8T4 16Z" fill="none" stroke="#b6c0c3" stroke-width="2"/><path d="M5 5l22 22" stroke="#707b80" stroke-width="2.5"/><circle cx="16" cy="16" r="3" fill="#9ba7aa"/>',
"icon_blocked": '<path d="M16 4a12 12 0 1 0 0 24 12 12 0 0 0 0-24Z" fill="#963f3b" stroke="#e28c78" stroke-width="1.5"/><path d="M8 8l16 16" stroke="#ffe1d1" stroke-width="3" stroke-linecap="round"/>',
"icon_road": '<path d="M10 4L5 28m14-24l5 24" stroke="#c5b99c" stroke-width="5"/><path d="M15 6l1 5m1 5l1 5m1 5l1 2" stroke="#6c6253" stroke-width="1.5"/>',
"icon_passable": '<path d="M5 16l7 7L27 8" fill="none" stroke="#83c18b" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>',
"icon_flyer": '<path d="M3 18q5-12 13-12t13 12q-8-5-13-2Q8 13 3 18Z" fill="#78c9e5" stroke="#255b78" stroke-width="1.5"/><path d="M16 16l-4 10 5-4 5 4-4-10" fill="#d9f6ff"/>',
"icon_danger": '<path d="M16 3L30 27H2Z" fill="#c64e42" stroke="#f2b17e" stroke-width="1.5"/><path d="M16 10v8m0 4v.5" stroke="#fff1d7" stroke-width="2.5" stroke-linecap="round"/>',
"icon_selected": '<circle cx="16" cy="16" r="12" fill="none" stroke="#f2ce69" stroke-width="2.5"/><circle cx="16" cy="16" r="7" fill="none" stroke="#fff1b0" stroke-width="1.2"/><path d="M16 0v5m0 22v5M0 16h5m22 0h5" stroke="#f2ce69" stroke-width="1.5"/>',
}
def generate_indicators(out: Path) -> int:
    for name, icon in INDICATORS.items():
        body = f'''<circle cx="16" cy="16" r="15" fill="#101923" stroke="#394956" stroke-width="1.3"/><circle cx="16" cy="16" r="12.8" fill="url(#panel)" stroke="#fff" stroke-opacity=".12"/><g>{icon}</g>'''
        write(out, name, 32, 32, body)
    return len(INDICATORS)

# ---------------------------------------------------------------------------
# 10 interface panels with bevels, corner hardware and state-specific accents
# ---------------------------------------------------------------------------
def panel_body(w: int, h: int, accent: str, state: str = "normal") -> str:
    r = 7 if min(w,h) > 70 else 5
    inner = max(4, min(w,h)*.07)
    state_overlay = ""
    if state == "hover":
        state_overlay = f'<rect x="3" y="3" width="{w-6}" height="{h-6}" rx="{r}" fill="#f1cf75" opacity=".09"/>'
    elif state == "pressed":
        state_overlay = f'<rect x="3" y="3" width="{w-6}" height="{h-6}" rx="{r}" fill="#8c3e39" opacity=".18"/><path d="M8 7h{w-16}" stroke="#d87a63" opacity=".35"/>'
    # fine engraved rules scale gracefully for both wide and tall panels
    if w > h:
        ornament = f'<path d="M{inner+5} {h-7}H{w-inner-5}" stroke="{accent}" opacity=".28"/><path d="M{inner+5} 7H{w-inner-5}" stroke="#fff" opacity=".08"/>'
    else:
        ornament = f'<path d="M7 {inner+5}V{h-inner-5}" stroke="{accent}" opacity=".28"/><path d="M{w-7} {inner+5}V{h-inner-5}" stroke="#fff" opacity=".08"/>'
    return f'''<rect x="1" y="1" width="{w-2}" height="{h-2}" rx="{r}" fill="#070c12" stroke="#05080c" stroke-width="2"/>
<rect x="2.5" y="2.5" width="{w-5}" height="{h-5}" rx="{r-1}" fill="url(#panel)" stroke="{accent}" stroke-width="1.5"/>
<rect x="{inner}" y="{inner}" width="{w-2*inner}" height="{h-2*inner}" rx="3" fill="none" stroke="#d8d1bc" stroke-opacity=".16" stroke-width=".8"/>
{ornament}{state_overlay}
<g fill="{accent}" stroke="#171d22" stroke-width=".6"><circle cx="7" cy="7" r="1.7"/><circle cx="{w-7}" cy="7" r="1.7"/><circle cx="7" cy="{h-7}" r="1.7"/><circle cx="{w-7}" cy="{h-7}" r="1.7"/></g>
<path d="M5 13V{h-13}" stroke="#fff" opacity=".035" stroke-width="1"/>'''

PANELS = [
 ("panel_top_dark",512,64,"#c59a50","normal"),("panel_bottom_dark",512,64,"#c59a50","normal"),
 ("panel_side_left",64,512,"#c59a50","normal"),("panel_side_right",64,512,"#c59a50","normal"),
 ("panel_card_dark",256,256,"#c59a50","normal"),("panel_tooltip",256,96,"#9b8acb","normal"),
 ("panel_button_normal",192,48,"#c59a50","normal"),("panel_button_hover",192,48,"#f1cf75","hover"),
 ("panel_button_pressed",192,48,"#b65d4c","pressed"),("panel_resource_slot",64,64,"#c59a50","normal"),
]
def generate_panels(out: Path) -> int:
    for name,w,h,accent,state in PANELS:
        body = panel_body(w,h,accent,state)
        if name == "panel_resource_slot":
            body += '<circle cx="32" cy="32" r="20" fill="#080e15" fill-opacity=".5" stroke="#d0a958" stroke-opacity=".5" stroke-width="1.5"/><circle cx="32" cy="32" r="15" fill="none" stroke="#fff" stroke-opacity=".08"/>'
        write(out,name,w,h,body)
    return len(PANELS)

# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------
def main() -> None:
    parser = argparse.ArgumentParser(description="Regenerate all 60 detailed Eidolon: Seam Wars SVG assets.")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2],
                        help="Project root (default: two levels above this script).")
    parser.add_argument("--output", type=Path, default=None,
                        help="Optional output directory; if omitted, use the game's original asset folders.")
    args = parser.parse_args()
    root = args.root.resolve()
    if args.output:
        base = args.output.resolve()
        map_dir = base / "map"
        objects_dir = map_dir / "objects"
        resources_dir = base / "ui" / "resources"
        indicators_dir = base / "ui" / "map"
        panels_dir = base / "ui" / "panels"
    else:
        map_dir = root / "game" / "assets" / "sprites" / "map"
        objects_dir = map_dir / "objects"
        resources_dir = root / "game" / "assets" / "sprites" / "ui" / "resources"
        indicators_dir = root / "game" / "assets" / "sprites" / "ui" / "map"
        panels_dir = root / "game" / "assets" / "sprites" / "ui" / "panels"

    total = (generate_biomes(map_dir) + generate_tokens(objects_dir) +
             generate_resources(resources_dir) + generate_indicators(indicators_dir) +
             generate_panels(panels_dir))
    assert total == 60, f"Expected 60 assets, generated {total}"
    print(f"Generated {total} SVG files.")
    for folder in (map_dir, objects_dir, resources_dir, indicators_dir, panels_dir):
        print(f"  {folder}")

if __name__ == "__main__":
    main()
