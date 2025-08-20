using UnityEngine;

public class Mng_ItemHandler : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Mng_InventoryManager inventoryManager;

    [Header("Player References")]
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Settings")]
    [SerializeField] private float rayDistance = 5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI Elements")]
    [SerializeField] private GameObject pickupItemUI;

    private bool isUIVisible = false;
    private GameObject pickupItem;

    void Start()
    {
        pickupItemUI.SetActive(false);
    }

    void Update()
    {
        // Make a ray from the screen center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Draw the ray in the Scene view (green line)
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.green);

        // Perform the raycast
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
        {
            Debug.Log("Stick is pointing at: " + hit.collider.name);
            pickupItem = hit.collider.gameObject;
            if (!isUIVisible)
            {
                isUIVisible = true;
            }
        }
        else
        {
            if (isUIVisible)
            {
                isUIVisible = false;
            }
        }


        if (isUIVisible)
        {
            pickupItemUI.SetActive(true);
        }
        else
        {
            pickupItemUI.SetActive(false);
        }
        
        // Pickup the item when the player presses the "E" key
        if (isUIVisible && Input.GetKeyDown(KeyCode.E))
        {
            if (pickupItem != null)
            {
                IItem item = pickupItem.GetComponent<IItem>();
                if (item != null)
                {
                    var itemToAdd = item.UseItem();
                    if (itemToAdd != null)
                    {
                        // Add the item to the inventory
                        inventoryManager.AddItemToMainInventory(itemToAdd);
                    }
                }
                Destroy(pickupItem);
                pickupItem = null;
            }
            isUIVisible = false;
        }
    }

}
