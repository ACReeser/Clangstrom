using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using VolumetricLines;
using System;

public class LaserBeam : NetworkBehaviour {
    public VolumetricLineBehavior mahLaser;
    public AudioSource audioS;
    public Light laserSight;
    
    public int Range = 500;

    //private int DamageMax = 1;
    //private int DamageMin = 1;
    
    private Player player;

    [SyncVar(hook = "OnSighting")]
    public bool Sighting = false;

    [SyncVar]
    private bool Beaming = false;

    [SyncVar]
    public float BeamLength;
    private MeshRenderer meshRenderer;


    // This [Command] code is run on the server!
    [Command]
    public void CmdFire(bool start)
    {
        if (start && (player.Energy >= 1f))
        {
            Beaming = true;
            ServerFireLaserBeam();
        }
        else
            Beaming = false;

        RpcBeamChange(this.netId, Beaming);
    }

    [Server]
    private void ServerFireLaserBeam()
    {
        if (player.Energy < 1f)
        {
            CmdFire(false);
        }
        else
        {
            player.Energy -= 1;
            ServerRaycast(transform.position, transform.forward);
        }
    }

    [Server]
    private void ServerRaycast(Vector3 origin, Vector3 direction)
    {
        var hitInfo = new RaycastHit();
        var didHit = Physics.Raycast(new Ray()
        {
            origin = origin,
            direction = direction
        }, out hitInfo, Range);

        if (didHit)
        {
            BeamLength = Vector3.Distance(origin, hitInfo.point);
            if (hitInfo.collider != null)
            {
                Player other = hitInfo.collider.GetComponent<Player>();
                if (other == null)
                {
                    //Debug.Log("hit something that wasn't a player");
                }
                else
                {
                    Debug.Log("hit a player " + other.netId);
                    var damage = GetDamage();
                    other.TakeDamage(player, damage, Player.DamageSource.LaserBeam);
                }
            }
            else
            {
                //Debug.Log("hit non-collider?");
            }
        }
        else
        {
            BeamLength = Range;
            //Debug.Log("hit nothing");
        }
    }

    [ClientRpc]
    void RpcBeamChange(NetworkInstanceId firer, bool isStarting)
    {
        if (isLocalPlayer)
        {
            GetComponent<GUIManager>().Beam();
        }
        
        if (netId == firer)
        {
            if (isStarting)
                audioS.Play();
            else
                audioS.Stop();

            mahLaser.enabled = isStarting;
            meshRenderer.enabled = isStarting;
        }
    }
    
    void Awake()
    {
        this.laserSight.enabled = false;
        this.player = GetComponent<Player>();
        this.meshRenderer = this.mahLaser.GetComponent<MeshRenderer>();
    }
    
    void Update()
    {
        if (Beaming)
        {
            if (isServer)
            {
                ServerFireLaserBeam();
            }

            if (isClient)
            {
                SetLaserBeam(BeamLength);
            }
        }
    }

    private void SetLaserBeam(float beamLength)
    {
        mahLaser.SetStartAndEndPoints(Vector3.down * .2f, Vector3.forward * beamLength); //this.transform.TransformPoint(
    }

    private int GetDamage()
    {
        return 1;// UnityEngine.Random.Range(DamageMin, DamageMax + 1);
    }

    public void OnSighting(bool sighting)
    {
        this.Sighting = sighting;
        this.laserSight.enabled = sighting;
    }
}
