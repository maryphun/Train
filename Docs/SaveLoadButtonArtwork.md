# Save and Load button artwork

Assets: `Assets/Graphic/UI/Icons/SaveButton.png` and
`Assets/Graphic/UI/Icons/LoadButton.png`.

Confirmed on 2026-10-03: two separate icon-only buttons, matching the existing
rounded-square back button. Both use the same ivory-and-gold floppy-disk motif on a
blush-pink face. Save has a downward arrow entering the disk; Load has an upward arrow
leaving it. The arrow directions are the visual distinction between the buttons.

Each PNG is 1254 × 1254 with transparent surroundings and corners. Both are imported
as Single Unity UI sprites with a centered pivot, 100 pixels per unit, trilinear
filtering, generated mipmaps, clamp wrapping, and no texture compression. These
filtering settings differ from `BackButton.png` because the Save/Load art is displayed
at about 80 UI pixels in the reported 1970 × 1080 screen. Use **Image Type: Simple**
and **Preserve Aspect** with a square
RectTransform. The icon and frame are a single sprite, so do not nine-slice or stretch.

This delivers artwork and importer settings only. No scene objects, prefabs, button
behavior, save/load calls, or localization entries were changed. The connected planning
sources were unavailable in this session; project context and the existing artwork
were used for the visual match.

## Initial generation

Mode: built-in image editing with `transparent_background: true`, using the existing
`BackButton.png` as the Save reference and the generated Save button as the Load
reference. No local pixel editing was used.

## Small-screen revision (2026-10-03)

The user reported aliasing at small display sizes and confirmed the scope is Save and
Load only. They requested both a simpler redraw and improved filtering. The final
artwork keeps the existing disk-and-opposite-arrow meanings, blush-pink face, and
ivory/gold palette, with broader contours and fewer narrow bevels. The two PNGs were
replaced in place while preserving their Unity GUIDs. Mipmaps and trilinear filtering
were enabled on these two sprite imports. No scene/layout or behavior was changed.
The originals were backed up in ignored `Library/SaveLoadButtonBackup`.

The relevant live Trello cards could not be opened in this session. The current
project screenshot, assets, and Unity 6.3 sprite import settings informed the fix.

Revision mode: built-in image editing with `transparent_background: true`, using the
previous Save sprite as the Save reference and the newly generated Save sprite as the
Load reference. No local pixel editing was used.

Revised Save prompt:

> Edit the supplied SAVE button sprite into a small-screen UI version intended to display at about 80 x 80 pixels. Preserve its rounded-square silhouette, blush-pink face, ivory-and-champagne-gold palette, centered floppy-disk symbol, and downward arrow entering the disk. Simplify the artwork substantially: one smooth medium-weight gold border around the pink square, only a restrained broad ivory edge highlight, and a bold ivory disk-and-arrow silhouette with one clean gold outline. Remove narrow concentric rims, hairline contours, tiny glints, fine inset bevels, and any detail that would become subpixel at 80 px. Use clean regular geometry, balanced spacing, and strong legibility while retaining gentle polished shading. The button shape must be square and straight-on, with a transparent exterior and no detached shadows, text, checkerboard, sparkles, or other objects. Output one transparent PNG sprite, same square canvas and approximate framing as the reference.

Revised Load prompt:

> Create the matching small-screen LOAD button sprite from the supplied simplified SAVE button. Keep the button's rounded-square shape, simple broad gold rim, blush-pink face, ivory floppy-disk icon, bold stroke weights, icon size and position, soft broad shading, and transparent exterior the same. Change only the downward arrow into an upward arrow emerging from the disk opening. It must be clear at about 80 x 80 display pixels, with no narrow hairline detail, tiny glints, extra decoration, text, detached shadow, or checkerboard. Transparent PNG, same square canvas and approximate framing.

## Initial prompt archive

Save prompt:

> Create a Save-button sprite by editing the supplied button image. Keep the same blush-pink rounded square, ivory and champagne-gold trim, transparent outside corners, proportions, lighting, and canvas. Change only the center back arrow into an ivory-and-gold floppy-disk icon with a small downward arrow entering the disk. The icon must be clear at small UI sizes. No text, no additional elements, no background.

Load prompt:

> Create the matching LOAD button sprite from this SAVE button image. Preserve the same rounded-square blush-pink enamel button, ivory/champagne-gold border, disk icon, placement, dimensions, lighting, transparency, and all proportions exactly. Change ONLY the central arrow: make it point upward and outward from the disk's top opening, clearly meaning Load, at the same scale and with the same ivory enamel and gold edging. Keep the disk shape and all other artwork consistent. No text, no additional elements, transparent outside the button.

## Pressed states (2026-10-03)

The user confirmed **darker pink with an inset look** for both pressed buttons.
`Assets/Graphic/UI/Icons/SaveButton_Pressed.png` and
`Assets/Graphic/UI/Icons/LoadButton_Pressed.png` are separate transparent 1254 × 1254
PNG sprites. They retain the normal buttons' layout and arrow meanings. Their Unity
import settings match the normal Save/Load sprites, including mipmaps and trilinear
filtering. Assign each pressed sprite to its corresponding UI Button's **Pressed
Sprite** under a **Sprite Swap** transition when wiring the buttons. No scene or
Button component was changed here.

Mode: built-in image editing with `transparent_background: true`. Save uses its normal
sprite as the edit target; Load uses its normal sprite as the edit target and the
pressed Save sprite as a style reference. No local pixel editing was used.

Pressed Save prompt:

> Edit this exact small-screen SAVE button sprite into its PRESSED state. Keep the 1254x1254 square canvas, transparent exterior, button outline, rounded-square silhouette, floppy-disk icon, downward arrow, geometry, spacing, scale, and palette family aligned with the reference so swapping states in a Unity Button does not jump. Darken the blush-pink face visibly but gently to a richer muted rose, reduce the bright top surface highlight, and give the inside edge a broad soft inset shadow so the button reads as physically depressed. Make the gold rim slightly less luminous, while retaining its clean bold shape. Keep the ivory disk-and-arrow symbol bright and readable at about 80x80 display pixels; no new details, hairlines, sparkles, text, checkerboard, detached shadow, or background. Preserve genuine transparent alpha outside the button. Output one PNG sprite.

Pressed Load prompt:

> Create one PRESSED-state LOAD button sprite. The FIRST image is the exact normal Load button whose rounded-square outline, geometry, ivory floppy disk, upward arrow, placement, scale, transparent canvas and centered composition must be retained. The SECOND image is the matching pressed Save button and defines the pressed-state treatment: a visibly darker muted-rose face, broad soft inset shadow, reduced top highlight, and slightly subdued gold rim. Apply that same pressed treatment to the FIRST image, preserving its upward arrow and all geometry so normal/pressed swapping in Unity does not jump. Keep bold, uncluttered details legible at about 80x80 display pixels. Same 1254x1254 square canvas, genuinely transparent exterior. No text, hairlines, extra decoration, detached shadow, checkerboard, or background.
