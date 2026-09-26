# Shared back-button artwork

Asset: `Assets/Graphic/UI/BackButton.png`.

Confirmed on 2026-09-27: artwork only, a left arrow for returning to the previous screen,
a rounded-square silhouette, and the recent pink, ivory and gold UI style.

The PNG is 1254 × 1254 with its generated RGBA alpha preserved. Its surroundings and corners
are transparent. It contains one ivory left arrow with gold edging on a blush-pink face.
The source image was copied byte-for-byte from the built-in image tool's output.

## Unity use

Imported as **Sprite (2D and UI)**, **Single**, centered pivot, 100 pixels per unit,
bilinear filtering, clamp wrapping, no mipmaps and no texture compression.
Assign to a UI Image or a Button's target Image. Use **Image Type: Simple** and
**Preserve Aspect**, with a square RectTransform at the desired UI size.
The icon and background are one sprite; do not nine-slice or stretch it.

This adds artwork and import settings only. No scene, prefab, navigation behavior or
localization entry was changed.

## Context and validation

The live Trello cards for the main screen, general UI artwork, main UI theme and overall
UI reference, and the main planning deck were refreshed on 2026-09-27.
The user's choices above define this asset. Existing calendar and time-of-day artwork
were visually inspected for palette and rendering style.

Verified the dimensions, RGBA pixel format, transparent exterior pixels, and identical
SHA-256 hashes for the generated source and saved project PNG. No gameplay code changed.

## Generation

Mode: built-in image generation with `transparent_background: true`.
No local image editing or CLI fallback was used.

Final prompt:

Use case: stylized-concept.
Asset type: one reusable Unity UI back-button PNG sprite, artwork only.
Primary request: a rounded-square button containing a single centered LEFT-pointing back arrow, suitable for returning to the previous screen throughout a game. Match a soft pink, ivory and champagne-gold visual-novel interface.
Composition: straight-on flat 2D view, square canvas, compact rounded-square silhouette filling most of the canvas with a small even transparent margin. All corners have the same gentle radius. One icon, one button, no sheet of variants.
Style: polished, restrained game UI. Pale blush-pink enamel face, thin champagne-gold double rim with a fine warm-brown outer contour, narrow ivory edge highlight and subtle inset shading, similar to a softly shaded pink and gold calendar frame. The back arrow is a simple substantial left arrow with a straight horizontal shaft and clear arrowhead, rounded ends, warm ivory fill, champagne-gold edge and a thin deeper warm-brown contour so it stays readable at small UI sizes. Visually centered, generous internal space, consistent clean antialiasing. Very subtle depth, never a perspective render or physical product photo.
Constraints: true RGBA transparent background outside the rounded-square silhouette; do not draw a checkerboard, colored backdrop, or opaque white canvas. No words, letters, numbers, exit-door symbol, cross, circular button, flowers, hearts, sparkles, ribbons, ornate decoration, detached shadows, mockup or watermark. Keep the entire button intact and unclipped.

