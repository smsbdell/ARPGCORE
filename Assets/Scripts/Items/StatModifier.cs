using System;
using UnityEngine;

[Serializable]
public class StatModifier
{
    [Header("Core")]
    public float maxHealth;
    public float moveSpeed;
    public float baseDamage;

    [Header("Offensive")]
    public float critChance;
    public float critMultiplier;
    public float attackSpeedMultiplier;
    public int projectileCount;
    public float projectileSpreadAngle;
    public float weaponAttackSpeed;
    public int chainCount;
    public int splitCount;

    [Header("Defensive")]
    public float armor;
    public float dodgeChance;

    [Header("Progression & Cooldowns")]
    public float xpGainMultiplier;
    public float cooldownReduction;
}
