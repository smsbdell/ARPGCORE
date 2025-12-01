using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;

    private bool _isOpen;

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        _isOpen = !_isOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(_isOpen);
    }
}
