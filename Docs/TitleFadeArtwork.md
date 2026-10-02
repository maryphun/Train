# Title horizontal fade artwork

Asset: `Assets/Graphic/UI/TitleWhiteHorizontalFade.png`.

On 2026-10-03 the user requested the existing white horizontal title strip be recolored
to match `Assets/Graphic/UI/UI_Button.png`, with the same gold-and-ivory border on its
top and bottom. They confirmed that the border should fade to transparency at both
ends with the strip. `UI_Button.png` superseded an earlier TimeOfDayBadge reference.

The final PNG remains 1024 × 128. Its horizontal alpha curve is copied exactly from
the original strip. For each output row, the color and rim alpha come from the center
column of `UI_Button.png`, mapped over the strip's height. Output alpha is the product
of the original fade alpha and the button-row alpha. This reproduces the button's
pink, ivory/gold trim and warm contour without adding side borders or a vertical fade
through the main fill. The center pink samples to RGB 249, 174, 206. The original
image and importer were backed up under ignored `Library/TitleFadeWork`.

The Unity sprite GUID and 9-slice border values are unchanged. Its filter mode is now
Bilinear because the three MainMenu `TitleBack` Images display the 1024 × 128 source
at approximately 595 × 74. No scene objects, UI text, or layout changed.

The built-in image editor was tried first with the original strip as the edit target
and `UI_Button.png` as the style reference. It changed the 8:1 shape and added side
borders, so that output was rejected. The user then explicitly authorized local image
processing. The delivered PNG was produced locally from the two existing source
images; no generated-image prompt was used for the final asset. A 595 × 74 preview
was inspected before replacement.
