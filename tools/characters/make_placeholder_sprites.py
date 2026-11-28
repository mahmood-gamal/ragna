#!/usr/bin/env python3
"""Draws the placeholder character sprite sheets and their SpriteFrames resource.

Usage: python3 tools/characters/make_placeholder_sprites.py   (needs Pillow)

Writes, in assets/characters/placeholder/:

- placeholder_idle.png, placeholder_walk.png, placeholder_attack.png: one sheet per
  animation. 64x96 px frames, one row per direction (row 0 = south, then clockwise:
  s, sw, w, nw, n, ne, e, se), one column per frame. Feet on the bottom pixel row,
  centred. Each frame has its direction written in the top-left corner.
- a Godot .import file next to each sheet.
- placeholder_frames.tres: the SpriteFrames, with idle_s ... attack_se cut from the
  sheets, plus plain idle, walk and attack (the south row) as a fallback.

The layout is Ragna.Core.Characters.SpriteSheetLayout. See docs/characters.md.
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

FRAME_W, FRAME_H = 64, 96
CHARACTER = "placeholder"
OUT = Path(__file__).resolve().parents[2] / "assets/characters" / CHARACTER
RES_DIR = f"res://assets/characters/{CHARACTER}"

# name, frame count, frames per second, loops
ANIMATIONS = [("idle", 4, 4.0, True), ("walk", 6, 10.0, True), ("attack", 4, 10.0, False)]

# Row order. Matches Ragna.Core.Characters.FacingDirection.
DIRECTIONS = ["s", "sw", "w", "nw", "n", "ne", "e", "se"]
# The east side is the west side drawn mirrored.
MIRRORED = {"ne": "nw", "e": "w", "se": "sw"}

OUTLINE = (40, 28, 24, 255)
SKIN = (240, 200, 160, 255)
HAIR = (120, 70, 40, 255)
TUNIC = (200, 60, 50, 255)
TUNIC_DARK = (150, 40, 35, 255)
PANTS = (70, 70, 110, 255)
BOOTS = (80, 50, 30, 255)
BLADE = (210, 215, 225, 255)
LABEL = (255, 255, 255, 255)
LABEL_BG = (0, 0, 0, 160)

# How each of the five drawn directions looks. "turn" is how far the character is turned to
# the screen's left: 0 = front or back, 1 = three-quarter, 2 = side. "front" is True when the
# face is towards the camera. "sword" is the screen side of the sword arm (-1 left, +1 right).
POSES = {
    "s": dict(turn=0, front=True, sword=-1),
    "sw": dict(turn=1, front=True, sword=-1),
    "w": dict(turn=2, front=None, sword=-1),
    "nw": dict(turn=1, front=False, sword=1),
    "n": dict(turn=0, front=False, sword=1),
}


def rect(d, x0, y0, x1, y1, fill):
    d.rectangle([x0, y0, x1, y1], fill=fill, outline=OUTLINE)


def draw_frame(d, direction, anim, i):
    pose = POSES[direction]
    turn, front, side = pose["turn"], pose["front"], pose["sword"]
    cx = FRAME_W // 2
    half = [13, 11, 8][turn]  # body half-width: narrower when turned
    bob = 0
    stride = [0, 0]  # left/right leg x offsets (towards the screen's left = -)
    lift = [0, 0]  # left/right leg lift in px
    arm = 0  # 0 = rest, 1..3 = swing stages

    if anim == "idle":
        bob = [0, 1, 2, 1][i]
    elif anim == "walk":
        step = [-6, -3, 0, 6, 3, 0][i]
        bob = [0, 1, 2, 0, 1, 2][i]
        # Side views stride left-right, front and back views lift the feet.
        stride = [step * turn // 2, -step * turn // 2]
        up = max(0, step) * (2 - turn) // 3, max(0, -step) * (2 - turn) // 3
        lift = list(up)
    elif anim == "attack":
        arm = i  # wind up, raise, strike, recover

    floor = FRAME_H - 1
    legs = (cx - 9 + turn * 2, cx + 2 - turn * 2)

    # In back views the sword arm is behind the body, so it is drawn first.
    behind = front is False
    if behind:
        draw_sword_arm(d, cx, half, floor - 48 + bob, arm, side, turn)

    for leg_x, off, up in zip(legs, stride, lift):
        rect(d, leg_x + off, floor - 22 - up, leg_x + off + 7, floor - 6 - up, PANTS)
        rect(d, leg_x + off - 1, floor - 6 - up, leg_x + off + 8, floor - up, BOOTS)

    top = floor - 48 + bob
    rect(d, cx - half, top, cx + half, floor - 20 + bob // 2, TUNIC)
    d.line([cx - half, top + 14, cx + half, top + 14], fill=TUNIC_DARK, width=2)

    draw_head(d, cx - turn * 2, top, turn, front)

    # The free arm, on the other side from the sword. Hidden behind the body in the side view.
    if turn < 2:
        ax = cx - side * (half + 3)
        rect(d, ax - 3, top + 2, ax + 2, top + 20, SKIN)

    if not behind:
        draw_sword_arm(d, cx, half, top, arm, side, turn)


def draw_head(d, hx, top, turn, front):
    head_top = top - 26
    box = [hx - 13, head_top, hx + 13, top + 2]
    if front is False and turn == 0:
        # The back of the head: all hair.
        d.ellipse(box, fill=HAIR, outline=OUTLINE)
        return

    d.ellipse(box, fill=SKIN, outline=OUTLINE)
    eye_y = head_top + 15
    if front is True:
        d.chord([hx - 14, head_top - 2, hx + 14, top - 6], 180, 360, fill=HAIR, outline=OUTLINE)
        shift = -3 * turn  # eyes move towards the way the face turns
        for ex in (hx - 6 + shift, hx + 3 + shift):
            d.rectangle([ex, eye_y, ex + 3, eye_y + 3], fill=OUTLINE)
    elif front is None:
        # Side view, facing the screen's left: hair on the back half, one eye at the front.
        d.chord([hx - 14, head_top - 2, hx + 14, top - 6], 180, 360, fill=HAIR, outline=OUTLINE)
        d.pieslice([hx - 13, head_top, hx + 13, top + 2], 270, 90, fill=HAIR, outline=OUTLINE)
        d.rectangle([hx - 9, eye_y, hx - 6, eye_y + 3], fill=OUTLINE)
    else:
        # Three-quarter back view: hair over most of the head, a sliver of cheek on the left.
        d.pieslice([hx - 13, head_top, hx + 13, top + 2], 230, 130, fill=HAIR, outline=OUTLINE)
        d.chord([hx - 14, head_top - 2, hx + 14, top - 6], 180, 360, fill=HAIR, outline=OUTLINE)


def draw_sword_arm(d, cx, half, top, arm, side, turn):
    """Arm and sword on screen side `side`. The swing goes from behind the head to forward and down."""
    s = side
    sx = cx + s * (half + 1) if turn < 2 else cx - 2
    if arm == 0:
        rect(d, min(sx, sx + s * 5), top + 2, max(sx, sx + s * 5), top + 20, SKIN)
        hx, hy = sx + s * 3, top + 20
        # Side view: the sword points forward and down, clear of the face.
        tip = (hx + s * 2, hy - 26) if turn < 2 else (hx + s * 14, hy + 16)
    elif arm == 1:  # wind up behind the head
        rect(d, min(sx - s * 2, sx + s * 4), top - 10, max(sx - s * 2, sx + s * 4), top + 4, SKIN)
        hx, hy = sx + s, top - 10
        tip = (hx - s * 18, hy - 18)
    elif arm == 2:  # overhead
        rect(d, min(sx - s * 8, sx - s * 2), top - 16, max(sx - s * 8, sx - s * 2), top + 2, SKIN)
        hx, hy = sx - s * 5, top - 16
        tip = (hx + s * 4, hy - 26)
    else:  # strike forward and down
        rect(d, min(sx, sx + s * 12), top + 4, max(sx, sx + s * 12), top + 10, SKIN)
        hx, hy = sx + s * 12, top + 7
        tip = (hx + s * 4, hy + 24)
    d.line([hx, hy, *tip], fill=OUTLINE, width=5)
    d.line([hx, hy, *tip], fill=BLADE, width=3)


def draw_label(frame, direction):
    d = ImageDraw.Draw(frame)
    font = ImageFont.load_default()
    text = direction.upper()
    x0, y0, x1, y1 = d.textbbox((0, 0), text, font=font)
    d.rectangle([0, 0, x1 - x0 + 3, y1 - y0 + 3], fill=LABEL_BG)
    d.text((2 - x0, 2 - y0), text, fill=LABEL, font=font)


def render_frame(direction, anim, i):
    source = MIRRORED.get(direction, direction)
    frame = Image.new("RGBA", (FRAME_W, FRAME_H), (0, 0, 0, 0))
    draw_frame(ImageDraw.Draw(frame), source, anim, i)
    if source != direction:
        frame = frame.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    draw_label(frame, direction)
    return frame


def godot_uid(seed):
    """A stable Godot resource UID derived from `seed`, so regenerating doesn't churn the .import files."""
    chars = "abcdefghijklmnopqrstuvwxy0123456789"
    n = int.from_bytes(hashlib.sha256(seed.encode()).digest()[:8], "big") & ((1 << 63) - 1)
    out = ""
    while n:
        out = chars[n % len(chars)] + out
        n //= len(chars)
    return "uid://" + out


