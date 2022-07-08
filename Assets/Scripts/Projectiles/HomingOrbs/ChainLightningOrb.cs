using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ChainLightningOrb : DamageOrb
{
    public int splitsPerBounce = 2;
    public int maxBounces = 2;
    public float maxRange = 15f;

    public int currentBounces = 0;

    public List<CharacterMaster> hitEnemies;
    public CharacterMaster startingEnemy;
    public CharacterMaster targetMaster;

    public override void Initialize()
    {
        travelTime = 0.1f;

        if (damage < 0.1f)
        {
            flaggedForRemoval = true;
            return;
        }

        if (target == null)
        {
            CharacterSearcher searcher = new CharacterSearcher();
            searcher.SetTeamMask(TeamMask.AllExcept(owner.GetComponent<CharacterMaster>().teamIndex));
            searcher.maxDistance = maxRange;
            searcher.searchOrigin = startingEnemy.bodyCenter.position;
            searcher.sortMode = CharacterSearcher.SortMode.Distance;
            searcher.excludedCharacters.Add(startingEnemy);
            List<CharacterMaster> characters = searcher.GetTargets();

            if (characters.Count == 0)
            {
                flaggedForRemoval = true;
                return;
            }

            target = characters[0].netIdentity;
        }

        EffectData effectData = new EffectData
        {
            genericCharacterReference = target,
            genericFloat = travelTime,
            origin = startPosition,
        };

        EffectManager.CreateEffect(Effects.ChainLightningOrbEffect, effectData, true);

        if (hitEnemies == null)
        {
            hitEnemies = new List<CharacterMaster>();
            hitEnemies.Add(startingEnemy);
        }
        targetMaster = target.GetComponent<CharacterMaster>();
        hitEnemies.Add(targetMaster);
    }

    public override void OnArrival()
    {
        DealDamage();
        if (currentBounces < maxBounces)
        {

            CharacterSearcher searcher = new CharacterSearcher();
            searcher.SetTeamMask(TeamMask.AllExcept(owner.GetComponent<CharacterMaster>().teamIndex));
            searcher.maxDistance = maxRange;
            searcher.searchOrigin = targetMaster.bodyCenter.position;
            searcher.sortMode = CharacterSearcher.SortMode.Distance;
            searcher.excludedCharacters.Add(targetMaster);
            List<CharacterMaster> characters = searcher.GetTargets();

            int numSplits = Mathf.Min(characters.Count, splitsPerBounce);
            for (int i = 0; i < numSplits; i++)
            {
                if (hitEnemies.Contains(characters[i]))
                {
                    characters.RemoveAt(i);
                    i--;
                    numSplits = Mathf.Min(characters.Count, splitsPerBounce);
                }
            }


            for (int i = 0; i < numSplits; i++)
            {
                ChainLightningOrb newOrb = new ChainLightningOrb();
                newOrb.target = characters[i].netIdentity;
                newOrb.owner = owner;
                newOrb.startPosition = targetMaster.bodyCenter.position;
                newOrb.splitsPerBounce = splitsPerBounce;
                newOrb.maxBounces = maxBounces;
                newOrb.maxRange = maxRange;
                newOrb.currentBounces = currentBounces + 1;
                newOrb.hitEnemies = hitEnemies;
                newOrb.damage = damage;
                newOrb.procMultiplier = procMultiplier;

                HomingOrbManager.instance.AddOrb(newOrb);
            }
        }
    }
}