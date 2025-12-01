using UnityEngine;

[CreateAssetMenu(menuName = "ARPG/Equipment Item", fileName = "NewEquipmentItem")]
public class EquipmentItem : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea]
    public string description;

    public Sprite icon;
    public EquipmentSlot slot;

    [Tooltip("Tags used later for synergy and weighting (e.g. fire, projectile, defensive).")]
    public string[] tags;

    public StatModifier modifiers;
}
