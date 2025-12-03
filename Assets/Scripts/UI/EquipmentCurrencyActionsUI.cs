using UnityEngine;

public class EquipmentCurrencyActionsUI : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private InventoryEquipmentItem targetItem;

    [Header("Services")]
    [SerializeField] private CurrencyService currencyService;

    public void BindTarget(InventoryEquipmentItem item)
    {
        targetItem = item;
    }

    public void OnRerollClicked()
    {
        if (currencyService == null)
        {
            Debug.LogWarning("EquipmentCurrencyActionsUI: CurrencyService not assigned.");
            return;
        }

        currencyService.TryRerollItem(targetItem);
    }

    public void OnAugmentClicked()
    {
        if (currencyService == null)
        {
            Debug.LogWarning("EquipmentCurrencyActionsUI: CurrencyService not assigned.");
            return;
        }

        currencyService.TryAugmentItem(targetItem);
    }

    [ContextMenu("Reroll Target Item")]
    private void ContextReroll()
    {
        OnRerollClicked();
    }

    [ContextMenu("Augment Target Item")]
    private void ContextAugment()
    {
        OnAugmentClicked();
    }
}
