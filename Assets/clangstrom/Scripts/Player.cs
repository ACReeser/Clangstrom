using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class Player : NetworkBehaviour {
    public GameObject ExplosionPrefab;
    public enum DamageSource { SniperRifle, LaserBeam, Gravity }
    public Transform[] disableOnRagdoll;
    public Light ExplosionLight;
    public ParticleSystem ThermalExplosion;

    [SyncVar(hook = "OnHealthChange")]
    public int Health = 100;
    [SyncVar]
    public int MaxHealth = 100;

    [SyncVar(hook = "OnEnergyChange")]
    public int Energy = 100;
    [SyncVar]
    public int MaxEnergy = 100;

    [SyncVar]
    public int Kills = 0;
    [SyncVar]
    public int Deaths = 0;

    [SyncVar]
    public string PlayerName = "PlayerName";

    internal bool IsDead = false;
    private int EnergyPerHalfSecond = 1;
    private RigidbodyFPS inputScript;

    [Server]
    internal void TakeDamage(Player dealer, int damage, DamageSource source, uint obstacleDealerId = 0)
    {
        Health -= damage;

        if (Health <= 0)
        {
            Health = 0;

            if (dealer != null)
            {
                dealer.Kills++;
                KillPlayer(dealer.netId.Value, source);
            }
            else
            {
                KillPlayer(obstacleDealerId, source);
            }
        }
        else
        {
            if (dealer != null)
                RpcDamage(dealer.netId.Value, damage, playerControllerId);
            else
                RpcDamage(obstacleDealerId, damage, playerControllerId);
        }
    }

    [ClientRpc]
    private void RpcDamage(uint dealer, int damage, short taker)
    {
        Debug.Log(taker +" took damage, I am "+playerControllerId);
        if (isLocalPlayer && playerControllerId == taker)
        {
            gui.StartDamageHit(damage);
        }
    }

    [Server]
    public void KillPlayer(uint killerId, DamageSource source)
    {
        IsDead = true;
        DoKillPlayer(killerId, source);
    }

    [Server]
    private void DoKillPlayer(uint killerId, DamageSource source)
    {
        Health = 0;
        Energy = 0;
        Deaths++;
        
        var explosion = (GameObject)Instantiate(
             this.ExplosionPrefab,
             this.transform.position,
             Quaternion.identity);
        NetworkServer.Spawn(explosion);

        RagdollPlayer();
        RpcKillPlayer(netId.Value, killerId, source);

        StartCoroutine(RespawnAfter(3f));
    }

    [Server]
    private IEnumerator RespawnAfter(float v)
    {
        yield return new WaitForSeconds(v);

        Respawn();
    }

    [Server]
    private void Respawn()
    {
        var spawn = NetworkManager.singleton.GetStartPosition();
        var newPlayerObj = (GameObject)Instantiate(NetworkManager.singleton.playerPrefab, spawn.position, spawn.rotation);
        NetworkServer.Destroy(this.gameObject);
        Destroy(this.gameObject);
        var newPlayer = newPlayerObj.GetComponent<Player>();
        newPlayer.Health = newPlayer.MaxHealth;
        newPlayer.Energy = newPlayer.MaxEnergy;
        Debug.Log("Setting deaths");
        newPlayer.Deaths = Deaths; //todo: use connectionToClient.connectionId to store k/d, and have client pull from server to fill this out
        newPlayer.Kills = Kills;
        newPlayer.PlayerName = PlayerName;
        NetworkServer.ReplacePlayerForConnection(this.connectionToClient, newPlayerObj, this.playerControllerId);
        Debug.Log("Calling respawn");
        RpcRespawn(newPlayer.netId.Value);
    }

    [ClientRpc]
    internal void RpcRespawn(uint respawnId)
    {
        //not received, probably because is being created after the rpc goes out
        Debug.Log("Receiving respawn");
        if (netId.Value == respawnId && isLocalPlayer)
        {
            Debug.Log("I'm respawning!");
            gui.Refresh(this);
        }
    }

    public void OnHealthChange(int health)
    {
        Health = health;

        if (isLocalPlayer)
        {
            gui.Refresh(this);
        }
    }

    public void OnEnergyChange(int energy)
    {
        Energy = energy;

        if (isLocalPlayer)
            gui.Refresh(this);
    }

    public override void OnStartLocalPlayer()
    {
        if (isLocalPlayer)
        {
            Debug.Log("I'm starting up!");
            gui.Refresh(this);
            gui.SetPlayerName(this.PlayerName);
            GUIAnchor.Instance.SetCursor(false);
        }
    }

    [ClientRpc]
    public void RpcKillPlayer(uint killedID, uint killerID, DamageSource source)
    {
        if (netId.Value == killedID)
        {
            RagdollPlayer();
            foreach (Camera c in GetComponentsInChildren(typeof(Camera)))
            {
                c.transform.localPosition -= Vector3.back * 3f;
            }

        }

        if (isLocalPlayer)
            gui.Refresh(this);
    }

    private void RagdollPlayer()
    {
        GetComponent<RigidbodyFPS>().UseGravity = false;
        GetComponent<Rigidbody>().useGravity = true;
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<CapsuleCollider>().height = 1f;
        GetComponent<CapsuleCollider>().center = Vector3.zero;
        
        ThermalExplosion.Play();
        ExplosionLight.enabled = true;
        StartCoroutine(DisableExplosionLightAfter(5));
        foreach(Transform t in disableOnRagdoll)
        {
            t.gameObject.SetActive(false);
        }

        GetComponent<AudioSource>().Play();
    }

    private IEnumerator DisableExplosionLightAfter(int counter)
    {
        while (counter > 0)
        {
            counter--;
            yield return null;
        }
        ExplosionLight.enabled = false;
    }

    void Awake()
    {
        this.VisualsRoot = this.transform.FindChild("renderers");
        this.gui = GetComponent<GUIManager>();
        this.inputScript = GetComponent<RigidbodyFPS>();
    }
    
    private float energyTime = 0f, visualDrainTime = 0f;
    void Update()
    {
        if (isServer)
        {
            if (Energy < 100f)
            {
                energyTime += Time.deltaTime;

                if (energyTime > .5f)
                {
                    Energy += EnergyPerHalfSecond;
                    energyTime -= .5f;
                }
            }

            if (inputScript.HasCamoOn(CamouflageState.Visual))
            {
                visualDrainTime += Time.deltaTime;

                if (visualDrainTime > .5f)
                {
                    visualDrainTime -= .5f;

                    if (Energy > 0)
                    {
                        Energy -= VisualCamo.EnergyPerHalfSecond;
                    }
                    else
                    {
                        inputScript.CmdSetCamouflage(CamouflageState.Visual, false);
                    }
                }
            }
        }
    }
    internal Transform VisualsRoot;
    internal GUIManager gui;
}
