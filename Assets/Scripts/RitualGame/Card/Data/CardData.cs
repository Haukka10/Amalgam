using UnityEngine;
using Unity.VisualScripting;
using static CardGame.Structures.Structures;

[CreateAssetMenu(fileName = "NewCard", menuName = "Rituals/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardDomain domain;
    public CardType cardType;
    public int basePower;
    public Sprite artwork;

    [TextArea] public string description;

    // Referencja do Visual Scripting Graph
    public ScriptGraphAsset abilityGraph;
}
