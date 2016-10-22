using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class SniperRifle : NetworkBehaviour
{
    public GameObject smokeTrail, staticBall, ShellPrefab;
    public AudioSource audioS;
    public Light muzzleFlash;
    public ParticleSystem muzzleFlashThermal;
    public Transform ejectionPort;

    public float ReloadTime = 1.2f;
    public int Range = 500;
    public int DamageMax = 50;
    public int DamageMin = 33;
    private Component muzzleHalo;
    private bool loaded = true;
    private Player player;

    private GameObject currentShell;
    // This [Command] code is run on the server!
    [Command]
    public void CmdFire(Vector3 origin, Vector3 direction)
    {
        if (!loaded)
            return;

        // create the bullet object locally
        var smokeTrail = (GameObject)Instantiate(
             this.smokeTrail,
             origin,
             Quaternion.identity);
        
        var hitInfo = new RaycastHit();
        var didHit = Physics.Raycast(new Ray()
        {
            origin = origin,
            direction = direction
        }, out hitInfo, Range);

        float trailLength = Range;
        if (didHit)
        {
            trailLength = Vector3.Distance(origin, hitInfo.point);
            if (hitInfo.collider != null)
            {
                Player other = hitInfo.collider.GetComponent<Player>();
                if (other == null)
                {
                    Debug.Log("hit something that wasn't a player");
                }
                else
                {
                    Debug.Log("hit a player " + other.netId);
                    var damage = GetDamage();
                    other.TakeDamage(player, damage, Player.DamageSource.SniperRifle);
                }
            }
            else
            {
                Debug.Log("hit non-collider?");
            }
        }
        else
        {
            Debug.Log("hit nothing");
        }

        var staticBall = (GameObject)Instantiate(
             this.staticBall,
             origin + direction.normalized * .5f,
             Quaternion.identity);

        Destroy(staticBall, this.audioS.clip.length);

        // spawn the bullet on the clients
        NetworkServer.Spawn(smokeTrail);
        NetworkServer.Spawn(staticBall);
        
        loaded = false;
        audioS.PlayOneShot(audioS.clip);

        // when the bullet is destroyed on the server it will automaticaly be destroyed on clients
        Destroy(smokeTrail, 4.0f);
        StartCoroutine(Reload());
        RpcFireSniper(this.netId, 
            ReloadTime, 
            smokeTrail.GetComponent<NetworkIdentity>().netId,
            origin,
            direction,
            trailLength);
    }

    [ClientRpc]
    void RpcFireSniper(
        NetworkInstanceId firer, 
        float reloadTime, 
        NetworkInstanceId smokeTrailID,
        Vector3 origin,
        Vector3 direction,
        float trailLength)
    {
        if (isLocalPlayer)
        {
            GetComponent<GUIManager>().Reload(reloadTime);
        }

        if (netId == firer)
        {
            SetMuzzleFlash(true);
            StartCoroutine(DisableMuzzleFlash(4));
        }

        SetSmoke(ClientScene.FindLocalObject(smokeTrailID), origin, direction, trailLength); 
    }

    private void SetSmoke(GameObject smokeTrail, Vector3 origin, Vector3 direction, float trailLength)
    {
        var line1 = smokeTrail.GetComponent<LineRenderer>();
        var line2 = smokeTrail.transform.GetChild(0).GetComponent<LineRenderer>();
        SetSmokeTrail(origin, direction, trailLength, line1);
        SetSmokeTrail(origin, direction, trailLength, line2);
    }

    private void SetMuzzleFlash(bool state)
    {
        if (currentShell == null)
        {
            currentShell = (GameObject)GameObject.Instantiate(ShellPrefab, ejectionPort.position, ejectionPort.rotation);
        }
        currentShell.transform.position = ejectionPort.position;
        currentShell.transform.rotation = ejectionPort.rotation;
        currentShell.SetActive(true);
        currentShell.GetComponent<Rigidbody>().AddForce(ejectionPort.up * 3f, ForceMode.Impulse);

        muzzleFlash.enabled = state;
        (muzzleHalo as Behaviour).enabled = state;
        muzzleFlashThermal.Play();
    }

    private IEnumerator DisableMuzzleFlash(int frames)
    {
        while(frames > 0)
        {
            frames--;
            yield return null;
        }
        SetMuzzleFlash(false);
    }

    void Awake()
    {
        this.player = GetComponent<Player>();
        this.muzzleHalo = muzzleFlash.transform.GetComponent("Halo");
    }

    private IEnumerator Reload()
    {
        yield return new WaitForSeconds(ReloadTime);
        loaded = true;
        currentShell.SetActive(false);
    }

    private void SetSmokeTrail(Vector3 origin, Vector3 direction, float trailLength, LineRenderer line1)
    {
        line1.SetVertexCount(2);
        line1.SetPosition(0, origin + Vector3.down * .1f);
        line1.SetPosition(1, origin + direction * trailLength);
    }

    private int GetDamage()
    {
        return UnityEngine.Random.Range(DamageMin, DamageMax + 1);
    }
}
