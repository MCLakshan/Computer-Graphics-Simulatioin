using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public Transform inventoryItemParentAnchor;
    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryItemParentAnchor.childCount == 0)
        {
            InventoryItem inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();
            inventoryItem.parentAfterDrag = inventoryItemParentAnchor;
        }
    }
}
