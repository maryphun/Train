# Dialogue character placement and motion

Updated 2026-10-06 following the user's confirmed behavior:

- Apply initial position, scale, and flip before a newly displayed character becomes visible.
- Ease all position changes of characters already visible over `0.25` seconds, including explicit
  `instant`, zero, omitted, and other supplied move durations.
- Place a newly displayed or hidden character at its intended starting position before revealing it.

## Default framing

The game's dialogue renderer and both dialogue scenes use the same defaults as the scenario editor:
character height `1.3` times the stage height and bottom offset `-600` reference pixels at 1080p.
The offset scales by `stageHeight / 1080`; negative values place the sprite's bottom below the screen.
Command scale multiplies the base height while retaining that bottom anchor. Ordinary sprites and
Toka's layered body/face use the same framing.

## First appearance

The converter combines immediate placement commands following `char show` into that show's initial
configuration. It considers consecutive commands before dialogue, choices, or other timing boundaries,
including commands split across spreadsheet rows. For example:

```text
[char:show:momoka:Ch_Momoka_TF_default:0.5:0.3:false]
[char:move:momoka:0.75:instant]
[char:scale:momoka:1.1:instant]
```

The character now starts its fade at `0.75` and scale `1.1`; it does not first fade at the default
center/scale and then snap to its placement. Flip setup is handled the same way. Other characters'
initial show/setup commands may be interleaved within the same block. The last immediate setup value
for each property is used, preserving the final configuration of the original command sequence.

Dialogue text, timed moves/scales, hide/face changes, branch indentation changes,
control flow, and unknown commands preserve initial-setup boundaries. Expressions are not folded. Known
stage effects and waits are coordinated as described below. If a placement
change follows initial placement as a separate move, its visible movement uses the runtime's fixed
`0.25`-second easing.

The runtime keeps newly created character objects inactive until their sprite or Toka body/face
layers, geometry, flip, scale, and initial position are configured. Initial scale is an optional
argument after display order:

```text
[char:show:ID:Sprite:X:Fade:Flip:Order:InitialScale]
```

`keep` in the optional order/scale slots preserves the existing value. The exporter supplies these
when extending older commands, so their ordering and retained scale remain compatible. It also adds
an internal `instant` placement marker after initial scale when an explicit instant move was folded.
Older generated commands containing that marker remain compatible, but the marker no longer bypasses
the fixed easing when the character is already visible.

## Existing characters

`char move` and `char position` use smoothstep ease-in/out over exactly `0.25` seconds for a visible
character, regardless of the supplied move duration. This includes `instant`, `none`, zero, omitted
durations, and longer positive values. An unchanged position adds no movement delay.

A visible character shown again at a different position also moves over `0.25` seconds, independently
of its show fade duration. This applies to instant shows and folded placement commands. Visual
replacements retain their existing fade-out/fade-in behavior; first appearances and hidden characters
being revealed start directly at their intended position.

The existing normalized position mapping, 250-reference-pixel overscan, flip semantics,
and profile-driven Toka layering are preserved.

## Speaker focus

When dialogue appears, the game fades the speaker to full authored color and other characters
to 40% brightness over `0.5` seconds. Narration fades everyone back to full authored color.
This focus multiplier applies equally to ordinary sprites and Toka's body/face layers, independently
of authored tint and opacity. The transition does not delay dialogue, movement, or other effects.
Rapid speaker changes start from the currently visible color; repeated lines from the same speaker
do not restart the fade. Removal and dialogue lifecycle cleanup cancel outstanding focus fades.

The current speaker always draws in front, including after new sprites or order commands.
Promotion sets its runtime order to `0` with the newest sequence, and that position is retained
after it stops speaking. Each subsequent speaker moves in front without restoring the previous
speaker's configured order. An explicit `char:order` can reposition a former speaker, while the
active speaker stays foremost. Narration retains the resulting stack. Source spreadsheet and Yarn
commands are unchanged; these focus changes apply only in the game, not the web preview.

## Coordinated presentation

The confirmed priority for characters in one command group is:

