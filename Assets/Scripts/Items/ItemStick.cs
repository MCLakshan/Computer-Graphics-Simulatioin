using UnityEngine;

public class ItemStick : MonoBehaviour, IItem
{
    [Header("Item")]
    [SerializeField] private Item item;
    public Item UseItem()
    {
        return item;
    }
}
