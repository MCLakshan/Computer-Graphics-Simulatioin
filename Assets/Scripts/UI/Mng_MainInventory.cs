using UnityEngine;
using UnityEngine.InputSystem;

public class Mng_MainInventory : MonoBehaviour
{
    [SerializeField] private GameObject mainInventoryUI;
    [SerializeField] private GameObject player;
    
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
}
