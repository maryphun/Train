# MainMenu tutorial presentation

Implementation: `Assets/Scripts/MainMenuTurorial.cs` (existing class/file spelling retained).

The education tutorial now derives from this presentation in `EducateTutorial.cs`, with a separate
copied UI hierarchy, four Tutorial.Teach steps, its own completion flag and no new-day handoff.
MainMenu's existing eight-step flow is unchanged. See `Docs/EducateTutorial.md`.

- Keeps the existing eight `Tutorial.MainMenu-1` through `Tutorial.MainMenu-8` localization keys,
  authored wording and ordering. No localization/source content is edited.
- All tutorial states accept a click immediately. During text reveal, the first click completes
  the current text and textbox sizing without changing steps. Once text is fully revealed,
  the next click advances. Entry and exit fades remain skippable. A frame guard prevents the
  same physical click being handled twice. This applies to all tutorials using this shared script.
- One owned DOTween Sequence is canceled before changing steps. Old text/layout callbacks cannot
  overwrite a later step or complete the tutorial again. Normal existing timing is retained:
  1-second entry/exit fades, 0.25-second textbox sizing, 2-second text reveal; they use unscaled time.
- Every step centers its textbox at parent center with a centered pivot, instead of moving it to
  individual highlighted objects.
- Textbox width/height are measured through TMP.GetPreferredValues using the full localized text.
  Short lines use a narrower box; long lines wrap within the configured screen-relative width and
  grow vertically. The box is sized before the typewriter reveal, not character-by-character.
  A reserved right-side gutter fits the next arrow plus a gap; text has balanced top/bottom
  padding without the earlier extra bottom band. The triangle is anchored near the bottom-right corner.
- No player-profile, Action-button or Action-board clones/highlights are created. The gray button
  pulse and requirement to press a copied Action button are removed. The narrator portrait remains.
- The protagonist is the only highlight. Default scope is **step 2**, preserving the original
  introduction highlight. The optional scope question was not answered during implementation;
  use **Highlight Protagonist Throughout** to keep that highlight across all eight text steps.
- Completion still marks Tutorials.MainMenu and calls MainMenuManager.StartNewDay(false) once.
  External disable cancels presentation/cleans the highlight without marking completion or advancing
  the day. Existing scene references and script GUID are preserved.

GoingOutTutorial and EndDayTutorial also reuse this presentation. The End tutorial overrides
the highlight target to show a non-interactive visual copy of End Today on End-3 only.
See `Docs/EndDayTutorial.md` for the Energy condition and sequence handoff.

## Inspector controls

Under **Centered text layout**:

- **Maximum Width Ratio**: initial implementation default 0.8 of the parent width.
- **Text Box Padding**: defaults to 80 horizontal / 50 vertical UI units, matching the original
  text inset totals. Arrow width and its gap are reserved separately on the right, not below text.
- **Next Arrow Text Gap**: editable implementation default 20 UI units between text and triangle.
- **Next Arrow Corner Inset**: editable default 12x12 UI units from the box's right/bottom edges.
  Arrow positioning preserves its Inspector pivot (Y = 0.5) and accounts for that pivot when
  calculating the visible edge insets. The script no longer changes the arrow pivot.
- **Minimum Text Box Height**: default 100, matching the previous minimum height.
- **Highlight Protagonist Throughout**: false = introduction step only; true = all text steps.

These are editable layout defaults, not additional user-confirmed numeric requirements. Text font,
styling, tutorial art, narration and other tutorial classes are unchanged. The old serialized fields
for removed object/button highlights may remain in scene YAML until Unity next saves it; they are
unused and no scene rewrite was needed.

## Verification

Runtime and Editor source assemblies compile with Unity 6000.3.15f1's bundled Roslyn compiler
(existing unrelated warnings remain). Forty-seven headless checks against the production tutorial
with UI/TMP-measurement/tween doubles pass: all-step skipping/centering, text-size growth/wrapping,
right-side arrow clearance and vertically balanced padding, protagonist-only intro/throughout modes,
disabled highlight input, rapid-click guards, cancellation
of old sequences, completion exactly once, cleanup, restart and the unchanged new-day handoff.

Actual font glyph metrics, final visual arrangement and Play Mode playback remain unverified
because Unity MCP is not exposed. Checks: ignored `Library/MainMenuTutorialLayoutVerification`.
