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
        public bool ContainsItem(string droneId)
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

    public class Formation
    {
        public enum State
        {
            Preparing,
            Ready,
            Building,
            Working,
            Finished,
            Decommissioning
        }

        public Dictionary<string, Drone> Drones = new Dictionary<string, Drone>();
        private List<AstarPath> routes = new List<AstarPath>();
        public int PortKey = -1;
        public State Condition = State.Preparing;
        [ReadOnly] public string FormationLog = string.Empty;

        public int DroneCount
        {
            get
            {
                return Drones.Count;
            }
        }
        public Drone HeadDrone
        {
            get
            {
                if (DroneCount == 0) return null;
                return Drones.First().Value.GetHeadDrone();
            }
        }

        public void DefinePortKey(int key)
        {
            PortKey = key;
        }
        public void DefineRoutes(List<AstarPath> routes)
        {
            this.routes = routes;
        }
        public void AddDrone(string id, Drone drone)
        {
            Drones.Add(id, drone);
        }

        public bool RequestReady()
        {
            if (PortKey != -1 && routes.Count > 0 && Drones.Count > 0)
            {
                Condition = State.Ready;
                print("[DRONE FORMATION] Formation ready state 승격 요청 완료");
                return true;
            }
            else
            {
                print("[DRONE FORMATION] Ready state의 요구 조건 불충족, 다시 요청하시오!");
                return false;
            }
        }

    }

    public class Port
    {
        [SerializeField] private List<GameObject> portAreas = new List<GameObject>();
        [SerializeField] private Queue<int> availabe = new Queue<int>();

        private float safetyDistance = 2f;

        public Port()
        {
            UpdatePort();
        }
        public void UpdatePort()
        {
            // 런타임에서는 포트 개수가 변할 수 없습니다
            if (Application.isEditor)
            {
                availabe.Clear();
                if (portAreas.Count > 0)
                {
                    for (int i = 0; i < portAreas.Count; i++)
                    {
                        availabe.Enqueue(i);
                    }
                }
            }
            else
            {
                Debug.LogError("[PORT] 이 함수는 런타임에서 실행할 수 없습니다!!");
            }
        }
        public bool ReservePort(out int key)
        {
            key = -1;
            if (availabe.Count > 0)
            {
                // 예약자에게 PORT 키를 줍니다
                key = availabe.Dequeue();

                return true;
            }
            else
            {
                return false;
            }
        }
        public void ReturnPort(int key)
        {
            if (!availabe.Contains(key))
            {
                availabe.Enqueue(key);
            }
            else
            {
                print("[PORT] 이미 키가 반납 되었습니다");
            }
        }
        public Vector3 GetPortPosition(int key)
        {
            return portAreas[key].transform.position + new Vector3(0, safetyDistance, 0);
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

    [BoxGroup("FORMATION"), OdinSerialize] public List<Formation> formations = new List<Formation>();
    [BoxGroup("FORMATION/PORT"), OdinSerialize] public Port port = new Port();
    [BoxGroup("FORMATION/PORT"), Button(ButtonSizes.Medium)] private void UpdatePort()
    {
        port.UpdatePort();
    }

    [SerializeField, BoxGroup("DEBUG")] private LineRenderer lineRendererPrefab = default;
    [SerializeField, BoxGroup("DEBUG")] private GameObject linePointPrefab = default;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private List<GameObject> linePoints = new List<GameObject>();


    [BoxGroup("GROUP"), SerializeField] public Dictionary<string, Drone> DroneDic = new Dictionary<string, Drone>();
    [BoxGroup("GROUP"), OdinSerialize] public Pool DronePool = new Pool();

    public int TotalDrone
    {
        get
        {
            return DroneDic.Count;
        }
    }
    public int AvailableDrone
    {
        get
        {
            return DronePool.PoolListCount();
        }
    }
    public int WorkingDrone
    {
        get
        {
            return TotalDrone - AvailableDrone;
        }
    }


    // Coroutines
    private Coroutine findRouteRoutine = default;

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

    public void OverviewDroneFormation(List<Vector3> nodes, Action<bool> OnFinished)
    {
        // 예상 경로 추가
        nodes.Insert(0, port.GetPortPosition(0));
        nodes.Insert(0, GetFirstDrone().Position);

        // 지정 받은 경로로 요청
        StartCoroutine(OverviewDroneFormationRoutine(nodes, OnFinished));
    }
    private IEnumerator OverviewDroneFormationRoutine(List<Vector3> nodes, Action<bool> OnFinished)
    {
        // Variables
        bool isFinding = true;
        bool isFindable = false;

        List<AstarPath> routes = new List<AstarPath>();


        for (int i = 1; i < nodes.Count; i++)
        {
            // Variables
            isFinding = true;
            isFindable = false;

            // Find the path
            while (!isFindable)
            {
                AstarPathRequestManager.RequestPath(new PathRequest(nodes[i - 1], nodes[i], false, (Vector3[] waypoints, bool pathSucessful) =>
                {
                    isFindable = pathSucessful;
                    isFinding = false;
                    if (pathSucessful)
                    {
                        routes.Add(new AstarPath(waypoints, nodes[i - 1], 2f));
                    }
                }));
                for (; ; )
                {
                    if (!isFinding)
                    {
                        break;
                    }
                    yield return null;
                }
                if (!isFindable)
                {
                    // 경로를 찾지 못한다면 동적 Pathing 을 포기하고 다시 검색
                    print("[" + name + "] 경로 탐색 실패! 맵을 초기화합니다");
                    AstarPathRequestManager.RequestUpdateGrid();
                }
                yield return null;
            }
        }

        // Draw debug line
        ClearLine();
        for (int i = 0; i < routes.Count; i++)
        {
            DrawLine(routes[i], false);
        }

        // Finalize
        OnFinished?.Invoke(true);
    }


    /// <summary>
    /// 드론 포메이션을 정의해주는 함수
    /// </summary>
    /// <param name="count"></param>
    public void DefineDroneFormation(int count, List<Vector3> requestNodes, Action<bool> OnFinished)
    {
        // 작업 가능한 드론을 가져오는 예제
        if (DronePool.PoolListCount() < count)
        {
            print("[DEFINE FORMATION] 잔여 드론이 사용할 드론보다 적습니다. 잔여드론 : " + DronePool.PoolListCount());
            return;
        }

        Formation targetFormation = new Formation();
        for (int i = 0; i < count; i++)
        {
            Drone targetDrone = DronePool.PopFromPool();
            targetFormation.AddDrone(targetDrone.GetID(), targetDrone);
        }

        StartCoroutine(DefineDroneFormationRoutine(targetFormation, requestNodes, OnFinished));
    }
    private IEnumerator DefineDroneFormationRoutine(Formation targetFormation, List<Vector3> requestNodes, Action<bool> OnFinished)
    {
        int key = -1;
        for (; ; )
        {
            if (port.ReservePort(out key))
            {
                break;
            }
            yield return null;
        }

        // Assign key
        targetFormation.PortKey = key;


        // Finding route
        bool isWorking = true;
        bool isSuccess = false;
        FindRoute(requestNodes, targetFormation, (bool success) =>
        {
            isSuccess = success;
            isWorking = false;
        });
        

        // 경로 탐색 대기
        while (isWorking) yield return null;


        // 만약 경로 요청을 실패 했다면...
        if(!isSuccess)
        {
            foreach(KeyValuePair<String, Drone> item in targetFormation.Drones)
            {
                DronePool.PushToPool(item.Value);
            }
            OnFinished?.Invoke(false);
            yield break;
        }

        // 상태 승격 요청
        if (targetFormation.RequestReady())
        {
            formations.Add(targetFormation);
            OnFinished?.Invoke(true);
        }
        else
        {
            OnFinished?.Invoke(false);
        }
        yield break;
    }


    /// <summary>
    /// 포메이션이 정의된 드론 그룹에 한해서 편대 비행 폼을 구축한다
    /// </summary>
    /// <param name="code"></param>
    public void BuildDroneFormation(int code)
    {
        StartCoroutine(BuildDroneFormationRoutine(code));
    }

    private IEnumerator BuildDroneFormationRoutine(int code)
    {
        Queue<Drone> q = new Queue<Drone>();
        int index = 0;
        int cnt = 0;

        foreach (KeyValuePair<string, Drone> drone in formations[code].Drones)
        {
            q.Enqueue(drone.Value);
            drone.Value.MoveUp(2 + (index * 1f), -1f, 200, () =>
            {
                cnt++;
            });
            index++;
        }

        // 이전 작업이 끝날때까지 기다립니다
        while (cnt < formations[code].DroneCount) yield return null;

        // Dynamic A* 맵 Rebake
        AstarPathRequestManager.RequestUpdateGrid();

        // 재 정의
        index = 0;
        cnt = 0;
        Vector3 destination = port.GetPortPosition(formations[code].PortKey);

        foreach (KeyValuePair<string, Drone> drone in formations[code].Drones)
        {
            drone.Value.DefineFormation(q, destination, () =>
            {
                cnt++;
            });
            yield return new WaitForSeconds(1f);
        }

        // 드론 포메이션 구축 대기
        while (cnt < formations[code].DroneCount) yield return null;


        // 포메이션 구축 완료, 맵 리베이크
        AstarPathRequestManager.RequestUpdateGrid();
    }


    #endregion


    #region Path finding
    public void FindRoute(List<Vector3> nodes, Formation formation, Action<bool> OnFinished)
    {
        if (nodes.Count < 2)
        {
            print("[DRONE ROUTER] 요쳥 노드 부족, 최소 2개의 노드가 필요합니다");
            OnFinished?.Invoke(false);
            return;
        }
        if (findRouteRoutine != null) StopCoroutine(findRouteRoutine);
        findRouteRoutine = StartCoroutine(FindRouteRoutine(nodes, formation, OnFinished));
    }
    private IEnumerator FindRouteRoutine(List<Vector3> nodes, Formation formation, Action<bool> OnFinished)
    {
        // Variables
        bool isFinding = true;
        bool isFindable = false;
        List<AstarPath> routes = new List<AstarPath>();
        for (int i = 1; i < nodes.Count; i++)
        {
            // Find the path
            while (!isFindable)
            {
                AstarPathRequestManager.RequestPath(new PathRequest(nodes[i - 1], nodes[i], false, (Vector3[] waypoints, bool pathSucessful) =>
                {
                    isFindable = pathSucessful;
                    isFinding = false;
                    if (pathSucessful)
                    {
                        routes.Add(new AstarPath(waypoints, nodes[i - 1], 2f));
                    }
                }));
                for (; ; )
                {
                    if (!isFinding)
                    {
                        break;
                    }
                    yield return null;
                }
                if (!isFindable)
                {
                    // 경로를 찾지 못한다면 동적 Pathing 을 포기하고 다시 검색
                    print("[" + name + "] 경로 탐색 실패! 맵을 초기화합니다");
                    AstarPathRequestManager.RequestUpdateGrid();
                }
                yield return null;
            }
        }

        // Assign path to formation
        formation.DefineRoutes(routes);

        // Finalize
        OnFinished?.Invoke(true);

        yield break;
    }
    public List<Node> FindPath(Vector3 start, Vector3 destination)
    {
        Graph.PathFindingMethod method = spaceGraph.ThetaStar;
        List<Node> path = spaceGraph.FindPath(method, start, destination, space);
        return path;
    }
    #endregion


    #region Debug
    private void DrawLine(AstarPath path, bool clear = true)
    {
        // 경로 기록 삭제 요청
        if (clear)
        {
            ClearLine();
        }

        // 경로 시각화
        LineRenderer lr = Instantiate(lineRendererPrefab, transform);
        lr.positionCount = path.LookPoints.Length;
        for (int i = 0; i < path.LookPoints.Length; i++)
        {
            // 포인트 생성 및 기록
            GameObject point = Instantiate(linePointPrefab, transform);
            point.transform.position = path.LookPoints[i];
            linePoints.Add(point);

            // 라인 생성
            lr.SetPosition(i, path.LookPoints[i]);
        }

        // 경로 기록에 추가
        lineRenderers.Add(lr);
    }
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
    public void ClearLine()
    {
        foreach (LineRenderer l in lineRenderers) Destroy(l.gameObject);
        lineRenderers.Clear();

        foreach (GameObject g in linePoints) Destroy(g);
        linePoints.Clear();
    }
    #endregion


    #region Getter and Setter
    public Drone GetFirstDrone()
    {
        return DroneDic.First().Value;
    }
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
