using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PriorityQueue;
using Sirenix.OdinInspector;


public class Drone : Entity
{
    #region Class
    public class Routine
    {
        public Coroutine Task = default;
        public int Priority = 100;

        public Routine(Coroutine routine, int Priority)
        {
            this.Task = routine;
            this.Priority = Priority;
        }
    }

    [Serializable]
    public class DroneGroup
    {
        public Drone Parent = null;
        public Drone LeftChild = null;
        public Drone RightChild = null;
    }

    #endregion

    #region Variable
    [FoldoutGroup("Property"), ShowInInspector, ReadOnly] private bool isDead = false;
    [FoldoutGroup("Property"), ReadOnly] public bool IsWorking = false;
    [FoldoutGroup("Property"), SerializeField] private float speed = 0.5f;
    [FoldoutGroup("Property"), SerializeField] private AnimationCurve timeCurve = default;

    [BoxGroup("Components"), SerializeField, ReadOnly] private DroneManager droneManager = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private Rigidbody rb = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private List<DroneSensor> droneSensors = new List<DroneSensor>();

    [BoxGroup("Formation"), ReadOnly] public DroneGroup droneGroup = new DroneGroup();
    [BoxGroup("Formation"), ReadOnly] public int ChildCount = 1;
    [BoxGroup("Formation"), ReadOnly] public int FormationCode = default;


    [BoxGroup("Resources"), SerializeField] private Transform explosionPrefab = default;

    [BoxGroup("Position"), SerializeField] private float startX = 1;
    [BoxGroup("Position"), SerializeField] private float startZ = 1;

    [BoxGroup("PathFind")] private int[] dx = new int[] {0,1,1,1,0,-1,-1,-1, 0,1,1,1,0,-1,-1,-1, 0,1,1,1,0,-1,-1,-1};
    [BoxGroup("PathFind")] private int[] dy = new int[] {1,1,1,1,1,1,1,1, 0,0,0,0,0,0,0,0, -1,-1,-1,-1,-1,-1,-1,-1};
    [BoxGroup("PathFind")] private int[] dz = new int[] {-1,-1,0,1,1,1,0,-1, -1,-1,0,1,1,1,0,-1, -1,-1,0,1,1,1,0,-1};

    public Vector3 Velocity
    {
        get { return rb.velocity; }
    }
    public Vector3 Position
    {
        get { return transform.position; }
    }
    public float X
    {
        get { return transform.position.x; }
    }
    public float Y
    {
        get { return transform.position.y; }
    }
    public float Z
    {
        get { return transform.position.z; }
    }

    // Routines
    private SimplePriorityQueue<Routine> routinesQueue = new SimplePriorityQueue<Routine>();
    

    #endregion

    #region Life cycle

    /// <summary>
    /// 드론 생성자
    /// </summary>
    /// <param name="id">드론 아이디</param>
    /// <param name="speed">드론 고유 속도</param>
    /// <param name="droneManager">드론 생성자 및 매니저</param>
    public void Initialize(string id, float speed, DroneManager droneManager)
    {
        // Generate new id
        //GenerateNewID(); --> use drone name as id (important!)
        this.id = id;

        // Assign variables
        this.speed = speed;
        this.droneManager = droneManager;

        startX = X; startZ = Z;

        // Assign components
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Change the object name (using id)
        gameObject.name = id;

        // Initialize sensors
        droneSensors.Clear();
        droneSensors = GetComponentsInChildren<DroneSensor>().ToList();
        foreach (DroneSensor sensor in droneSensors)
        {
            sensor.Initialize(this);
        }
    }

    public void OnSensorDetected(GameObject other)
    {
        AvoidFromOther(ref other);
    }

