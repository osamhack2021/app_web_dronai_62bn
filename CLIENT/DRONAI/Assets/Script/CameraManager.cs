using System.IO;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;


public class CameraManager : MonoBehaviour
{
    // Components
    [BoxGroup("Components"), SerializeField] private Camera mainCamera = default;
    [BoxGroup("Components"), SerializeField] private Camera captureCamera = default;
    [BoxGroup("Components"), SerializeField] private UI uiManager = default;

    // Free camera variables
    private Transform targetTransform = default;
    private Transform defaultTargetTransform = default;
    private Vector3 targetPosition = default;
    private bool isVector = false;
    private Vector3 target
    {
        get
        {
            if (isVector)
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
    [BoxGroup("Property"), SerializeField] private float followingSpeed = 4f;
    [BoxGroup("Property"), SerializeField] private float wheelSpeed = 20f;
    private Vector3 previousPosition;

    public void Initialize(Transform target)
    {
        // Initial target
        defaultTargetTransform = target;
        SetToDefaultTarget();
    }
    private void Update()
    {
        if(uiManager.Interacting) return;
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

            previousPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 newPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;

            float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = direction.y * 180; // camera moves vertically

            mainCamera.transform.position = target;

            mainCamera.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
            mainCamera.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

            mainCamera.transform.Translate(new Vector3(0, 0, -distanceToTarget));

            previousPosition = newPosition;
        }

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followingSpeed);


        if (Input.GetMouseButtonDown(1))
        {
            previousPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 newPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;

            float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = direction.y * 180; // camera moves vertically

            mainCamera.transform.position = target;

            mainCamera.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
            mainCamera.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

            mainCamera.transform.Translate(new Vector3(0, 0, -distanceToTarget));

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

    // Screenshot
    public string TakeScreenShot(Transform target)
    {
        // 경로 정의
        string path = Application.persistentDataPath + "/Capture/";
        DirectoryInfo dir = new DirectoryInfo(path);
        if (!dir.Exists)
        {
            Directory.CreateDirectory(path);
        }

        // 해상도 정의
        int resWidth = Screen.width;
        int resHeight = Screen.height;

        // 저장 경로 정의
        string destination;
        destination = path + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";

        // 캡쳐
        captureCamera.gameObject.SetActive(true);
        Vector3 capturePos = target.position;
        capturePos.z -= 4;
        captureCamera.transform.localPosition = capturePos;

        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        captureCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        Rect rec = new Rect(0, 0, screenShot.width, screenShot.height);
        captureCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(destination, bytes);
        captureCamera.gameObject.SetActive(false);

        // 사진 경로 반환
        return destination;
    }

}
