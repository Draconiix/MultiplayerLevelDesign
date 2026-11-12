using UnityEngine;

// Add TinyInspector namespace
using TinyInspector;

[MonoscriptInfo("Demo Item Script", "A demo script to showcase TinyInspector features for item configuration.")]
public class DemoItemScript : MonoBehaviour
{
    [BoxGroup("Basic Information")] public string itemName = "New Item";
    [MultilineTextArea(), BoxGroup("Basic Information")]  public string description = "Item description...";
    [EnumToggle, BoxGroup("Basic Information")] public ItemType itemType;

    [BoxGroup("Item Properties")]
    [Min(0)]
    public int value = 10;

    [BoxGroup("Item Properties")]
    [Range(1, 64)]
    public int maxStackSize = 1;

    [BoxGroup("Item Properties")]
    [Range(0, 100)]
    public float durability = 100f;

    [Required,BoxGroup("Visuals")]
    public Sprite icon;
    [Required(false), BoxGroup("Visuals")]
    public GameObject modelPrefab;

    [Switch, BoxGroup("Usage Settings")]
    public bool isConsumable = false;
    [BoxGroup("Usage Settings")]
    public float cooldownTime = 0f;

    [Required(false), BoxGroup("Audio", TinyIcon.Audio)]
    public AudioClip useSound;
    [Required(false), BoxGroup("Audio")]
    public AudioClip pickupSound;

    [BoxGroup("Requirements", TinyIcon.Warning)]
    [Min(0)]
    public int requiredLevel = 0;
    [EnumToggle, BoxGroup("Requirements")]
    public ItemRarity rarity;
}