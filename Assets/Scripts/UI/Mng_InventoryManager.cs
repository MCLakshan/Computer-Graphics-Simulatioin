using UnityEngine;
using UnityEngine.InputSystem;

public class Mng_InventoryManager : MonoBehaviour
{
    [SerializeField] private GameObject mainInventoryUI;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject inventoryItemPrefab; // Prefab for the inventory item
    
    [SerializeField] private InventorySlot[] mainInventorySlots; // Array of main inventory slots
    [SerializeField] private InventorySlot[] hotbarSlots; // Array of hotbar slots
    
    [Header("Debugging")]
    [SerializeField] private Item testItem; // Debugging item to add to the inventory
    
    private bool isMainInventoryOpen = false;

    private void Start()
    {
        isMainInventoryOpen = false;
        mainInventoryUI.SetActive(false);
    }
    
    private  void Update(){
        // Opeant the main inventory when the "I" key or "Tab" key is pressed
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMainInventory();
        }
        
        // Debugging: add an item to the main inventory when the "A" key is pressed
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddItemToMainInventory(testItem);
        }
    }
    
    // Toggle the main inventory UI
    private void ToggleMainInventory()
    {
        isMainInventoryOpen = !isMainInventoryOpen;
        mainInventoryUI.SetActive(isMainInventoryOpen);
        
        // Pause the game when the inventory is open
        if (isMainInventoryOpen)
        {
            // Pause the game
            Time.timeScale = 0f; 
            
            // Disable player controls (Disabling player Input component)
            player.GetComponent<PlayerInput>().enabled = false;
            
            // enable the cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Resume the game
            Time.timeScale = 1f; 
            
            // Enable player controls (Enabling player Input component)
            player.GetComponent<PlayerInput>().enabled = true;
            
            // Lock the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    // Add a item to the inventory
    // Returns true if the item was added successfully, false if there are no empty slots
    public bool AddItemToMainInventory(Item item)
    {
        // Find an empty slot in the main inventory
        for (int i = 0; i < mainInventorySlots.Length; i++)
        {
            InventorySlot slot = mainInventorySlots[i];
            var itemInSlot = slot.GetComponentInChildren<InventoryItem>();
            
            // If the slot is empty, create a new InventoryItem and set it in the slot
            if (itemInSlot == null)
            {
                // Spawn a new InventoryItem 
                GameObject newItemObject = Instantiate(inventoryItemPrefab, slot.inventoryItemParentAnchor);
                InventoryItem inventoryItem = newItemObject.GetComponent<InventoryItem>();
                
                // Initialize the InventoryItem with the item data
                inventoryItem.Initialize(item);
                return true;
            }
        }
        return false;
    }
}
