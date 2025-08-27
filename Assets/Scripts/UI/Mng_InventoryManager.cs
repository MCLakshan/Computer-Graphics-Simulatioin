using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mng_InventoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainInventoryUI;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject inventoryItemPrefab; // Prefab for the inventory item
    
    [Header("Inventory Slots")]
    [SerializeField] private InventorySlot[] mainInventorySlots; // Array of main inventory slots
    [SerializeField] private InventorySlot[] hotbarSlots; // Array of hotbar slots
    
    [Header("Crafting")]
    [SerializeField] private TMP_Text craftingConsoleText; // Text field for crafting console output
    
    [Header("Craftable Items")]
    [SerializeField] private Item FirePlace; // Example of a craftable item
    
    [Header("Hints")]
    [SerializeField] private TMP_Text showHintText; // Text field to show hints to the player
    
    [Header("Debugging")]
    [SerializeField] private Item testItem; // Debugging item to add to the inventory
    
    private bool isMainInventoryOpen = false;
    private int selectedHotbarSlot = -1; // -1 means no slot is selected

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
        
        // Change the selected hotbar slot when the number keys (1-6) are pressed
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ChangeSelectedSlot(i);
            }
        }
        
        // Press "x" to deselect the currently selected hotbar slot
        if (Input.GetKeyDown(KeyCode.X))
        {
            ChangeSelectedSlot(-1);
        }
        
        // Debugging: add an item to the main inventory when the "A" key is pressed
        if (Input.GetKeyDown(KeyCode.L))
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
            
            // If the slot already contains an item and it is stackable, try to add to the existing stack
            if (itemInSlot != null && itemInSlot.item == item && itemInSlot.itemCount < item.maxStackSize)
            {
                itemInSlot.itemCount++;
                itemInSlot.RefreshCount();
                return true; // Item was successfully added to an existing stack
            }
            
            // If the slot is empty or contains a different item, or the item is not stackable
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
    
    // Change the selected hotbar slot
    private void ChangeSelectedSlot(int newSelectedSlot)
    {
        if (newSelectedSlot == -1)
        {
            if (selectedHotbarSlot >= 0 && selectedHotbarSlot < hotbarSlots.Length)
            {
                hotbarSlots[selectedHotbarSlot].Deselect();
                selectedHotbarSlot = -1; // Reset the selected slot
            }
        }
        
        // Deselect the previously selected slot
        if (selectedHotbarSlot >= 0 && selectedHotbarSlot < hotbarSlots.Length)
        {
            hotbarSlots[selectedHotbarSlot].Deselect();
        }
        
        // Select the new slot
        if (newSelectedSlot >= 0 && newSelectedSlot < hotbarSlots.Length)
        {
            hotbarSlots[newSelectedSlot].Select();
            selectedHotbarSlot = newSelectedSlot;
        }

        if (newSelectedSlot < 0 || newSelectedSlot >= hotbarSlots.Length)
        {
            ShowHint("");
        }
        else
        {
            // If the selected slot item is spawnable in the world, show a hint
            var itemInSlot = hotbarSlots[selectedHotbarSlot].GetComponentInChildren<InventoryItem>();
            if (itemInSlot != null && itemInSlot.item.isSpawnableInWorld)
            {
                ShowHint("Press 'F' to spawn the item in the world.");
            }
        }
        
    }

    #region - CRAFTING -

    private void CraftItem(Item item)
    {
        // get all the things that are crafting materials toa list
        List<CraftingRequirement> availableCraftingMaterials = new List<CraftingRequirement>();
        
        // All inventory slots in the main inventory + hotbar
        InventorySlot[] allInventorySlots = new InventorySlot[mainInventorySlots.Length + hotbarSlots.Length];
        mainInventorySlots.CopyTo(allInventorySlots, 0);
        hotbarSlots.CopyTo(allInventorySlots, mainInventorySlots.Length);
        
        bool hasEnough = false;
        
        // Find all crafting materials in the main inventory and the hotbar
        foreach (var slot in allInventorySlots)
        {
            var itemInSlot = slot.GetComponentInChildren<InventoryItem>();
            if (itemInSlot != null && itemInSlot.item.itemType == ItemType.CraftingMaterial)
            {
                // Check if the item is already in the list
                bool found = false;
                foreach (var requirement in availableCraftingMaterials)
                {
                    if (requirement.requiredItem == itemInSlot.item)
                    {
                        requirement.requiredAmount += itemInSlot.itemCount; // Add the count to the existing item
                        found = true;
                        break;
                    }
                }
                
                // If not found, add a new requirement
                if (!found)
                {
                    availableCraftingMaterials.Add(new CraftingRequirement { requiredItem = itemInSlot.item, requiredAmount = itemInSlot.itemCount });
                }
            }
        }
        
        // Check if we have enough materials to craft the item
        int totalRequiredAmount = 0;
        foreach (var requirement in availableCraftingMaterials)
        {
            foreach (var req in item.craftingRequirements)
            {
                if (req.requiredItem == requirement.requiredItem && requirement.requiredAmount >= req.requiredAmount)
                {
                    totalRequiredAmount += 1;
                }
            }
        }
        
        Debug.Log("Total required crafting materials: " + totalRequiredAmount + "Needed: " + item.craftingRequirements.Count);
        
        if(totalRequiredAmount == item.craftingRequirements.Count)
        {
            hasEnough = true; // We have enough materials to craft the item
        }
        else
        {
            hasEnough = false;
        }
        
        // If we have enough materials, craft the item
        if (hasEnough)
        {
            // First, remove the required materials from the inventory
            foreach (var req in item.craftingRequirements)
            {
                int requiredAmount = req.requiredAmount;
                // Loop through all inventory slots to find and remove the required materials
                for (int i = 0; i < allInventorySlots.Length && requiredAmount > 0; i++)
                {
                    InventorySlot slot = allInventorySlots[i];
                    var itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                    
                    // If the slot contains the required item
                    if (itemInSlot != null && itemInSlot.item == req.requiredItem)
                    {
                        // Remove the required amount from the slot
                        if (itemInSlot.itemCount >= requiredAmount)
                        {
                            itemInSlot.itemCount -= requiredAmount; // Reduce the count
                            requiredAmount = 0; // All required amount is removed
                            
                            // If the count reaches zero, destroy the item in the slot
                            if (itemInSlot.itemCount <= 0)
                            {
                                Destroy(itemInSlot.gameObject);
                            }
                        }
                        else
                        {
                            requiredAmount -= itemInSlot.itemCount; // Reduce the required amount by the count in the slot
                            Destroy(itemInSlot.gameObject); // Destroy the item in the slot
                        }
                    }
                }
            }
            
            // Update the main inventory UI to reflect the changes
            // allInventorySlots is already updated, so we can just copy the changes to the main inventory slots
            for (int i = 0; i < allInventorySlots.Length; i++)
            {
                InventorySlot slot = allInventorySlots[i];
                if (i < mainInventorySlots.Length)
                {
                    mainInventorySlots[i] = slot; // Copy the updated slot to the main inventory slots
                }
                else
                {
                    hotbarSlots[i - mainInventorySlots.Length] = slot; // Copy the updated slot to the hotbar slots
                }
            }
            
            // Now, add the crafted item to the inventory
            bool added = AddItemToMainInventory(item);
            if (added)
            {
                craftingConsoleText.text = $"Crafted {item.name} successfully!";
            }
            else
            {
                craftingConsoleText.text = "Failed to craft item: No empty slots available.";
            }
        }
        else
        {
            // Not enough materials to craft the item
            craftingConsoleText.text = "Not enough materials to craft this item.";
        }
    }

    public void CraftFirePlace()
    {
        CraftItem(FirePlace);
    }

    #endregion
    
    public void DisplayMessage(string message)
    {
        craftingConsoleText.text = message;
    }
    
    public void ShowHint(string hint)
    {
        showHintText.text = hint;
        showHintText.gameObject.SetActive(true);
    }
    
    
}
