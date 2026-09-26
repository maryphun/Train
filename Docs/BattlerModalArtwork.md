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

## Generation prompt

Use case: stylized-concept. Create a standalone Unity UI BACKGROUND SPRITE for a battler recruitment modal. Input image is ONLY a layout reference, not content to reproduce. Target portrait aspect ratio exactly 395.7296 wide by 586.4601 high (width/height 0.674777). Generate just the empty card background, tightly filling the portrait canvas. Art direction explicitly chosen by user: DARK PLUM card with restrained blush pink accents and thin warm champagne-gold trim, belonging to a soft polished 2D anime visual-novel UI, but visually distinct from the pale pink/ivory calendar UI. Refined bevelled double gold rim, subtly clipped or softened ornamental corners, a slim muted blush inner keyline, understated plum edge shading. Keep decorations limited to the narrow perimeter, not large flourishes. Large uniform muted dark-plum middle gives contrast behind a character portrait. The upper area and bottom area must remain clear for existing separate name/level/cost/button UI. No compartment lines, no inset button shapes, no name plaque, no boxes or dividers. Absolutely NO character, monster, silhouette, illustration, letters, text, numbers, icons, logo, watermark, binder rings, calendar grid, stars, sparkles, embossed motif or central decoration. Flat straight-on orthographic game sprite, not a physical photographed card and not a screen mockup. True alpha transparency only outside the small corner cutouts; the dark-plum center is solid opaque. No checkerboard. Preserve all edges, no cropped border. Fine edge details readable at about 396 by 586 screen units.
