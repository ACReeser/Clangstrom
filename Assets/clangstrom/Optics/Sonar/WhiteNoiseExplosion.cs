using UnityEngine;
using System.Collections;
using System;

public class WhiteNoiseExplosion : MonoBehaviour {

    private float audioStaticLength = 1.9f;
    private bool isExploding = false;
    private Material staticMat1, staticMat2;
    private float audioStaticTime = 0f;
    private MeshRenderer renderer1, renderer2;


    // Use this for initialization
    void Start ()
    {
        this.renderer1 = GetComponent<MeshRenderer>();
        this.renderer2 = transform.GetChild(0).GetComponent<MeshRenderer>();
        this.staticMat1 = this.renderer1.material;
        this.staticMat2 = this.renderer2.material;
        this.renderer2.material = staticMat1;
        this.isExploding = true;
    }
	
	// Update is called once per frame
	void Update () {
	    if (isExploding)
        {
            audioStaticTime += Time.deltaTime;

            float staticValue = audioStaticTime / audioStaticLength;
            //float extraScale = (float)(-Math.Pow(staticValue, 3) - 1.623 * (staticValue * staticValue) + 3 * staticValue);// *maxStaticScale;//;Mathfx.Sinerp(0f, maxStaticScale, staticValue);
            float extraScale = (float)(-Math.Pow(staticValue / 3, 3) - 2.225 * (staticValue * staticValue) + 3 * staticValue);// *maxStaticScale;//;Mathfx.Sinerp(0f, maxStaticScale, staticValue);

            transform.localScale = Vector3.one * 10 * extraScale;//;(extraScale * extraScale * extraScale + extraScale + 1f);
            staticMat1.SetFloat("_Alpha", Mathfx.Sinerp(1f, 0f, staticValue));
            staticMat2.SetFloat("_Alpha", Mathfx.Sinerp(1f, 0f, staticValue));

            if (audioStaticTime > audioStaticLength)
            {
                isExploding = false;
                GameObject.Destroy(this.gameObject);
            }
        }
	}

    internal void Explode(float length)
    {
        this.isExploding = true;
        this.audioStaticLength = length;
    }
}
