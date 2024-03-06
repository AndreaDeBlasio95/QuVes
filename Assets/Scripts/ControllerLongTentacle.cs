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
        _animator.SetBool("Reset", false);
        _animator.enabled = true;
    }
    public void DeactiveFromOutsideObject ()
    {
        StartCoroutine(ResetAnimation());
    }

    // Reset Can Perform Hook
    private IEnumerator ResetAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        _animator.SetBool("Reset", true);
        StartCoroutine(DeactiveObject());
    }
    private IEnumerator DeactiveObject()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }
}
