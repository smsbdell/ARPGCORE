using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SkillLoadoutEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text levelText;

    private System.Action _onHover;
    private System.Action _onExit;

    public void Configure(string displayName, int level, Sprite icon, System.Action onHover, System.Action onExit)
    {
        _onHover = onHover;
        _onExit = onExit;

        if (nameText != null)
        {
            nameText.text = displayName;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv {level}";
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onHover?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onExit?.Invoke();
    }
}