1. Fade out characters explicitly being removed.
2. Finish all position changes of retained characters, keeping those characters visible.
3. Fade in new characters.

This applies regardless of the order in which different characters' commands are written.
Unchanged retained characters are untouched by a background change. Timed removals finish before
the background fade starts; instant hides/clears run at the fade peak, in the first fully covered
midpoint frame when using opaque black.
The background holds its peak while retained movement finishes, then swaps/reveals before new
characters appear. Instant background changes remain instant and add no blackout.

| Command family | Start timing |
| --- | --- |
| BGM | Background fade peak; at the sprite swap for instant backgrounds; immediately without a background change |
| SE / shake | Immediately, overlapping presentation |
| Screen fade / dialogue UI / wait | After character presentation finishes |

Shakes retain their supplied duration and completion wait, with their offset restored on cleanup.
Music fade/crossfade durations and character fade durations remain unchanged. A missing background
does not discard the group's audio or character commands.

Both the Unity converter and web exporter coordinate consecutive literal commands before dialogue
or choices, including command-only spreadsheet rows. Intervening BGM, SE, shake, screen fade,
dialogue UI, and wait commands do not split the group or delay initial pose setup. Initial setup
is still folded before a new character becomes visible. Temporary show-then-hide effects for the
same ID, a second background change, branch indentation, control-flow commands, unknown commands,
and dynamic expressions preserve their sequence boundaries.

Author commands in the spreadsheet keep their existing syntax. Yarn contains an internal
`dialogue_stage` command whose UTF-8/Base64 payload avoids Yarn interpreting JSON braces as
expressions. The runtime identifies retained and new characters from the current scene, so it
also handles entry through different Nodes. A single standalone command keeps its usual handler.

## Verification

The current generated export was updated using the same preparation helper. It has 14 compiled Nodes
including Prologue, zero Yarn errors/warnings, and unchanged non-command content. Initial setup
consolidation reduces its command count from 62 to 34.

Runtime/Editor assemblies compiled with Unity 6000.3.15f1's Roslyn compiler. Headless checks passed:
16 export setup/boundary checks, 20 display/motion checks against the actual controller with UI/Yarn
doubles (including first-visible-frame geometry and layered Toka), plus 32 existing converter checks.
Harnesses are in ignored `Library/DialogueFlowVerification`. Unity Play Mode visual playback was not
tested through a live Editor connection.

The framing update additionally compiled both assemblies and passed 35 controller display/motion
checks, including default height/offset and equivalent vertical framing at 720p, 1080p, and 1440p
for ordinary sprites and layered Toka.

The fixed movement update compiled both assemblies and passed 127 controller display/motion checks.
Checks cover all supported duration cases, folded instant placement, independent short/long show fades,
unchanged positions, hidden-character reveal, ordinary sprites, and layered Toka.

The coordinated presentation update compiled both assemblies and passed 435 stage timing checks,
including reordered mixed commands, first-opaque-frame removal, music at the fade peak, immediate
SE/shake starts, post-presentation effects/waits, instant/missing backgrounds, and shake cleanup.
These use the production character, background, audio, and shake controllers with headless doubles;
screen fade/dialogue UI dispatch and audio output are observed with doubles. All 74 converter checks
and 36 relevant web tests passed, and the production web build succeeded. A mixed-command Yarn
fixture and the updated 14-Node current export compile with zero diagnostics. Both exporters match
for those scenarios, and current dialogue/choice/Node content is unchanged. Play Mode audiovisual
playback is still unverified.

The complete web suite currently reports 40 passes and 23 unrelated backend failures: the local
`apps-script/Code.gs` still exposes API v7 and lacks `instructionRule_`, while the instruction-style
tests expect v8. The animation update did not change that backend file. Relevant tests, Vue rendering,
and the production build pass.

The speaker-focus update compiled both assemblies and passed 56 checks using the production
controller with UI/Yarn doubles, covering fade timing, rapid speaker changes, concurrent tint/motion/
opacity, Toka layers, cleanup, persistent speaker promotion and later explicit order changes.
All 127 motion and 435 stage checks also passed. Unity Play Mode visual playback remains unverified.
