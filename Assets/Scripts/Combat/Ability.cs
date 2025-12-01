using System;

[Serializable]
public class Ability
{
    public string id;
    public string displayName;

    public float cooldown;
    public float baseDamage;

    public string projectilePrefabName;
    public float projectileSpeed;

    public float duration = 2f;
    public int maxLevel = 5;

    public string iconSpriteName;

    public string[] tags;
    public bool autoCast = true;
}
