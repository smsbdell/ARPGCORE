using UnityEngine;

/// <summary>
/// Extra combat-related stats for CharacterStats, defined as a partial class.
/// This assumes your original CharacterStats class is also declared as 'public partial class CharacterStats : MonoBehaviour'.
/// </summary>
public partial class CharacterStats : MonoBehaviour
{
    [Header("Critical Strike")]
    [Tooltip("Chance (0-1) to deal a critical hit when an attack lands.")]
    public float critChance = 0f;

    [Tooltip("Multiplier applied to damage on a critical hit (e.g. 1.5 = 150% damage).")]
    public float critMultiplier = 1.5f;

    [Header("Weapon Attack Speed")]
    [Tooltip("Attack speed multiplier contributed by the weapon (1 = normal).")]
    public float weaponAttackSpeed = 1f;
}