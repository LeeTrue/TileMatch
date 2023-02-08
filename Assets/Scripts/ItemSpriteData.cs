using UnityEngine;

public enum SpriteType
{
    Default,

    Candy1_Blue,
    Candy1_Green,
    Candy1_Orange,
    Candy1_Purple,
    Candy1_Red,
    Candy1_Yellow,

    Candy2_Blue,
    Candy2_Green,
    Candy2_Orange,
    Candy2_Purple,
    Candy2_Red,
    Candy2_Yellow,

    Candy3_Blue,
    Candy3_Green,
    Candy3_Orange,
    Candy3_Purple,
    Candy3_Red,
    Candy3_Yellow,

    Candy4_Blue,
    Candy4_Green,
    Candy4_Orange,
    Candy4_Purple,
    Candy4_Red,
    Candy4_Yellow,

    Candy5_Blue,
    Candy5_Green,
    Candy5_Orange,
    Candy5_Purple,
    Candy5_Red,
    Candy5_Yellow,

    Candy6_Blue,
    Candy6_Green,
    Candy6_Orange,
    Candy6_Purple,
    Candy6_Red,
    Candy6_Yellow,

    Bomb,
    Anything,
    Empty
}

[CreateAssetMenu(fileName = "Item Sprite Data", menuName = "Scriptable Object/Item Sprite Data")]
public class ItemSpriteData : ScriptableObject
{
    public SpriteType type;
    public Sprite[] sprite; // [0]:standard, [1]:row, [2]:column
}
