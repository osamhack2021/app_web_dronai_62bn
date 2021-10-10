using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Dronai.Path;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

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

    [SerializeField, BoxGroup("SPAWN SETTING")] private GameObject dronePrefab = default;
    [SerializeField, BoxGroup("SPAWN SETTING")] private Transform droneParent = default;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(0.1f, 20f)] private float droneSpeed = 1f;
    [SerializeField, BoxGroup("SPAWN SETTING")] private int spawningSize = 10;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(0, 10)] private float spawningHeight = 1f;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(1, 20)] private float spawningDistance = 2f;

    [SerializeField, BoxGroup("PATH FINDER SETTING")] private GameObject worldMap = default;
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private float mapSize = 16;
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private int octreeLevel = 8; // 8을 초과한 값을 넣지 않는 편이 좋음
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private Vector3 worldCenter = Vector3.zero;
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private Graph.GraphType graphType = default;
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private bool progressive = true;

    private Octree space = default;
    private Graph spaceGraph = default;

    [SerializeField, BoxGroup("DEBUG")] private LineRenderer lineRendererPrefab = default;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    [BoxGroup("OBJECTS"), SerializeField] public Dictionary<string, Drone> DroneDic = new Dictionary<string, Drone>();
    [BoxGroup("OBJECTS"), OdinSerialize] public Pool DronePool = new Pool();


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

                target.Initialize(droneName, droneSpeed, this);

                // Adding a drone to dictionary and pool
                DroneDic.Add(droneName, target);
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
        if (DroneDic.Count > 0)
        {
            foreach (Drone target in DroneDic.Values)
            {
                if (target == null) continue;
                if (Application.isEditor) DestroyImmediate(target.gameObject);
                else if (Application.isPlaying) Destroy(target.gameObject);
            }
            DroneDic.Clear();
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
        // 드론 초기화 시작
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        // 월드 초기화
        space = progressive ? new ProgressiveOctree(mapSize, worldCenter - Vector3.one * mapSize / 2, octreeLevel) : new Octree(mapSize, worldCenter - Vector3.one * mapSize / 2, octreeLevel);
        space.BuildFromGameObject(worldMap);
        spaceGraph =
            graphType == Graph.GraphType.CENTER ? space.ToCenterGraph() :
            graphType == Graph.GraphType.CORNER ? space.ToCornerGraph() : space.ToCrossedGraph();


        // 드론 부트 업 [대기 상태]
        foreach (Drone target in DroneDic.Values)
        {
            target.MoveUp(4f);
            yield return new WaitForSeconds(0.1f);
        }
        yield break;
    }


    #endregion


    #region Physics
    public void MoveSingleDrone(string id, Vector3 position)
    {
        DroneDic[id].MoveTo(position);
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


        List<Drone> drones = new List<Drone>();
        for (int i = 0; i < n; i++)
        {
            drones.Add(DronePool.PopFromPool());
        }

        working = true;
        for (int i = 0; i < drones.Count; i++)
        {
            if (i == drones.Count - 1)
            {
                drones[i].MoveUp(4f, -1f, 200, () =>
                {
                    working = false;
                });
            }
            else
            {
                drones[i].MoveUp(4f, -1f, 200);
            }
        }

        // 이전 작업이 끝날때까지 기다립니다
        while (working) yield return null;

        // Dynamic A* 맵 Rebake
        AstarPathRequestManager.RequestUpdateGrid();

        int buildFinished = 0;
        Queue<Drone> q = new Queue<Drone>();
        foreach (Drone drone in drones)
        {
            q.Enqueue(drone);
        }
        for (int i = 0; i < drones.Count; i++)
        {
            drones[i].DefineFormation(q, destination, ()=>
            {
                buildFinished++;
            });
            yield return new WaitForSeconds(1f);
        }

        // 드론 포메이션 구축 대기
        while(buildFinished < n)
        {
            yield return null;
        }

        // 포메이션 구축 완료, 맵 리베이크
        AstarPathRequestManager.RequestUpdateGrid();
    }


    #endregion


    #region Path finding
    public List<Node> FindPath(Vector3 start, Vector3 destination)
    {
        Graph.PathFindingMethod method = spaceGraph.ThetaStar;
        List<Node> path = spaceGraph.FindPath(method, start, destination, space);
        return path;
    }
    public void FindDynamicPath(Vector3 start, Vector3 destination, bool history, Action<Vector3[], bool> result)
    {
        AstarPathRequestManager.RequestPath(new PathRequest(start, destination, history, result));
    }
    #endregion


    #region Debug
    private void DrawLine(List<Node> nodes, bool clear = true)
    {
        // 경로 기록 삭제 요청
        if (clear)
        {
            ClearLine();
        }

        // 경로 시각화
        LineRenderer lr = Instantiate(lineRendererPrefab, transform);
        lr.positionCount = nodes.Count;
        for (int i = 0; i < nodes.Count; i++)
        {
            lr.SetPosition(i, nodes[i].center);
        }

        // 경로 기록에 추가
        lineRenderers.Add(lr);
    }
    private void DrawLine(List<Vector3> nodes, bool clear = true)
    {
        // 경로 기록 삭제 요청
        if (clear)
        {
            ClearLine();
        }

        // 경로 시각화
        LineRenderer lr = Instantiate(lineRendererPrefab, transform);
        lr.positionCount = nodes.Count;
        for (int i = 0; i < nodes.Count; i++)
        {
            lr.SetPosition(i, nodes[i]);
        }

        // 경로 기록에 추가
        lineRenderers.Add(lr);
    }
    private void ClearLine()
    {
        foreach (LineRenderer l in lineRenderers) Destroy(l.gameObject);
        lineRenderers.Clear();
    }
    #endregion


    #region Getter and Setter
    public bool ContainsDrone(string id)
    {
        return DroneDic.ContainsKey(id);
    }
    public Vector3 GetDronePositionById(string id)
    {
        return DroneDic[id].transform.position;
    }
    public List<string> GetDronesId()
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, Drone> target in DroneDic)
        {
            result.Add(target.Value.GetID());
        }
        return result;
    }
    #endregion


    #region Destory

    /// <summary>
    /// 드론 단일 객체를 강제로 폭파시키는 실험용 함수
    /// </summary>
    /// <param name="droneId">드론 아이디</param>
    public void DestroyDrone(string droneId)
    {
        Drone target = DroneDic[droneId];
        target.OnDroneCollapsed(gameObject);
    }

    public void OnDroneDestroy(string droneId)
    {
    }

    #endregion
}
