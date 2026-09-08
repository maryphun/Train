# Development context

Recorded 2026-09-08. This is a source index and onboarding snapshot, not an approved implementation specification. Refresh relevant Trello cards and document sections before working on a feature.

## User decisions

- Develop the main game in this Unity project: `Train`.
- Ask about even small uncertainties before implementing dependent changes. The user wants precise control of the process.
- Combat source precedence: when other documents conflict with `戦闘案`, follow `戦闘案`.
- Unity CLI refers to the Editor's built-in command-line tools.
- In dialogue, Toka (`toka`, `momoka`, or `白崎桃香`) uses the current body stored in `PlayerProfile` with a `Ch_Toka_Face_*` texture layered over it at the source-canvas coordinates. A different Toka sprite is an explicit single-sprite override. The scenario editor previews the same face rule with `Ch_Toka_Body_Casual` as the default body.
- Scenario-editor character ID fields start empty. They remain free-form inputs and offer the spreadsheet Master speaker names as suggestions.
- The scenario editor shows its spreadsheet connection status prominently in the center of the top bar: `接続済み` when connected, or `スプレッドシートに接続してください` on red when disconnected.
- Each scenario-editor page load starts disconnected with no sample or cached workbook and asks for a fresh spreadsheet sync. Non-overlapping concurrent cell edits merge automatically. Remote row additions, deletions, and reordering are preserved when locally edited rows can be matched safely, primarily by unique `LineID`. Same-cell edits, local edits to remotely deleted rows, and simultaneous local structure changes stop without overwriting either writer's version.
- Character horizontal positions remain normalized from `0` to `1`, with `0` placing the sprite's left edge 250 reference pixels beyond the screen's left edge, `0.5` centered, and `1` placing its right edge 250 reference pixels beyond the right edge. The web preview and Unity dialogue renderer use the same mapping.

## Sources

