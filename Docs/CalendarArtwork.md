# Calendar artwork

## Calendar-shaped page (revised request)

Use `Assets/Graphic/UI/CalendarPagePanel.png` for the clarified expanded-calendar design.
It is **1093×876**, matching the user's requested RectTransform. It has four gold binding rings,
an ivory blank page, pink backing and fine gold trim, with **no header band, title, grid or content**.
Exterior pixels are transparent. Use a UI Image with **Type: Simple**, **Preserve Aspect** enabled,
and a 1093×876 RectTransform. No scene assignment or click behavior was changed.

This replaces the plain-frame concept below as the intended design; the earlier asset is retained.
Generated using the built-in image tool at 1401×1123, cleaned with the previously authorized local
background-removal workflow, then resampled to exactly 1093×876. No rings were stretched from the
earlier square design: the composition was regenerated at the requested landscape proportions.

Final prompt: Adapt the calendar-like artwork to landscape 1093:876 proportions. Preserve the ivory
page, thin blush-pink backing, fine champagne-gold border and four small binding rings along the
top edge. Recompose for the wider layout rather than stretching the rings. Omit all header bands,
title compartments and dividers. Keep the interior completely blank for future functional UI:
no dates, grid, text, numbers, symbols, buttons, ornaments or watermark. Flat polished front-facing
2D artwork with restrained edge shading, minimal padding, the whole foreground intact, and
genuine alpha transparency outside the silhouette and between the protruding rings.

## Blank detailed-calendar panel

`Assets/Graphic/UI/CalendarDetailPanel.png` (1402×1122, approximately 5:4) is the blank panel requested
for the expanded calendar. The pale blush center is uninterrupted: no title, buttons, dates, grid,
or other content is baked in. The gold/ivory frame matches the calendar and time-of-day badge.
Assign it to a UI Image and use **Image Type: Sliced** to resize while preserving its corners;
the sprite has 48-pixel borders on each side. No scene or click behavior was changed for this artwork request.

Mode: built-in image generation, followed by the previously authorized local alpha cleanup for the
tiny exterior corner cutouts (foreground RGB unchanged). The supplied expanded-calendar screenshot
was the layout reference; `calendar.png` was the palette/style reference.

Prompt: Generate only a standalone blank 5:4 landscape Unity panel: near-uniform very pale blush-pink
center, restrained champagne-gold double rim, fine warm-brown outline, ivory edge highlights, tiny corners.
Match the screenshot's large panel and the existing calendar's soft polished 2D style. Maximize blank
content space. No title, back button, icons, rings, calendar grid, dates, cells, dividers, numbers, tabs,
ornaments, room background, or dark overlay. Tight frame with transparent outer corners.

## Blank detailed-calendar panel

`Assets/Graphic/UI/CalendarDetailPanel.png` (1402×1122, approximately 5:4) is the blank panel requested
for the expanded calendar. The pale blush center is uninterrupted: no title, buttons, dates, grid,
or other content is baked in. The gold/ivory frame matches the calendar and time-of-day badge.
Assign it to a UI Image and use **Image Type: Sliced** to resize while preserving its corners;
the sprite has 48-pixel borders on each side. No scene or click behavior was changed for this artwork request.

Mode: built-in image generation, followed by the previously authorized local alpha cleanup for the
tiny exterior corner cutouts (foreground RGB unchanged). The supplied expanded-calendar screenshot
was the layout reference; `calendar.png` was the palette/style reference.

Prompt: Generate only a standalone blank 5:4 landscape Unity panel: near-uniform very pale blush-pink
center, restrained champagne-gold double rim, fine warm-brown outline, ivory edge highlights, tiny corners.
Match the screenshot's large panel and the existing calendar's soft polished 2D style. Maximize blank
content space. No title, back button, icons, rings, calendar grid, dates, cells, dividers, numbers, tabs,
ornaments, room background, or dark overlay. Tight frame with transparent outer corners.

## Matching time-of-day badge

`Assets/Graphic/UI/TimeOfDayBadge.png` is the matching pink/gold badge for MainMenu's `Time` Image.
The image is 1955×447 with actual alpha transparency, bilinear filtering, and uncompressed Sprite import.
The scene retains its existing position, size, font, text, and component IDs. Only the background sprite/tint
and text color (dark warm brown `#55413F`) change; no time-progression logic was added or changed.

The built-in image generator supplied the artwork but baked in a checkerboard. On 2026-09-26 the user
explicitly authorized local processing to remove it. Local processing traced the gold silhouette, removed
the neutral exterior, filled enclosed regions, and retained fractional edge alpha. Foreground RGB values
were not repainted. The source image remains unchanged. A full-canvas transparent copy and the local
cleanup script are under ignored `Library/TimeBadgeWork`, along with a pre-edit scene backup.

Generation prompt: Create a blank, slim horizontal rounded-rectangle Unity time-of-day badge matching
the calendar's soft blush pink, ivory highlights, and fine champagne-gold double edge. Keep the center
empty for separately rendered Japanese text. No binder rings, symbols, letters, numbers, or scene mockup.
Use transparent surroundings, tight framing, and a roughly 4.15:1 silhouette for a 207×50 UI element.

Removal prompt: Remove the background from this image. Keep all foreground subjects unchanged and
fully intact, with clean, smooth edges. Make the background transparent. The foreground is the entire
pink-and-gold badge; the gray checkerboard is the background to remove.

## Calendar sprite

Asset: `Assets/Graphic/UI/calendar.png`.

Generated with the built-in image-generation tool. Reference 1 was the old blank calendar sprite;
reference 2 was the user's MainMenu screenshot, specifically its pink/gold lower-left button.
The new source is 1254×1254 with transparent corners. The existing sprite GUID and sliced-sprite
internal ID are retained, so existing scene references continue to point to it. The `DAY` and day-number
text are still drawn by the scene; they are not baked into the image.

## Prompt

Use case: precise-object-edit. Edit target: Image 1 is the existing blank Unity calendar sprite. Image 2 is ONLY a palette and UI-style reference: a softly colored anime bedroom screenshot, especially the bottom-left pale pink button with a fine gold border. Deliver only a replacement for Image 1, not a screenshot or mockup.
Redesign the blank calendar sprite to fit that gentle pink/cream/gold visual-novel UI. Front-facing square ring-bound calendar with softly rounded corners, a subdued blush-pink header band, warm ivory paper center, a fine champagne-gold double border with a very restrained warm charcoal contour. Four small understated warm-gold binder rings across the top. Flat polished 2D game UI artwork with very subtle depth, low visual noise, clean smooth antialiasing. Match the quiet rounded pink button, avoid saturated red or thick black outlines.
Preserve original composition and functional layout: square canvas, calendar fills almost the entire width and height with minimal transparent outer margin. Binding/header stays only in top 25 percent. Large completely blank pale ivory area from 28 percent down to 92 percent of image, particularly the center where the game draws DAY at y=37.5% and the day number at y=66%. No letters, numbers, grids, weekday labels, logos, watermark, floral art, hearts or ornate decorations. Actual transparent background outside the silhouette; no baked checkerboard or room background. Keep approximately original 512 by 512 aspect/layout so existing Unity text overlays align.
