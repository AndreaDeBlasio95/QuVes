using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationControllerTarget : MonoBehaviour
{
    public float rotationSpeed = 100.0f;
    public float minX = -45.0f; // Minimum X rotation
    public float maxX = 45.0f;  // Maximum X rotation
    public float minY = -45.0f; // Minimum Y rotation
    public float maxY = 45.0f;  // Maximum Y rotation

    private float eulerAngleX;
    private float eulerAngleY;

    private void OnEnable()
    {
        transform.SetLocalPositionAndRotation(new Vector3(0,1.8f,0), Quaternion.Euler(0,0,0));
    }

    private void Update()
    {
        // Get input from mouse (or replace with your input method)
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = -Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // Accumulate rotation amounts
        eulerAngleX += mouseY;
        eulerAngleY += mouseX;

        // Clamp the angles to the specified ranges
        eulerAngleX = Mathf.Clamp(eulerAngleX, minX, maxX);
        eulerAngleY = Mathf.Clamp(eulerAngleY, minY, maxY);

        // Apply the rotation to the object
        transform.localEulerAngles = new Vector3(eulerAngleX, eulerAngleY, 0.0f);
    }
}

