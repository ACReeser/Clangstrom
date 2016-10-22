using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class KillFloor : NetworkBehaviour {
    
    [Server]
    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<Player>();

        if (player != null && !player.IsDead)
        {
            player.TakeDamage(null, 999, Player.DamageSource.Gravity, netId.Value);
        }
    }
}
