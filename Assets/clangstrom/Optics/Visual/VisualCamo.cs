using UnityEngine;
using System.Collections;
using System;

public class VisualCamo : MonoBehaviour {
    public static int EnergyPerHalfSecond = 4;

    public Renderer[] normalVisualRenderers;
    public ParticleSystem thruster;

    public TextureOffsetAnimate offsetScript;
    public MeshRenderer distortRenderer;
    public float MovementAmount {
        set
        {
            this.distortMaterial.SetFloat("_BumpAmt", Mathf.Clamp((value * 256f), 0f, 128f));
        }
    }

    private Material distortMaterial;
    private bool IsDistorting = false;
    private int thrusterMaxParticles;

    // Use this for initialization
    void Start ()
    {
        this.distortMaterial = distortRenderer.material;
        this.distortRenderer.enabled = false;
        this.offsetScript.enabled = false;
        this.thrusterMaxParticles = this.thruster.maxParticles;
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    internal void SetState(bool isOn)
    {
        IsDistorting = isOn;

        distortRenderer.enabled = isOn;
        offsetScript.enabled = isOn;

        foreach(Renderer r in normalVisualRenderers)
        {
            r.enabled = !isOn;
        }
        ParticleSystem.EmissionModule em = thruster.emission;
        em.enabled = !isOn;
    }
}
