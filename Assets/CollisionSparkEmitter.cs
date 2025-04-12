using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSparkEmitter : MonoBehaviour
{
    [Header("Particles")]
    public GameObject sparkPrefab;
    public float minImpactVelocity = 2f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < minImpactVelocity) return;

        if (sparkPrefab != null)
        {
            ContactPoint contact = collision.contacts[0];
            GameObject spark = Instantiate(sparkPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(spark, 2f);
        }
    }
}
