using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Assets.SimpleLocalization.Scripts;
using DG.Tweening;

namespace Train.Battle
{
    // Uses the scene's assigned UI, or builds the prototype UI when no scene UI is assigned.
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private BattleSetup standaloneSetup;
        [SerializeField] private BattleUIReferences sceneUI;

        [Header("Action presentation")]
        [Tooltip("Additional wait after the heroine's cut-in, or after her action event when there is no cut-in.")]
        [Min(0)] [SerializeField] private float heroineActionDelay;
        [Tooltip("Additional wait after the monster's cut-in, or after its response event when there is no cut-in.")]
        [Min(0)] [SerializeField] private float monsterActionDelay;
        [Tooltip("Duration of the fallback Cut In Image. SpellAnimation uses its own completion time.")]
        [Min(0)] [SerializeField] private float cutInDuration = 0.8f;
        [SerializeField] private UnityEvent onHeroineActionStarted = new UnityEvent();
        [SerializeField] private UnityEvent onMonsterActionStarted = new UnityEvent();

        [Header("Gauge animation")]
        [Tooltip("Seconds to ease changed gauge values. Initial values appear instantly; zero disables easing.")]
        [Min(0)] [SerializeField] private float gaugeAnimationDuration = 0.5f;
        [SerializeField] private Ease gaugeEase = Ease.OutCubic;

        public BattleTurnReport CurrentTurnReport { get; private set; }

