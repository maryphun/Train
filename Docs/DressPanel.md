# MainMenu dress selection

Open the panel with the existing Dress Button, which calls `DressPanel.DisplayUI(true)`.
Opening refreshes available IDs, positions the cards, and selects the equipped dress. Selecting
a different available card enables Change Dress without applying it yet. Change Dress updates
`PlayerProfile.TokaCurrentBody`, refreshes the MainMenu Character Image, and closes the panel.
The Exit button closes without applying a pending choice. There is no automatic disk save.

The integer IDs in `PlayerProfile.TokaAvailableBody` still refer to `Resources/TokaBodyList.spriteList`.
As the user requested, modals match that list by their assigned `dressSprite.name`, using an exact,
case-sensitive comparison. Preserve existing body-list index order and unique sprite names.
The current MainMenu has cards for indices 0, 1, 2 and 3; the default available IDs are 0, 2 and 3.
An available ID without a corresponding modal does not create a new card automatically.

Unavailable cards remain visible, with their buttons disabled, dress images hidden, and a question
mark shown. Available cards come first in the configured modal order, followed by locked cards.
The entire row is centered; it scrolls horizontally if it exceeds the viewport width. Existing
card sizes are preserved. The proposed 24-unit gap is an editable implementation default, not a
user-confirmed numeric requirement.

## Test

1. Start a new game from Title so the profile is initialized, then open the Dress Panel.
2. Expect three available choices and one question-mark card that cannot be selected.
3. Click a different available dress. The highlight moves and Change Dress becomes interactable,
   while the equipped body remains unchanged.
4. Click the equipped dress again: Change Dress becomes disabled. Select a different choice,
   then Exit and reopen: the equipped dress is selected again.
5. Select another available dress and press Change Dress. The panel closes, the Character Image
   changes, and reopening highlights the new dress. Save/load preserves the body selection and
   available-ID list through the existing save system.

## Customize

- Select Dress Panel and edit **Modal Gap** to change spacing. The panel uses its existing Scroll View.
- To add a card, duplicate a DressChoice modal, assign its `dressSprite` and `dressImage`, and add its
  Sprite to the end of TokaBodyList if needed. All modal components under the panel, including inactive
  ones, are discovered when it opens; the serialized modal array controls ordering first.
- The existing shared Selection GameObject follows the selected card. Separate frames per card are
  also supported. Highlight Graphics have raycasts disabled so they cannot intercept clicks.
- Assign a different **Unavailable Icon** to replace the question mark. The script creates a centered
  Image on each unavailable card; the original dress image and its authored placement are preserved.
- `PlayerProfile.TokaBodyChanged` refreshes MainMenu's assigned `tokaBodyImage` on changes. Save/load
  and new-game initialization also use the notifying body setter.

## Artwork and verification

`Assets/Graphic/UI/Icons/DressUnknown.png` is a 1254×1254 transparent PNG generated with the built-in
image-generation tool. Imported as a Single UI Sprite, centered pivot, Bilinear filtering, no mipmaps
or compression. Original generated alpha is preserved; corner alpha was verified as zero.

Generation prompt:

> Use case: stylized-concept. Asset type: Unity game UI icon for an unavailable dress choice. Generate
> one standalone question mark "?" centered on an actually transparent RGBA background. Reference:
> the project uses restrained navy/plum panels, warm gold rims, blush-pink accents and ivory highlights.
> Make a bold gently curved question mark with ivory face, thin warm gold outline, subtle blush inner
> shading, and a small separate round dot; polished hand-painted 2D game UI finish, crisp smooth
> silhouette legible at 100px. Square canvas, glyph occupies about 70% of height, generous clear padding.
> No card, no frame, no badge, no sparkles, no background color, no checkerboard painted into the image,
> no other text or objects.

Runtime/Editor compilation and 19 production-script interaction/layout checks using UI doubles
passed, including availability, name matching, locked placeholder, centered spacing, highlight,
confirmation/cancel/reopen, duplicate listeners, revoked unlocks and an empty list. Existing
save/load verification also passed headlessly. Live Unity Play Mode visual verification is pending.
