using System.Collections.Generic;
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
    
    [Header("Crafting Settings")]
    public bool isCraftable;
    public List<CraftingRequirement> craftingRequirements; // List of requirements for crafting this item

    
    
}

public enum ItemType
{
    Food,
    Tool,
    CraftingMaterial,
    Weapon,
}

[System.Serializable]
public class CraftingRequirement
{
    public Item requiredItem;  // Which item is needed
    public int requiredAmount; // How many are needed
}