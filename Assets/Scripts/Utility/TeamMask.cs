using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct TeamMask
{
	public TeamMask(int index)
    {
		mask = none.mask;
		AddTeam(index);
    }

	public bool HasTeam(int index)
	{
		return (mask & 1 << index) == (1 << index);
	}

	public void AddTeam(int index)
	{
		if (index < 0)
		{
			return;
		}
		mask |= (1 << index);
	}

	public void RemoveTeam(int index)
	{
		if (index < 0)
		{
			return;
		}
		mask &= (~(1 << index));
	}
	static TeamMask()
	{
		all.mask = ~0;
		enemyTeam.AddTeam(1);
		playerTeam.AddTeam(0);
	}

	public static TeamMask AllExcept(int index)
	{
		TeamMask result = all;
		result.RemoveTeam(index);
		return result;
	}

	public static TeamMask AllExcept(TeamMask teamMask)
    {
		TeamMask newMask = teamMask;
		teamMask.mask = ~(teamMask.mask);
		return newMask;
    }

	public int mask;

	public static readonly TeamMask none;
	public static readonly TeamMask playerTeam;
	public static readonly TeamMask enemyTeam;

	public static readonly TeamMask all = default(TeamMask);
}
