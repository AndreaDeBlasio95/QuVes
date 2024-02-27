using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerStopLongTentacleAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private ControllerLongTentacle _controllerLongTentacle;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            _animator.SetTrigger("Deactive");
            _controllerLongTentacle.DeactiveFromOutsideObject();
        }
    }
}
