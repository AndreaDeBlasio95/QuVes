using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerLongTentacle : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    // Start is called before the first frame update
    void OnEnable()
    {
        _animator.enabled = true;
    }
    public void DeactiveFromOutsideObject ()
    {
        StartCoroutine(DeactiveObject());
    }

    // Reset Can Perform Hook
    private IEnumerator DeactiveObject()
    {
        yield return new WaitForSeconds(1.0f);
        gameObject.SetActive(false);
    }
}
