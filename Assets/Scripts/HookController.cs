using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookController : MonoBehaviour
{
    [SerializeField]
    private float rayLength = 50f; // Maximum distance the ray will check for collisions

    [SerializeField]
    private GameObject hookTargetObject;

    // Update is called once per frame
    public Vector3? HookCheck()
    {
        // Create a ray that points forward from the player
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Define the layer mask for the "Wall" layer
        int layerMask = 1 << LayerMask.NameToLayer("Wall");

        // Cast the ray and check if it hits an object
        if (Physics.Raycast(ray, out hit, rayLength, layerMask))
        {
            // Output the name of the object detected in front of the player
            Debug.Log("Detected object: " + hit.collider.gameObject.name);
            // Log the exact hit position
            Debug.Log("Hit position: " + hit.point);

            hookTargetObject = hit.collider.gameObject;
            // Return the hit position
            return hit.point;
        }

        // Return null if no hit is detected
        return null;
    }

    public GameObject HookedObject()
    {
        return hookTargetObject;
    }
}