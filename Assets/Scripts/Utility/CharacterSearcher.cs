using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class CharacterSearcher
{
    public static List<CharacterMaster> searchTargets = new List<CharacterMaster>();
    public IEnumerable<SearchTargetInfo> searchTargetsEnumerable;

    public float? maxDistance = null;
    public float? minDistance = null;

    private bool targetsProcessed = false;

    public Vector3? forward;

    public float minAngle
    {
        set
        {
            maxDot = Mathf.Cos(Mathf.Clamp(value, 0f, 180f) * Mathf.Deg2Rad);
        }
    }

    public float maxAngle
    {
        set
        {
            minDot = Mathf.Cos(Mathf.Clamp(value, 0f, 180f) * Mathf.Deg2Rad);
        }
    }

    private float? minDot;
    private float? maxDot;

    public struct SearchTargetInfo
    {
        public CharacterMaster characterMaster;
        public float angleDot;
        public float distance;
    }

    public Vector3 searchOrigin;

    public enum SortMode
    {
        None,
        Distance,
        Angle,
        DistanceAndAngle
    }

    public List<CharacterMaster> excludedCharacters = new List<CharacterMaster>();

    public TeamMask teamMask = TeamMask.all;
    public SortMode sortMode;

    public void SetTeamMask(TeamMask teamMask)
    {
        this.teamMask = teamMask;
    }

    public void IncludeTeam(int team)
    {
        IncludeTeams(new int[] { team });
    }

    public void IncludeTeams(int[] teams)
    {

    }

    public void ExcludeTeam(int team)
    {
        ExcludeTeams(new int[] { team });
    }

    public void ExcludeTeams(int[] teams)
    {

    }

    public void ProcessTargets()
    {
        targetsProcessed = true;
        searchTargetsEnumerable = (from characterMaster in searchTargets where teamMask.HasTeam(characterMaster.teamIndex) select characterMaster).Select(GetCandidateInfo);

        if (excludedCharacters.Count > 0)
        {
            searchTargetsEnumerable = searchTargetsEnumerable.Where(delegate (SearchTargetInfo candidateInfo)
            {
                return !excludedCharacters.Contains(candidateInfo.characterMaster);
            });
        }

        searchTargetsEnumerable = searchTargetsEnumerable.Where(delegate (SearchTargetInfo candidateInfo)
        {
            return !candidateInfo.characterMaster.dead;
        });

        if (minDot != null)
        {
            searchTargetsEnumerable = searchTargetsEnumerable.Where(delegate (SearchTargetInfo candidateInfo)
            {
                return candidateInfo.angleDot > minDot;
            });
        }
        if (maxDot != null)
        {
            searchTargetsEnumerable = searchTargetsEnumerable.Where(delegate (SearchTargetInfo candidateInfo)
            {
                return candidateInfo.angleDot < maxDot;
            });
        }
        if (minDistance != null)
        {
            searchTargetsEnumerable = searchTargetsEnumerable.Where(delegate (SearchTargetInfo candidateInfo)
            {
                return candidateInfo.distance > minDistance;
            });
        }
        if (maxDistance != null)
        {
            searchTargetsEnumerable = searchTargetsEnumerable.Where(delegate (SearchTargetInfo candidateInfo)
            {
                return candidateInfo.distance < maxDistance;
            });
        }

        if (sortMode != SortMode.None)
        {
            searchTargetsEnumerable = searchTargetsEnumerable.OrderBy(GetSortMode());
        }


    }

    public void AddExcludedCharacter(CharacterMaster characterMaster)
    {
        excludedCharacters.Add(characterMaster);
    }

    public Func<SearchTargetInfo, float> GetSortMode()
    {
        switch (sortMode)
        {
            case SortMode.Angle:
                {
                    return (SearchTargetInfo searchTargetInfo) => -searchTargetInfo.angleDot;
                }
            case SortMode.Distance:
                {
                    return (SearchTargetInfo searchTargetInfo) => searchTargetInfo.distance;
                }
            case SortMode.DistanceAndAngle:
                {
                    return (SearchTargetInfo searchTargetInfo) => (1f - searchTargetInfo.angleDot) * searchTargetInfo.distance;
                }
        }
        return null;
    }

    public SearchTargetInfo GetCandidateInfo(CharacterMaster characterMaster)
    {
        SearchTargetInfo candidateInfo = new SearchTargetInfo();
        candidateInfo.characterMaster = characterMaster;
        Vector3 diff = characterMaster.bodyCenter.position - searchOrigin;
        candidateInfo.distance = diff.magnitude;

        if (sortMode == SortMode.Angle || sortMode == SortMode.DistanceAndAngle || maxDot != null || minDot != null)
        {
            if (forward == null)
            {
                Debug.LogError("Forward has not been set on the characterSearcher!");
            }
            candidateInfo.angleDot = Vector3.Dot(diff.normalized, forward.Value);
        }

        return candidateInfo;
    }

    public List<CharacterMaster> GetTargets()
    {
        if (!targetsProcessed)
        {
            ProcessTargets();
        }
        IEnumerable<CharacterMaster> mastersEnumerable = (from candidateInfo in searchTargetsEnumerable select candidateInfo.characterMaster);
        List<CharacterMaster> test = mastersEnumerable.ToList();
        for (int i = 0; i < test.Count; i++)
        {
            SearchTargetInfo targetInfo = GetCandidateInfo(test[i]);
        }
        return mastersEnumerable.ToList();
    }
}
