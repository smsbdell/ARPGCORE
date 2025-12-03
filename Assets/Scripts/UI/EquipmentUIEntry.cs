using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentItemUIEntry : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;

    private InventoryEquipmentItem _item;
    private EquipmentUI _owner;

    public void Init(InventoryEquipmentItem item, EquipmentUI owner)
    {
        _item = item;
        _owner = owner;

        EquipmentItem equipment = item != null ? item.equipment : null;
        Sprite sprite = equipment != null ? equipment.icon : item?.icon;

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
        }

        if (nameText != null)
        {
            string displayName = equipment != null ? equipment.displayName : item?.displayName;
            nameText.text = string.IsNullOrWhiteSpace(displayName) ? "Unnamed" : displayName;
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_owner != null && _item != null)
        {
            _owner.OnItemSelected(_item);
        }
    }
}
