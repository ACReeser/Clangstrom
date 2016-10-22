using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class RocketLauncher : NetworkBehaviour {
    public GameObject bulletPrefab;
    internal Collider collide;

    [Command]
    public void CmdFire()
    {
        // This [Command] code is run on the server!

        // create the bullet object locally
        var bullet = (GameObject)Instantiate(
             bulletPrefab,
             transform.position - transform.forward,
             transform.rotation);
        //Quaternion.identity);

        var bulletRigid = bullet.GetComponent<Rigidbody>();
        bulletRigid.gameObject.AddComponent<ConstantForce>().relativeForce = Vector3.forward * 80;
        //bulletRigid.velocity = transform.forward * 8;
        Physics.IgnoreCollision(bullet.transform.GetChild(0).GetComponent<Collider>(), this.collide);


        // spawn the bullet on the clients
        NetworkServer.Spawn(bullet);

        // when the bullet is destroyed on the server it will automaticaly be destroyed on clients
        Destroy(bullet, 4.0f);
    }
}
