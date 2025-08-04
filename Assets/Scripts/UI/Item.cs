using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    [Header("Item Game Properties")]
    public GameObject gameObjectPrefab;
    public ItemType itemType;
    
    [Header("Item UI Properties")]
    public Sprite image;
    public bool isStackable;
    public int maxStackSize = 10;
}

public enum ItemType
{
    Food,
    Tool,
    CraftingMaterial,
    Weapon,
}