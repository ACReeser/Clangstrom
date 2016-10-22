using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;
using System;

public class Pinger : MonoBehaviour {
    public Transform pingShell;
    public AudioClip outgoing, incoming;
    public RigidbodyFPS controller;

    private bool audioPinging = false;
    private float sweepTime = 0f;
    private float sweepDuration = 2f;
    private Camera cam;
    private float pingWidth = 25f;
    private float minRange = .01f;
    private float minMaxRange = 4f;
    private float maxRange = 100f;
    //private float pingSpeed = 170f;
    private float pingSpeed = 40f;
    private DepthOfField dof;
    private AudioSource audioOut;

    void Awake()
    {
        this.cam = GetComponent<Camera>();
        this.dof = GetComponent<DepthOfField>();
        this.audioOut = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update () {
	    if (controller.IsPinging)
        {
            var sweepDepth = sweepTime * pingSpeed;

            if (controller.isLocalPlayer)
            {
                cam.farClipPlane = UnityEngine.Mathf.Max(minMaxRange, UnityEngine.Mathf.Min(maxRange, sweepDepth));
                //Debug.Log(cam.farClipPlane);
                this.dof.focalLength = UnityEngine.Mathf.Max(minMaxRange, UnityEngine.Mathf.Min(maxRange, sweepDepth - pingWidth));

            }

            sweepTime += Time.deltaTime;
            if (sweepTime > sweepDuration)
                sweepTime = 0f;
        }

        if (audioPinging != controller.IsPinging)
        {
            audioPinging = controller.IsPinging;
            RefreshPingAudio();
        }
	}

    private void RefreshPingAudio()
    {
        if (audioPinging)
        {
            if (controller.isLocalPlayer)
            {
                this.audioOut.clip = outgoing;
            }
            else
            {
                this.audioOut.clip = incoming;
            }
            this.audioOut.Play();
        }
        else
        {
            this.audioOut.Stop();
        }
    }
}
