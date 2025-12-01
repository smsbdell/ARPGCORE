using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentItemUIEntry : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;

    private EquipmentItem _item;
    private EquipmentUI _owner;

    public void Init(EquipmentItem item, EquipmentUI owner)
    {
        _item = item;
        _owner = owner;

        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = (item.icon != null);
        }

        if (nameText != null)
        {
            nameText.text = item.displayName;
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
