using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class PlayerEquipment : MonoBehaviour
{
    [Header("Equipped Items")]
    public EquipmentItem helmet;

    public EquipmentItem ring1;
    public EquipmentItem ring2;
    public EquipmentItem ring3;
    public EquipmentItem ring4;
    public EquipmentItem ring5;
    public EquipmentItem ring6;
    public EquipmentItem ring7;
    public EquipmentItem ring8;
    public EquipmentItem ring9;
    public EquipmentItem ring10;

    public EquipmentItem mainHand;
    public EquipmentItem offHand;
    public EquipmentItem boots;
    public EquipmentItem gloves;
    public EquipmentItem pants;

    private CharacterStats _stats;
    private StatModifier _currentTotalMods = new StatModifier();

    private void Awake()
    {
        _stats = GetComponent<CharacterStats>();
        ApplyEquipmentModifiers();
    }

    // Handy in editor / debug: right-click the component header and choose this.
    [ContextMenu("Reapply Equipment Modifiers")]
    private void ReapplyFromContextMenu()
    {
        if (_stats == null)
            _stats = GetComponent<CharacterStats>();

        ApplyEquipmentModifiers();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (_stats == null)
            _stats = GetComponent<CharacterStats>();

        ApplyEquipmentModifiers();
    }

    public void ApplyEquipmentModifiers()
    {
        if (_stats == null)
            return;

        // Remove currently applied mods.
        ApplyMods(_currentTotalMods, -1);

        // Recompute from equipped items.
        _currentTotalMods = new StatModifier();

        Accumulate(helmet);

        Accumulate(ring1);
        Accumulate(ring2);
        Accumulate(ring3);
        Accumulate(ring4);
        Accumulate(ring5);
        Accumulate(ring6);
        Accumulate(ring7);
        Accumulate(ring8);
        Accumulate(ring9);
        Accumulate(ring10);

        Accumulate(mainHand);
        Accumulate(offHand);
        Accumulate(boots);
        Accumulate(gloves);
        Accumulate(pants);

        // Apply new combined mods.
        ApplyMods(_currentTotalMods, +1);
    }

    private void Accumulate(EquipmentItem item)
    {
        if (item == null || item.modifiers == null)
            return;

        var m = item.modifiers;

        _currentTotalMods.maxHealth += m.maxHealth;
        _currentTotalMods.moveSpeed += m.moveSpeed;
        _currentTotalMods.baseDamage += m.baseDamage;

        _currentTotalMods.critChance += m.critChance;
        _currentTotalMods.critMultiplier += m.critMultiplier;
        _currentTotalMods.attackSpeedMultiplier += m.attackSpeedMultiplier;
        _currentTotalMods.projectileCount += m.projectileCount;
        _currentTotalMods.projectileSpreadAngle += m.projectileSpreadAngle;
        _currentTotalMods.weaponAttackSpeed += m.weaponAttackSpeed;
        _currentTotalMods.chainCount += m.chainCount;
        _currentTotalMods.splitCount += m.splitCount;

        _currentTotalMods.armor += m.armor;
        _currentTotalMods.dodgeChance += m.dodgeChance;

        _currentTotalMods.xpGainMultiplier += m.xpGainMultiplier;
        _currentTotalMods.cooldownReduction += m.cooldownReduction;
    }

    private void ApplyMods(StatModifier m, int sign)
    {
        if (m == null)
            return;

        _stats.maxHealth += sign * m.maxHealth;
        _stats.moveSpeed += sign * m.moveSpeed;
        _stats.baseDamage += sign * m.baseDamage;

        _stats.critChance += sign * m.critChance;
        _stats.critMultiplier += sign * m.critMultiplier;
        _stats.attackSpeedMultiplier += sign * m.attackSpeedMultiplier;
        _stats.projectileCount += sign * m.projectileCount;
        _stats.projectileSpreadAngle += sign * m.projectileSpreadAngle;
        _stats.weaponAttackSpeed += sign * m.weaponAttackSpeed;
        _stats.chainCount += sign * m.chainCount;
        _stats.splitCount += sign * m.splitCount;

        _stats.armor += sign * m.armor;
        _stats.dodgeChance += sign * m.dodgeChance;

        _stats.xpGainMultiplier += sign * m.xpGainMultiplier;
        _stats.cooldownReduction += sign * m.cooldownReduction;
    }

    public void Equip(EquipmentItem item)
    {
        if (item == null)
            return;

        switch (item.slot)
        {
            case EquipmentSlot.Helmet:
                helmet = item;
                break;

            case EquipmentSlot.Ring1: ring1 = item; break;
            case EquipmentSlot.Ring2: ring2 = item; break;
            case EquipmentSlot.Ring3: ring3 = item; break;
            case EquipmentSlot.Ring4: ring4 = item; break;
            case EquipmentSlot.Ring5: ring5 = item; break;
            case EquipmentSlot.Ring6: ring6 = item; break;
            case EquipmentSlot.Ring7: ring7 = item; break;
            case EquipmentSlot.Ring8: ring8 = item; break;
            case EquipmentSlot.Ring9: ring9 = item; break;
            case EquipmentSlot.Ring10: ring10 = item; break;

            case EquipmentSlot.MainHand:
                mainHand = item;
                break;
            case EquipmentSlot.OffHand:
                offHand = item;
                break;
            case EquipmentSlot.Boots:
                boots = item;
                break;
            case EquipmentSlot.Gloves:
                gloves = item;
                break;
            case EquipmentSlot.Pants:
                pants = item;
                break;
        }

        ApplyEquipmentModifiers();
    }

    public EquipmentItem GetEquipped(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Helmet: return helmet;

            case EquipmentSlot.Ring1: return ring1;
            case EquipmentSlot.Ring2: return ring2;
            case EquipmentSlot.Ring3: return ring3;
            case EquipmentSlot.Ring4: return ring4;
            case EquipmentSlot.Ring5: return ring5;
            case EquipmentSlot.Ring6: return ring6;
            case EquipmentSlot.Ring7: return ring7;
            case EquipmentSlot.Ring8: return ring8;
            case EquipmentSlot.Ring9: return ring9;
            case EquipmentSlot.Ring10: return ring10;

            case EquipmentSlot.MainHand: return mainHand;
            case EquipmentSlot.OffHand: return offHand;
            case EquipmentSlot.Boots: return boots;
            case EquipmentSlot.Gloves: return gloves;
            case EquipmentSlot.Pants: return pants;

            default:
                return null;
        }
    }
}
