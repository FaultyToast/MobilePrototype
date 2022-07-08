using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Mirror;

public class CharacterHealth : NetworkBehaviour
{
    private Text debugCounter;

    [SyncVar(hook = nameof(OnMaxHealthChanged))]
    public float maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private float syncHealth;
    [System.NonSerialized] public float baseMaxHealth;
    public bool scaleHealthWithPlayerNumber = false;

    public float health
    {
        set
        {
            if (NetworkServer.active)
            {
                value = Mathf.Min(maxHealth, value);
                syncHealth = value;

                if (characterMaster != null)
                {
                    characterMaster.RecalculateStatsHealth();
                }

                if (value <= 0 && !destroyed)
                {
                    destroyed = true;
                    Death();
                }
            }
            else
            {
                Debug.LogWarning("Health is being modified on the client");
            }
        }
        get
        {
            return syncHealth;
        }
    }

    [System.NonSerialized] public Healthbar healthbar;
    private Coroutine displayHealthBarCoroutine;
    private float healthBarDisplayTime = 5f;

    public HealthType healthType;
    public HealthbarType healthbarType;
    public Vector3 floatingHealthBarPos;
    public float floatingHealthBarWidthMultiplier = 1f;
    public bool spawnHitPrefabs = true;
    public bool createDamageNumbers = true;
    public bool endGameOnDeath = false;

    public static GameObject damageNumberPrefab = null;

    [Header("Character health attributes")]
    public CharacterMaster characterMaster;

    [Header("Generic health attributes")]
    public int genericTeamIndex;

    [Header("What happens when this object runs out of health")]
    public UnityEvent onDeath;

    [Header("What happens when this object takes damage")]
    public UnityEvent onDamageTaken;

    [System.NonSerialized] public bool destroyed = false;

    [System.NonSerialized] public bool invincible = false;

    public delegate bool DamageCheckMethod(DamageInfo damageInfo);

    List<DamageCheckMethod> damageConditions = new List<DamageCheckMethod>();


    public void Death()
    {
        if (characterMaster != null)
        {
            characterMaster.ServerDeath();
        }
        else
        {
            PostDeath();
        }
    }

    public void PostDeath()
    {
        onDeath.Invoke();
        RpcClientDeath();
    }

    [ClientRpc]
    public void RpcClientDeath()
    {
        ClientDeath();
    }

    public void ClientDeath()
    {
        // Remove healthbars
        switch (healthbarType)
        {
            case HealthbarType.Boss:
                {
                    UIManager.instance.HideBossHealthBar();
                    break;
                }
            case HealthbarType.Floating:
                {
                    if (healthbar != null)
                    {
                        healthbar.gameObject.SetActive(false);
                    }
                    break;
                }
        }
    }

    public void OnMaxHealthChanged(float oldMaxHealth, float newMaxHealth)
    {
        UpdateHealthBar(syncHealth, syncHealth, false);
    }

    public void OnHealthChanged(float oldHealth, float newHealth)
    {
        bool triggerDisplay = oldHealth > newHealth;
        UpdateHealthBar(oldHealth, newHealth, triggerDisplay);
    }

    public void Heal(float amount, CharacterMaster healer = null, bool createHealingNumbers = false)
    {
        if (healer == null)
        {
            healer = characterMaster;
        }

        float totalHealing = amount;

        if (NetworkServer.active)
        {
            ServerHeal(totalHealing, createHealingNumbers);
        }
        else
        {
            CmdHeal(totalHealing, createHealingNumbers);
        }

    }

    [Command]
    public void CmdHeal(float totalHealing, bool createHealingNumbers)
    {
        ServerHeal(totalHealing, createHealingNumbers);
    }

    [Server]
    public void ServerHeal(float totalHealing, bool createHealingNumbers)
    {
        health += totalHealing;

        if (createHealingNumbers)
        {
            CmdCreateHealingNumber(totalHealing);
        }
    }

    public void UpdateHealthBar(float oldHealth, float newHealth, bool triggerDisplay = true)
    {
        if (healthbarType != HealthbarType.None && healthbar != null)
        {
            float lastPercent = oldHealth / maxHealth;
            float percent = newHealth / maxHealth;
            healthbar.SetValue(maxHealth, newHealth, lastPercent, gameObject);

            // Special cases depending on healthbar type if this change triggers the bar to display
            if (triggerDisplay && gameObject.activeSelf)
            {
                switch (healthbarType)
                {
                    case HealthbarType.Floating:
                        {
                            if (displayHealthBarCoroutine != null)
                            {
                                StopCoroutine(displayHealthBarCoroutine);
                            }
                            if (healthbar != null && healthbar.gameObject.activeSelf)
                            {
                                displayHealthBarCoroutine = StartCoroutine(DisplayHealthbarTemp(healthBarDisplayTime));
                            }
                            break;
                        }
                }
            }
        }
    }

