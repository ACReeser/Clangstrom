using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour {
    public float speed;
    public Vector3 axis = Vector3.up;

	// Update is called once per frame
	void Update () {
        this.transform.Rotate(this.axis, speed * Time.deltaTime);
	}
}
