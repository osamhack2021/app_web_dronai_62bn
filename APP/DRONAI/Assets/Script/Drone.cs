using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using PriorityQueue;


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
    [FoldoutGroup("Property"), SerializeField] private float speed = 0.5f;
    [FoldoutGroup("Property"), SerializeField] private AnimationCurve timeCurve = default;

    [BoxGroup("Components"), SerializeField, ReadOnly] private DroneManager droneManager = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private Rigidbody rb = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private List<DroneSensor> droneSensors = new List<DroneSensor>();

    [BoxGroup("Formation"), ReadOnly] public DroneGroup droneGroup = new DroneGroup();
    [BoxGroup("Formation"), ReadOnly] public int ChildCount = 1;

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

    #endregion

    #region Life cycle

    /// <summary>
    /// 드론 생성자
    /// </summary>
    /// <param name="id">드론 아이디</param>
    /// <param name="speed">드론 고유 속도</param>
    /// <param name="droneManager">드론 생성자 및 매니저</param>
    public void Initialize(string name, float speed, DroneManager droneManager)
    {
        // Generate new id
        GenerateNewID();

        // Assign variables
        this.speed = speed;
        this.droneManager = droneManager;

        // Assign components
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Change the object name
        gameObject.name = name;

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

            // Stop routines
            StopAllCoroutines();

            // Callback
            droneManager.OnDroneDestroy(id);

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
            if (priority > routinesQueue.FirstPriority)
            {
                yield return null;
                continue;
            }

            // Check
            if (droneGroup.Parent == null) break;

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
    public void MoveUp(float distance, float duration = -1f, int priority = 100, Action OnFinished = null)
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

    #region Assign
    public void AssignParent(Drone parent)
    {
        // Assign
        droneGroup.Parent = parent;

        // Count the number of child
        droneGroup.Parent.ChildCount++;

        // Start to following parent
        FollowParent();
    }
    #endregion
}
