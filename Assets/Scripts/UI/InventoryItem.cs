using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Item item;
    
    [Header("UI")]
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text itemCountText;

    [Header("For Dragging Unless Hide in Inspector")]
    public Transform parentAfterDrag;
    public int itemCount = 1; // Default item count
    
    private void Start()
    {
        // Initialize the item image when the script starts
        if (item != null)
        {
            Initialize(item);
        }
    }
    
    public void Initialize(Item newItem)
    {
        item = newItem;
        image.sprite = newItem.image;
        RefreshCount();
    }

    public void RefreshCount()
    {
        itemCountText.text = itemCount.ToString();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // At the time of dragging, we need to disable the raycast target
        // so that the item can be dragged without blocking other UI elements
        image.raycastTarget = false;
        
        // Store the parent transform before dragging starts
        // This allows us to return the item to its original position after dragging
        parentAfterDrag = transform.parent;
        
        // Set the item's parent to the canvas to ensure it is rendered on top of other UI elements
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Update the item's position to follow the mouse cursor
        transform.position = Input.mousePosition;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        // Re-enable the raycast target when dragging ends
        image.raycastTarget = true;
        
        // If the item is dropped outside a valid drop area, return it to its original parent
        transform.SetParent(parentAfterDrag);
        
        
        Debug.Log("Item dropped at: " + transform.position);
    }
}
