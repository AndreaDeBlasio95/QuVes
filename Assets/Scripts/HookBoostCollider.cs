using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookBoostCollider : MonoBehaviour
{
    public int id;  // 0 or 1
    public HookBoostController hookBoostController;
    public bool hitWall;

    private void OnEnable()
    {
        hitWall = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall")){
            hitWall = true;
            hookBoostController.SetHit(id);
        }
    }
}
