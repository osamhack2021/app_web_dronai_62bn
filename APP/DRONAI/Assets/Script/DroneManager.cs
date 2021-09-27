using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using System.Linq;

public class DroneManager : SerializedMonoBehaviour
{
    #region Class

    public class Pool
    {
        [BoxGroup("Property"), ShowInInspector, ReadOnly] public int Workable => poolList.Count;
        [BoxGroup("Pool"), Tooltip("작업 가능 드론"), SerializeField] private Dictionary<string, Drone> poolList = new Dictionary<string, Drone>();
        


        public int PoolListCount()
        {
            return poolList.Count;
        }

        // 리스트 안에 item이 있나?
        public bool IsItemInList(string droneId)
        {
            return poolList.ContainsKey(droneId);
        }

        public void DeleteItemInList(Drone item)
        {
            poolList.Remove(item.GetID());
        }

        public void PushToPool(Drone item)
        {
            // 넣기 전에 item이 poolList에 있는지 확인
            //if (IsItemInList(item.GetID())) return;

            item.IsWorking = false;
            poolList.Add(item.GetID(), item);
            print("push -> " + item.GetID());
        }

        public Drone PopFromPool()
        {
            int result = UnityEngine.Random.Range(0, PoolListCount());

            Drone item = poolList.ElementAt(result).Value;
            //poolList.TryGetValue("Drone_" + result, out Drone item);
            item.IsWorking = true;
            poolList.Remove(item.GetID());

            return item;
        }

        private Drone CreateItem(GameObject prefab, Transform parent = null)
        {
            Drone item = UnityEngine.Object.Instantiate(prefab).GetComponent<Drone>();
            item.transform.SetParent(parent);
            item.IsWorking = false;

            return item;
        }

        public void CleanUp()
        {
            // Releasing the list and variable
            poolList.Clear();
        }
    }

    #endregion


    #region Variable

