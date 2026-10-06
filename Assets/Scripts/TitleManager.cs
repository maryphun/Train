using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Train.Battle;
using Assets.SimpleLocalization.Scripts;

public class TitleManager : MonoBehaviour
{
    [SerializeField] CanvasGroup title;
    [SerializeField] CanvasGroup VFXs;
    [SerializeField] Transform character_Mask;
    [SerializeField] Transform character;
    [SerializeField] Image btnBackground;
    [SerializeField] private Sprite heroineBattlePortrait;
    [SerializeField] private Sprite battleBackground;
    [SerializeField] private BattlerData enemyData;
    [SerializeField] private Sprite spritePunch;
    [SerializeField] private Sprite spriteKick;

    private void Start()
    {
        CanvasGroup shadow = character_Mask.GetComponent<CanvasGroup>();
        CanvasGroup chara = character.GetComponent<CanvasGroup>();

        shadow.DOFade(1.0f, 1f).SetEase(Ease.InCirc);
        chara.DOFade(1.0f, 1f).SetEase(Ease.InCirc);
        character_Mask.localPosition = new Vector3(0.0f, character.localPosition.y, character.position.z);
        character.localPosition = new Vector3(0.0f, character.localPosition.y, character.position.z);


        character_Mask.DOLocalMoveX(-100.0f, 1.0f, false).SetEase(Ease.OutCubic);
        character.DOLocalMoveX(-80.0f, 1.0f, false).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                character
                    .DOLocalMoveX(character.localPosition.x + 4.0f, 3.7f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                character
                    .DOLocalMoveY(character.localPosition.y + 6.0f, 2.8f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            });
        DOVirtual.DelayedCall(1f, () =>
        {
            title.DOFade(1.0f, 1f).SetEase(Ease.InCirc);
        });
        DOVirtual.DelayedCall(2f, () =>
        {
            VFXs.DOFade(1.0f, 1f).SetEase(Ease.InCirc);
        }); 
        DOVirtual.DelayedCall(2.5f, () =>
        {
            btnBackground.DOFade(0.8f, 1f).SetEase(Ease.Linear);
            btnBackground.GetComponent<RectTransform>().DOSizeDelta(new Vector2(2400.0f, 276.23f), 1.0f).SetEase(Ease.Linear);
        }); 
    }

    public void OnClickStart()
    {
        // init player profile
        PlayerProfile.Initialization();

        // change scene
        DOTween.Sequence()
                     .AppendCallback(() => btnBackground.GetComponent<CanvasGroup>().interactable = false)
                     .Append(btnBackground.GetComponent<RectTransform>().DOSizeDelta(new Vector2(0.0f, 276.23f), 1.0f).SetEase(Ease.Linear))
                     .AppendCallback(() =>
                     {
                         //SetupBattle(enemyData);
                         if (DialogueFlow.Setup("Prologue", "Battle"))
                         {
                             SetupBattle(enemyData);
                             SceneTransitionManager.Instance.LoadScene("Dialogue", 0.75f);
                         }
                     });
    }

    public void OnClickLoad()
    {
        btnBackground.GetComponent<RectTransform>().DOSizeDelta(new Vector2(0.0f, 276.23f), 1.0f).SetEase(Ease.Linear);
        btnBackground.GetComponent<CanvasGroup>().interactable = false;
    }

    public void OnCloseLoadPanel()
    {
        btnBackground.GetComponent<RectTransform>().DOSizeDelta(new Vector2(2400.0f, 276.23f), 1.0f).SetEase(Ease.Linear);
        btnBackground.GetComponent<CanvasGroup>().interactable = true;
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void SetupBattle(BattlerData battler)
    {
        if (battler == null)
            return;

        var setup = new BattleSetup
        {
            background = battleBackground,
            heroine = new BattleCharacter
            {
                id = "toka",
                displayName = LocalizationManager.Localize("Heroine.Peach"),
                portrait = heroineBattlePortrait
            },
            monster = new BattleCharacter
            {
                id = battler.BattlerID,
                displayName = LocalizationManager.Localize(battler.BattlerNameID),
                portrait = battler.BattlerSprite
            },

            startingEnergy = 100,
            startingMonsterHealth = 100,
            turnLimit = 5,

            // Return to whichever scene called this method.
            returnSceneName = SceneManager.GetActiveScene().name,

            heroineSkills = new List<BattleHeroineSkill>
            {
                new BattleHeroineSkill
                {
                    id = "attack",
                    displayName = LocalizationManager.Localize("Heroine.PeachPunch"),
                    energyCost = 5,
                    monsterDamage = 15,
                    cutIn = spritePunch,
                    animationDirection = BattleAnimationDirection.LeftToRight
                },
                new BattleHeroineSkill
                {
                    id = "attack2",
                    displayName = LocalizationManager.Localize("Heroine.PeachKick"),
                    energyCost = 25,
                    monsterDamage = 50,
                    cutIn = spriteKick,
                    animationDirection = BattleAnimationDirection.RightToLeft
                },
                new BattleHeroineSkill
                {
                    id = "recover",
                    displayName = LocalizationManager.Localize("Heroine.PeachRecover"),
                    energyRecovery = 25
                }
            },

            monsterSkills = new List<BattleSkill>
            {
                new BattleSkill
                {
                    id = "physical",
                    displayName = "çUåÇ",
                    energyDamage = 25,
                    physicalDamage = 50
                }
            }
        };

        BattleFlow.Setup(setup);
        //BattleFlow.Enter(setup);
    }
}
