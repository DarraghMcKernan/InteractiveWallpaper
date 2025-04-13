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
    public float uprightThreshold = 0.98f;
    public float checkDelay = 2f;
    public float uprightSpeed = 3f;

    [Header("Wander Settings")]
    public float walkSpeed = 2f;
    public float maxX = 5f;
    public float maxZ = 5f;
    public float targetReachThreshold = 1f;
    public float turnSpeed = 120f;
    public float minAngleToMove = 5f;
    public float minMoveDistance = 2f;

    [Header("Hover Settings")]
    public float hoverHeight = 0.1f;

    [Header("Look Around Settings")]
    public float lookAngle = 45f;
    public float lookSpeed = 90f;
    public float lookPauseTime = 0.5f;

    private Camera mainCamera;
    private Rigidbody rb;

    private bool isDragging = false;
    private bool isGrounded = false;
    private bool isTryingToUpright = false;
    private bool hasHoverStarted = false;
    private bool isLookingAround = false;

    private float fallTimer;
    private Vector3 offset;
    private Vector3 currentVelocity;

    private Vector3 walkTarget;
    private bool hasWalkTarget = false;
    private bool readyToWalk = true;

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
            SmoothUprightRotation();
        }
        else if (!isDragging && isGrounded && IsUpright())
        {
            if (!hasHoverStarted)
            {
                EnterHoverMode();
            }

            MaintainHoverHeight();
            if (!isLookingAround)
                HandleWalking();
        }
    }

    void SmoothUprightRotation()
    {
        Quaternion currentRot = rb.rotation;
        Quaternion targetRot = Quaternion.FromToRotation(transform.up, Vector3.up) * currentRot;
        Quaternion newRotation = Quaternion.RotateTowards(currentRot, targetRot, uprightSpeed);
        rb.MoveRotation(newRotation);

        float uprightness = Vector3.Dot(transform.up, Vector3.up);
        if (uprightness >= uprightThreshold)
        {
            isTryingToUpright = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void MaintainHoverHeight()
    {
        Vector3 pos = rb.position;
        pos.y = hoverHeight;
        rb.MovePosition(pos);
    }

    void EnterHoverMode()
    {
        hasHoverStarted = true;
        rb.useGravity = false;
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
                hasWalkTarget = false;
                hasHoverStarted = false;
                rb.useGravity = true;
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            Vector3 throwVelocity = currentVelocity * throwMultiplier;
            rb.velocity = Vector3.ClampMagnitude(throwVelocity, maxVelocity);

            hasWalkTarget = false;
            readyToWalk = true;
            isLookingAround = false;
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
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
    }

    void CancelUpright()
    {
        isTryingToUpright = false;
        rb.constraints = RigidbodyConstraints.None;
    }

    void HandleWalking()
    {
        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);

        if (hasWalkTarget)
        {
            Vector3 flatTarget = new Vector3(walkTarget.x, 0, walkTarget.z);
            float distanceToTarget = Vector3.Distance(flatPos, flatTarget);

            if (distanceToTarget < targetReachThreshold && !isLookingAround)
            {
                isLookingAround = true;
                readyToWalk = false;
                StartCoroutine(LookAroundBeforeWalking());
                return;
            }

            if (!readyToWalk) return;

            Vector3 direction = (flatTarget - flatPos).normalized;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(-direction);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
            }

            float angle = Vector3.Angle(transform.forward, -direction);

            if (angle < minAngleToMove)
            {
                Vector3 desiredVelocity = direction * walkSpeed;
                rb.velocity = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
        else if (!isLookingAround && readyToWalk)
        {
            StartCoroutine(LookAroundBeforeWalking());
            isLookingAround = true;
            readyToWalk = false;
        }
    }


    IEnumerator LookAroundBeforeWalking()
    {
        isLookingAround = true;
        rb.velocity = Vector3.zero;

        yield return new WaitForSeconds(lookPauseTime);

        float totalRotation = 0f;
        while (totalRotation < lookAngle)
        {
            float step = lookSpeed * Time.deltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0, -step, 0));
            totalRotation += step;
            yield return null;
        }

        yield return new WaitForSeconds(lookPauseTime);

        totalRotation = 0f;
        while (totalRotation < lookAngle * 2)
        {
            float step = lookSpeed * Time.deltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0, step, 0));
            totalRotation += step;
            yield return null;
        }

        yield return new WaitForSeconds(lookPauseTime);

        totalRotation = 0f;
        while (totalRotation < lookAngle)
        {
            float step = lookSpeed * Time.deltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0, -step, 0));
            totalRotation += step;
            yield return null;
        }

        yield return new WaitForSeconds(lookPauseTime);

        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 newTarget;
        do
        {
            newTarget = new Vector3(
                Random.Range(-maxX, maxX),
                hoverHeight,
                Random.Range(-maxZ, maxZ)
            );
        } while (Vector3.Distance(flatPos, new Vector3(newTarget.x, 0, newTarget.z)) < minMoveDistance);

        walkTarget = newTarget;
        hasWalkTarget = true;
        readyToWalk = true;
        isLookingAround = false;
    }

    bool IsUpright()
    {
        return Vector3.Dot(transform.up, Vector3.up) >= uprightThreshold;
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