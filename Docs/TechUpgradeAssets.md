# TechUpgrade data and icons

Create a data asset from **Create > Scriptable Objects > TechUpgradeData**.

| Field | Use |
| --- | --- |
| `TechID` | Stable string ID for the upgrade. |
| `RequiredUpgrades` | Drag prerequisite `TechUpgradeData` assets here. The list represents all required upgrades; leave it empty for a starting node. |
| `RequiredResearchPoint` | Research-point cost, editable in the Inspector. |
| `TechNameID` | Localization key for the displayed name. |
| `TechDescriptionID` | Localization key for the description. |
| `TechSprite` | The upgrade's icon sprite. |

These are definitions, not player unlock state. The existing `TechUpgradeButton` still uses its own
serialized fields and `TechType`; the new class does not automatically replace its node configuration,
spend points, implement effects, or migrate saved technology unlocks. Avoid self-references and cycles
when defining prerequisites. No upgrade costs, effects, or localization entries were invented here.

## Starter icon pack

PNG sprites are in `Assets/Graphic/UI/TechUpgrade`. Their white/cyan/electric-blue palette is based on
the actual `btn_circle.png` and `window_01_01_frame.png` used by `TechUpgrade.unity`.
They have transparent backgrounds, no text, and no button frames baked into the artwork.

| File | Suggested visual meaning |
| --- | --- |
| `Tech_Research.png` | Research flask |
| `Tech_MonsterDevelopment.png` | DNA / monster development |
| `Tech_HireDiscount.png` | Monster recruitment cost reduction |
| `Tech_Funding.png` | Funding / income |
| `Tech_Equipment.png` | Laboratory equipment / efficiency |
| `Tech_Analysis.png` | Scientific analysis |

These are reusable art concepts, not approved gameplay upgrades. The research planning card only
proposes hiring-cost and money-related upgrades; most scene node names are placeholders.
The scene has not been assigned arbitrary icon-to-node meanings.

For a node, keep its existing circular background Image. Add a child UI Image, assign an icon as
its Source Image, center/stretch the child inside the node, enable Preserve Aspect, and disable
Raycast Target so hover input reaches the existing node. The transparent padding lets the original
cyan frame remain visible. `TechSprite` is also ready to assign on a data asset for later UI binding.

Import settings: Sprite (2D and UI), Single, Full Rect, bilinear filtering, alpha transparency,
no mipmaps, and no compression. Maximum imported size is 512; the generated source PNGs are retained.

## Generation prompts

Generated with the built-in image-generation tool, one call per icon. The common prompt was:

> Use case: ui-mockup. Asset type: ONE standalone square transparent PNG icon for a Unity sci-fi research-tree node, designed to sit inside an existing dark graphite circular button with thin cyan and electric-blue rims. Style: crisp vector-like game UI pictogram, solid icy-white and pale-cyan shapes, restrained bright-cyan and electric-blue edge accents, strong simple silhouette readable at 64 pixels. Match a clean dark futuristic laboratory interface. Center the single symbol with equal 15 percent clear padding on all sides. Genuine alpha transparency, no background panel, no badge, no circle frame, no text, no letters, no numbers, no watermark, no photographic texture, no cast shadow, no bloom outside the symbol. Square canvas.

Each prompt appended one primary request:

- Research: A bold Erlenmeyer research flask containing cyan liquid, one simple bubble and a clean white highlight. One flask only.
- Monster development: A stylized DNA double helix with two thick smoothly twisting icy-white/cyan strands and four simple connecting rungs. One DNA symbol only.
- Hire discount: One friendly horned monster head silhouette beside a single small coin and a clear downward arrow, tightly composed as one recruitment-cost-reduction symbol. Head uses geometric shapes and two small cyan eye cutouts, not a character portrait.
- Funding: Two simple short stacks of circular research-funding coins with one bold upward arrow. No currency letters or marks. Tightly composed single funding symbol.
- Equipment: A substantial six-tooth laboratory equipment cog with a bold lightning bolt cutout at its center. One clean equipment symbol only.
- Analysis: A bold magnifying glass enclosing a simple three-node molecular diagram. One clean scientific-analysis symbol only.
