using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;


public class UI : MonoBehaviour
{
    // Components
    [BoxGroup("Components"), SerializeField] private DroneManager droneManager = default;
    [BoxGroup("Components"), SerializeField] private Animation anim = default;


    // Windows
    [BoxGroup("Window"), SerializeField] private GameObject[] windows = default;
    private GameObject previousWindow = null;
    
    private int currentWindow = 0;


    // Input variables
    [BoxGroup("Input"), SerializeField] private TMP_InputField droneIdInput = default;
    [BoxGroup("Input"), SerializeField] private TMP_InputField dronePosInput = default;


    // Selection variables
    [BoxGroup("Drone Selection"), SerializeField] private GameObject selectionWindow = default;
    [BoxGroup("Drone Selection"), SerializeField] private GameObject selectionPrefab = default;
    [BoxGroup("Drone Selection"), SerializeField] private Transform selectionParent = default;
    private string selectedDroneId = string.Empty;


    // Conditions
    private bool isWindowEnabled = false;
    private bool isSelectionWindowEnabled = false;



    #region Life cycle
    public void Initialize()
    {
        IntializeDroneSelection();
    }

    public void IntializeDroneSelection()
    {

        // 드론 리스트 UI 생성자
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
            ShowUI(isWindowEnabled);
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CallSelectionWindow(!isSelectionWindowEnabled);
        }
    }
    #endregion

    #region Window
    private void ShowUI(bool state)
    {
        if (!state)
        {
            // change state
            isWindowEnabled = true;

            // Call
            UpdateWindow(currentWindow);

            anim.Stop();
            anim.Play("UI_Intro");
        }
        else
        {
            // change state
            isWindowEnabled = false;

            anim.Stop();
            anim.Play("UI_Outro");
        }
    }

    private void UpdateWindow(int code)
    {
        currentWindow = code;

        Animation targetAnim;

        if (previousWindow != null)
        {
            targetAnim = previousWindow.GetComponent<Animation>();

            targetAnim.Stop();
            targetAnim.Play("Area_Outro");
        }

        previousWindow = windows[code];

        targetAnim = windows[code].GetComponent<Animation>();
        targetAnim.Play("Area_Intro");
    }

    /// <summary>
    /// 버튼으로부터 윈도우 변경 호출을 받는 함수 / 중복검사 함
    /// </summary>
    /// <param name="code">호출할 페이지 코드</param>
    public void CallWindow(int code)
    {
        if (currentWindow == code) return;
        else UpdateWindow(code);
    }


    private void CallSelectionWindow(bool state)
    {
        if (state)
        {
            Animation windowAnimation = selectionWindow.GetComponent<Animation>();
            PlayAnimationSafe(windowAnimation, "FadeIn_Canvas");
        }
        else
        {
            Animation windowAnimation = selectionWindow.GetComponent<Animation>();
            PlayAnimationSafe(windowAnimation, "FadeOut_Canvas");
        }

        // Change the state
        isSelectionWindowEnabled = state;
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
        if (!droneIdInput.text.Contains("Drone")) // 편대 구축할 드론의 수 입력
        {
            string[] position = dronePosInput.text.Split(',');
            if (position.Length != 3) 
            {
                print("Please re-enter");
                return;
            }

            Vector3 pos = new Vector3(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]));
            //droneManager.MoveSingleDrone(droneIdInput.text, pos);

            droneManager.BuildDroneFormation(pos, int.Parse(droneIdInput.text.ToString()));
            return;
        }

        if (dronePosInput.text.Length == 0) // 선택된 드론 폭파
        {
            droneManager.DestroyDrone(droneIdInput.text);
            return;
        }

        string[] astarTestMove = dronePosInput.text.Split(',');
        if (astarTestMove.Length != 3) {
            print("Please re-enter");
            return;
        }

        Vector3 astarPos = new Vector3(float.Parse(astarTestMove[0]), float.Parse(astarTestMove[1]), float.Parse(astarTestMove[2]));
        droneManager.DroneDic[droneIdInput.text].astarPathFind(astarPos);
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
