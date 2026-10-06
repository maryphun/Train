# Battle skill button artwork

Created 2026-10-06 with the built-in image generation tool.

The user requested a more button-like version of the previous wide sprite, both normal and
pressed states, and clarified that the shape should be near-square.

## Assets

- `Assets/Graphic/UI/BattleSkillButton_Normal.png`: raised ivory face, blush bevel, gold rim.
- `Assets/Graphic/UI/BattleSkillButton_Pressed.png`: darker blush face with inset shading.

Both PNGs are 1254×1254 RGBA with centered pivots, true exterior transparency, and matching
import settings. The main opaque bounds differ by one pixel vertically; generated alpha is
retained unmodified. No pixel editing or resizing was performed. Single UI sprites use 280-pixel
borders, no compression, mipmaps and trilinear filtering for small display sizes. The earlier
wide `BattleSkillButton.png` remains available. The new files are artwork only; scene/prefab
assignments and button dimensions are left for the user's Canvas.

## Assign to the template

1. Use the normal sprite as the template Image's **Source Image**.
2. For the intended near-square shape, use **Simple** Image type and **Preserve Aspect**.
   If resizing the frame independently, **Sliced** is also supported by matching borders.
3. Set Button **Transition → Sprite Swap**.
4. Assign the pressed sprite to **Pressed Sprite**. Use the normal sprite for Highlighted and
   Selected if no separate states are wanted; clear other overrides to retain the source image.
5. Keep the skill label as a separate TMP child.

Visual inspection confirms clear raised versus inset states and blank centers. Image inspection
confirms matching dimensions, transparent corner pixels, and full alpha range 0–255. Unity Play
Mode appearance has not been checked.

The live battle Trello card was refreshed; it contains no additional button artwork specification.
The linked planning deck could not be accessed. The user's screenshot and requested shape/states
provide the visual direction.

## Generation prompts

Normal:

> Use case: ui-mockup. Edit target: the attached existing Unity BattleSkillButton sprite. Create a redesigned NORMAL/unpressed button asset for the user's battle action popup. Change the wide ornamental panel into ONE NEAR-SQUARE button with width about 1.08 times height, softly rounded square corners, simple warm champagne-gold rim, substantial pale blush-pink beveled sidewalls, and a gently raised ivory face. It must immediately read as a clickable tactile button: clear upper-left edge highlight, subtly shaded lower bevel and compact integrated lower depth, no surrounding drop-shadow cloud. Retain the ivory/blush/gold palette of the source UI. Preserve a large totally blank interior for separate dark Japanese TMP skill label. Restrained attractive anime JRPG UI artwork, straight-on orthographic 2D sprite. Centered, tightly framed with at most 3 percent clear margin on each side. True transparent alpha everywhere outside the button silhouette, opaque face. Exactly one button, no text, icons, stars, hearts, flowers, ornamental corner cutouts, enclosing popup, grid, scenery, watermark or checkerboard. Canvas roughly square. Return only this NORMAL state; pressed counterpart will be requested separately.

Pressed:

> Use case: precise-object-edit. Edit target: the attached near-square NORMAL battle button. Produce exactly its PRESSED/DOWN state for Unity Button Sprite Swap. CRITICAL: keep the exact source canvas dimensions, centered position, outer silhouette, transparency boundary, corner radius, rim width, and outer gold-frame geometry unchanged so switching sprites causes no size or placement jump. Change ONLY internal surface shading and colors: visibly darker dusty blush-pink face, inset/sunken center with a soft inner shadow along the top and left, subdued highlights, bottom inner edge catching a restrained ivory glow. The center should look pushed down into the same gold surround instead of raised, but remain bright enough for dark Japanese text. Keep it subtle and elegant matching the normal state, gold frame and pink popup. Preserve blank interior, actual alpha transparency outside the identical silhouette. ONE button only, no text, icons, symbols, ornaments, scenery, surrounding panel, watermark or checkerboard. Return only the pressed state.

