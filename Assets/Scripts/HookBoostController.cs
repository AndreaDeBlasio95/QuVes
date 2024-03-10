using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookBoostController : MonoBehaviour
{
    [SerializeField] private MainCharacterController _mainCharacterController;
    public bool[] hookBoostHits;
    public Animator[] hookBoostAnimators;
    public float timerResetHits;

    // Start is called before the first frame update
    private void OnEnable()
    {
        StartCoroutine(ResetHits());
        hookBoostHits[0] = false;
        hookBoostHits[1] = false;
    }
    public void SetHit(int _indexHookBoost)
    {
        hookBoostHits[_indexHookBoost] = true;
        if (hookBoostHits[0] == true && hookBoostHits[1] == true)
        {
            hookBoostAnimators[0].gameObject.SetActive(true);
            hookBoostAnimators[1].gameObject.SetActive(true);

            _mainCharacterController._frozenHookBoostOrientation = _mainCharacterController.gameObject.transform.rotation;
            _mainCharacterController.SetHookBoost(true);
        }
    }
    private IEnumerator ResetHits()
    {
        yield return new WaitForSeconds(timerResetHits);
        if (hookBoostHits[0] == true && hookBoostHits[1] == true)
        {
            _mainCharacterController.HookBoostJump();
        }
        hookBoostHits[0] = false;
        hookBoostHits[1] = false;
        hookBoostAnimators[0].gameObject.SetActive(false);
        hookBoostAnimators[1].gameObject.SetActive(false);
        _mainCharacterController.SetHookBoost(false);
        gameObject.SetActive(false);
    }
}
