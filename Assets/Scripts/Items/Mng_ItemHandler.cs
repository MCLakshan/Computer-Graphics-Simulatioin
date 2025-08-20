using UnityEngine;

public class Mng_ItemHandler : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Mng_InventoryManager inventoryManager;

    [Header("Player References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] GameObject itemSpawnPoint; // This can be used to spawn the item in the world if needed

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
            PickupItem();
        }
    }

    private void PickupItem()
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

    public void DropItem(Item item)
    {
        GameObject droppedItem = Instantiate(item.gameObjectPrefab, itemSpawnPoint.transform.position, Quaternion.identity);
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                    
        // Set a small random forward velocity and small tilt to the dropped item
        Vector3 dropVelocity = new Vector3(Random.Range(0f, 1f), 
                                           Random.Range(0f, 1f), 
                                           Random.Range(0f, 1f));
        dropVelocity += itemSpawnPoint.transform.forward * 5f; // Add a small forward velocity
        dropVelocity += new Vector3(0, Random.Range(-0.5f, 0.5f), 0); // Add a small vertical component for randomness
        
        // Player = parent of the item spawn point
        // drop item rotation = player rotation + a small random tilt
        Quaternion dropRotation = itemSpawnPoint.transform.parent.rotation * Quaternion.Euler(Random.Range(-5f, 5f),
                                                                                        Random.Range(-5f, 5f),
                                                                                        Random.Range(-5f, 5f));
        droppedItem.transform.rotation = dropRotation;
        
        if (rb != null)
        {
            // Add a small forward velocity to the dropped item
            rb.linearVelocity = dropVelocity;
        }
    }

}
