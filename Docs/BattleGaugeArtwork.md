# Battle gauge artwork

## Confirmed design

The user requested heroine energy and enemy bars with separate fills, and confirmed
**horizontal gauges matching the current ivory, pink and gold UI**, **pink heroine energy**,
and **red enemy HP**. Both empty frames use the same matching artwork; the fills distinguish
the two gauges. No extra labels, icons or gameplay rules were introduced.

## Assets

| Gauge | Empty frame | Separate fill |
| --- | --- | --- |
| Heroine energy | `Assets/Graphic/UI/HeroineEnergyBar.png` | `Assets/Graphic/UI/HeroineEnergyFill.png` |
| Enemy HP | `Assets/Graphic/UI/EnemyHealthBar.png` | `Assets/Graphic/UI/EnemyHealthFill.png` |

All four files are 2172 × 724 RGBA PNGs, copied unchanged from built-in imagegen output.
The two frame files intentionally share identical pixels. No programmatic pixel editing was
used for these gauges. Each asset has its own GUID and named sprite.

Unity import uses **Sprite (2D and UI)**, **Multiple** with **one cropped sprite**, **Full Rect**,
centered pivot, zero borders, no mipmaps, no compression, bilinear filtering, Clamp wrapping,
and a 4096 maximum texture size to retain the source resolution. Native Sprite Editor rectangles
exclude unused transparent canvas margins without modifying the PNG. Bounds were measured from
the visible alpha silhouette, with a two-pixel antialias margin; both fills use one shared crop
rectangle. Frame sprite size is **2156 × 214**; fill sprite size is **2081 × 147**.
Generated alpha is preserved, including slight variation at the edges. The JSON companion
records GUIDs, sprite file IDs, rectangles and file hashes.

## Unity setup

1. Expand each PNG in the Project window and drag its named child sprite into a UI Image.
2. Use the empty frame as the background Image with **Image Type = Simple**. Keep its
   width/height proportional to the artwork.
3. Create a child Image for the separate fill, placed inside the recessed trough. Resize
   its RectTransform to fit the trough; the generated fill's full canvas is not the frame's
   inner dimensions. Keep its color white with full alpha.
4. Set the fill Image to **Filled → Horizontal → Origin Left**, with **Fill Amount = 1**
   for a full bar. Disable **Raycast Target** for decorative frame/fill Images.
5. Assign the pink Image to **BattleUIReferences → Energy Fill**, and the red Image to
   **Monster Health Fill**. The existing controller changes their `fillAmount` from the
   current battle energy and monster HP.

Scene placement and reference assignment are not part of this artwork task. Generated textures,
visible alpha bounds, palettes and importer metadata were checked; Unity scene playback was
not tested. The live Battle Trello card was refreshed and adds no artwork requirements.

## Exact generation prompts

Mode: **built-in imagegen**, `transparent_background = true` on all calls. The empty frame
was generated once and reused for both gauges. Pink fill edits that generated frame; red fill
edits the pink fill. No CLI/API fallback was used.

### Empty frame

```text
Use case: stylized-concept. Asset type: Unity RPG battle UI HEROINE ENERGY BAR FRAME, one standalone transparent PNG sprite. Create ONE very wide, slender horizontal rounded rectangular gauge frame, approximately eight times wider than tall. Match an elegant ivory, blush pink and polished gold UI: clean rounded gold outer rim, a narrow ivory/blush beveled surround, subtle raised gold highlights, and an EMPTY recessed pale ivory-pink inner trough with a soft inner shadow. The inner trough spans almost the entire length and is blank: no colored energy fill, no segmentation. Clear readable gold edge; restrained depth, not ornate. Straight-on orthographic 2D game sprite, centered, clean symmetrical rounded ends, no perspective. Frame is designed for a separate pink fill sprite to be placed over the recessed trough later. Tight framing around the long bar with minimal clear margins; horizontal landscape composition. Genuine alpha transparency outside the outer silhouette; the empty ivory trough is opaque, not a transparent hole. No characters, icons, hearts, stars, lettering, Japanese text, numbers, symbols, HUD labels, surrounding UI, scenery, watermark, checkerboard, external shadow or glow. Return only ONE empty frame.
```

### Heroine pink fill

```text
Use case: precise-object-edit. Edit target: the supplied horizontal ivory-and-gold empty gauge frame. Make the HEROINE ENERGY FILL as a separate standalone Unity UI sprite. Keep the exact source canvas dimensions and the inner trough's position, length, height and pill-shaped rounded ends. Replace only the inner trough with a fully filled opaque glossy rose-pink energy shape: medium saturated pink body, soft lighter pink highlight along its upper interior, subtle darker pink lower edge, polished but restrained. DELETE the ENTIRE gold rim, ivory/blush casing, frame, empty background and all shadows outside this pink shape; those regions must become actual alpha transparency. Deliver ONLY the pink fill pill occupying the original frame's inner trough, so it can be layered into that frame as an Image of Filled Horizontal type. No outline or enclosing border around the fill, no gold, no ivory casing, no meter frame. No text, labels, numbers, icons, hearts, stars, characters, scenery, checkerboard, watermark, external glow or shadow. Exactly one plain pink fill, filled to 100 percent, with full opaque interior and genuine transparency everywhere outside its silhouette. Preserve the source canvas; do not add another bar or a second sample.
```

### Enemy red fill

```text
Use case: precise-object-edit. Edit target: the supplied pink heroine energy fill sprite. Create its matching ENEMY HP FILL variant. Change ONLY the pink surface colors to a strong warm crimson-red body with a lighter red/soft coral highlight across the top and a deeper wine-red lower edge. Keep the highlight layout, polish, exact source canvas dimensions, position, outer silhouette, pill end geometry, and alpha transparency boundary identical to the pink source so both gauges use the same geometry. This is a filled-to-100-percent red pill shape without any surrounding frame. Preserve genuine transparency everywhere outside the red fill and an opaque red interior. No gold rim, no ivory casing, no enclosing meter, no text, labels, numbers, icons, hearts, stars, characters, scenery, watermark, checkerboard, external glow or shadow. Return exactly ONE separate red fill sprite on the unchanged source canvas.
```
