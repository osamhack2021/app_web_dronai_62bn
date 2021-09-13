using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


public class Drone : Entity
{
    #region Variable
    [FoldoutGroup("Property"), ShowInInspector, ReadOnly] private bool isDead = false;
    [FoldoutGroup("Property"), SerializeField] private float speed = 2f;
    [BoxGroup("Components"), SerializeField, ReadOnly] private DroneManager droneManager = default;
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
    private Coroutine moveToRoutine = default;

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

        // Change the object name
        gameObject.name = name;

        // Initialize sensors
        droneSensors.Clear();
        droneSensors = GetComponentsInChildren<DroneSensor>().ToList();
        foreach(DroneSensor sensor in droneSensors)
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
        if(other.tag.Equals("Drone")) 
        {
            Drone drone = other.GetComponent<DroneSensor>().GetDrone();
            print("센서 감지됨! [나: " + this.name + "]" + " |  [상대: " + drone.name+"]");

            Vector3 otherVector = transform.position - other.transform.position;
            //otherVector.y = transform.position.y;
            //print(name + "이 가는 방향벡터 : " + otherVector.x + ", " + otherVector.y + ", " + otherVector.z);
            MoveTo(otherVector + transform.position);
        }
        else // 지형과 부딪힐 때
        {
            print("센서 감지됨! [나: " + this.name + "]" + " |  [상대: " + other.name+"]");
            // 나중에 지형 생기면 할 것

        }
        

        
    }
    #endregion


    #region Physics

    #region Move
    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    public void MoveTo(Vector3 destination)
    {
        MoveTo(destination, -1, null);
    }

    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 + callback]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveTo(Vector3 destination, Action OnFinished)
    {
        MoveTo(destination, -1, OnFinished);
    }

    /// <summary>
    /// 드론을 해당위치까지 움직여주는 함수 [선형 보간 + 시간 고정 + callback]
    /// </summary>
    /// <param name="destination">목표 지점 (3차원 벡터 값)</param>
    /// <param name="duration">소요 시간 (시간 고정 처리시 값 입력)</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveTo(Vector3 destination, float duration = -1, Action OnFinished = null)
    {
        if (moveToRoutine != null) StopCoroutine(moveToRoutine);
        if (gameObject.activeSelf)
        {
            if (duration == -1)
            {
                moveToRoutine = StartCoroutine(MoveToRoutine(destination, OnFinished));
            }
            else
            {
                moveToRoutine = StartCoroutine(MoveAsTimeRoutine(destination, duration, OnFinished));
            }
        }
    }
    private IEnumerator MoveToRoutine(Vector3 destination, Action OnFinished)
    {
        for (; ; )
        {
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
        yield break;
    }
    private IEnumerator MoveAsTimeRoutine(Vector3 destination, float duration, Action OnFinished)
    {
        // Variables
        float timer = 0;
        Vector3 startPos = transform.position;

        // Physics
        while (timer <= duration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, destination, timer / duration);
            yield return null;
        }

        // Fixed result position
        transform.position = destination;

        // Call finished event
        OnFinished?.Invoke();

        // Exit
        yield break;
    }


    /// <summary>
    /// 드론을 Y 좌표로만 증가시켜주는 함수
    /// </summary>
    /// <param name="distance">거리</param>
    /// <param name="duration">소요 시간</param>
    /// <param name="OnFinished">함수 완료시 호출</param>
    public void MoveUp(float distance, float duration = -1f, Action OnFinished = null)
    {
        Vector3 result = transform.position;
        result.y += distance;
        MoveTo(result, duration, OnFinished);
    }


    /// <summary>
    /// 즉시 해당 위치로 움직여주는 함수
    /// </summary>
    /// <param name="position">좌표</param>
    public void FixedMove(Vector3 position)
    {
        if (moveToRoutine != null) StopCoroutine(moveToRoutine);

        transform.position = position;
    }
    #endregion



    #endregion
}
