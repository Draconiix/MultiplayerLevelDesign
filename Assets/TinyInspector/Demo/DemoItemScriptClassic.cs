using UnityEngine;

public class DemoItemScriptClassic : MonoBehaviour
{
    [Header("Basic Information")]
    public string itemName = "New Item";

    [TextArea(3, 5)]
    public string description = "Item description...";

    public ItemType itemType;

    [Header("Item Properties")]
    [Min(0)]
    public int value = 10;

    [Range(1, 64)]
    public int maxStackSize = 1;

    [Range(0, 100)]
    public float durability = 100f;

    [Header("Visuals")]
    public Sprite icon;
    public GameObject modelPrefab;

    [Header("Usage Settings")]
    public bool isConsumable = false;
    public float cooldownTime = 0f;

    [Header("Audio")]
    public AudioClip useSound;
    public AudioClip pickupSound;

    [Header("Requirements")]
    [Min(0)]
    public int requiredLevel = 0;

    public ItemRarity rarity;
}

public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Material,
    QuestItem,
    Key
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}