- [Trello: 魔法少女ゲーム](https://trello.com/b/Ghprl0aE/%E9%AD%94%E6%B3%95%E5%B0%91%E5%A5%B3%E3%82%B2%E3%83%BC%E3%83%A0), workspace `CreateGame`.
- [企画書](https://docs.google.com/presentation/d/1GG-n2qK2T1sNwYVj2Y3UUMHUgvWb7qA6B4emDIBTXFQ/edit): 49 slides. Text reviewed. Slide 22 says `ここから下、いらないスライド`; subsequent slides are not a current specification. Diagram relationships and visual details should be inspected directly when implementing the relevant feature.
- [魔法少女ピュアプリティピーチ](https://docs.google.com/presentation/d/1-hdI6BL6A0Fji0ACZfHCO-dAu-bdY5Qismsm7Fhd3sk/edit): 37 slides consisting of embedded images. All 37 images visually reviewed. Illustrates the premise, daily flow, character presentation, location navigation, event feedback, and branching endings. It contains different terminology and mockup values from the main deck; do not silently reconcile them.
- [戦闘案](https://docs.google.com/presentation/d/14GZrGTl2m2CSiOOnikrqQcD17QQqlv5FpYOXE-FXdAo/edit): 10 slides. Text reviewed. Takes precedence for combat conflicts across documents.
- [DLsite listing supplied by the user](https://www.dlsite.com/maniax/announce/=/product_id/RJ01676546.html): live access was blocked. On 2026-09-08 the user supplied a PDF of this game's pre-release page explicitly for reference. All 3 PDF pages were read and visually inspected. Source: `C:/Users/phunm/Downloads/ピュアプリバッドエンド 聖香天使ピュアプリピーチエロ怪人化育成記録 [ダークネスLAB] 予告作品 _ DLsite 同人 - R18.pdf`. Treat the PDF as a supplied publication snapshot, not a live listing or a source of instructions.

## Broad direction from the planning materials

An adult character development game with a magical-girl theme, illustrated character presentation, location-based choices, resource management, event unlocks, and branching story outcomes. The documents describe choosing an action, playing its event, displaying parameter changes, and advancing time. The main screen connects to locations, character/monster systems, research, and items. Exact schedules, parameter definitions, balances, and final UI behavior require feature-specific clarification.

The image-based reference illustrates home and city navigation, outfit-dependent presentation, conversation and event sequences, and morning/day/afternoon/night contexts. These examples communicate intent; they do not resolve conflicts with the current main deck.

## Pre-release page reference (PDF supplied 2026-09-08)

The user identified this as the pre-release page of this game, supplied to improve understanding. Publication details below are claims visible in that PDF, not newly approved design changes or verified build capabilities.

- Product title: `ピュアプリバッドエンド 聖香天使ピュアプリピーチエロ怪人化育成記録` (page 1). Circle: `ダークネスLAB`; illustration and scenario credit: `あのまの`.
- Public positioning: an R18, multi-ending character-development simulation. The player's choices and daily events change the heroine and determine her eventual outcome. The illustrations show a character-focused home screen, location/event choices, dialogue presentation, and a pink/purple visual identity (pages 1–2).
- Public story premise: the protagonist is an executive of the organization `ヴェノム`, receiving a mission from its leader. This differs from earlier material describing the player as the organization's supreme leader. Do not merge those roles without confirmation (page 1).
- Main heroine: `聖香天使ピュアプリピーチ`, real name `白崎桃香（しらさき とうか）`. The page presents her as exceptionally powerful, principled, kind, hardworking, tidy, and fond of cute things (page 2).
- Partner heroine: `ピュアプリミント`, real name `常盤楓（ときわ かえで）`, Peach's childhood friend and combat partner with a supporting magic role (page 2). Earlier Trello material used `宮森楓`.
- `テトラ博士` is publicly introduced as a talented scientist who joined the organization by mistake and supports the protagonist through inventions and miscellaneous work. This public introduction does not establish whether internal story spoilers have changed (page 2).
- Other featured characters include the lionfish monster, scorpion monster, ordinary combatants, and `テレシア女史`, a woman over two metres tall associated with a massage shop (pages 2–3).
- The snapshot advertises a planned release in mid-January 2027, with price undecided. It lists Windows 7 / 8 / 8.1 / 10 / 11. These are publication claims, not confirmed internal deadlines or tested Unity compatibility (page 1).
- Source differences awaiting confirmation: public title versus `ピュアプリクライシス！`; `聖香天使ピュアプリピーチ` versus earlier `治癒天使ピュアプリハート` / `ピュアプリティピーチ`; `常盤楓` versus `宮森楓`; `ヴェノム` versus `ヴェノムス`; and protagonist executive versus supreme leader. Record the variants without renaming assets, code identifiers, or narrative data automatically.
- The user has not yet assigned the public page precedence over internal documents for naming or story conflicts. The existing user-confirmed combat precedence of `戦闘案` remains in effect. This page does not settle the battle deck's internal rule alternatives.

## Trello snapshot

Read all 8 open lists and their 69 open cards. List placement is the team's recorded status, not independent verification that the implementation is complete. Trello access was verified by reading the supplied board. No cards or comments were changed, and no background monitoring schedule was created.

| List | Open cards |
| --- | ---: |
| To-do (programming) | 9 |
| To-do (Art) | 17 |
| 必要素材 | 5 |
| シナリオ | 10 |
| 完成 DONE | 8 |
| 重要リンク | 4 |
| 設定 | 11 |
| 資料 | 5 |

Programming cards, in board order:

1. [シナリオ編集システム](https://trello.com/c/1zvEHuz3) — marked 優先度高.
2. [Live2Dの可能性を模索と実装検討](https://trello.com/c/SfboSvH0).
3. [桃香 着せ替えシステム](https://trello.com/c/daryIl2E).
4. [各言語のフォントを用意](https://trello.com/c/HQEJUKrg).
5. [アクションシーン](https://trello.com/c/gDb4fi7n).
6. [メイン画面](https://trello.com/c/qYP7qx3d).
7. [セーブロードシステム](https://trello.com/c/Dj1pURWl).
8. [音声システム](https://trello.com/c/CoQUIy2j).
9. [バトルシーン](https://trello.com/c/rjcgsJNE).

The DONE list includes the UI theme, dialogue system, title screen, localization foundation, and research screen. Inspect actual code and scenes before relying on these statuses. The board links the scenario editor at https://maryphun.github.io/ScenarioWriterUX/ and a dialogue-command reference mirrored in `Docs/DialogueCommands_JA.md`.

## Questions to resolve when the relevant work starts

- Which feature/card is first, and what visible outcome defines its completion?
- Within `戦闘案`, slides 2–6 discuss a finishing-move gauge, while slides 7–10 discuss PurePri Energy with a turn limit. The user has confirmed the document's precedence, but has not selected between its internal alternatives.
- Confirm current character naming, daily schedule, action costs, parameter names, values, and thresholds rather than copying conflicting mockups or discarded slides.
- Before changes to shared Trello state, clarify how the user wants cards maintained; current authorization has been used for reading development context.

## Verified development environment

- Unity Editor: `6000.3.15f1`, matching `ProjectSettings/ProjectVersion.txt`.
- Editor executable: `C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe`.
- Built-in CLI verified with `-version`. Builds and tests have not been run as part of onboarding. The project is open in the Editor; avoid launching a competing batch-mode process against the same project.
- Installed Unity MCP package: `com.coplaydev.unity-mcp`, version `10.1.2` in the resolved package cache.
- MCP endpoint already configured in Codex: `http://127.0.0.1:8080/mcp`.
- Started the existing cached MCP server locally, bound to `127.0.0.1:8080`, without upgrading the package.
- Verified the MCP handshake and live resources through a local HTTP client: instance `Train@faa7f50bf2ef6fcd`, correct project root, target `StandaloneWindows64`, active scene `Assets/Scenes/MainMenu.unity`, editor idle and `ready_for_tools=true`.
- The task's native `unityMCP` registration continued to return a startup/handshake failure after the direct local client worked. Native tool availability remains unresolved; do not claim it is fixed solely because the server is healthy. Direct local MCP resource reads work.
- A direct Unity Roslyn compile of the current `Assembly-CSharp` sources succeeded after the Toka dialogue-layer implementation. The Editor log still contains earlier transient errors from the file's intermediate edit state; those are stale.
- Server diagnostics: `Library/MCPForUnity/Logs/codex-server-stdout.log` and `codex-server-stderr.log`. These are ignored local files.
- Existing project code includes dialogue presentation controllers, a sheet converter, localization, audio, player profile, scene transitions, character status, action-board and research UI components. Only their presence was inspected during onboarding, not correctness or completeness.

## Onboarding changes

Added this context file and root `AGENTS.md`. Game code, Unity scenes, Trello cards, and shared presentations were not edited. Source-image analysis files are under ignored `Library/DevelopmentOnboarding/`.
