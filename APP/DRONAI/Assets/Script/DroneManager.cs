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
        StartCoroutine(IterateDroneRoutine(0.1f));
        //StartCoroutine(CircleFirstRoutine(0.4f));
    }
    #endregion

    #region Physics
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
    #endregion

    #region Formation

    public void PickDrones(int n = 5)
    {
        print("PickDrones Call");

        List<string> pickDronesList = new List<string>();
        pickDronesList.Add("Drone_0");
        pickDronesList.Add("Drone_7");
        pickDronesList.Add("Drone_6");
        pickDronesList.Add("Drone_20");
        pickDronesList.Add("Drone_3");

        foreach (string droneID in pickDronesList)
        {
            drones[droneID].MoveUp(2f);
            InsertDroneFormation(droneID, drones[pickDronesList[0]]);
        }

        foreach (string droneID in pickDronesList) {
            print(droneID + "의 부모는 " + drones[droneID].droneGroup.Parent?.name);
        }
    }

    private void InsertDroneFormation(string droneID, Drone headDrone)
    {
        if (drones[droneID] == headDrone)
        {
            // head drone일 경우
        }
        else
        {
            bool isLeft = true;

            while (headDrone)
            {
                // 부모 설정
                drones[droneID].AssignParent(headDrone);

                // Id of drones로 비교
                if (NameCompare(drones[droneID].name, headDrone.name))
                {
                    //print("left : " + drones[droneID].name + " and " + headDrone.name);
                    headDrone = headDrone.droneGroup.LeftChild;

                    isLeft = true;
                }
                else
                {
                    //print("right : " + drones[droneID].name + " and " + headDrone.name);
                    headDrone = headDrone.droneGroup.RightChild;

                    isLeft = false;
                }
            }

            // 부모의 자식 설정
            if (isLeft) drones[droneID].droneGroup.Parent.droneGroup.LeftChild = drones[droneID];
            else drones[droneID].droneGroup.Parent.droneGroup.RightChild = drones[droneID];
        }
    }

    /// <summary>
    /// 드론 이름 비교 함수
    /// </summary>
    /// <param name="name1"></param>
    /// <param name="name2"></param>
    /// <returns></returns>
    private bool NameCompare(string name1, string name2)
    {
        int length1 = name1.Length, length2 = name2.Length;
        if (length1 < length2) return true;
        else if (length1 == length2)
        {
            for (int i = 0; i < length1; i++)
            {
                if (name1[i] < name2[i]) return true;
                else if (name1[i] > name2[i]) break;
            }
            return false;
        }
        else return false;
    }

    #endregion

    #region Getter and Setter
    public bool ContainsDrone(string id)
    {
        return drones.ContainsKey(id);
    }
    public Vector3 GetDronePositionById(string id)
    {
        return drones[id].transform.position;
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
}
