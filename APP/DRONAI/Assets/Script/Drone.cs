using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


public class Drone : Entity
{
    public class PhysicsRoutine
    {
        private Coroutine routine = default;

        private int priority = 0;

        public PhysicsRoutine(Coroutine routine, int priority)
        {
            this.priority = priority;
            this.routine = routine;
        }


        public Coroutine GetRoutine()
        {
            return routine;
        }
        public int GetPriority()
        {
            return priority;
        }
    }

    public class Heap
    {

        private List<PhysicsRoutine> A = new List<PhysicsRoutine>();

        public int Count { get { return A.Count; } }

        public void Add(PhysicsRoutine value)
        {
            // add at the end
            A.Add(value);

            // bubble up
            int i = A.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (A[parent].GetPriority() < A[i].GetPriority()) // MinHeap에선 반대
                {
                    Swap(parent, i);
                    i = parent;
                }
                else
                {
                    break;
                }
            }
        }

        public PhysicsRoutine RemoveOne()
        {
            if (A.Count == 0)
                throw new InvalidOperationException();

            PhysicsRoutine root = A[0];

            // move last to first 
            // and remove last one
            A[0] = A[A.Count - 1];
            A.RemoveAt(A.Count - 1);

            // bubble down
            int i = 0;
            int last = A.Count - 1;
            while (i < last)
            {
                // get left child index
                int child = i * 2 + 1;

                // use right child if it is bigger                
                if (child < last &&
                    A[child].GetPriority() < A[child + 1].GetPriority()) // MinHeap에선 반대
                    child = child + 1;

                // if parent is bigger or equal, stop
                if (child > last ||
                   A[i].GetPriority() >= A[child].GetPriority()) // MinHeap에선 반대
                    break;

                // swap parent/child
                Swap(i, child);
                i = child;
            }

            return root;
        }

        public int GetMaxPriority()
        {
            if (A.Count == 0) return -1;
            return A[0].GetPriority();
        }

        private void Swap(int i, int j)
        {
            PhysicsRoutine t = A[i];
            A[i] = A[j];
            A[j] = t;
        }
    }


    #region Variable
    [FoldoutGroup("Property"), ShowInInspector, ReadOnly] private bool isDead = false;
    [FoldoutGroup("Property"), SerializeField] private float speed = 2f;
    [BoxGroup("Components"), SerializeField, ReadOnly] private DroneManager droneManager = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private Rigidbody rb = default;
    [BoxGroup("Components"), SerializeField, ReadOnly] private List<DroneSensor> droneSensors = new List<DroneSensor>();
    [BoxGroup("Resources"), SerializeField] private Transform explosionPrefab = default;

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
    private Heap routinesHeap = new Heap();

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
    public void OnSensorDetected(GameObject other)
    {
        AvoidFromOther(ref other);
    }


    #endregion


    #region Physics

    public void AvoidFromOther(ref GameObject other)
    {
        if (other.tag.Equals("Drone"))
        {
            Drone drone = other.GetComponent<DroneSensor>().GetDrone();
            print("센서 감지됨! [나: " + this.name + "]" + " |  [상대: " + drone.name + "]");

            Vector3 otherVector = transform.position - other.transform.position;
            //otherVector.y = transform.position.y;
            //print(name + "이 가는 방향벡터 : " + otherVector.x + ", " + otherVector.y + ", " + otherVector.z);
            MoveTo(otherVector + transform.position, 100);
        }
        else // 지형과 부딪힐 때
        {
            print("센서 감지됨! [나: " + this.name + "]" + " |  [상대: " + other.name + "]");
            // 나중에 지형 생기면 할 것
        }
    }


    #region Move

    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 ]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    public void MoveTo(Vector3 destination)
    {
        MoveTo(destination, -1, 0, null);
    }


    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 + callback]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    /// <param name="priority">작업 순위</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveTo(Vector3 destination, int priority = 0, Action OnFinished = null)
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
    public void MoveTo(Vector3 destination, float duration = -1, int priority = 0, Action OnFinished = null)
    {
        if (gameObject.activeSelf)
        {
            if (duration == -1)
            {
                routinesHeap.Add(new PhysicsRoutine(StartCoroutine(MoveToRoutine(destination, priority, OnFinished)), priority));
            }
            else
            {
                routinesHeap.Add(new PhysicsRoutine(StartCoroutine(MoveAsTimeRoutine(destination, duration, priority, OnFinished)), priority));
            }
        }
    }
    private IEnumerator MoveToRoutine(Vector3 destination, int priority, Action OnFinished)
    {
        for (; ; )
        {
            if (priority < routinesHeap.GetMaxPriority())
            {
                yield return null;
                continue;
            }

            transform.position = Vector3.Lerp(transform.position, destination, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, destination) < 0.01f)
            {
                transform.position = destination;
                break;
            }
            yield return null;
        }

        // Call finished event
        OnFinished?.Invoke();

        // Exit
        routinesHeap.RemoveOne();
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
            if (priority < routinesHeap.GetMaxPriority())
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, destination, timer / duration);
            yield return null;
        }

        // Fixed result position
        transform.position = destination;

        // Call finished event
        OnFinished?.Invoke();

        // Exit
        routinesHeap.RemoveOne();
        yield break;
    }



    /// <summary>
    /// 드론을 Y 좌표로만 증가시켜주는 함수
    /// </summary>
    /// <param name="distance">거리</param>
    /// <param name="duration">소요 시간</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveUp(float distance, float duration = -1f, int priority = 0, Action OnFinished = null)
    {
        Vector3 result = transform.position;
        result.y += distance;
        MoveTo(result, duration, priority, OnFinished);
    }


    public void MoveByDirection(Vector3 direction, float speed, int priority)
    {
        routinesHeap.Add(new PhysicsRoutine(StartCoroutine(MoveByDirectionRoutine(direction, speed, priority)), priority));
    }
    private IEnumerator MoveByDirectionRoutine(Vector3 direction, float speed, int priority)
    {
        for (; ; )
        {
            if (priority < routinesHeap.GetMaxPriority())
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
}