def import_file(res_path):
    ctex = f"res://.godot/imported/{res_path.rsplit('/', 1)[1]}-{hashlib.md5(res_path.encode()).hexdigest()}.ctex"
    return f"""[remap]

importer="texture"
type="CompressedTexture2D"
uid="{godot_uid(res_path)}"
path="{ctex}"
metadata={{
"vram_texture": false
}}

[deps]

source_file="{res_path}"
dest_files=["{ctex}"]

[params]

compress/mode=0
compress/high_quality=false
compress/lossy_quality=0.7
compress/uastc_level=0
compress/rdo_quality_loss=0.0
compress/hdr_compression=1
compress/normal_map=0
compress/channel_pack=0
mipmaps/generate=false
mipmaps/limit=-1
roughness/mode=0
roughness/src_normal=""
process/channel_remap/red=0
process/channel_remap/green=1
process/channel_remap/blue=2
process/channel_remap/alpha=3
process/fix_alpha_border=true
process/premult_alpha=false
process/normal_map_invert_y=false
process/hdr_as_srgb=false
process/hdr_clamp_exposure=false
process/size_limit=0
detect_3d/compress_to=1
"""


def animation_entry(name, sub_ids, fps, loop):
    frames = ", ".join(f'{{\n"duration": 1.0,\n"texture": SubResource("{sid}")\n}}' for sid in sub_ids)
    return (
        "{\n"
        f'"frames": [{frames}],\n'
        f'"loop": {"true" if loop else "false"},\n'
        f'"name": &"{name}",\n'
        f'"speed": {fps}\n'
        "}"
    )


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    exts, subs, anims = [], [], []

    for anim_index, (anim, count, fps, loop) in enumerate(ANIMATIONS):
        file_name = f"{CHARACTER}_{anim}.png"
        res_path = f"{RES_DIR}/{file_name}"
        sheet = Image.new("RGBA", (FRAME_W * count, FRAME_H * len(DIRECTIONS)), (0, 0, 0, 0))
        for row, direction in enumerate(DIRECTIONS):
            for i in range(count):
                sheet.paste(render_frame(direction, anim, i), (i * FRAME_W, row * FRAME_H))
        sheet.save(OUT / file_name)
        (OUT / f"{file_name}.import").write_text(import_file(res_path))

        ext_id = f"{anim_index + 1}_{anim}"
        exts.append(f'[ext_resource type="Texture2D" uid="{godot_uid(res_path)}" path="{res_path}" id="{ext_id}"]')

        rows = {}
        for row, direction in enumerate(DIRECTIONS):
            ids = []
            for i in range(count):
                sid = f"AtlasTexture_{anim}_{direction}_{i}"
                subs.append(
                    f'[sub_resource type="AtlasTexture" id="{sid}"]\n'
                    f'atlas = ExtResource("{ext_id}")\n'
                    f"region = Rect2({i * FRAME_W}, {row * FRAME_H}, {FRAME_W}, {FRAME_H})\n"
                )
                ids.append(sid)
            rows[direction] = ids
            anims.append(animation_entry(f"{anim}_{direction}", ids, fps, loop))
        # Plain name as a fallback for anything without directions: the south row.
        anims.append(animation_entry(anim, rows["s"], fps, loop))

    tres = (
        f'[gd_resource type="SpriteFrames" load_steps={len(exts) + len(subs) + 1} format=3]\n\n'
        + "\n".join(exts)
        + "\n\n"
        + "\n".join(subs)
        + "\n[resource]\n"
        + f"animations = [{', '.join(anims)}]\n"
    )
    (OUT / f"{CHARACTER}_frames.tres").write_text(tres)


if __name__ == "__main__":
    main()
