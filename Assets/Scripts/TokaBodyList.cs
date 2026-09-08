using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TokaBodyList", menuName = "Scriptable Objects/TokaBodyList")]
public class TokaBodyList : ScriptableObject
{
    [SerializeField] public Sprite defaultSprite;
    [SerializeField] public List<Sprite> spriteList = new List<Sprite>();

    [SerializeField] public Sprite defaultFace;
    [SerializeField] public List<Sprite> faceList = new List<Sprite>();
}
