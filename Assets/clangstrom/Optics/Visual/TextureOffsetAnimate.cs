using UnityEngine;
using System.Collections;

public class TextureOffsetAnimate : MonoBehaviour {
    public bool AnimateBumpMap = false;
    public float maxOffset = 1f;
    public float offsetSpeed = .25f;
    public Vector2 axes = Vector2.right;

    private Material mat;

    // Use this for initialization
    void Start () {
        this.mat = GetComponent<Renderer>().material;
	}
	
	// Update is called once per frame
	void Update () {
	    if (AnimateBumpMap)
        {
            mat.SetTextureOffset("_BumpMap", axes * offsetSpeed * Time.time);
        }
        else
        {
            mat.SetTextureOffset("_MainTex", axes * offsetSpeed * Time.time);
        }
	}
}
