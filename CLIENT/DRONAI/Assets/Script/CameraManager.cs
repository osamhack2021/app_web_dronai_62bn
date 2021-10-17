using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;


public class CameraManager : Singleton<CameraManager>
{
    // Components
    [BoxGroup("Components"), SerializeField] private Camera cam;

    // Free camera variables
    private Transform targetTransform = default;
    private Transform defaultTargetTransform = default;
    private Vector3 targetPosition = default;
    private bool isVector = false;
    private Vector3 target
    {
        get
        {
            if(isVector)
            {
                return targetPosition;
            }
            else
            {
                return targetTransform.position;
            }
        }
    }
    [BoxGroup("Property"), SerializeField, Range(4, 100)] private float distanceToTarget = 10;
    private Vector3 previousPosition;

    [BoxGroup("Property"), SerializeField] private float followingSpeed = 4f;
    [BoxGroup("Property"), SerializeField] private float wheelSpeed = 20f;


    public void Initialize(Transform target)
    {
        // Initial target
        defaultTargetTransform = target;
        SetToDefaultTarget();
    }
    private void Update()
    {
        float scroll = -(Input.GetAxis("Mouse ScrollWheel") * wheelSpeed);
        if (Mathf.Abs(scroll) > 0)
        {
            if (scroll > 0 && distanceToTarget > 100)
            {
                distanceToTarget = 100;
            }
            if (scroll < 0 && distanceToTarget < 4)
            {
                distanceToTarget = 4;
            }
            distanceToTarget += scroll;

            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            Vector3 newPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;

            float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = direction.y * 180; // camera moves vertically

            cam.transform.position = target;

            cam.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
            cam.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

            cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));

            previousPosition = newPosition;
        }

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followingSpeed);


        if (Input.GetMouseButtonDown(1))
        {
            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 newPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;

            float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = direction.y * 180; // camera moves vertically

            cam.transform.position = target;

            cam.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
            cam.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

            cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));

            previousPosition = newPosition;
        }
    }

    public void ChangeTarget(Transform target)
    {
        isVector = false;
        targetTransform = target;
    }
    public void ChangeTarget(Vector3 position)
    {
        isVector = true;
        targetPosition = position;
    }
    public void SetToDefaultTarget()
    {
        ChangeTarget(defaultTargetTransform);
    }

}
