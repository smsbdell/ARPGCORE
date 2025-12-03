using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elemental damage, resistances, and skill tag gating for CharacterStats.
/// </summary>
public partial class CharacterStats : MonoBehaviour
{
    [Header("Elemental Damage")]
    [Tooltip("Minimum fire damage contributed to abilities that scale with elemental rolls.")]
    public float fireDamageMin = 0f;

    [Tooltip("Maximum fire damage contributed to abilities that scale with elemental rolls.")]
    public float fireDamageMax = 0f;

    [Tooltip("Minimum cold damage contributed to abilities that scale with elemental rolls.")]
    public float coldDamageMin = 0f;

    [Tooltip("Maximum cold damage contributed to abilities that scale with elemental rolls.")]
    public float coldDamageMax = 0f;

    [Tooltip("Minimum lightning damage contributed to abilities that scale with elemental rolls.")]
    public float lightningDamageMin = 0f;

    [Tooltip("Maximum lightning damage contributed to abilities that scale with elemental rolls.")]
    public float lightningDamageMax = 0f;

    [Header("Elemental Resistances")]
    [Tooltip("Flat fire damage mitigation before health loss.")]
    public float fireResistance = 0f;

    [Tooltip("Flat cold damage mitigation before health loss.")]
    public float coldResistance = 0f;

    [Tooltip("Flat lightning damage mitigation before health loss.")]
    public float lightningResistance = 0f;

    [Header("Elemental Ailments")]
    [Tooltip("Chance (0-1) to inflict shock when lightning damage is applied.")]
    [Range(0f, 1f)] public float shockDamageChance = 0f;

    [Header("Skill Gating")]
    [Tooltip("If provided, only skills with at least one matching tag can be acquired.")]
    public List<string> allowedSkillTags = new List<string>();

    public IReadOnlyList<string> AllowedSkillTags => allowedSkillTags;
}
