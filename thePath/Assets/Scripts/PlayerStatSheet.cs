using AML.Survivors;
using UnityEngine;

public class PlayerStatSheet : MonoBehaviour
{
    public static PlayerStatSheet instance;

    public int DamageReduction = 0;
    public float DamageModifier = 1f;
    [Range(0, 100)]
    public float CriticalStrikeChance = 0f;
    public float CriticalStrikeDamageModifier = 1f;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    /*
    public int ModifyDamage(int damage)
    {
        float adjustedDamage = damage * DamageModifier;
        return (int)adjustedDamage;
    }

    public int ModifyCriticalStrikeDamage(int damage)
    {
        float adjustedDamage = damage * CriticalStrikeDamageModifier;
        return (int)adjustedDamage;
    }

    public bool DidCrit(float abilityCritChance)
    {
        float Chance = Random.value * 100;

        if (abilityCritChance + CriticalStrikeChance > Chance)
        {
            return true;
        }
        return false;
    }*/
}
