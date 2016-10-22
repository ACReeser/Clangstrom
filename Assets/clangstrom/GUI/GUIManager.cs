using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityStandardAssets.ImageEffects;

public class GUIManager : NetworkBehaviour {
    public NoiseAndScratches UINoise;
    public Color ActiveSpectrumTextColor;

    private WeaponState CrosshairType;

    public void Refresh(Player p)
    {
        GUIAnchor.Instance.healthTxt.text = string.Format("{0} Health", p.Health);
        GUIAnchor.Instance.healthImg.fillAmount = p.Health / (float)p.MaxHealth;

        GUIAnchor.Instance.energyTxt.text = string.Format("{0} Energy", p.Energy);
        GUIAnchor.Instance.energyImg.fillAmount = p.Energy / (float)p.MaxEnergy;

        GUIAnchor.Instance.KDRatioTxt.text = String.Format("{0}K/{1}D", p.Kills, p.Deaths);
    }

    public void StartDamageHit(int amount)
    {
        if (amount > 33)
        {
            damageGrainIntensityMax = 2f;
            damageGrainSizeMax = 4f;
            damageGrainDuration = .75f;
        }
        else
        {
            damageGrainIntensityMax = 1f;
            damageGrainSizeMax = 2f;
            damageGrainDuration = .5f;
        }
        
        IsShowingGrainDamage = true;
        UINoise.enabled = true;
    }

    public void UpdateCurrentSpectrum(ViewState spectrum)
    {
        GUIAnchor.Instance.visualSpectrumTxt.color = spectrum == ViewState.Visual ? ActiveSpectrumTextColor : Color.white;
        GUIAnchor.Instance.thermalSpectrumTxt.color = spectrum == ViewState.Thermal ? ActiveSpectrumTextColor : Color.white;
        GUIAnchor.Instance.echoSpectrumTxt.color = spectrum == ViewState.Echolocation ? ActiveSpectrumTextColor : Color.white;
    }

    public void SetPlayerName(string name)
    {
        GUIAnchor.Instance.PlayerNameTxt.text = name;
    }

    internal void UpdateWeapon(WeaponState activeWeapon)
    {
        CrosshairType = activeWeapon;
        GUIAnchor.Instance.laserCrosshair.enabled = CrosshairType == WeaponState.Laser_Beam;
        GUIAnchor.Instance.crosshair.enabled = CrosshairType == WeaponState.Sniper_Rifle;
        GUIAnchor.Instance.grenadeCrosshair.enabled = CrosshairType == WeaponState.Grenade_Launcher;

        GUIAnchor.Instance.weaponTxt.text = activeWeapon.ToString().Replace('_', ' ');
        weaponTextFadeOut = true;
        weaponTextOpacity = 1f;
    }

    internal bool IsReloading = false; //needs to be per-weapon?
    private float reloadDuration = 0f;
    private float currentReloadTime = 0f;
    internal void Reload(float time)
    {
        IsReloading = true;
        currentReloadTime = 0f;
        reloadDuration = time;
    }

    private bool IsShowingGrainDamage = false;
    private float damageGrainDuration = 0f;
    private float damageGrainValue = 0f;
    private float damageGrainIntensityMax = 0.1f;
    private float damageGrainSizeMax = 4f;
    private float weaponTextOpacity = 1f;
    private bool weaponTextFadeOut = true;

    void Update()
    {
        if (IsReloading)
        {
            GUIAnchor.Instance.crosshair.fillAmount = currentReloadTime / reloadDuration;
            currentReloadTime += Time.deltaTime;
            if (currentReloadTime > reloadDuration)
            {
                IsReloading = false;
                GUIAnchor.Instance.crosshair.fillAmount = 1f;
            }
        }

        if (IsShowingGrainDamage)
        {
            damageGrainValue += Time.deltaTime * 1 / damageGrainDuration;

            //UINoise.grainIntensityMax = Mathfx.Hermite(0, damageGrainIntensityMax, Mathf.Clamp(damageGrainValue, 0f, 1f));
            //UINoise.grainSize = Mathfx.Hermite(0, damageGrainSizeMax, Mathf.Clamp(damageGrainValue, 0f, 1f));
            //UINoise.grainIntensityMax = damageGrainIntensityMax * Mathfx.Bounce(Mathf.Clamp(damageGrainValue, 0f, 1f));
            //UINoise.grainSize = damageGrainSizeMax * Mathfx.Bounce(Mathf.Clamp(damageGrainValue, 0f, 1f));
            UINoise.grainIntensityMax = damageGrainIntensityMax * Mathfx.SingleBounce(Mathf.Clamp(damageGrainValue, 0f, 1f));
            UINoise.grainSize = damageGrainSizeMax * Mathfx.SingleBounce(Mathf.Clamp(damageGrainValue, 0f, 1f));

            if (damageGrainValue > damageGrainDuration)
            {
                damageGrainValue = 0;
                IsShowingGrainDamage = false;
                UINoise.enabled = false;
            }
        }

        if (weaponTextFadeOut)
        {
            Color c = GUIAnchor.Instance.weaponTxt.color;

            weaponTextOpacity = Math.Max(0f, weaponTextOpacity - Time.deltaTime); //1s fade out
            c.a = weaponTextOpacity;

            GUIAnchor.Instance.weaponTxt.color = c;

            if (weaponTextOpacity <= 0f)
            {
                weaponTextFadeOut = false;
                weaponTextOpacity = 1f;
            }
        }
    }

    internal void Beam()
    {
        
    }
}
