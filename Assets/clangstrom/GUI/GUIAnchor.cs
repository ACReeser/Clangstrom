using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class GUIAnchor : MonoBehaviour {

    public static GUIAnchor Instance { get; set; }
	
	void Awake () {
        Instance = this;
        this.HUDCanvas = this.GetComponent<Canvas>();
        this.HUDCanvas.enabled = false;
	}

    public Text healthTxt, energyTxt, KDRatioTxt, PlayerNameTxt, weaponTxt;
    public Text visualSpectrumTxt, thermalSpectrumTxt, echoSpectrumTxt;
    public Image healthImg, energyImg, crosshair, crosshairHit, laserCrosshair, grenadeCrosshair;
    public Canvas MenuCanvas;

    internal Canvas HUDCanvas;

    public void SetCursor(bool state)
    {
        Cursor.visible = state;
    }

    public void OnQuitToDesktop()
    {
        Application.Quit();
    }

    internal void SetMenu(bool paused)
    {
        this.SetCursor(paused);
        MenuCanvas.enabled = paused;
    }
}