        private BattleSession session;
        private TMP_FontAsset font;
        private Image backgroundImage;
        private Image heroineImage;
        private Image monsterImage;
        private RectTransform energyFill;
        private RectTransform monsterHealthFill;
        private Image sceneEnergyFill;
        private Image sceneMonsterHealthFill;
        private Image cutInImage;
        private SpellAnimation spellAnimation;
        private TMP_Text energyText;
        private TMP_Text monsterHealthText;
        private TMP_Text monsterIntentText;
        private TMP_Text turnsText;
        private TMP_Text damageText;
        private TMP_Text projectedStatusText;
        private TMP_Text logText;
        private GameObject resultOverlay;
        private TMP_Text resultTitle;
        private TMP_Text resultStatusText;
        private Button returnButton;
        private GameObject actionPopup;
        private Button[] skillButtons;
        private bool resolvingTurn;
        private Tween energyGaugeTween;
        private Tween monsterHealthGaugeTween;
        private float energyGaugeTarget;
        private float monsterHealthGaugeTarget;
        private bool gaugesInitialized;

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
                if (sceneUI != null)
                    BindSceneInterface();
                else
                    BuildInterface();
                ApplyBackground();
                RefreshInterface($"{session.Setup.heroine.displayName}の行動を選んでください。\n行動後、怪人が自動で反撃します。");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Battle setup failed: {exception}");
                BuildErrorInterface(exception.Message);
            }
        }

        private void BindSceneInterface()
        {
            sceneUI.ValidateReferences();
            EnsureEventSystem();
            backgroundImage = sceneUI.backgroundImage;
            heroineImage = sceneUI.heroineImage;
            monsterImage = sceneUI.monsterImage;
            sceneEnergyFill = sceneUI.energyFill;
            sceneMonsterHealthFill = sceneUI.monsterHealthFill;
            energyText = sceneUI.energyText;
            monsterHealthText = sceneUI.monsterHealthText;
            monsterIntentText = sceneUI.monsterIntentText;
            turnsText = sceneUI.turnsText;
            damageText = sceneUI.damageText;
            projectedStatusText = sceneUI.projectedStatusText;
            logText = sceneUI.logText;
            cutInImage = sceneUI.cutInImage;
            spellAnimation = sceneUI.spellAnimation;
            resultOverlay = sceneUI.resultOverlay;
            resultTitle = sceneUI.resultTitle;
            resultStatusText = sceneUI.resultStatusText;
            returnButton = sceneUI.returnButton;
            actionPopup = sceneUI.actionPopup;

            SetText(sceneUI.heroineNameText, session.Setup.heroine.displayName);
            SetText(sceneUI.monsterNameText, session.Setup.monster.displayName);
            SetText(sceneUI.skillsHeadingText, session.Setup.heroine.displayName + "の行動");
            if (monsterImage != null)
            {
                monsterImage.sprite = session.Setup.monster.portrait;
                monsterImage.enabled = monsterImage.sprite != null;
            }
            if (cutInImage != null) cutInImage.gameObject.SetActive(false);
            if (spellAnimation != null) spellAnimation.EndAnimation();
            SetPopupVisible(resultOverlay, false);
            SetPopupVisible(actionPopup, false);
            returnButton.onClick.AddListener(ReturnFromBattle);

            int count = session.Setup.heroineSkills.Count;
            skillButtons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                int selectedIndex = i;
                BattleSkillButtonUI skillUI = Instantiate(sceneUI.skillButtonTemplate,
                    sceneUI.skillButtonParent, false);
                skillUI.Bind(session.Setup.heroineSkills[i], () => UseSkill(selectedIndex));
                skillUI.gameObject.SetActive(true);
                skillButtons[i] = skillUI.Button;
            }
        }

        private void ApplyBackground()
        {
            backgroundImage.sprite = session.Setup.background;
            backgroundImage.overrideSprite = null;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = session.Setup.background != null ? Color.white : Color.black;
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = false;
        }

        private void BuildInterface()
        {
            RectTransform canvas = CreateCanvas();
            backgroundImage = MakePanel(canvas, "Background", Vector2.zero, Vector2.one,
                Color.black).GetComponent<Image>();

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
                new Vector2(0.04f, 0.23f), new Vector2(0.96f, 0.86f));
            monsterIntentText = MakeText(monsterPanel, "Monster Intent", "", 20, Muted,
                TextAlignmentOptions.Center, new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.23f));
            monsterHealthText = MakeText(monsterPanel, "Monster Health", "", 24, Color.white,
                TextAlignmentOptions.Center, new Vector2(0.04f, 0.075f), new Vector2(0.96f, 0.16f));
            RectTransform healthBar = MakePanel(monsterPanel, "Monster Health Bar",
                new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.07f), Background);
            monsterHealthFill = MakePanel(healthBar, "Monster Health Fill", Vector2.zero,
                Vector2.one, new Color(0.95f, 0.58f, 0.30f));

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
            MakeText(statusPanel, "Status Heading", "勝利時のステータス変化（予測）", 22, Accent,
                TextAlignmentOptions.Left, new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.38f));
            projectedStatusText = MakeText(statusPanel, "Projected Changes", "", 21, Muted,
                TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.28f));

            RectTransform skillsPanel = MakePanel(canvas, "Heroine Skills", new Vector2(0.035f, 0.035f),
                new Vector2(0.62f, 0.27f), Panel);
            actionPopup = skillsPanel.gameObject;
            MakeText(skillsPanel, "Skills Heading", session.Setup.heroine.displayName + "の行動", 25, Accent,
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
            int count = session.Setup.heroineSkills.Count;
            skillButtons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                int selectedIndex = i;
                BattleHeroineSkill skill = session.Setup.heroineSkills[i];
                float left = 0.03f + i * 0.94f / count;
                float right = 0.03f + (i + 1) * 0.94f / count - 0.012f;
                RectTransform rectangle = MakePanel(parent, skill.id, new Vector2(left, 0.10f),
                    new Vector2(right, 0.75f), PanelLight);
                Button button = rectangle.gameObject.AddComponent<Button>();
                button.targetGraphic = rectangle.GetComponent<Image>();
                button.onClick.AddListener(() => UseSkill(selectedIndex));
                skillButtons[i] = button;
                MakeText(rectangle, "Label", skill.displayName, 27, Color.white,
                    TextAlignmentOptions.Center, new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.98f));
                var values = new StringBuilder();
                if (skill.monsterDamage > 0) values.Append($"体力 -{skill.monsterDamage}\n");
                if (skill.energyCost > 0) values.Append($"エナジー -{skill.energyCost}\n");
                if (skill.energyRecovery > 0) values.Append($"エナジー +{skill.energyRecovery}\n");
                if (skill.incomingDamageMultiplier < 1f)
                    values.Append($"被ダメージ ×{skill.incomingDamageMultiplier:0.##}");
                MakeText(rectangle, "Values", values.ToString().TrimEnd(), 19, Muted,
                    TextAlignmentOptions.Center, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.60f));
            }
        }

        private void UseSkill(int index)
        {
            if (resolvingTurn || !session.CanUseHeroineSkill(index)) return;
            BattleTurnReport report = session.UseHeroineSkill(index);
            CurrentTurnReport = report;
            resolvingTurn = true;
            var message = new StringBuilder();
            message.Append(session.Setup.heroine.displayName).Append(": ")
                .Append(report.heroineSkill.displayName);
            if (report.monsterHealthBefore != report.monsterHealthAfter)
                message.Append("  体力 -").Append(report.monsterHealthBefore - report.monsterHealthAfter);
            if (report.energyRecovered > 0) message.Append("  エナジー +").Append(report.energyRecovered);
            if (report.monsterSkill != null)
                message.Append('\n').Append(session.Setup.monster.displayName).Append(": ")
                    .Append(report.monsterSkill.displayName).Append("  エナジー -")
                    .Append(report.incomingEnergyDamage);
            foreach (BattlePortraitStage stage in report.reachedStages)
            {
                message.Append('\n').Append(stage.label);
                if (stage.bonusTurns > 0) message.Append("  残された時間 +").Append(stage.bonusTurns);
            }

            RefreshInterface(message.ToString());
            StartCoroutine(ShowTurn(report, message.ToString()));
        }

        private IEnumerator ShowTurn(BattleTurnReport report, string message)
        {
            try
            {
                onHeroineActionStarted?.Invoke();
                yield return ShowAbility(report.heroineSkill.displayName,
                    report.heroineSkill.cutIn, report.heroineSkill.animationDirection);
                if (heroineActionDelay > 0f) yield return new WaitForSecondsRealtime(heroineActionDelay);
                if (report.monsterSkill != null)
                {
                    onMonsterActionStarted?.Invoke();
                    yield return ShowAbility(report.monsterSkill.displayName,
                        report.monsterSkill.cutIn, report.monsterSkill.animationDirection);
                    if (monsterActionDelay > 0f) yield return new WaitForSecondsRealtime(monsterActionDelay);
                }
            }
            finally
            {
                resolvingTurn = false;
                RefreshInterface(message);
                if (session.Result != null) ShowResult();
            }
        }

        private IEnumerator ShowAbility(string abilityName, Sprite sprite, BattleAnimationDirection direction)
        {
            if (sprite == null) yield break;
            if (spellAnimation != null)
            {
                spellAnimation.InitAnimation(abilityName, sprite, direction);
                while (spellAnimation != null && spellAnimation.IsPlaying) yield return null;
                yield break;
            }

            if (cutInImage == null) yield break;
            cutInImage.sprite = sprite;
            cutInImage.gameObject.SetActive(true);
            if (cutInDuration > 0f) yield return new WaitForSecondsRealtime(cutInDuration);
            cutInImage.gameObject.SetActive(false);
        }

        private void RefreshInterface(string message)
        {
            SetText(energyText, $"{session.EnergyRemaining} / {session.Setup.startingEnergy}");
            RefreshGauges();
            UpdateTurnText();
            SetText(monsterHealthText, $"体力  {session.MonsterHealthRemaining} / {session.Setup.startingMonsterHealth}");
            SetText(monsterIntentText, session.NextMonsterSkill != null
                ? "次の行動: " + session.NextMonsterSkill.displayName : "");
            for (int i = 0; i < skillButtons.Length; i++)
                skillButtons[i].interactable = !resolvingTurn && session.CanUseHeroineSkill(i);
            SetText(damageText, $"攻撃ダメージ   {session.PhysicalDamage}\n" +
                              $"快楽ダメージ   {session.PleasureDamage}\n" +
                              $"混乱ダメージ   {session.ConfusionDamage}");
            if (heroineImage != null)
            {
                heroineImage.sprite = session.HeroinePortrait;
                heroineImage.enabled = heroineImage.sprite != null;
            }
            SetText(logText, message);
            if (actionPopup != null)
                SetPopupVisible(actionPopup, !resolvingTurn && session.Outcome == BattleOutcome.InProgress);

            var projection = new StringBuilder();
            foreach (BattleStatusChange change in session.PreviewStatusChanges(BattleOutcome.HeroineVictory))
            {
                if (projection.Length > 0) projection.Append('\n');
                projection.Append(change.displayName).Append("  ").Append(change.before)
                    .Append(" → ").Append(change.after)
                    .Append("  (").Append(change.delta >= 0 ? "+" : "")
                    .Append(change.delta).Append(')');
            }
            SetText(projectedStatusText, projection.Length > 0 ? projection.ToString() : "No status rules configured");
        }

        private void RefreshGauges()
        {
            UpdateGauge(sceneEnergyFill, energyFill,
                (float)session.EnergyRemaining / session.Setup.startingEnergy,
                ref energyGaugeTween, ref energyGaugeTarget);
            UpdateGauge(sceneMonsterHealthFill, monsterHealthFill,
                (float)session.MonsterHealthRemaining / session.Setup.startingMonsterHealth,
                ref monsterHealthGaugeTween, ref monsterHealthGaugeTarget);
            gaugesInitialized = true;
        }

        private void UpdateGauge(Image fill, RectTransform prototypeFill, float target,
            ref Tween tween, ref float previousTarget)
        {
            if (gaugesInitialized && Mathf.Approximately(previousTarget, target)) return;
            tween?.Kill();
            tween = null;
            previousTarget = target;

            if (!gaugesInitialized || gaugeAnimationDuration <= 0f)
            {
                SetGaugeValue(fill, prototypeFill, target);
                return;
            }
            if (fill == null && prototypeFill == null) return;

            float displayed = fill != null ? fill.fillAmount : prototypeFill.anchorMax.x;
            tween = DOTween.To(() => displayed, value =>
                {
                    displayed = value;
                    SetGaugeValue(fill, prototypeFill, value);
                }, target, gaugeAnimationDuration)
                .SetEase(gaugeEase)
                .SetUpdate(true);
        }

        private static void SetGaugeValue(Image fill, RectTransform prototypeFill, float value)
        {
            if (fill != null) fill.fillAmount = value;
            if (prototypeFill != null) prototypeFill.anchorMax = new Vector2(value, 1f);
        }

        private void CancelGaugeAnimations()
        {
            energyGaugeTween?.Kill();
            monsterHealthGaugeTween?.Kill();
            energyGaugeTween = null;
            monsterHealthGaugeTween = null;
            gaugesInitialized = false;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private void OnEnable()
        {
            LocalizationManager.OnLocalizationChanged += UpdateTurnText;
            UpdateTurnText();
            if (session != null && !gaugesInitialized) RefreshGauges();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLocalizationChanged -= UpdateTurnText;
            CancelGaugeAnimations();
        }

        private void UpdateTurnText()
        {
            if (session != null && turnsText != null)
                turnsText.text = LocalizationManager.Localize("Battle.TurnLeft", session.TurnsRemaining);
        }

        private static void SetPopupVisible(GameObject popup, bool visible)
        {
            if (popup == null) return;
            CanvasGroup group = popup.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }
            popup.SetActive(visible);
        }

        public void ReturnFromBattle()
        {
            if (session?.Result != null)
                BattleFlow.Complete(session.Result, session.Setup.returnSceneName);
        }

        private void OnDestroy()
        {
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnFromBattle);
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
            returnButton = buttonRect.gameObject.AddComponent<Button>();
            returnButton.targetGraphic = buttonRect.GetComponent<Image>();
            returnButton.onClick.AddListener(ReturnFromBattle);
            MakeText(buttonRect, "Label", "Return", 26, Background, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one);
            SetPopupVisible(resultOverlay, false);
        }

        private void ShowResult()
        {
            foreach (Button button in skillButtons) button.interactable = false;
            SetText(resultTitle, session.Setup.heroine.displayName +
                (session.Result.HeroineWon ? "の勝利" : "の敗北"));
            var text = new StringBuilder();
            text.Append(session.Result.outcome == BattleOutcome.HeroineVictory ? "怪人の体力が0になりました。" :
                session.Result.outcome == BattleOutcome.TurnLimitReached ? "時間切れで怪人が撤退しました。" :
                "ピュアプリエナジーが0になりました。").Append("\n\n");
            foreach (BattleStatusChange change in session.Result.statusChanges)
            {
                text.Append(change.displayName).Append("    ").Append(change.before)
                    .Append(" → ").Append(change.after)
                    .Append("    (").Append(change.delta >= 0 ? "+" : "")
                    .Append(change.delta).Append(")\n");
            }
            if (text.Length == 0) text.Append("No status rules configured");
            text.Append("\nエナジー ").Append(session.Result.energyRemaining)
                .Append("  怪人の体力 ").Append(session.Result.monsterHealthRemaining)
                .Append("\n残された時間 ").Append(session.Result.turnsRemaining).Append(" ターン");
            SetText(resultStatusText, text.ToString());
            SetPopupVisible(resultOverlay, true);
        }

        private void BuildErrorInterface(string error)
        {
            if (sceneUI != null)
            {
                SetPopupVisible(sceneUI.actionPopup, false);
                SetPopupVisible(sceneUI.resultOverlay, false);
                SetText(sceneUI.logText, "Battle setup is missing or invalid:\n" + error);
                return;
            }
            RectTransform canvas = CreateCanvas();
            MakePanel(canvas, "Background", Vector2.zero, Vector2.one, Color.black);
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

            EnsureEventSystem();

            return rect;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var eventObject = new GameObject("Battle EventSystem", typeof(EventSystem));
                eventObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

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
