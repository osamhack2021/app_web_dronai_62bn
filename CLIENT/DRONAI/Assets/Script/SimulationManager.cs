using System;
using UnityEngine;
using Sirenix.OdinInspector;


public class SimulationManager : Singleton<SimulationManager>
{

    // Components
    [BoxGroup("Components"), SerializeField] private DroneManager droneManager = default;
    [BoxGroup("Components"), SerializeField] private CameraManager cameraManager = default;
    [BoxGroup("Components"), SerializeField] private UI uiManager = default;

    [BoxGroup("References"), SerializeField] private Transform defaultCameraPosition = default;

    [Button("Refresh", ButtonSizes.Large)]
    private void UpdateEditor()
    {
        if (droneManager == null) droneManager = FindObjectOfType<DroneManager>();
        if (cameraManager == null) cameraManager = FindObjectOfType<CameraManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UI>();
    }

    // Action and Events
    [HideInInspector] public Action OnInitialize = default;
    [HideInInspector] public Action OnInitialized = default;


    #region Life cycle
    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 시뮬레이션 초기화를 시작하는 함수입니다.
    /// 유니티 라이프 사이클 'OnEnable' 이후 실행하십시오.
    /// </summary>
    private void Initialize()
    {
        // Starting initialize
        OnInitialize?.Invoke();

        // Intialize components
        droneManager.Initialize();
        cameraManager.Initialize(defaultCameraPosition);
        uiManager.Initialize();

        // Finalize
        Initialized();
    }
    private void Initialized()
    {
        OnInitialized?.Invoke();
    }
    #endregion
}
