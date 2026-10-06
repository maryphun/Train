using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using Train.Battle;

public class SpellAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup cnvsGrp;
    [SerializeField] private TMP_Text abilityName;
    [SerializeField] private Image image;

    private Sequence animationSequence;
    private const float EdgePosition = 955f;
    private const float CenterPosition = 100f;
    private const float GraphicFadeDuration = 0.5f;
    private const float GlideDuration = 1f;
    private const float MovementDuration = GraphicFadeDuration * 2f + GlideDuration;
    private static readonly AnimationCurve MovementEase = CreateMovementEase();

    public bool IsPlaying => animationSequence != null && animationSequence.IsActive();

    internal void ValidateReferences()
    {
        if (cnvsGrp == null || abilityName == null || image == null)
            throw new InvalidOperationException("Assign Canvas Group, Ability Name and Image on SpellAnimation.");
    }

    public void InitAnimation(string abilityname, Sprite sprite)
    {
        InitAnimation(abilityname, sprite, BattleAnimationDirection.RightToLeft);
    }

    public void InitAnimation(string abilityname, Sprite sprite, BattleAnimationDirection direction)
    {
        if (sprite == null)
        {
            EndAnimation();
            return;
        }

        ValidateReferences();
        StopAnimation();
        gameObject.SetActive(true);
        cnvsGrp.alpha = 0.0f;
        cnvsGrp.interactable = false;
        cnvsGrp.blocksRaycasts = true;
        image.color = new Color(1, 1, 1, 0);
        image.sprite = sprite;
        float side = direction == BattleAnimationDirection.LeftToRight ? -1f : 1f;
        image.rectTransform.localPosition = new Vector3(EdgePosition * side, 0.0f, 0);
        abilityName.text = abilityname;

        // Mirror movement only; the supplied artwork keeps its original orientation.
        animationSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(cnvsGrp.DOFade(1.0f, 0.25f).SetEase(Ease.InOutSine))
            .Append(image.rectTransform.DOLocalMoveX(-EdgePosition * side, MovementDuration)
                .SetEase(MovementEase))
            .Join(image.DOFade(1f, GraphicFadeDuration).SetEase(Ease.InOutSine))
            .Insert(0.25f + GraphicFadeDuration + GlideDuration,
                image.DOFade(0f, GraphicFadeDuration).SetEase(Ease.InOutSine))
            .Append(cnvsGrp.DOFade(0.0f, 0.25f).SetEase(Ease.InOutSine))
            .OnComplete(() =>
            {
                animationSequence = null;
                EndAnimation();
            });
    }

    private static AnimationCurve CreateMovementEase()
    {
        float travel = EdgePosition * 2f;
        float glideSpeed = CenterPosition * 2f / GlideDuration;
        float glideSlope = glideSpeed * MovementDuration / travel;

        // Matching tangents keep speed continuous through the middle glide.
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(GraphicFadeDuration / MovementDuration,
                (EdgePosition - CenterPosition) / travel, glideSlope, glideSlope),
            new Keyframe((GraphicFadeDuration + GlideDuration) / MovementDuration,
                (EdgePosition + CenterPosition) / travel, glideSlope, glideSlope),
            new Keyframe(1f, 1f, 0f, 0f));
    }

    public void EndAnimation()
    {
        StopAnimation();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private void StopAnimation()
    {
        animationSequence?.Kill();
        animationSequence = null;
        if (cnvsGrp == null) return;
        cnvsGrp.alpha = 0f;
        cnvsGrp.interactable = false;
        cnvsGrp.blocksRaycasts = false;
    }
}
