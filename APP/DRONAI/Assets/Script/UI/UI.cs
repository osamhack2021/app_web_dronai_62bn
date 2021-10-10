using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Text;

public class UI : MonoBehaviour
{
    // Components
    [BoxGroup("Components"), SerializeField] private DroneManager droneManager = default;
    [BoxGroup("Components"), SerializeField] private Animation anim = default;


    // Windows
    [BoxGroup("Window"), SerializeField] private GameObject[] windows = default;
    private GameObject previousWindow = null;

    private int currentWindow = 0;

    // Command variables
    [BoxGroup("Command"), SerializeField] private TMP_Text droneFormationInfoHeader = default;
    [BoxGroup("Command"), SerializeField] private TMP_InputField droneFormationCountInput = default;
    [BoxGroup("Command"), SerializeField] private TMP_Text droneFormationCheckResultText = default;
    [BoxGroup("Command"), SerializeField] private TMP_InputField dronePathInput = default;
    [BoxGroup("Command"), SerializeField] private TMP_Text droneFormationLogText = default;



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

    /// <summary>
    /// 윈도우를 표시하기 전 윈도우 요소들을 최신화 해주는 함수
    /// </summary>
    /// <param name="code"></param>
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

        // 요소 최신화
        StringBuilder sb = new StringBuilder();
        sb.Append("드론 편대 [온라인 : " + droneManager.AvailableDrone + "대 | 작업 중 : " + droneManager.WorkingDrone.ToString() + "대 | 전체 : " + droneManager.TotalDrone.ToString() + "]");
        droneFormationInfoHeader.text = sb.ToString();
        sb.Clear();

        sb.Append("Console is ready...waiting");
        droneFormationLogText.text = sb.ToString();
        sb.Clear();

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

    #region Event
    public void OnFormationCheckButtonDown()
    {
        int count = 0;
        try
        {
            count = int.Parse(droneFormationCountInput.text);
        }
        catch
        {
            droneFormationCheckResultText.text = "<color=\"red\">입력 오류</color>";
            return;
        }

        if (count <= droneManager.AvailableDrone)
        {
            droneFormationCheckResultText.text = "<color=\"green\">출동 가능</color>";
        }
        else
        {
            droneFormationCheckResultText.text = "<color=\"red\">드론 부족</color>";
        }
    }
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
    }

    #endregion
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
