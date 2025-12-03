using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Wave Scaling/Monster Wave Scaling Config")]
public class MonsterWaveScalingConfig : ScriptableObject
{
    [Header("Monster Controller Multipliers")]
    [SerializeField] private AnimationCurve _moveSpeedMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 2f);
    [SerializeField] private AnimationCurve _contactDamageMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 3f);
    [SerializeField] private AnimationCurve _xpRewardMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 2f);

    [Header("Character Stats Multipliers")]
    [SerializeField] private AnimationCurve _maxHealthMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 5f);
    [SerializeField] private AnimationCurve _armorMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 2f);
    [SerializeField] private AnimationCurve _dodgeChanceMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 1f);

    public MonsterSpawnContext GetContextForWave(int waveIndex)
    {
        waveIndex = Mathf.Max(1, waveIndex);

        return new MonsterSpawnContext
        {
            WaveIndex = waveIndex,
            MoveSpeedMultiplier = Evaluate(_moveSpeedMultiplier, waveIndex),
            ContactDamageMultiplier = Evaluate(_contactDamageMultiplier, waveIndex),
            XpRewardMultiplier = Evaluate(_xpRewardMultiplier, waveIndex),
            MaxHealthMultiplier = Evaluate(_maxHealthMultiplier, waveIndex),
            ArmorMultiplier = Evaluate(_armorMultiplier, waveIndex),
            DodgeChanceMultiplier = Evaluate(_dodgeChanceMultiplier, waveIndex)
        };
    }

    private static float Evaluate(AnimationCurve curve, int waveIndex)
    {
        if (curve == null || curve.length == 0)
            return 1f;

        return Mathf.Max(0f, curve.Evaluate(waveIndex));
    }
}

[Serializable]
public struct MonsterSpawnContext
{
    public int WaveIndex;

    [Header("Monster Controller")]
    public float MoveSpeedMultiplier;
    public float ContactDamageMultiplier;
    public float XpRewardMultiplier;

    [Header("Character Stats")]
    public float MaxHealthMultiplier;
    public float ArmorMultiplier;
    public float DodgeChanceMultiplier;

    public static MonsterSpawnContext Default => new MonsterSpawnContext
    {
        WaveIndex = 1,
        MoveSpeedMultiplier = 1f,
        ContactDamageMultiplier = 1f,
        XpRewardMultiplier = 1f,
        MaxHealthMultiplier = 1f,
        ArmorMultiplier = 1f,
        DodgeChanceMultiplier = 1f
    };

    public MonsterSpawnContext WithDefaults()
    {
        return new MonsterSpawnContext
        {
            WaveIndex = Mathf.Max(1, WaveIndex),
            MoveSpeedMultiplier = NormalizeMultiplier(MoveSpeedMultiplier),
            ContactDamageMultiplier = NormalizeMultiplier(ContactDamageMultiplier),
            XpRewardMultiplier = NormalizeMultiplier(XpRewardMultiplier),
            MaxHealthMultiplier = NormalizeMultiplier(MaxHealthMultiplier),
            ArmorMultiplier = NormalizeMultiplier(ArmorMultiplier),
            DodgeChanceMultiplier = NormalizeMultiplier(DodgeChanceMultiplier)
        };
    }

    private static float NormalizeMultiplier(float value)
    {
        return value <= 0f ? 1f : value;
    }
}
