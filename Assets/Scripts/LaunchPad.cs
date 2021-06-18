using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchPad : MonoBehaviour
{
    public float force = 15.0f;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody otherRB = other.transform.GetComponent<Rigidbody>();

        if (otherRB != null) otherRB.AddForce(transform.forward * force, ForceMode.Impulse);
    }
}