    public void OnDroneCollapsed(GameObject other)
    {
        if (!isDead)
        {
            // Change the state
            isDead = true;

            // 리스트 안에 드론이 있다면 없애줘야함
            if (droneManager.DronePool.IsItemInList(this.GetID())) droneManager.DronePool.DeleteItemInList(this);

            // Stop routines
            StopAllCoroutines();

            // Callback
            droneManager.OnDroneDestroy(name);

            // Cleaning
            Destroy(Instantiate(explosionPrefab, transform.position, transform.rotation).gameObject, 2f);
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Physics

    public void AvoidFromOther(ref GameObject other)
    {
        //print("센서 감지됨! [나: " + name + "]" + " |  [상대: " + other.name + "]");

        // Remove previous process
        for (; ; )
        {
            if (routinesQueue.FirstPriority == 0)
            {
                Routine target = routinesQueue.Dequeue();
                StopCoroutine(target.Task);
            }
            else
            {
                break;
            }
        }

        Vector3 directionVector = (transform.position - other.transform.position).normalized;

        float minX = 0.02f, maxX = 0.05f;
        Vector3 ramdomVector = new Vector3(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minX, maxX));

        // Run physics
        MoveTo(directionVector + transform.position + ramdomVector, 0.5f, 0);
    }

    /// <summary>
    /// 부모 드론을 계속 따라가는 함수
    /// </summary>
    /// <param name="priority">작업 우선순위</param>
    public void FollowParent(int priority = 10, float gap = 2f)
    {
        Coroutine routine = StartCoroutine(FollowParentRoutine(priority, gap));
        routinesQueue.Enqueue(new Routine(routine, priority), priority);
    }
    private IEnumerator FollowParentRoutine(int priority, float gap)
    {
        for (; ; )
        {
            // Check
            if (droneGroup.Parent == null) {
                routinesQueue.Dequeue();
                break;
            }

            if (priority > routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }

            // Follow
            Vector3 destinationVector3 = droneGroup.Parent.Position;
            float destinationX = destinationVector3.x;
            float destinationZ = destinationVector3.z;

            // X좌표는 level로 결정 -> 부모와의 x좌표 차이는 1
            destinationX -= 1;

            // Z좌표는 자식 수로 결정
            if (droneGroup.Parent.droneGroup.LeftChild == this) 
            {
                // 왼쪽 자식이면 (오른쪽 자식 + 1) * gap
                int cnt = 0;
                if (droneGroup.RightChild) cnt += droneGroup.RightChild.ChildCount;
                destinationZ += (cnt + 1) * gap;
            }
            else 
            {
                // 오른쪽 자식이면 (왼쪽 자식 + 1) * gap
                int cnt = 0;
                if (droneGroup.LeftChild) cnt += droneGroup.LeftChild.ChildCount;
                destinationZ -= (cnt + 1) * gap;
            }
            destinationVector3 = new Vector3(destinationX, destinationVector3.y, destinationZ);

            transform.position = Vector3.Lerp(transform.position, destinationVector3, Time.deltaTime * speed);

            // Yield
            yield return null;
        }
        yield break;
    }

    // reconnoiter : 정찰하다
    // 정찰이 끝난 후
    private void AfterReconnoiter()
    {
        // 부모 드론 해제
        if (droneGroup.Parent) droneGroup.Parent = null;

        // 제자리 and (Y좌표 = 9) 위치로 돌아가기
        MoveTo(new Vector3(startX, 9, startZ), 300);

        // 자식들이 있으면 일 끝났다고 알려줘야함
        if (droneGroup.LeftChild) droneGroup.LeftChild.AfterReconnoiter();
        if (droneGroup.RightChild) droneGroup.RightChild.AfterReconnoiter();

        // 일 하는 중 아님 -> list에 넣기
        // droneManager.DronePool.PushToPool(this);

        // 초기화
        droneGroup.LeftChild = null;
        droneGroup.RightChild = null;
        ChildCount = 1;
    }

    #region Move

    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 ]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    public void MoveTo(Vector3 destination)
    {
        MoveTo(destination, -1, 100, null);
    }

    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 + callback]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    /// <param name="priority">작업 순위</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveTo(Vector3 destination, int priority = 100, Action OnFinished = null)
    {
        MoveTo(destination, -1, priority, OnFinished);
    }

    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 + 시간 고정 + callback]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    /// <param name="duration">소요 시간 (시간 고정 처리시 값 입력)</param>
    /// <param name="priority">작업 순위</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveTo(Vector3 destination, float duration = -1, int priority = 100, Action OnFinished = null)
    {
        if (gameObject.activeSelf)
        {
            if (duration == -1)
            {
                Coroutine routine = StartCoroutine(MoveToRoutine(destination, priority, OnFinished));
                routinesQueue.Enqueue(new Routine(routine, priority), priority);
            }
            else
            {
                Coroutine routine = StartCoroutine(MoveAsTimeRoutine(destination, duration, priority, OnFinished));
                routinesQueue.Enqueue(new Routine(routine, priority), priority);
            }
        }
    }

    private IEnumerator MoveToRoutine(Vector3 destination, int priority, Action OnFinished)
    {
        for (; ; )
        {
            if (priority > routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }

            transform.position = Vector3.Lerp(transform.position, destination, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, destination) < 0.02f)
            {
                transform.position = destination;

                // 처음 지점으로 가는 루틴 -> 안 죽는 루틴
                if(priority < 1000) break;

                // 일 끝났으면 다시 일 할수 있게 가동
                if (IsWorking)
                {
                    droneManager.DronePool.PushToPool(this);
                }
            }
            yield return null;
        }

        // Exit
        routinesQueue.Dequeue();
        // print(name + " | MVROUTINE 제거됨 --> " + priority + " 현재 FIRST --> " + routinesQueue.FirstPriority);

        // 정찰 끝
        if (priority == 100) 
        {
            AfterReconnoiter();
        }

        // Call finished event
        OnFinished?.Invoke();
        yield break;
    }
    private IEnumerator MoveAsTimeRoutine(Vector3 destination, float duration, int priority, Action OnFinished)
    {
        // Variables
        float timer = 0;
        Vector3 startPos = transform.position;

        // Physics
        while (timer <= duration)
        {
            if (priority > routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, destination, timeCurve.Evaluate(timer / duration));
            yield return null;
        }

        // Fixed result position
        transform.position = destination;

        // Exit
        routinesQueue.Dequeue();
        // print(name + " | MVTROUTINE 제거됨 --> " + priority + " 현재 FIRST --> " + routinesQueue.FirstPriority);

        // Call finished event
        OnFinished?.Invoke();
        yield break;
    }



    /// <summary>
    /// 드론을 Y 좌표로만 증가시켜주는 함수
    /// </summary>
    /// <param name="distance">거리</param>
    /// <param name="duration">소요 시간</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveUp(float distance, float duration = -1f, int priority = 1000, Action OnFinished = null)
    {
        Vector3 result = transform.position;
        result.y += distance;
        MoveTo(result, duration, priority, OnFinished);
    }

    public void MoveByDirection(Vector3 direction, float speed, int priority)
    {
        Coroutine routine = StartCoroutine(MoveByDirectionRoutine(direction, speed, priority));
        routinesQueue.Enqueue(new Routine(routine, priority), priority);
    }
    private IEnumerator MoveByDirectionRoutine(Vector3 direction, float speed, int priority)
    {
        for (; ; )
        {
            if (priority < routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }
            rb.MovePosition(direction * Time.deltaTime * speed);
            yield return null;
        }
    }

    #endregion

    #endregion

    #region Formation
    
    public void AssignParent(Drone parent)
    {
        // Assign
        droneGroup.Parent = parent;

        // Count the number of child
        droneGroup.Parent.ChildCount++;

        // Start to following parent
        FollowParent();
    }

    // ChildCount를 다시 계산해야 하는 시점이 있음 -> 자식 드론이 폭파할 때
    public void CalulateChildCount()
    {
        ChildCount = 1;
        if (droneGroup.LeftChild) ChildCount += droneGroup.LeftChild.ChildCount;
        if (droneGroup.RightChild) ChildCount += droneGroup.RightChild.ChildCount;
    }
    #endregion

    #region Path Find

    public class PathFindNode
    {
        public Vector3 Position;

        public PathFindNode PreviousPathFindNode;

        public int GCost;

        public PathFindNode(Vector3 Position, PathFindNode PreviousPathFindNode, int GCost)
        {
            this.Position = Position;
            this.PreviousPathFindNode = PreviousPathFindNode;
            this.GCost = GCost;
        }
    }

    private void FindPath(Vector3 endPoint)
    {
        SimplePriorityQueue<PathFindNode> openPoints = new SimplePriorityQueue<PathFindNode>();
        Dictionary<Vector3, PathFindNode> closedPoints = new Dictionary<Vector3, PathFindNode>();

        float gridSize = droneManager.gridSize;

        Vector3 currentVector3 = transform.position;
        PathFindNode startNode = new PathFindNode(currentVector3, null, 0);
        openPoints.Enqueue(startNode, 0);

        while (openPoints.Count > 0)
        {
            PathFindNode currentNode = openPoints.Dequeue();
            closedPoints.Add(currentNode.Position, currentNode);

            if (Vector3.Distance(currentNode.Position, endPoint) < 0.02f) break;

            for(int i=0; i<24; i++)
            {
                Vector3 neighborVector3 = currentNode.Position + new Vector3(gridSize*dx[i], gridSize*dy[i], gridSize*dz[i]);
                PathFindNode nextNode = new PathFindNode(neighborVector3, currentNode, currentNode.GCost + 1);
                float fCost = nextNode.GCost + GetDistance(neighborVector3, endPoint);

                if (Physics.OverlapSphere(neighborVector3, 0.5f).Length > 0) continue;
                
                if (closedPoints.TryGetValue(neighborVector3, out PathFindNode node))
                {
                    if (fCost < node.GCost + GetDistance(neighborVector3, endPoint))
                    {
                        closedPoints.Add(neighborVector3, nextNode);
                        if (!openPoints.Contains(nextNode)) openPoints.Enqueue(nextNode, fCost);
                    }
                }
            }
        }

        return;
    }

    private float GetDistance(Vector3 node1, Vector3 node2)
    {
        return Vector3.Distance(node1, node2);
    }

    private Vector3[] RetracePath(PathFindNode startNode, PathFindNode endNode)
    {
        List<PathFindNode> path = new List<PathFindNode>();
        PathFindNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.PreviousPathFindNode;
        }

        Vector3[] wayPoints = SimplifyPath(path);
        Array.Reverse(wayPoints);
        return wayPoints;
    }

    Vector3[] SimplifyPath(List<PathFindNode> path)
    {
        List<Vector3> wayPoints = new List<Vector3>();
        Vector3 oldVector3 = wayPoints[0];
        wayPoints.Add(oldVector3);

        for(int i=1; i<wayPoints.Count; i++)
        {
            Vector3 newVector3 = path[i].Position;
            if (oldVector3 != newVector3) wayPoints.Add(newVector3);
            oldVector3 = newVector3;
        }

        return wayPoints.ToArray();
    }

    #endregion
}
