using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PriorityQueue;
using Sirenix.OdinInspector;
using Dronai.Path;


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

    #endregion

    #region Variable
    [FoldoutGroup("Property"), ShowInInspector, ReadOnly] private bool isDead = false;
    [FoldoutGroup("Property"), ReadOnly] public bool IsWorking = false;
    [FoldoutGroup("Property"), SerializeField] private float speed = 2f;
    [FoldoutGroup("Property"), SerializeField] private float turnSpeed = 3f;
    [FoldoutGroup("Property"), SerializeField] private float turnDst = 5f;
    [FoldoutGroup("Property"), SerializeField] private AnimationCurve timeCurve = default;

    // 현재 할당 되어있는 Path
    private AstarPath currentPath = default;

    [BoxGroup("Components"), SerializeField, ReadOnly] private DroneManager droneManager = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private Rigidbody rb = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private List<DroneSensor> droneSensors = new List<DroneSensor>();


    [BoxGroup("Formation"), SerializeField] private float startX = 1;
    [BoxGroup("Formation"), SerializeField] private float startZ = 1;
    [BoxGroup("Formation"), SerializeField] private Vector3 formationPosition = default;
    public Vector3 HeadPosition
    {
        get
        {
            Vector3 result = transform.position;
            result.y -= 1f;
            return result;
        }
    }
    [BoxGroup("Formation"), SerializeField, ReadOnly] private int formationIndex = 1;
    [BoxGroup("Formation"), SerializeField] private Queue<Drone> formationOrder = new Queue<Drone>();

    [SerializeField, BoxGroup("DEBUG")] private LineRenderer lineRendererPrefab = default;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    [SerializeField, BoxGroup("DEBUG")] private Transform lineRendererParent = default;



    [BoxGroup("Resources"), SerializeField] private Transform explosionPrefab = default;


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
    private Coroutine pathFindingRoutine = default;


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


    #region Move

    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 ]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    public void MoveTo(Vector3 destination)
    {
        formationPosition = destination;
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
            if (Vector3.Distance(transform.position, destination) < 0.1f)
            {
                transform.position = destination;

                break;
            }
            yield return null;
        }

        // Exit
        routinesQueue.Dequeue();
        // print(name + " | MVROUTINE 제거됨 --> " + priority + " 현재 FIRST --> " + routinesQueue.FirstPriority);

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

    public void MoveViaPathFinder(AstarPath path, int priority = 50, Action OnFinished = null)
    {
        if (gameObject.activeSelf)
        {
            Coroutine routine = StartCoroutine(MoveViaPathFinderRoutine(path, priority, OnFinished));
            routinesQueue.Enqueue(new Routine(routine, priority), priority);
        }
    }

    private IEnumerator MoveViaPathFinderRoutine(AstarPath path, int priority, Action OnFinished)
    {
        bool followingPath = true;
        int pathIndex = 0;
        transform.LookAt(path.LookPoints[0]);

        while (followingPath)
        {
            if (priority > routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }

            if (path.TurnBoundaries[pathIndex].HasCrossedLine(Position))
            {
                if (pathIndex == path.finishLineIndex)
                {
                    followingPath = false;
                }
                else
                {
                    pathIndex++;
                }
            }

            if (followingPath)
            {
                Quaternion targetRotation = Quaternion.LookRotation(path.LookPoints[pathIndex] - Position);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * speed * 10, Space.Self);
            }


            yield return null;
        }

        // Fix the final position
        transform.position = path.LookPoints[pathIndex];

        // Exit
        routinesQueue.Dequeue();
        // print(name + " | MVROUTINE 제거됨 --> " + priority + " 현재 FIRST --> " + routinesQueue.FirstPriority);

        // Call finished event
        OnFinished?.Invoke();
        yield break;
    }

    #endregion

    #endregion

    #region Formation

    /// <summary>
    /// 드론 Formation의 Index를 계산해주는 함수
    /// 계산을 많이 요하는 함수이므로 정말 필요한 상황이 아니면 호출하지 마시오!
    /// </summary>
    private void UpdateIndex()
    {
        if (formationOrder.Count > 0)
        {
            formationIndex = formationOrder.ToArray().ToList().IndexOf(this);
        }
        return;
    }

    public void DefineFormation(Queue<Drone> formationOrder, Vector3 indexPosition, Action OnFinished)
    {
        // 변수 정의
        int priority = 20;
        this.formationOrder = formationOrder;


        if (formationOrder.Peek().id.Equals(id)) // 헤드 드론입니다
        {
            formationIndex = -1;

            // Build Formation Routine [20] 생성 및 마무리
            formationPosition = indexPosition;
            Coroutine routine = StartCoroutine(BuildFormation(formationPosition, priority, OnFinished));
            routinesQueue.Enqueue(new Routine(routine, priority), priority);
        }
        else // 자식 드론입니다
        {
            // =================================
            //      Calculate formation pos
            // =================================

            // 인덱스 계산
            UpdateIndex();

            Vector3 destination = indexPosition;

            // 각도 및 반지름 계산
            float radian = 2f * (float)Math.PI / (formationOrder.Count - 1);
            float radius = 2f / (float)Math.Sin(radian / 2);

            // X좌표는 반지름 * cos(x), z좌표는 반지름 * sin(x)
            destination.x += radius * (float)Math.Cos(radian * formationIndex);
            destination.z += radius * (float)Math.Sin(radian * formationIndex);

            // 고유 Y 좌표 부여
            formationPosition = destination;
            formationPosition.y += -(formationIndex * 0.5f);

            // Build Formation Routine [20] 생성 및 마무리
            Coroutine routine = StartCoroutine(BuildFormation(formationPosition, priority, OnFinished));
            routinesQueue.Enqueue(new Routine(routine, priority), priority);
        }
    }
    private IEnumerator BuildFormation(Vector3 position, int priority, Action OnFinished)
    {
        // Variables
        bool isFinding = true;
        bool findable = false;
        AstarPath path = default;

        // Find the path
        while (!findable)
        {
            AstarPathRequestManager.RequestPath(new PathRequest(Position, position, true, (Vector3[] waypoints, bool pathSucessful) =>
            {
                findable = pathSucessful;
                isFinding = false;
                if (pathSucessful)
                {
                    path = new AstarPath(waypoints, Position, turnDst);
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
            if (!findable)
            {
                // 경로를 찾지 못한다면 동적 Pathing 을 포기하고 다시 검색
                print("[" + name + "] 경로 탐색 실패! 맵을 초기화합니다");
                AstarPathRequestManager.RequestUpdateGrid();
            }
            yield return null;
        }

        // 경로를 따라가기 시작
        bool followingPath = true;
        int pathIndex = 0;

        DrawLine(path);
        // transform.LookAt(path.LookPoints[0]);

        while (followingPath)
        {
            if (priority > routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }

            while (path.TurnBoundaries[pathIndex].HasCrossedLine(Position))
            {
                if (pathIndex == path.finishLineIndex)
                {
                    followingPath = false;
                    break;
                }
                else
                {
                    pathIndex++;
                }
            }

            if (followingPath)
            {
                // Quaternion targetRotation = Quaternion.LookRotation(path.LookPoints[pathIndex] - Position);
                // transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                // transform.Translate(Vector3.forward * Time.deltaTime * speed * 10, Space.Self);
                transform.position = Vector3.MoveTowards(Position, path.LookPoints[pathIndex], Time.deltaTime * speed * 10);
            }
            yield return null;
        }

        // Fomration Routine [+ 10] 생성 및 마무리 -> Formation Routine은 항상 Build Formation 보다 Priority 값이 높아야 한다
        priority += 10;
        Coroutine routine = StartCoroutine(FormationRoutine(priority));
        routinesQueue.Enqueue(new Routine(routine, priority), priority);


        // Finalize
        OnFinished?.Invoke();
        routinesQueue.Dequeue();
        yield break;
    }
    private IEnumerator FormationRoutine(int priority)
    {
        // 라인 삭제
        ClearLine();

        for (; ; )
        {
            if (priority > routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }

            // Formation order가 비워지면 formation 종료를 의미한다
            if (formationOrder.Count == 0)
            {
                // Exit
                routinesQueue.Dequeue();
                break;
            }

            // 부모드론 파괴시 예약된 다음 부모 노드를 받아옵니다
            if (formationOrder.Peek() == null)
            {
                formationOrder.Dequeue();

                // 인덱스 계산
                UpdateIndex();
            }

            if (formationOrder.Peek().id.Equals(id)) // 헤드 드론입니다
            {
                // Do something in here
            }
            else
            {
                // Follow
                Vector3 destination = formationOrder.Peek().HeadPosition;

                // 각도 및 반지름 계산
                float radian = 2f * (float)Math.PI / (formationOrder.Count - 1);
                float radius = 2f / (float)Math.Sin(radian / 2);

                // X좌표는 반지름 * cos(x), z좌표는 반지름 * sin(x)
                destination.x += radius * (float)Math.Cos(radian * formationIndex);
                destination.z += radius * (float)Math.Sin(radian * formationIndex);

                // 고유 Y 좌표 부여
                formationPosition = destination;
                formationPosition.y += -(formationIndex * 0.5f);

                transform.position = Vector3.Lerp(transform.position, formationPosition, Time.deltaTime * speed);
            }

            // Yield
            yield return null;
        }
        yield break;
    }

    public void MoveFormation()
    {
        if (!formationOrder.Peek().id.Equals(id))
        {
            // 헤드 드론이 아니면 Formation 통제 권한을 갖을 수 없습니다.
            return;
        }
    }

    #endregion

    #region Path find

    [Button]
    private void FindDynamicPathDebug()
    {
        FindDynamicPath(new Vector3(5, 5, 5));
    }
    private void FindDynamicPath(Vector3 destination)
    {
        // 항상 기존 경로 탐색 루틴을 죽이고 새로 탐색합니다
        if (pathFindingRoutine != null) StopCoroutine(pathFindingRoutine);
        pathFindingRoutine = StartCoroutine(FindDynamicPathRoutine(destination));
    }
    private IEnumerator FindDynamicPathRoutine(Vector3 destination)
    {
        // Variables
        bool isFinding = true;
        bool findable = false;

        AstarPathRequestManager.RequestPath(new PathRequest(Position, destination, false, (Vector3[] waypoints, bool result) =>
        {
            currentPath = new AstarPath(waypoints, Position, turnDst);
            findable = result;
            isFinding = false;
        }));

        // 경로 탐색 중...
        while (isFinding)
        {
            yield return null;
        }

        if (findable)
        {
            DrawLine(currentPath, true);
        }

        yield break;
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
        LineRenderer lr = Instantiate(lineRendererPrefab, lineRendererParent);
        lr.positionCount = path.LookPoints.Length + 1;
        lr.SetPosition(0, Position);
        for (int i = 0; i < path.LookPoints.Length; i++)
        {
            lr.SetPosition(i + 1, path.LookPoints[i]);
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
        LineRenderer lr = Instantiate(lineRendererPrefab, lineRendererParent);
        lr.positionCount = nodes.Count + 1;
        lr.SetPosition(0, Position);
        for (int i = 0; i < nodes.Count; i++)
        {
            lr.SetPosition(i + 1, nodes[i].center);
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
        LineRenderer lr = Instantiate(lineRendererPrefab, lineRendererParent);
        lr.positionCount = nodes.Count + 1;
        lr.SetPosition(0, Position);
        for (int i = 0; i < nodes.Count; i++)
        {
            lr.SetPosition(i + 1, nodes[i]);
        }

        // 경로 기록에 추가
        lineRenderers.Add(lr);
    }
    private void ClearLine()
    {
        int len = lineRenderers.Count;
        for (int i = 0; i < len; i++)
        {
            if (lineRenderers[i] != null) Destroy(lineRenderers[i].gameObject);
        }
        lineRenderers.Clear();
    }
    private void OnDrawGizmos()
    {
        if (currentPath != null)
        {
            currentPath.DrawWithGizmos();
        }
    }
    #endregion
}
