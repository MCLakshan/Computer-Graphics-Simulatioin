using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    [Header("References")]
    public Transform inventoryItemParentAnchor;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color defaultColor;
    
    private void Start()
    {
        // Initialize the background color to the default color
        backgroundImage.color = defaultColor;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryItemParentAnchor.childCount == 0)
        {
            InventoryItem inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();
            inventoryItem.parentAfterDrag = inventoryItemParentAnchor;
        }
    }

    public void Select()
    {
        backgroundImage.color = selectedColor;
    }
    
    public void Deselect()
    {
        backgroundImage.color = defaultColor;
    }
}
