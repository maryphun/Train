using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;

public class TitleManager : MonoBehaviour
{
    [SerializeField] CanvasGroup title;
    [SerializeField] CanvasGroup VFXs;
    [SerializeField] Transform character_Mask;
    [SerializeField] Transform character;
    [SerializeField] Image btnBackground;

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
        SceneTransitionManager.Instance.LoadScene("MainMenu", 0.75f);
    }

    public void OnClickLoad()
    {
        // todo
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}
