using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ara;

public class WeaponTrailHandler : MonoBehaviour
{
    //private ParticleSystem _weaponTrail;
    private AraTrail _weaponTrail;
    private float initalThickness;

    //public ParticleSystem weaponTrail
    //{
    //    get
    //    {
    //        return _weaponTrail;
    //    }
    //    set
    //    {
    //        _weaponTrail = value;
    //        _weaponTrail.Stop();
    //    }
    //}

    public AraTrail weaponTrail
    {
        get
        {
            return _weaponTrail;
        }
        set
        {
            _weaponTrail = value;
            _weaponTrail.emit = false;
            initalThickness = weaponTrail.thickness;
        }
    }

    public void OnEnable()
    {
        //if (weaponTrail != null)
        //{
        //    weaponTrail.Play();
        //}
        if (weaponTrail != null)
        {
            _weaponTrail.emit = true;
            _weaponTrail.thickness = initalThickness;
        }
    }

    public void OnDisable()
    {
        //if (weaponTrail != null)
        //{
        //    weaponTrail.Stop();
        //}
        if (weaponTrail != null)
        {
            _weaponTrail.emit = false;
            _weaponTrail.thickness = 0;
        }
    }
}
