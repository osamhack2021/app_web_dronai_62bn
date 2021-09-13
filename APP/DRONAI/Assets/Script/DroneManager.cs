using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


public class DroneManager : SerializedMonoBehaviour
{
    [SerializeField, BoxGroup("SPAWN SETTING")] private GameObject dronePrefab = default;
    [SerializeField, BoxGroup("SPAWN SETTING")] private Transform dronesParent = default;
    [SerializeField, BoxGroup("SPAWN SETTING")] private int spawningSize = 10;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(0, 10)] private float spawningHeight = 1f;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(1, 20)] private float spawningDistance = 2f;
    [SerializeField] private Dictionary<string, Drone> drones = new Dictionary<string, Drone>();



    #region Editor 

    [ButtonGroup("Drone Spawning"), Button(ButtonSizes.Medium)]
    private void InstantiateDrone()
    {
        if (dronePrefab == null || dronesParent == null) return;

        // Variable declare
        float x = -(spawningDistance * (spawningSize / 2) - (spawningSize % 2 == 0 ? (spawningDistance * 0.5f) : 0));
        float y = -(spawningDistance * (spawningSize / 2) - (spawningSize % 2 == 0 ? (spawningDistance * 0.5f) : 0));

        string droneName = string.Empty;
        int droneCnt = 0;

        // Clearing the drone list
        Cleanup();

        int flip = 1;
        // Assigning
        for (int i = 0; i < spawningSize; i++)
        {
            for (int j = 0; j < spawningSize; j++)
            {
                droneName = "Drone_" + (droneCnt++);

                Drone drone = Instantiate(dronePrefab, new Vector3(x, spawningHeight, y), Quaternion.identity, dronesParent).GetComponent<Drone>();
                drone.Initialize(droneName, 2, this);
                drones.Add(droneName, drone);

                x += flip * spawningDistance;
            }
            x = flip * (spawningDistance * (spawningSize / 2) - (spawningSize % 2 == 0 ? (spawningDistance * 0.5f) : 0));
            y += spawningDistance;
            flip *= -1;
        }
    }

    [GUIColor(1, 0, 0), ButtonGroup("Drone Spawning"), Button(ButtonSizes.Medium)]
    private void Cleanup()
    {
        // Clearing the drone list
        if (drones.Count > 0)
        {
            foreach (Drone target in drones.Values)
            {
                if (target == null) continue;
                if (Application.isEditor) DestroyImmediate(target.gameObject);
                else if (Application.isPlaying) Destroy(target.gameObject);
            }
            drones.Clear();
        }

        int until = dronesParent.childCount;
        for (int i = 0; i < until; i++)
        {
            if (Application.isEditor) DestroyImmediate(dronesParent.GetChild(i).gameObject);
            else if (Application.isPlaying) Destroy(dronesParent.GetChild(i).gameObject);
        }
    }
    #endregion

    #region Life cycle
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        StartCoroutine(IterateDroneRoutine(0.4f));
        //StartCoroutine(CircleFirstRoutine(0.4f));
    }
    #endregion

    #region Action and Events
    public void OnDroneDestroy(string droneId)
    {
        try
        {
            // drones.Remove(droneId);
        }
        catch
        {
            print("Wrong drone id");
        }

    }
    #endregion


    public void MoveSingleDrone(string id, Vector3 position)
    {
        drones[id].MoveTo(position);
    }


    private IEnumerator IterateDroneRoutine(float delay)
    {
        foreach (Drone target in drones.Values)
        {
            target.MoveUp(4f);
            yield return new WaitForSeconds(delay);
        }
        yield break;
    }

    private IEnumerator CircleFirstRoutine(float delay)
    {
        int index = 0;
        float divide = 360 / drones.Count;
        foreach (Drone target in drones.Values)
        {
            target.MoveUp(4f, delay);

            yield return new WaitForSeconds(delay);

            StartCoroutine(CircleSecondRoutine(target, divide * index));
            index++;
        }
        yield return new WaitForSeconds(2f);

        yield break;
    }
    private IEnumerator CircleSecondRoutine(Drone target, float targetAngle)
    {
        Vector2 result = new Vector2(target.Z, target.X) - Vector2.zero;
        float angle = Mathf.Atan2(result.y, result.x) * Mathf.Rad2Deg;

        float radius = 4f;

        for (; ; )
        {
            radius = Mathf.Lerp(radius, 10f, Time.deltaTime);

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;


            if (Mathf.Abs(angle) >= targetAngle)
            {
                target.MoveTo(new Vector3(x, 8f, z));
            }
            else
            {
                angle += Time.deltaTime * 0.5f;
            }
            if (Mathf.Abs(angle) > 360) angle = 0;

            

            yield return null;
        }
    }
}
