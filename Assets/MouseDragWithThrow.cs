using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MouseDragWithThrowAndGravity : MonoBehaviour
{
    [Header("Drag Settings")]
    public float dragSpeed = 20f;
    public float throwMultiplier = 10f;
    public float maxVelocity = 15f;

    [Header("Upright Settings")]
    public string groundTag = "Ground";
    public float uprightThreshold = 0.7f;
    public float checkDelay = 2f;
    public float uprightTorque = 20f;

    private Camera mainCamera;
    private Rigidbody rb;

    private bool isDragging = false;
    private bool isGrounded = false;
    private bool isTryingToUpright = false;

    private float fallTimer;
    private Vector3 offset;
    private Vector3 currentVelocity;

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.angularDrag = 0.05f;
    }

    void Update()
    {
        HandleMouseInput();
        CheckIfShouldUpright();
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(transform.position);
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                screenPoint.z));

            Vector3 targetPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z) + offset;
            currentVelocity = (targetPos - transform.position) * dragSpeed;

            rb.velocity = Vector3.ClampMagnitude(currentVelocity, maxVelocity);
        }
        else if (isTryingToUpright)
        {
            // Continuously apply torque until upright
            Quaternion uprightRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
            uprightRotation.ToAngleAxis(out float torqueAngle, out Vector3 torqueAxis);

            rb.AddTorque(torqueAxis * (uprightTorque * Mathf.Deg2Rad * torqueAngle));

            float uprightness = Vector3.Dot(transform.up, Vector3.up);
            if (uprightness >= uprightThreshold)
            {
                isTryingToUpright = false;
                rb.constraints = RigidbodyConstraints.None;
            }
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
            {
                isDragging = true;

                Vector3 screenPoint = mainCamera.WorldToScreenPoint(transform.position);
                Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(
                    Input.mousePosition.x,
                    Input.mousePosition.y,
                    screenPoint.z));

                offset = transform.position - new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
                rb.velocity = Vector3.zero;

                CancelUpright();
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            Vector3 throwVelocity = currentVelocity * throwMultiplier;
            rb.velocity = Vector3.ClampMagnitude(throwVelocity, maxVelocity);
        }
    }

    void CheckIfShouldUpright()
    {
        float uprightness = Vector3.Dot(transform.up, Vector3.up);

        if (!isDragging && isGrounded && uprightness < uprightThreshold)
        {
            fallTimer += Time.deltaTime;

            if (fallTimer >= checkDelay && !isTryingToUpright)
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
        isTryingToUpright = true;

        // Freeze position so it doesn't slide while trying to upright
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
    }

    void CancelUpright()
    {
        isTryingToUpright = false;
        rb.constraints = RigidbodyConstraints.None;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(groundTag))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag(groundTag))
        {
            isGrounded = false;
        }
    }
}
