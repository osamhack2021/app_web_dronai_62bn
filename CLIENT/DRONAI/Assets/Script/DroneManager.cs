using Dronai.Data;
using Dronai.Path;
using Dronai.Network;
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;



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
    public class Port
    {
        [SerializeField] private List<GameObject> portAreas = new List<GameObject>();
        [SerializeField] private Queue<int> availabe = new Queue<int>();

        [SerializeField, Range(2, 10)] private float safetyDistance = 4f;

        public Port()
        {
            UpdatePort();
        }
        /// <summary>
        /// Port 상태를 최신화 합니다 [Editor only]
        /// </summary>
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
            Vector3 result = portAreas[key].transform.position;
            result.y += safetyDistance;
            return result;
        }
    }

    #endregion

    #region Variable

    // Instantiate
    [SerializeField, BoxGroup("SPAWN SETTING")] private GameObject dronePrefab = default;
    [SerializeField, BoxGroup("SPAWN SETTING")] private Transform droneParent = default;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(0.1f, 20f)] private float droneSpeed = 1f;
    [SerializeField, BoxGroup("SPAWN SETTING")] private int spawningSize = 10;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(0, 10)] private float spawningHeight = 1f;
    [SerializeField, BoxGroup("SPAWN SETTING"), Range(1, 20)] private float spawningDistance = 2f;


    // Path finding
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private float mapSize = 16;
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private int octreeLevel = 8; // 8을 초과한 값을 넣지 않는 편이 좋음
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private Vector3 worldCenter = Vector3.zero;
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private Graph.GraphType graphType = default;
    [SerializeField, BoxGroup("PATH FINDER SETTING")] private bool progressive = true;
    private Octree space = default;
    private Graph spaceGraph = default;


    // Formation
    [BoxGroup("FORMATION"), OdinSerialize] public List<Formation> Formations = new List<Formation>();
    [BoxGroup("FORMATION/PORT"), OdinSerialize] public Port port = new Port();

    [BoxGroup("FORMATION/PORT"), Button(ButtonSizes.Medium)]
    private void UpdatePort()
    {
        port.UpdatePort();
    }

    // Drone Evnet
    public List<DroneEvent> DroneEvents = new List<DroneEvent>();


    // Debug
    [SerializeField, BoxGroup("DEBUG")] private LineRenderer lineRendererPrefab = default;
    [SerializeField, BoxGroup("DEBUG")] private GameObject linePointPrefab = default;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private List<GameObject> linePoints = new List<GameObject>();


    // Group objects
    [BoxGroup("GROUP"), SerializeField] public Dictionary<string, Drone> DroneDic = new Dictionary<string, Drone>();
    [BoxGroup("GROUP"), OdinSerialize] public Pool DronePool = new Pool();


    // Information
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


    // Events
    [HideInInspector] public Action OnFormationUpdated = default;
    [HideInInspector] public Action OnDroneEventsUpdated = default;


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
        // space.BuildFromGameObject(worldMap);
        spaceGraph =
            graphType == Graph.GraphType.CENTER ? space.ToCenterGraph() :
            graphType == Graph.GraphType.CORNER ? space.ToCornerGraph() : space.ToCrossedGraph();

        yield break;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            AddEvent("Drone_0", GetDroneById("Drone_0").transform);
        }
    }

    #endregion

    #region Physics
    public void MoveSingleDrone(string id, Vector3 position)
    {
        DroneDic[id].MoveTo(position);
    }

    #endregion

    #region Formation
    public void UpdateFormationCondition(int code, Formation.State state)
    {
        Formations[code].UpdateCondition(state);
        OnFormationUpdated?.Invoke();
    }
    public void UpdateFormationRouteIndex(int code, int index)
    {
        Formations[code].UpdatePathPositionIndex(index);
        OnFormationUpdated?.Invoke();
    }


    public void OverviewDroneFormation(List<Vector3> nodes, Action<bool> OnFinished)
    {
        // 예상 경로 추가
        nodes.Insert(0, port.GetPortPosition(0));
        nodes.Insert(0, GetFirstDrone().Position);
        nodes.Insert(nodes.Count, port.GetPortPosition(0));

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
        StartCoroutine(DefineDroneFormationRoutine(count, requestNodes, OnFinished));
    }
    private IEnumerator DefineDroneFormationRoutine(int count, List<Vector3> requestNodes, Action<bool> OnFinished)
    {
        // 포메이션 구조 생성
        Queue<Drone> dronesQueue = new Queue<Drone>();
        Formation targetFormation = new Formation();
        for (int i = 0; i < count; i++)
        {
            Drone targetDrone = DronePool.PopFromPool();
            targetFormation.AddDrone(targetDrone.GetID(), targetDrone);
            dronesQueue.Enqueue(targetDrone);
        }

        // 포메이션의 각 드론들에게 서로의 정보 전달
        foreach (KeyValuePair<String, Drone> item in targetFormation.Drones)
        {
            item.Value.DefineFormation(dronesQueue);
        }

        // Getting port key
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


        // 복귀 경로 추가
        requestNodes.Insert(requestNodes.Count, port.GetPortPosition(key));


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
        if (!isSuccess)
        {
            foreach (KeyValuePair<String, Drone> item in targetFormation.Drones)
            {
                DronePool.PushToPool(item.Value);
            }
            OnFinished?.Invoke(false);
            yield break;
        }


        // 상태 승격 요청
        if (targetFormation.RequestReady())
        {
            Formations.Add(targetFormation);
            OnFormationUpdated?.Invoke();
            OnFinished?.Invoke(true);
        }
        else
        {
            OnFinished?.Invoke(false);
        }

        // Fianalize
        yield break;
    }


    /// <summary>
    /// 포메이션이 정의된 드론 그룹에 한해서 편대 비행 폼을 구축한다
    /// </summary>
    /// <param name="code">포메이션 코드</param>
    public void BuildDroneFormation(int code, Action OnFinished = null)
    {
        if (!Formations[code].Commandable)
        {
            print("[DRONE FORMATION] CLOSING 상태이므로 명령을 받을 수 없습니다!");
            return;
        }
        if (Formations[code].Routine != null) StopCoroutine(Formations[code].Routine);
        Formations[code].Routine = StartCoroutine(BuildDroneFormationRoutine(code, OnFinished));
    }
    private IEnumerator BuildDroneFormationRoutine(int code, Action OnFinished)
    {
        // 상태 정의
        UpdateFormationCondition(code, Formation.State.Building);

        // 변수 정의
        int index = 0;
        int cnt = 0;

        // 비행 준비
        foreach (KeyValuePair<string, Drone> drone in Formations[code].Drones)
        {
            drone.Value.MoveUp(2 + (index * .5f), -1f, 200, () =>
            {
                cnt++;
            });
            index++;
        }

        // 이전 작업이 끝날때까지 기다립니다
        while (cnt < Formations[code].DroneCount) yield return null;

        // Dynamic A* 맵 Rebake
        AstarPathRequestManager.RequestUpdateGrid();

        // 재 정의
        index = 0;
        cnt = 0;
        Vector3 destination = port.GetPortPosition(Formations[code].PortKey);

        foreach (KeyValuePair<string, Drone> drone in Formations[code].Drones)
        {
            drone.Value.BuildFormation(destination, () =>
            {
                cnt++;
            });
            yield return new WaitForSeconds(0.6f);
        }

        // 드론 포메이션 구축 대기
        while (cnt < Formations[code].DroneCount) yield return null;


        // 포메이션 구축 완료, 맵 리베이크
        AstarPathRequestManager.RequestUpdateGrid();


        // Finalize
        UpdateFormationCondition(code, Formation.State.Workable);
        MoveDroneFormation(code);

        OnFinished?.Invoke();
        yield break;
    }


    /// <summary>
    /// 드론 포메이션을 움직여주는 함수
    /// </summary>
    /// <param name="code">포메이션 코드</param>
    public void MoveDroneFormation(int code)
    {
        if (!Formations[code].Commandable)
        {
            print("[DRONE FORMATION] CLOSING 상태이므로 명령을 받을 수 없습니다!");
            return;
        }
        if (Formations[code].Routine != null) StopCoroutine(Formations[code].Routine);
        Formations[code].Routine = StartCoroutine(MoveDroneFormationRoutine(code));
    }
    private IEnumerator MoveDroneFormationRoutine(int code)
    {
        // 상태 정의
        UpdateFormationCondition(code, Formation.State.Working);

        // 변수 정의
        Drone head = Formations[code].HeadDrone;
        List<Vector3> movePoints = Formations[code].GetAllMovePoints();

        bool working = true;
        int index = 0;
        foreach (Vector3 destination in movePoints)
        {
            //재 정의
            working = true;
            head.MoveFormation(destination, () =>
            {
                working = false;
            });

            while (working)
            {
                yield return null;
            }

            // 경로 인덱스 증가
            index++;

            // 다음 경로
            UpdateFormationRouteIndex(code, index);
        }

        // Fianlize
        UpdateFormationCondition(code, Formation.State.Finished);
        CloseDroneFormation(code);
        yield break;
    }


    /// <summary>
    /// 드론 포메이션을 종료합니다.
    /// </summary>
    /// <param name="code">포메이션 코드</param>
    public void CloseDroneFormation(int code)
    {
        if (!Formations[code].Commandable)
        {
            print("[DRONE FORMATION] 이미 CLOSING 상태이므로 명령을 받을 수 없습니다!");
            return;
        }
        if (Formations[code].Routine != null) StopCoroutine(Formations[code].Routine);
        Formations[code].Routine = StartCoroutine(CloseDroneFormationRoutine(code));
    }
    private IEnumerator CloseDroneFormationRoutine(int code)
    {
        // 상태 정의
        UpdateFormationCondition(code, Formation.State.Closing);

        // Dynamic A* 맵 Rebake
        AstarPathRequestManager.RequestUpdateGrid();

        // 정의
        int cnt = 0;
        Vector3 destination = port.GetPortPosition(Formations[code].PortKey);

        foreach (KeyValuePair<string, Drone> drone in Formations[code].Drones)
        {
            drone.Value.CloseFormation();
            drone.Value.Recall(() =>
            {
                cnt++;
            });
            yield return new WaitForSeconds(0.6f);
        }

        // 드론 포메이션 정리 대기
        while (cnt < Formations[code].DroneCount) yield return null;

        // Dynamic A* 맵 Rebake
        AstarPathRequestManager.RequestUpdateGrid();

        // 드론 반환
        foreach (KeyValuePair<String, Drone> item in Formations[code].Drones)
        {
            item.Value.OnDroneClosed();
            DronePool.PushToPool(item.Value);
        }

        // 키 반환
        port.ReturnPort(Formations[code].PortKey);

        // 포메이션 해체
        Formations.RemoveAt(code);
        OnFormationUpdated?.Invoke();

        // Finalize
        yield break;
    }
    #endregion

    #region Path finding

    /// <summary>
    /// 드론 그룹의 순환 경로를 찾아줍니다
    /// </summary>
    /// <param name="nodes">지나가는 지점들</param>
    /// <param name="formation">드론 포메이션</param>
    /// <param name="OnFinished">함수 작업 완료시 콜백</param>
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

    #region Network
    public void AddEvent(string droneId, Transform target)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("발견자 : {0}\n발견 시각 : {1}\n발견 위치 : {2}", droneId, System.DateTime.Now.ToString("yyyy년 MM월 dd일 HH시 mm분 ss초"), target.position.ToString());
        string detail = sb.ToString();
        string imgPath = CameraManager.Instance.TakeScreenShot(target);
        sb.Clear();

        DroneEvent droneEvent = new DroneEvent(droneId, detail, imgPath);
        DroneEvents.Add(droneEvent);

        OnDroneEventsUpdated?.Invoke();
        NetworkManager.Instance.AddEvent(droneEvent);

        // Show alert
        UI.Instance.ShowAlert("드론이 이상요소를 발견하였습니다!");
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

    #region Getter and Setter
    public Drone GetFirstDrone()
    {
        return DroneDic.First().Value;
    }
    public bool ContainsDrone(string id)
    {
        return DroneDic.ContainsKey(id);
    }
    public Drone GetDroneById(string id)
    {
        return DroneDic[id];
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
    public string GetFormationNameById(string id)
    {
        for (int i = 0; i < Formations.Count; i++)
        {
            if (Formations[i].Drones.ContainsKey(id))
            {
                return "그룹 " + i;
            }
        }
        return string.Empty;
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
}
public class Formation
{
    public enum State
    {
        Opening,
        Buildable,
        Building,
        Workable,
        Working,
        Finished,
        Closing
    }

    #region  Variables

    // 그룹 정보
    private List<AstarPath> routes = new List<AstarPath>();
    private List<Vector3> pathPoints = new List<Vector3>();
    [FoldoutGroup("Formation"), SerializeField, ReadOnly] private int currentPathPointsIndex = 0;
    [FoldoutGroup("Formation"), ReadOnly] public Dictionary<string, Drone> Drones = new Dictionary<string, Drone>();
    [FoldoutGroup("Formation"), ReadOnly] public int PortKey = -1;

    // 포메이션 상태
    [BoxGroup("Formation/State")] private State condition = State.Opening;
    [BoxGroup("Formation/State"), ReadOnly] public string FormationLog = string.Empty;

    // 포메이션 작업
    [HideInInspector] public Coroutine Routine = default;

    // 정보
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
    public bool Commandable
    {
        get
        {
            return condition == State.Closing ? false : true;
        }
    }

    #endregion


    // Define & build
    public void AddDrone(string id, Drone drone)
    {
        Drones.Add(id, drone);
    }
    public void DefinePortKey(int key)
    {
        PortKey = key;
    }
    public void DefineRoutes(List<AstarPath> routes)
    {
        this.routes = routes;
        foreach (AstarPath p in routes)
        {
            foreach (Vector3 v in p.LookPoints)
            {
                pathPoints.Add(v);
            }
        }
    }


    // Route and nodes
    public void UpdatePathPositionIndex(int index)
    {
        currentPathPointsIndex = index;
    }
    public int GetPathPosition()
    {
        return currentPathPointsIndex;
    }
    public string GetPathPositionString(int index, string prefix)
    {
        if (index >= pathPoints.Count)
        {
            return prefix + "\n(CLOSING)";
        }
        if (index < 0)
        {
            return prefix + "\n(OPENING)";
        }
        return prefix + "\n" + pathPoints[index].ToString("F0");
    }
    public List<Vector3> GetAllMovePoints()
    {
        List<Vector3> results = new List<Vector3>();
        foreach (AstarPath path in routes)
        {
            foreach (Vector3 point in path.LookPoints)
            {
                results.Add(point);
            }
        }
        return results;
    }


    // States and conditions
    public bool RequestReady()
    {
        if (PortKey != -1 && routes.Count > 0 && Drones.Count > 0)
        {
            UpdateCondition(State.Buildable);
            return true;
        }
        else
        {
            Debug.Log("[DRONE FORMATION] Ready state의 요구 조건 불충족, 다시 요청하시오!");
            return false;
        }
    }
    public void UpdateCondition(State state)
    {
        condition = state;
        // Debug.Log("[DRONE FORMATION] Formation [" + GetStateString() + "] 상태로 승격 요청 완료");
    }
    public int GetConditionIndex()
    {
        return (int)condition;
    }
    public string GetStateString()
    {
        return condition.ToString();
    }
    public string[] GetAllStateString()
    {
        string[] result = Enum.GetNames(typeof(State));
        return result;
    }
}