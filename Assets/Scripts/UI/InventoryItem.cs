using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Item item;
    
    [Header("UI")]
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text itemCountText;

    [Header("For Dragging Unless Hide in Inspector")]
    public Transform parentAfterDrag;
    public int itemCount = 1; // Default item count
    
    private Mng_ItemHandler itemHandler; // Reference to the item handler for spawning items in the world
    
    private void Start()
    {
        // Initialize the item image when the script starts
        if (item != null)
        {
            Initialize(item);
        }
        
        // Get the item handler reference from the scene
        itemHandler = FindObjectOfType<Mng_ItemHandler>();
        if (itemHandler == null)
        {
            Debug.LogError("Mng_ItemHandler not found in the scene. Please ensure it is present.");
        }
    }
    
    public void Initialize(Item newItem)
    {
        if(image == null || itemCountText == null)
        {
            Debug.Log("Image or Item Count Text is not assigned in the InventoryItem script.");
            return;
        }
        
        item = newItem;
        image.sprite = newItem.image;
        RefreshCount();
        
        parentAfterDrag = transform.parent; // Store the initial parent for dragging
    }

    public void RefreshCount()
    {
        if (!item.isStackable)
        {
            // If the item is not stackable, hide the count text
            itemCountText.gameObject.SetActive(false);
            itemCount = 1; // Reset item count to 1 for non-stackable items
        }
        
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
    
    // Detect right clicks to drop items
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            GameObject itemToDrop = item.gameObjectPrefab;
            int countToDrop = itemCount;
            
            // Small front velocity to the item when dropped
            Vector3 dropVelocity = new Vector3(0, 0, 5f);
            
            if (itemToDrop != null)
            {
                // Check if the item is droppable
                if (!item.isCanDroppable)
                {
                    Mng_InventoryManager inventoryManager = FindObjectOfType<Mng_InventoryManager>();
                    if (inventoryManager != null)
                    {
                        inventoryManager.DisplayCraftingConsoleMessage("This item cannot be dropped.");
                    }
                    return;
                }
                
                // Instantiate the number of items to drop
                for (int i = 0; i < countToDrop; i++)
                {
                    itemHandler.DropItem(item);
                    
                    // Optionally, you can destroy the inventory item after dropping
                    Destroy(gameObject);
                }
            }
        }
    }
}