    [SerializeField,BoxGroup("SPAWN SETTING")] private GameObject dronePrefab = default;
    [SerializeField,BoxGroup("SPAWN SETTING")] private Transform droneParent = default;
    [SerializeField, BoxGroup("SPAWN SETTING")] private int spawningSize = 10;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(0, 10)] private float spawningHeight = 1f;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(1, 20)] private float spawningDistance = 2f;
    [BoxGroup("Dictionary"), SerializeField] private Dictionary<string, Drone> droneDic = new Dictionary<string, Drone>();
    [BoxGroup("Pulling"), OdinSerialize] public Pool DronePool = new Pool();

    [BoxGroup("Grid")] public float gridSize = 1;



    #endregion


    #region Editor 

    [ButtonGroup("Drone Spawning"), Button(ButtonSizes.Medium)]
    private void InstantiateDrone()
    {
        // Checking the resources
        if (dronePrefab == null || droneParent == null)
        {
            print("Inspector에서 Prefab과 Parent를 할당했는지 확인하세요.");
            return;
        }

        // Variable declare
        float x = -(spawningDistance * (spawningSize / 2) - (spawningSize % 2 == 0 ? (spawningDistance * 0.5f) : 0));
        float y = -(spawningDistance * (spawningSize / 2) - (spawningSize % 2 == 0 ? (spawningDistance * 0.5f) : 0));

        string droneName = string.Empty;
        int droneCnt = 0, flip = 1;

        // Clearing the drone list
        Cleanup();

        // Instantiate using drone pool
        for (int i = 0; i < spawningSize; i++)
        {
            for (int j = 0; j < spawningSize; j++)
            {
                // Use drone name as id (important!)
                droneName = "Drone_" + (droneCnt++);

                // Pulling a drone from pool
                Drone target = Instantiate(dronePrefab, new Vector3(x, spawningHeight, y), Quaternion.identity, droneParent).GetComponent<Drone>();

                target.Initialize(droneName, 0.5f, this);

                // Adding a drone to dictionary and pool
                droneDic.Add(droneName, target);
                DronePool.PushToPool(target);

                // Fliping the spawning position
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
        if (droneDic.Count > 0)
        {
            foreach (Drone target in droneDic.Values)
            {
                if (target == null) continue;
                if (Application.isEditor) DestroyImmediate(target.gameObject);
                else if (Application.isPlaying) Destroy(target.gameObject);
            }
            droneDic.Clear();
        }

        // Clearing the drone pool
        DronePool.CleanUp();

        // Removing actual objects
        int until = droneParent.childCount;
        for (int i = 0; i < until; i++)
        {
            if (Application.isEditor) DestroyImmediate(droneParent.GetChild(i).gameObject);
            else if (Application.isPlaying) Destroy(droneParent.GetChild(i).gameObject);
        }
    }
    #endregion


    #region Life cycle
    public void Initialize()
    {
        StartCoroutine(IterateDroneRoutine(0.1f));
        //StartCoroutine(CircleFirstRoutine(0.4f));
    }
    #endregion


    #region Physics
    public void MoveSingleDrone(string id, Vector3 position)
    {
        droneDic[id].MoveTo(position);
    }

    private IEnumerator IterateDroneRoutine(float delay)
    {
        foreach (Drone target in droneDic.Values)
        {
            target.MoveUp(4f);
            yield return new WaitForSeconds(delay);
        }
        yield break;
    }
    private IEnumerator CircleFirstRoutine(float delay)
    {
        int index = 0;
        float divide = 360 / droneDic.Count;
        foreach (Drone target in droneDic.Values)
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

    public void BuildDroneFormation(Vector3 destination, int n = 5)
    {
        StartCoroutine(BuildDroneFormationRoutine(destination, n));
    }

    private IEnumerator BuildDroneFormationRoutine(Vector3 destination, int n = 5)
    {
        bool working = false;

        // 작업 가능한 드론을 가져오는 예제
        if (DronePool.PoolListCount() < n)
        {
            print("잔여 드론이 사용할 드론보다 적습니다. 잔여드론 : " + DronePool.PoolListCount());
            yield break;
        }

        // 헤드 드론 및 자식 드론 선출
        Drone head = DronePool.PopFromPool();
        List<Drone> childs = new List<Drone>();
        for (int i = 0; i < n - 1; i++)
        {
            childs.Add(DronePool.PopFromPool());
        }


        head.MoveUp(4f, -1f, 200);

        working = true;
        for (int i = 0; i < childs.Count; i++)
        {
            if (i == childs.Count - 1)
            {
                childs[i].MoveUp(4f, -1f, 200, () =>
                {
                    working = false;
                });
            }
            else
            {
                childs[i].MoveUp(4f, -1f, 200);
            }
        }

        // 이전 작업이 끝날때까지 기다립니다
        while (working) yield return null;

        for (int i=0; i< childs.Count; i++)
        {
            InsertDroneFormation(childs[i].GetID(), head);
        }
        
        head.MoveTo(destination);
        print("잔여 드론 : " + DronePool.PoolListCount());
    }

    private void InsertDroneFormation(string droneID, Drone headDrone, Action OnFinished = null)
    {
        if (droneID.Equals(headDrone.GetID()))
        {
            // head drone일 경우
        }
        else
        {
            bool isLeft = true;

            while (headDrone)
            {
                // 부모 설정
                droneDic[droneID].AssignParent(headDrone);

                // Id of drones로 비교
                if (NameCompare(droneDic[droneID].name, headDrone.name))
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
            if (isLeft) droneDic[droneID].droneGroup.Parent.droneGroup.LeftChild = droneDic[droneID];
            else droneDic[droneID].droneGroup.Parent.droneGroup.RightChild = droneDic[droneID];
        }

        OnFinished?.Invoke();
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
        return droneDic.ContainsKey(id);
    }
    public Vector3 GetDronePositionById(string id)
    {
        return droneDic[id].transform.position;
    }
    public List<string> GetDronesId()
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, Drone> target in droneDic)
        {
            result.Add(target.Value.GetID());
        }
        return result;
    }
    #endregion


    #region Action and Events

    // test용 폭파함수
    public void DestroyNode(string droneId)
    {
        Drone target = droneDic[droneId];
        target.OnDroneCollapsed(gameObject);
    }

    public void OnDroneDestroy(string droneId)
    {
        // 루트부터 탐색해서 삭제할 name of drone과 비교

        Drone root = droneDic[droneId];
        while (root.droneGroup.Parent)
        {
            root = root.droneGroup.Parent;
        }

        root = DeleteDrone(root, droneId);
        ParentReassign(null, root);
    }

    private Drone DeleteDrone(Drone root, string droneID)
    {
        if (root == null) return root;

        if (droneID.Equals(root.name)) // 삭제할 드론
        {
            if (root.droneGroup.LeftChild == null) return root.droneGroup.RightChild;
            if (root.droneGroup.RightChild == null) return root.droneGroup.LeftChild;

            Drone tempDrone = root.droneGroup.RightChild; // 현재 트리에서 루트가 됨
            Drone leftDrone = root.droneGroup.LeftChild; // 루트의 왼쪽 자식노드
            Drone rightDrone = DeleteDrone(root.droneGroup.RightChild, FindMinID(root.droneGroup.RightChild)); // 루트의 오른쪽 자식노드

            root = tempDrone;
            root.droneGroup.LeftChild = leftDrone; ParentReassign(root, root.droneGroup.LeftChild);
            root.droneGroup.RightChild = rightDrone; ParentReassign(root, root.droneGroup.RightChild);
        }
        else if (NameCompare(droneID, root.name))
        {
            root.droneGroup.LeftChild = DeleteDrone(root.droneGroup.LeftChild, droneID);
            ParentReassign(root, root.droneGroup.LeftChild);
        }
        else
        {
            root.droneGroup.RightChild = DeleteDrone(root.droneGroup.RightChild, droneID);
            ParentReassign(root, root.droneGroup.RightChild);
        }

        root.CalulateChildCount();
        return root;
    }

    // 이진 트리에서 최솟값 찾기 -> DeleteDrone함수에서 사용
    private string FindMinID(Drone root)
    {
        string retID = root.name;
        while (root.droneGroup.LeftChild) // 제일 왼쪽으로 가면 됨
        {
            retID = root.droneGroup.LeftChild.name;
            root = root.droneGroup.LeftChild;
        }
        return retID;
    }

    private void ParentReassign(Drone parentDrone, Drone childDrone)
    {
        if (childDrone == null) return;
        childDrone.droneGroup.Parent = parentDrone;
    }

    #endregion
}
