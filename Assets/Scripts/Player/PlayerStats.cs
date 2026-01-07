using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerStats
{
    //Base stats
    public static float turnSpeed = 1000f;
    public static float baseSpeed = 15f;
    public static float baseDamage = 2f;
    public static float bulletDamage = 5f;
    public static float bulletSpeed = 30f;
    public static float critChance = 0.1f;
    public static float maxHealth = 100f;

    //Current Stats
    public static float speed = 15f;
    public static float health = 100f;
    public static float damage = 10f;
    public static float animSpeed = 1f;
    public static int lifeRegeneration = 5;

    //Engine
    public static bool engineExploded = false;

    //Dash
    public static float dashSpeed = 50f;
    public static float dashDuration = 0.2f;
    public static float dashCD = 1f;
    //Jump
    public static float gravityScale = -9f;
    public static float jumpHeight = 3;

    //Checkpoint
    public static Transform lastCheckpoint;

    //Sword effects
    public static bool useAquaAffinity = false;
    public static bool useMoonLight = false;
    public static bool useSunLight= false;
    public static bool useBloodSeeker = false;
    public static bool useBoomTrail = false;
    public static bool extendParry = false;
    //public static bool projectileReflect = false;
    //public static bool markEnemy = false;
    public static bool freezeOnHit = false;
    public static bool burnOnHit = false;

    public static string currentSword = "";

    //Passive effects
    public static float gladiatorBonus = 1.0f;
    public static float healingMultiplier = 1.0f;
    public static float hookRangeMultiplier = 1.0f;
    public static void ResetSwordEffects()
    {
        damage = baseDamage;
        turnSpeed = 1000f;
        critChance = 0.1f;
        useAquaAffinity = false;
        useBloodSeeker = false;
        useBoomTrail = false;
        extendParry= false;
        //projectileReflect = false;
        useMoonLight = false;
        useSunLight = false;
        freezeOnHit = false;
        burnOnHit = false;
    }

    public static void ApplySwordEffect(string swordName)
    {
        ResetSwordEffects(); //Solo una espada a la vez
        currentSword = swordName;

        switch (swordName)
        {
            case "Aquaffinity":
                useAquaAffinity = true;
                break;

            case "Blood Seeker":
                damage = baseDamage * 1.4f;
                useBloodSeeker = true;
                break;

            case "Blocker":
                damage = baseDamage;
                extendParry = true;
                break;

            case "Boom":
                damage = baseDamage * 1.15f;
                useBoomTrail = true;
                break;

            /*
            case "Contract":
                damage = baseDamage;
                markEnemy = true;
                break;
            */
            /*
            case "Mirage":
                damage = baseDamage * 1.25f;
                projectileReflect = true;
                break;
            */

            case "Moonlight Sword":
                freezeOnHit = true;
                break;

            case "Sunlight Sword":
                burnOnHit = true;
                break;

            case "Terminus":
                damage = baseDamage * 1.6f;
                //falta bajar attackspeed
                critChance += 0.2f;
                break;

            default:
                damage = baseDamage;
                break;
        }

    }

    public static void ApplyPassiveEffect(string passiveName)
    {
        ResetSwordEffects(); //Solo una espada a la vez
        currentSword = passiveName;

        switch (passiveName)
        {
            case "Marksman":
                bulletDamage *= 1.05f;
                break;

            case "Healthy":
                maxHealth *= 1.10f;
                health *= 1.2f; //Recupera parte de la vida actual
                GameUI.Instance.healthBar.maxValue = maxHealth;
                GameUI.Instance.UpdateHealthBar(health);
                break;

            case "Weak Point":
                critChance += 0.05f;
                break;

            case "Gladiator":
                gladiatorBonus += 0.1f;
                break;

            case "Hermes":
                speed *= 1.05f;
                break;

           

        }

    }
}
