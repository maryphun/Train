# Battler modal background

Asset: `Assets/Graphic/UI/BattlerModalBackground.png` (1030×1526 RGBA).

The user selected dark plum with blush accents and gold trim: same UI family, distinct from the
light calendar. The center is opaque and empty; the exterior is transparent. No character,
text, level, cost, name strip or hire button is baked into the artwork.

Assign it to the modal's background UI Image. Use **Image Type: Simple**, a **white tint**, and
RectTransform **Width: 395.7296**, **Height: 586.4601**. Leave Preserve Aspect off to fill that exact
rectangle (the source ratio differs by less than 0.04%). The high-resolution texture need not have
the same pixel dimensions as the RectTransform. Do not use Set Native Size for this layout.

Generated with the built-in image tool, followed by the previously authorized local transparency
cleanup. The source artwork is unchanged in RGB. The original screenshot was used as a layout
reference only. No scene, button, modal behavior or existing asset was replaced.

## Dark-blue and frame variants (2026-10-06)

- `Assets/Graphic/UI/BattlerModalBackgroundDarkBlue.png`: the original modal with a navy interior around **#182B49**, retaining its subtle texture and original gold/blush frame.
- `Assets/Graphic/UI/BattlerModalFrame.png`: the original perimeter frame with its interior and exterior transparent. The narrow plum backing between the gold rim and blush keyline remains part of the original frame.

Both are **1030×1526 RGBA** with matching centered pivots, Single sprite import, Bilinear filtering, no texture compression, and no resizing. Use **Image Type: Simple** and a **white tint**. For layering, give the frame Image the same anchors, pivot, position, width and height as the background Image. Place the frame after the character/content in the Canvas hierarchy to draw the frame over it. If the frame is decorative, disable its Image **Raycast Target**.

The user explicitly approved local processing to preserve alignment. The original PNG/meta and scene references were not changed. The connected generic-UI Trello card and main planning deck were refreshed. Verification checked matching dimensions, unchanged navy exterior alpha, original visible perimeter pixels, transparent frame center, and unchanged original hash; a 396×586 preview was inspected. These are artwork variants; no scene assignment was requested.

Processing brief: recolor only the existing interior navy around #182B49 while retaining texture and the exact gold/blush perimeter; extract that same perimeter as a transparent-center PNG without changing canvas geometry. No image-generator call was used after the user selected local processing.

## Selection overlay (2026-10-06)

Asset: `Assets/Graphic/UI/BattlerModalSelection.png` (**1030×1526 RGBA**).

The user selected a luminous **ivory/gold outline and soft glow**, with a small ivory/gold checkmark **inside the bottom-right corner**. They authorized local processing from the frame to preserve exact alignment. The existing perimeter supplies the outline geometry; the center is transparent apart from the perimeter halo and checkmark. No text, full-panel tint, or animation is baked in.

Add an Image above the modal's frame/character/content in Canvas draw order. Copy the modal's anchors, pivot, position and size, use **Image Type: Simple** with a **white tint**, and turn off **Raycast Target** so the overlay does not intercept selection clicks. Show this Image for a selected modal and hide it for an unselected modal. The artwork does not implement selection logic or change the current scenes.

Verified matching dimensions and pivot/import settings, transparent center, unchanged hashes of all source modal sprites, and normal/selected previews over both plum and navy at 396×586. The peripheral glow fades inside the unchanged canvas bounds.

Processing brief: derive the selection highlight directly from the existing frame, make its keyline ivory and rim luminous gold, add a restrained halo and a small bottom-right ivory/gold checkmark, retain transparency and original canvas alignment. Built with the user's authorized local processing rather than image generation.

### Original generation prompt

Use case: stylized-concept. Create a standalone Unity UI BACKGROUND SPRITE for a battler recruitment modal. Input image is ONLY a layout reference, not content to reproduce. Target portrait aspect ratio exactly 395.7296 wide by 586.4601 high (width/height 0.674777). Generate just the empty card background, tightly filling the portrait canvas. Art direction explicitly chosen by user: DARK PLUM card with restrained blush pink accents and thin warm champagne-gold trim, belonging to a soft polished 2D anime visual-novel UI, but visually distinct from the pale pink/ivory calendar UI. Refined bevelled double gold rim, subtly clipped or softened ornamental corners, a slim muted blush inner keyline, understated plum edge shading. Keep decorations limited to the narrow perimeter, not large flourishes. Large uniform muted dark-plum middle gives contrast behind a character portrait. The upper area and bottom area must remain clear for existing separate name/level/cost/button UI. No compartment lines, no inset button shapes, no name plaque, no boxes or dividers. Absolutely NO character, monster, silhouette, illustration, letters, text, numbers, icons, logo, watermark, binder rings, calendar grid, stars, sparkles, embossed motif or central decoration. Flat straight-on orthographic game sprite, not a physical photographed card and not a screen mockup. True alpha transparency only outside the small corner cutouts; the dark-plum center is solid opaque. No checkerboard. Preserve all edges, no cropped border. Fine edge details readable at about 396 by 586 screen units.
