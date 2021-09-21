using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;


public class UI : MonoBehaviour
{
    // Components
    [SerializeField] private DroneManager droneManager = default;


    // Input variables
    [BoxGroup("Input"), SerializeField] private TMP_InputField droneIdInput = default;
    [BoxGroup("Input"), SerializeField] private TMP_InputField dronePosInput = default;


    // Selection variables
    [BoxGroup("Drone Selection"), SerializeField] private GameObject selectionWindow = default;
    [BoxGroup("Drone Selection"), SerializeField] private GameObject selectionPrefab = default;
    [BoxGroup("Drone Selection"), SerializeField] private Transform selectionParent = default;
    private string selectedDroneId = string.Empty;


    // Conditions
    private int currentUI = 0;



    #region Life cycle
    public void Initialize()
    {
        IntializeDroneSelection();
    }

    public void IntializeDroneSelection()
    {
        List<string> dronesId = droneManager.GetDronesId();

        foreach (string id in dronesId)
        {
            DroneGridElementUI target = Instantiate(selectionPrefab, selectionParent).GetComponent<DroneGridElementUI>();
            target.Initialize(id, OnDroneSelected);
        }
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentUI != 1)
            {
                CallSelectionWindow(true);
            }
            else
            {
                CallSelectionWindow(false);
            }
        }
    }
    #endregion

    #region Window
    private void CallSelectionWindow(bool state)
    {
        if (state)
        {
            currentUI = 1;
            Animation windowAnimation = selectionWindow.GetComponent<Animation>();
            PlayAnimationSafe(windowAnimation, "FadeIn_Canvas");
        }
        else
        {
            currentUI = 0;
            Animation windowAnimation = selectionWindow.GetComponent<Animation>();
            PlayAnimationSafe(windowAnimation, "FadeOut_Canvas");
        }
    }
    #endregion

    public void OnDroneSelected(string id)
    {
        // Close th selection window when drone selected
        CallSelectionWindow(false);

        // Process
        selectedDroneId = id;

        droneIdInput.text = selectedDroneId;
        dronePosInput.text = string.Empty;

        AutoFill(id);
    }
    public void OnButtonDown()
    {
        if (droneIdInput.text.Length == 0)
        {
            droneManager.PickDrones();
            return;
        }

        if (dronePosInput.text.Length == 0)
        {
            droneManager.DestroyNode(droneIdInput.text);
            return;
        }

        string[] position = dronePosInput.text.Split(',');
        Vector3 pos = new Vector3(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]));
        droneManager.MoveSingleDrone(droneIdInput.text, pos);
    }

    #region Functions

    public void AutoFill(string text)
    {
        if (dronePosInput.text.Length == 0)
        {
            if (droneManager.ContainsDrone(text))
            {
                Vector3 pos = droneManager.GetDronePositionById(text);
                dronePosInput.text = pos.x + "," + pos.y + "," + pos.z;
            }
        }
    }


    /// <summary>
    /// 이전 애니메이션을 강제로 멈추고 요청받은 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="animation">애니메이션 컴포넌트</param>
    /// <param name="animationName">애니메이션 이름</param>
    public void PlayAnimationSafe(Animation animation, string animationName)
    {
        animation.Stop();
        animation.Play(animationName);
    }
    #endregion
}
