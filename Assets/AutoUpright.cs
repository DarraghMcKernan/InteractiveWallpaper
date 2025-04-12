using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoUpright : MonoBehaviour
{
    public float uprightThreshold = 0.7f; // How vertical is "vertical"? (dot product)
    public float checkDelay = 2f;         // Wait this long before trying to stand up
    public float uprightTorque = 20f;     // How strong the stand-up torque is

    private Rigidbody rb;
    private float fallTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Check how "upright" the object is
        float uprightness = Vector3.Dot(transform.up, Vector3.up);

        if (uprightness < uprightThreshold)
        {
            fallTimer += Time.deltaTime;

            if (fallTimer > checkDelay)
            {
                TryUpright();
            }
        }
        else
        {
            fallTimer = 0f;
        }
    }

    void TryUpright()
    {
        // Freeze X and Z position temporarily
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Find the shortest rotation back to upright
        Quaternion uprightRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
        Vector3 torqueAxis;
        float torqueAngle;

        uprightRotation.ToAngleAxis(out torqueAngle, out torqueAxis);

        // Apply torque to rotate back up (Y axis only)
        rb.AddTorque(torqueAxis * (uprightTorque * Mathf.Deg2Rad * torqueAngle));

        // Optionally: unfreeze after delay (in coroutine)
        StartCoroutine(UnfreezeAfterSeconds(1f));
    }

    private System.Collections.IEnumerator UnfreezeAfterSeconds(float time)
    {
        yield return new WaitForSeconds(time);

        // Re-enable full movement
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }
}
