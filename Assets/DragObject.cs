using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObject : MonoBehaviour
{
    [Header("Target to Drag")]
    public GameObject targetObject;

    [Header("Drag Settings")]
    public float dragSpeed = 10f;

    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;

    void Start()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("No targetObject assigned in MouseDragWithWeight.");
        }

        mainCamera = Camera.main;
    }

    void Update()
    {
        if (targetObject == null)
            return;

        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == targetObject)
                {
                    isDragging = true;

                    Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(
                        Input.mousePosition.x,
                        Input.mousePosition.y,
                        mainCamera.WorldToScreenPoint(targetObject.transform.position).z));

                    offset = targetObject.transform.position - new Vector3(worldPoint.x, worldPoint.y, targetObject.transform.position.z);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            DragTarget();
        }
    }

    void DragTarget()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            mainCamera.WorldToScreenPoint(targetObject.transform.position).z));

        Vector3 targetPos = new Vector3(worldMousePos.x, worldMousePos.y, targetObject.transform.position.z) + offset;

        targetObject.transform.position = Vector3.Lerp(
            targetObject.transform.position,
            targetPos,
            Time.deltaTime * dragSpeed);
    }
}
