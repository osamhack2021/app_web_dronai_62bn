using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{

    [SerializeField] private float speed = 2f;
    private Transform target = default;


    // Coroutines
    private Coroutine cameraRoutine = default;

    public void Initialize()
    {
        if (cameraRoutine != null) StopCoroutine(cameraRoutine);
        // cameraRoutine = StartCoroutine(CameraRoutine());
    }

    public void ChangeFollower(Transform target)
    {
        this.target = target;
    }
    private IEnumerator CameraRoutine()
    {
        for (; ; )
        {
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * speed);
            yield return null;
        }
    }
}
