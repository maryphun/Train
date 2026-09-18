using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Train.Battle
{
    // This component is the only object added to Battle.unity. Its serialized setup is a
    // playable Inspector-editable example; BattleFlow.Enter can replace it at runtime.
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private BattleSetup standaloneSetup;

        private BattleSession session;
        private TMP_FontAsset font;
        private Image heroineImage;
        private Image monsterImage;
        private RectTransform energyFill;
        private Image cutInImage;
        private TMP_Text energyText;
        private TMP_Text turnsText;
        private TMP_Text damageText;
        private TMP_Text projectedStatusText;
        private TMP_Text logText;
        private GameObject resultOverlay;
        private TMP_Text resultTitle;
        private TMP_Text resultStatusText;
        private Button[] skillButtons;
        private Coroutine cutInRoutine;

        private static readonly Color Background = new Color(0.075f, 0.055f, 0.12f);
        private static readonly Color Panel = new Color(0.15f, 0.105f, 0.22f, 0.96f);
        private static readonly Color PanelLight = new Color(0.23f, 0.15f, 0.33f, 0.98f);
        private static readonly Color Accent = new Color(0.98f, 0.40f, 0.73f);
        private static readonly Color Muted = new Color(0.77f, 0.70f, 0.83f);

        private void Awake()
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/KiwiMaru-Regular SDF");
            if (font == null) font = TMP_Settings.defaultFontAsset;

            BattleSetup setup = BattleFlow.TryTakeSetup(out BattleSetup requested)
                ? requested
                : standaloneSetup;
            try
            {
                session = new BattleSession(setup);
                BuildInterface();
                RefreshInterface("Choose a monster skill. Each use consumes one turn.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Battle setup failed: {exception}");
                BuildErrorInterface(exception.Message);
            }
        }

        private void BuildInterface()
        {
            RectTransform canvas = CreateCanvas();
            MakePanel(canvas, "Background", Vector2.zero, Vector2.one, Background);

            RectTransform header = MakePanel(canvas, "Header", new Vector2(0, 0.87f),
                Vector2.one, Panel);
            MakeText(header, "Title", "BATTLE", 48, Accent, TextAlignmentOptions.Left,
                new Vector2(0.03f, 0.12f), new Vector2(0.52f, 0.90f));
            turnsText = MakeText(header, "Turns", "", 30, Color.white, TextAlignmentOptions.Right,
                new Vector2(0.53f, 0.12f), new Vector2(0.97f, 0.90f));

            RectTransform heroinePanel = MakePanel(canvas, "Magical Girl", new Vector2(0.035f, 0.29f),
                new Vector2(0.295f, 0.845f), Panel);
            MakeText(heroinePanel, "Name", session.Setup.heroine.displayName, 29, Color.white,
                TextAlignmentOptions.Center, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.99f));
            heroineImage = MakePortrait(heroinePanel, "Heroine Portrait",
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.86f));

            RectTransform monsterPanel = MakePanel(canvas, "Monster", new Vector2(0.705f, 0.29f),
                new Vector2(0.965f, 0.845f), Panel);
            MakeText(monsterPanel, "Name", session.Setup.monster.displayName, 29, Color.white,
                TextAlignmentOptions.Center, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.99f));
            monsterImage = MakePortrait(monsterPanel, "Monster Portrait",
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.86f));

            RectTransform statusPanel = MakePanel(canvas, "Battle Status", new Vector2(0.31f, 0.29f),
                new Vector2(0.69f, 0.845f), PanelLight);
            MakeText(statusPanel, "Energy Label", "ピュアプリエナジー", 27, Color.white,
                TextAlignmentOptions.Center, new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.98f));
            energyText = MakeText(statusPanel, "Energy Value", "", 40, Accent,
                TextAlignmentOptions.Center, new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.86f));
            RectTransform bar = MakePanel(statusPanel, "Energy Bar", new Vector2(0.08f, 0.68f),
                new Vector2(0.92f, 0.73f), new Color(0.07f, 0.045f, 0.10f));
            energyFill = MakePanel(bar, "Energy Fill", Vector2.zero, Vector2.one, Accent);
            damageText = MakeText(statusPanel, "Damage Totals", "", 24, Color.white,
                TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.65f));
            MakeText(statusPanel, "Status Heading", "Character status on victory", 22, Accent,
                TextAlignmentOptions.Left, new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.38f));
            projectedStatusText = MakeText(statusPanel, "Projected Changes", "", 21, Muted,
                TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.28f));

            RectTransform skillsPanel = MakePanel(canvas, "Monster Skills", new Vector2(0.035f, 0.035f),
                new Vector2(0.62f, 0.27f), Panel);
            MakeText(skillsPanel, "Skills Heading", "MONSTER SKILLS", 25, Accent,
                TextAlignmentOptions.Left, new Vector2(0.03f, 0.79f), new Vector2(0.97f, 0.98f));
            BuildSkillButtons(skillsPanel);

            RectTransform logPanel = MakePanel(canvas, "Battle Log", new Vector2(0.635f, 0.035f),
                new Vector2(0.965f, 0.27f), Panel);
            MakeText(logPanel, "Log Heading", "BATTLE LOG", 25, Accent,
                TextAlignmentOptions.Left, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.98f));
            logText = MakeText(logPanel, "Log Text", "", 22, Color.white,
                TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.77f));

            cutInImage = MakePortrait(canvas, "Skill Cut-in", new Vector2(0.35f, 0.32f),
                new Vector2(0.65f, 0.83f));
            cutInImage.gameObject.SetActive(false);

            BuildResultOverlay(canvas);
            heroineImage.sprite = session.HeroinePortrait;
            monsterImage.sprite = session.Setup.monster.portrait;
            heroineImage.enabled = heroineImage.sprite != null;
            monsterImage.enabled = monsterImage.sprite != null;
        }

        private void BuildSkillButtons(RectTransform parent)
        {
            int count = session.Setup.monsterSkills.Count;
            skillButtons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                int selectedIndex = i;
                BattleSkill skill = session.Setup.monsterSkills[i];
                float left = 0.03f + i * 0.94f / count;
                float right = 0.03f + (i + 1) * 0.94f / count - 0.012f;
                RectTransform rectangle = MakePanel(parent, skill.id, new Vector2(left, 0.10f),
                    new Vector2(right, 0.75f), PanelLight);
                Button button = rectangle.gameObject.AddComponent<Button>();
                button.targetGraphic = rectangle.GetComponent<Image>();
                button.onClick.AddListener(() => UseSkill(selectedIndex));
                skillButtons[i] = button;
                MakeText(rectangle, "Label", skill.displayName, 27, Color.white,
                    TextAlignmentOptions.Center, new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.95f));
                MakeText(rectangle, "Values", $"Energy -{skill.energyDamage}", 20, Muted,
                    TextAlignmentOptions.Center, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.49f));
            }
        }

        private void UseSkill(int index)
        {
            if (session.Outcome != BattleOutcome.InProgress) return;
            BattleTurnReport report = session.UseMonsterSkill(index);
            var message = new StringBuilder();
            message.Append(session.Setup.monster.displayName).Append(": ")
                .Append(report.skill.displayName).Append('\n')
                .Append("Energy -").Append(report.energyBefore - report.energyAfter);
            foreach (BattlePortraitStage stage in report.reachedStages)
            {
                message.Append('\n').Append(stage.label);
                if (stage.bonusTurns > 0) message.Append("  Turns +").Append(stage.bonusTurns);
            }

            RefreshInterface(message.ToString());
            if (report.skill.cutIn != null)
            {
                if (cutInRoutine != null) StopCoroutine(cutInRoutine);
                cutInRoutine = StartCoroutine(ShowCutIn(report.skill.cutIn));
            }

            if (session.Result != null) ShowResult();
        }

        private IEnumerator ShowCutIn(Sprite sprite)
        {
            cutInImage.sprite = sprite;
            cutInImage.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.8f);
            cutInImage.gameObject.SetActive(false);
            cutInRoutine = null;
        }

        private void RefreshInterface(string message)
        {
            energyText.text = $"{session.EnergyRemaining} / {session.Setup.startingEnergy}";
            energyFill.anchorMax = new Vector2(
                (float)session.EnergyRemaining / session.Setup.startingEnergy, 1f);
            turnsText.text = $"TURN {session.TurnsRemaining} REMAINING";
            damageText.text = $"攻撃ダメージ   {session.PhysicalDamage}\n" +
                              $"快楽ダメージ   {session.PleasureDamage}\n" +
                              $"混乱ダメージ   {session.ConfusionDamage}";
            heroineImage.sprite = session.HeroinePortrait;
            heroineImage.enabled = heroineImage.sprite != null;
            logText.text = message;

            var projection = new StringBuilder();
            foreach (BattleStatusChange change in session.PreviewStatusChanges(BattleOutcome.MonsterVictory))
            {
                if (projection.Length > 0) projection.Append('\n');
                projection.Append(change.displayName).Append("  ").Append(change.before)
                    .Append(" → ").Append(change.after)
                    .Append("  (").Append(change.delta >= 0 ? "+" : "")
                    .Append(change.delta).Append(')');
            }
            projectedStatusText.text = projection.Length > 0 ? projection.ToString() : "No status rules configured";
        }

        private void BuildResultOverlay(RectTransform canvas)
        {
            RectTransform shade = MakePanel(canvas, "Result Overlay", Vector2.zero, Vector2.one,
                new Color(0.025f, 0.015f, 0.045f, 0.91f));
            resultOverlay = shade.gameObject;
            RectTransform panel = MakePanel(shade, "Result Panel", new Vector2(0.19f, 0.12f),
                new Vector2(0.81f, 0.88f), PanelLight);
            resultTitle = MakeText(panel, "Result Title", "", 42, Accent, TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f));
            MakeText(panel, "Status Heading", "CHARACTER STATUS CHANGES", 28, Color.white,
                TextAlignmentOptions.Center, new Vector2(0.07f, 0.70f), new Vector2(0.93f, 0.81f));
            resultStatusText = MakeText(panel, "Status Changes", "", 29, Color.white,
                TextAlignmentOptions.TopLeft, new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.69f));
            RectTransform buttonRect = MakePanel(panel, "Return Button", new Vector2(0.33f, 0.07f),
                new Vector2(0.67f, 0.20f), Accent);
            Button returnButton = buttonRect.gameObject.AddComponent<Button>();
            returnButton.targetGraphic = buttonRect.GetComponent<Image>();
            returnButton.onClick.AddListener(() => BattleFlow.Complete(session.Result, session.Setup.returnSceneName));
            MakeText(buttonRect, "Label", "Return", 26, Background, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one);
            resultOverlay.SetActive(false);
        }

        private void ShowResult()
        {
            foreach (Button button in skillButtons) button.interactable = false;
            resultTitle.text = session.Result.MonsterWon ? "MONSTER VICTORY" : "TURN LIMIT REACHED";
            var text = new StringBuilder();
            foreach (BattleStatusChange change in session.Result.statusChanges)
            {
                text.Append(change.displayName).Append("    ").Append(change.before)
                    .Append(" → ").Append(change.after)
                    .Append("    (").Append(change.delta >= 0 ? "+" : "")
                    .Append(change.delta).Append(")\n");
            }
            if (text.Length == 0) text.Append("No status rules configured");
            text.Append("\nEnergy ").Append(session.Result.energyRemaining)
                .Append("   Turns used ").Append(session.Result.turnsUsed);
            resultStatusText.text = text.ToString();
            resultOverlay.SetActive(true);
        }

        private void BuildErrorInterface(string error)
        {
            RectTransform canvas = CreateCanvas();
            MakePanel(canvas, "Background", Vector2.zero, Vector2.one, Background);
            MakeText(canvas, "Setup Error", "Battle setup is missing or invalid:\n" + error +
                "\n\nEdit the Battle Scene Controller setup in Battle.unity, or call BattleFlow.Enter(setup).",
                32, Color.white, TextAlignmentOptions.Center,
                new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.80f));
        }

        private RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("Battle UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (EventSystem.current == null)
            {
                var eventObject = new GameObject("Battle EventSystem", typeof(EventSystem));
                eventObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            return rect;
        }

        private static RectTransform MakePanel(Transform parent, string name, Vector2 min, Vector2 max,
            Color color)
        {
            RectTransform rect = MakeRect(parent, name, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static Image MakePortrait(Transform parent, string name, Vector2 min, Vector2 max)
        {
            RectTransform rect = MakeRect(parent, name, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text MakeText(Transform parent, string name, string value, float size,
            Color color, TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            RectTransform rect = MakeRect(parent, name, min, max);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.text = value;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform MakeRect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var element = new GameObject(name, typeof(RectTransform));
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
