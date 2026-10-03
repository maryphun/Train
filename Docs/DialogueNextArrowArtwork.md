# Dialogue next-arrow artwork

Asset: `Assets/Graphic/UI/Icons/DialogueNextArrow.png`.

Confirmed on 2026-10-03: a **plain white filled downward triangle** for a Japanese
JRPG-style dialogue cue telling the player to click to continue. The PNG is a
standalone icon with transparent surroundings. It has no button frame or gold trim.

The 1254 × 1254 generated PNG is copied without pixel changes. Imported as a Single
Unity UI sprite with centered pivot, 100 pixels per unit, clamp wrapping, trilinear
filtering, mipmaps, alpha transparency, and no texture compression. A 32 × 32 preview
was visually checked. Assign to a UI Image with Simple type and Preserve Aspect.
The triangle occupies about three quarters of the square sprite width.

This request delivers artwork. Dialogue placement, animation, and show/hide timing
remain unspecified and have not been implemented. The live MainMenu Trello card and
main planning deck were refreshed; the user's white-triangle choice defines this
asset independently of the pink/gold theme.

Mode: built-in image generation with `transparent_background: true`.

Final prompt:

> Create exactly one plain white filled downward-pointing triangle (▼), a minimalist Japanese JRPG dialogue click-to-continue indicator. One flat geometric isosceles triangle: horizontal top edge, symmetrical diagonal sides, bottom tip centered. Pure solid RGB white, no outline, no shading, no gradient, no bevel, no texture, no shadow, no text, no border, no button enclosure, no other elements. Smooth antialiased edges. Center it on a transparent square PNG canvas, triangle about 75 percent of canvas width, triangle height about 55 percent of canvas height. True transparent background outside the triangle; do not draw checkerboard. This is a standalone tiny game UI icon intended to be readable at roughly 20–32 screen pixels wide.
