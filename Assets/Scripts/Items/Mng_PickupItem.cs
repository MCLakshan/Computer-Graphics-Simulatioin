using UnityEngine;

public class Mng_PickupItem : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float rayDistance = 5f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private GameObject pickupItemUI;

    private bool isUIVisible = false;

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
    }

}
