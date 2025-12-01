using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // for Keyboard.current

[System.Serializable]
public class EquipmentSlotUI
{
    public EquipmentSlot slot;
    public Button button;
    public Image iconImage;
    public TMP_Text label;
}

public class EquipmentUI : MonoBehaviour
{
    [Header("Panel Root")]
    [Tooltip("The GameObject that contains the equipment UI (usually your EquipmentPanel).")]
    public GameObject panelRoot;

    [Header("References")]
    public PlayerEquipment playerEquipment;

    [Tooltip("All equipment items you want available to equip via this UI.")]
    public List<EquipmentItem> availableItems = new List<EquipmentItem>();

    [Header("Item List UI")]
    public Transform itemListParent;
    public EquipmentItemUIEntry itemEntryPrefab;

    [Header("Slot UI")]
    public List<EquipmentSlotUI> slotUIs = new List<EquipmentSlotUI>();

    [Header("Visibility")]
    [Tooltip("Whether the equipment panel should start visible when the game runs.")]
    public bool startVisible = false;

    private EquipmentItem _selectedItem;
    private bool _isVisible;

    private void Awake()
    {
        if (panelRoot == null)
        {
            // Fallback to this object if not assigned, but usually you want a separate panel object.
            panelRoot = gameObject;
        }

        _isVisible = startVisible;
        ApplyVisibility();
    }

    private void Start()
    {
        if (playerEquipment == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerEquipment = playerObj.GetComponent<PlayerEquipment>();
            }
        }

        BuildItemList();
        HookUpSlotButtons();
        RefreshSlots();
    }

    private void Update()
    {
        // New Input System: Keyboard.current.iKey
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleVisibility();
        }
    }

    private void ApplyVisibility()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(_isVisible);
        }
    }

    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        ApplyVisibility();
    }

    private void BuildItemList()
    {
        if (itemListParent == null || itemEntryPrefab == null)
            return;

        for (int i = itemListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemListParent.GetChild(i).gameObject);
        }

        foreach (EquipmentItem item in availableItems)
        {
            if (item == null)
                continue;

            EquipmentItemUIEntry entry = Instantiate(itemEntryPrefab, itemListParent);
            entry.Init(item, this);
        }
    }

    private void HookUpSlotButtons()
    {
        foreach (var slotUI in slotUIs)
        {
            if (slotUI.button == null)
                continue;

            slotUI.button.onClick.RemoveAllListeners();
            EquipmentSlot capturedSlot = slotUI.slot;
            slotUI.button.onClick.AddListener(() => OnSlotClicked(capturedSlot));
        }
    }

    public void OnItemSelected(EquipmentItem item)
    {
        _selectedItem = item;
        // Optional: add highlight behavior here.
    }

    private void OnSlotClicked(EquipmentSlot slot)
    {
        if (_selectedItem == null)
        {
            Debug.Log("EquipmentUI: No item selected to equip.");
            return;
        }

        if (_selectedItem.slot != slot)
        {
            Debug.Log($"EquipmentUI: '{_selectedItem.displayName}' cannot go into slot '{slot}'. It belongs to '{_selectedItem.slot}'.");
            return;
        }

        if (playerEquipment == null)
        {
            Debug.LogWarning("EquipmentUI: playerEquipment not assigned.");
            return;
        }

        playerEquipment.Equip(_selectedItem);
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        if (playerEquipment == null)
            return;

        foreach (var slotUI in slotUIs)
        {
            if (slotUI == null)
                continue;

            EquipmentItem equipped = playerEquipment.GetEquipped(slotUI.slot);

            if (equipped != null)
            {
                if (slotUI.iconImage != null)
                {
                    if (equipped.icon != null)
                    {
                        slotUI.iconImage.enabled = true;
                        slotUI.iconImage.sprite = equipped.icon;
                    }
                    else
                    {
                        slotUI.iconImage.enabled = false;
                        slotUI.iconImage.sprite = null;
                    }
                }

                if (slotUI.label != null)
                {
                    slotUI.label.text = equipped.displayName;
                }
            }
            else
            {
                if (slotUI.iconImage != null)
                {
                    slotUI.iconImage.enabled = false;
                    slotUI.iconImage.sprite = null;
                }

                if (slotUI.label != null)
                {
                    slotUI.label.text = slotUI.slot.ToString();
                }
            }
        }
    }
}
