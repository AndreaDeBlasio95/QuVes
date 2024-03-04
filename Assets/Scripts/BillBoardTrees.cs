using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoardTrees : MonoBehaviour
{
    [Header("Lock Rotation")]
    [SerializeField] private bool lockX;
    [SerializeField] private bool lockY;
    [SerializeField] private bool lockZ;

    private Vector3 originalRotation;

    [SerializeField] private BillboardType billboardType;

    public enum BillboardType { LookAtCamera, CameraForward };

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
    }
    // Update is called once per frame
    void LateUpdate()
    {
        switch (billboardType)
        {
            case BillboardType.LookAtCamera:
                transform.LookAt(Camera.main.transform.position, Vector3.up);
                break;
            case BillboardType.CameraForward:
                transform.LookAt(Camera.main.transform.forward);
                break;
            default:
                break;
        }
        Vector3 rotation = transform.rotation.eulerAngles;
        if (lockX)
        {
            rotation.x = originalRotation.x;
        }
        if (lockY)
        {
            rotation.y = originalRotation.y;
        }
        if (lockX)
        {
            rotation.z = originalRotation.z;
        }
        transform.rotation = Quaternion.Euler(rotation);
    }
}
