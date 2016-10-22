using UnityEngine;
using System.Collections;

public class LineFadeOut : MonoBehaviour {
    public float DelaySeconds = .3f;
    public float DurationSeconds = 1.6f;
    public LineRenderer[] renderers;

    private float fadeTime = 0f;

	// Update is called once per frame
	void Update () {

        if (fadeTime > DelaySeconds)
        {
            float fadeValue = Mathf.Min(1f, (fadeTime - DelaySeconds) / DurationSeconds);
            float newAlpha = Mathfx.Lerp(1, 0, fadeValue);
            Color newColor = new Color(255, 255, 255, newAlpha);
            foreach (LineRenderer lr in renderers)
            {
                lr.SetColors(newColor, newColor);
            }
        }

        fadeTime += Time.deltaTime;
	}
}