    public float GetPercent()
    {
        return health / maxHealth;
    }

    public void AddDamageCondition(DamageCheckMethod method)
    {
        damageConditions.Add(method);
    }

    public void RemoveDamageCondition(DamageCheckMethod method)
    {
        damageConditions.Remove(method);
    }

    public enum HealthType
    {
        Character,
        Generic,
        GenericUseMasterTeam
    }

    public enum HealthbarType
    {
        None,
        Boss,
        Player,
        Floating
    }

    public void Awake()
    {
        baseMaxHealth = maxHealth;

        HitboxGroup hitboxGroup = GetComponent<HitboxGroup>();
        if (hitboxGroup != null)
        {
            hitboxGroup.hitboxLayer = LayerMask.NameToLayer("Hurtbox");
        }
    }

    public void Start()
    {
        if (characterMaster == null)
        {
            characterMaster = GetComponent<CharacterMaster>();
        }

        if (damageNumberPrefab == null)
        {
            damageNumberPrefab = Resources.Load<GameObject>("UI/DamageNumber");
        }
    }


    public override void OnStartServer()
    {
        if (scaleHealthWithPlayerNumber)
        {
            int count = Mathf.Max(FracturedNetworkManager.playerCount, 1);
            if (characterMaster != null && characterMaster.isBoss)
            {
                baseMaxHealth *= count;
            }
            else baseMaxHealth *= (1f + (count-1) * 0.4f);
        }
        maxHealth = baseMaxHealth;
        health = maxHealth;
    }

    public override void OnStartClient()
    {
        if (healthbarType == HealthbarType.Player && !isLocalPlayer)
        {
            healthbarType = HealthbarType.Floating;
        }

        switch (healthbarType)
        {
            case HealthbarType.Boss:
                {
                    healthbar = UIManager.instance.bossHealthbar;
                    UIManager.instance.ShowBossHealthbar();
                    break;
                }
            case HealthbarType.Player:
                {
                    healthbar = UIManager.instance.playerHealthbar;
                    break;
                }
            case HealthbarType.Floating:
                {
                    healthbar = CreateFloatingHealthbar();
                    break;
                }
        }

        UpdateHealthBar(health, health, false);
    }

    public bool FilterDamage(DamageInfo damageInfo)
    {
        if (invincible)
        {
            return false;
        }
        if (damageInfo.excludeDamagingCharacter == netIdentity)
        {
            return false;
        }
        if (!damageInfo.doFriendlyFire)
        {
            switch (healthType)
            {
                case CharacterHealth.HealthType.Character:
                    {
                        if (damageInfo.teamIndex == characterMaster.teamIndex)
                        {
                            return false;
                        }
                        //onDamageDealt.Invoke();
                        break;
                    }
                case CharacterHealth.HealthType.Generic:
                    {
                        if (damageInfo.teamIndex == genericTeamIndex)
                        {
                            return false;
                        }
                        break;
                    }
                case CharacterHealth.HealthType.GenericUseMasterTeam:
                    {
                        if (damageInfo.teamIndex == characterMaster.teamIndex)
                        {
                            return false;
                        }
                        break;
                    }
            }
        }
        else
        {
            if (!damageInfo.hurtAttacker)
            {
                if (damageInfo.attacker != null)
                {
                    if (damageInfo.attacker == netIdentity)
                    {
                        return false;
                    }
                }
            }
        }

        bool damageConditionsFulfilled = true;

        foreach (DamageCheckMethod condition in damageConditions)
        {
            bool conditionFulfilled = condition(damageInfo);
            if (damageConditionsFulfilled)
            {
                damageConditionsFulfilled = conditionFulfilled;
            }
        }

        return damageConditionsFulfilled;
    }

    [Command(requiresAuthority = false)]
    public void AttemptDamage(byte[] damageInfoData)
    {
        DamageInfo damageInfo = DamageInfo.ReadDamageInfo(damageInfoData);

        if (!FilterDamage(damageInfo) || (characterMaster != null && characterMaster.godModeNoStagger))
        {
            return;
        }

        CharacterMaster attackerMaster = damageInfo.attacker != null ? damageInfo.attacker.GetComponent<CharacterMaster>() : null;
        if (characterMaster != null)
        {
            for (int i = 0; i < damageInfo.buffsInflicted.Count; i++)
            {
                characterMaster.AddTimedBuff(damageInfo.buffsInflicted[i].buff,
                    attackerMaster, damageInfo.buffsInflicted[i].stacks,
                    damageInfo.buffsInflicted[i].timeOverride > 0 ? damageInfo.buffsInflicted[i].timeOverride : null);
            }
        }
        
        if (damageInfo.damage < 0.01f)
        {
            return;
        }

        bool damageConditionsFulfilled = true;

        foreach (DamageCheckMethod condition in damageConditions)
        {
            bool conditionFulfilled = condition(damageInfo);
            if (damageConditionsFulfilled)
            {
                damageConditionsFulfilled = conditionFulfilled;
            }
        }

        if (damageConditionsFulfilled)
        {
            DealDamage(damageInfo);
        }
    }

    [Server]
    public void DealDamage(DamageInfo damageInfo)
    {
        RpcOnDamageTaken();

        if (invincible)
        {
            return;
        }

        if (characterMaster != null)
        {
            characterMaster.ModifyDamageTaken(damageInfo);
        }


        if (!damageInfo.processDamage)
        {
            return;
        }

        damageInfo.damage = Mathf.Min(damageInfo.damage, GameVariables.instance.damageCap);

        // To make sure the enemy isn't already dead for death triggers
        bool wasDestroyed = destroyed;

        if (damageInfo.damage > 0)
        {
            if (characterMaster != null && characterMaster.godMode)
            {
                // i'm too stupid to flip bools
            }
            else
            {
                health -= damageInfo.damage;
            }
        }

        switch (healthType)
        {
            case HealthType.Character:
                {
                    characterMaster.OnDamageTaken(damageInfo);
                    break;
                }
        }


        if (damageInfo.attacker != null && characterMaster != null)
        {
            CharacterMaster attackerMaster = damageInfo.attacker.GetComponent<CharacterMaster>();
            if (attackerMaster != null)
            {
                attackerMaster.OnDamageDealtConfirmed(damageInfo, characterMaster);
                if (destroyed && !wasDestroyed)
                {
                    attackerMaster.OnKilledEnemy(damageInfo, characterMaster);
                }
            }
        }
    }

    [ClientRpc]
    public void RpcOnDamageTaken()
    {
        onDamageTaken.Invoke();
    }

    [Command(requiresAuthority = false)]
    public void CmdCreateHealingNumber(float healingAmount)
    {
        RpcCreateHealingNumber(healingAmount);
    }

    [ClientRpc]
    public void RpcCreateHealingNumber(float healingAmount)
    {
        DamageNumber damageNumber = Instantiate(damageNumberPrefab).GetComponent<DamageNumber>();
        damageNumber.healingAmount = healingAmount;
        damageNumber.spawnPosition = characterMaster.bodyCenter.position;
    }

    [Command(requiresAuthority = false)]
    public void CmdCreateDamageNumber(byte[] damageInfoData)
    {
        RpcCreateDamageNumber(damageInfoData);
    }

    [ClientRpc]
    public void RpcCreateDamageNumber(byte[] damageInfoData)
    {
        DamageInfo damageInfo = DamageInfo.ReadDamageInfo(damageInfoData);
        DamageNumber damageNumber = Instantiate(damageNumberPrefab).GetComponent<DamageNumber>();
        damageNumber.damageInfo = damageInfo;
        damageNumber.spawnPosition = damageInfo.hitLocation;
    }

    public Healthbar CreateFloatingHealthbar()
    {
        GameObject newHealthBar = Instantiate(Resources.Load<GameObject>("UI/FloatingHealthBar"));
        newHealthBar.transform.SetParent(transform);
        newHealthBar.transform.localPosition = floatingHealthBarPos;
        newHealthBar.transform.localScale = new Vector3(floatingHealthBarWidthMultiplier, 1f, 1f);
        Healthbar newHealthBarScript = newHealthBar.GetComponent<Healthbar>();
        newHealthBarScript.canvasGroup.alpha = 0;
        return newHealthBarScript;
    }

    public IEnumerator DisplayHealthbarTemp(float duration)
    {
        healthbar.canvasGroup.alpha = 1;
        yield return new WaitForSeconds(duration);
        healthbar.canvasGroup.alpha = 0;
    }

    public void OnDrawGizmos()
    {
        // Draw floating health bar position
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.TransformPoint(floatingHealthBarPos), new Vector3(floatingHealthBarWidthMultiplier, 0.1f, 0.01f));
    }
}
