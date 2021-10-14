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
    [BoxGroup("Components"), SerializeField] private CameraManager cameraManager = default;
    [BoxGroup("Components"), SerializeField] private Animation anim = default;


    // Windows
    [BoxGroup("Window"), SerializeField] private GameObject[] windowsUI = default;
    [BoxGroup("Window"), SerializeField] private GameObject[] windowsOverview = default;
    private int currentWindow = 0;
    private GameObject previousWindow = null;


    // Overview variables
    private List<Vector3> currentOverviewNodes = new List<Vector3>();
    int overviewIndex = 0; // 카메라 타겟 인덱스


    // Command variables
    [BoxGroup("Command"), SerializeField] private TMP_Text droneFormationInfoHeader = default;
    [BoxGroup("Command"), SerializeField] private TMP_InputField droneFormationCountInput = default;
    [BoxGroup("Command"), SerializeField] private TMP_Text droneFormationCheckResultText = default;
    [BoxGroup("Command"), SerializeField] private TMP_InputField droneFormationPathInput = default;
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
    private bool isOverviewWindowEnabled = false;
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
            CallWindow(isWindowEnabled);
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CallSelectionWindow(!isSelectionWindowEnabled);
        }
    }
    #endregion

    #region Window
    private void CallWindow(bool state)
    {
        if (isOverviewWindowEnabled || isSelectionWindowEnabled)
        {
            return;
        }

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

        previousWindow = windowsUI[code];

        targetAnim = windowsUI[code].GetComponent<Animation>();
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
    public void OpenOverviewWindow(int code)
    {
        // Variables set
        isOverviewWindowEnabled = true;
        overviewIndex = 0;

        // Prepare the target window
        foreach (GameObject o in windowsOverview)
        {
            o.SetActive(false);
        }
        windowsOverview[code].SetActive(true);

        // Play
        PlayAnimationSafe(anim, "UI_Seperate_In");
    }
    public void CloseOverviewWindow()
    {
        // Variables set
        isOverviewWindowEnabled = false;

        // 드론 매니저가 생성한 디버그 라인들 전부 종료
        droneManager.ClearLine();

        // 카메라 원위치
        cameraManager.SetToDefaultTarget();

        // Play
        PlayAnimationSafe(anim, "UI_Seperate_Out");
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

    #region Overview UI

    /// <summary>
    /// Overview ui로 부터 카메라 타겟 요청을 받을 때 수행되는 함수
    /// </summary>
    /// <param name="direction">T is next F is previous</param>
    public void ChangeCameraTarget(bool direction)
    {
        if (direction)
        {
            // Next
            if (overviewIndex < currentOverviewNodes.Count - 1)
            {
                overviewIndex++;

                // Change the actual target
                cameraManager.ChangeTarget(currentOverviewNodes[overviewIndex]);
            }
        }
        else
        {
            // Previous
            if (overviewIndex > 0)
            {
                overviewIndex--;

                // Change the actual target
                cameraManager.ChangeTarget(currentOverviewNodes[overviewIndex]);
            }
        }
    }

    #endregion


    #region Formation UI
    private void ClearFormationInput()
    {
        droneFormationPathInput.text = "";
        droneFormationCountInput.text = "";
    }
    private int CheckCountInput()
    {
        // 요소 최신화
        StringBuilder sb = new StringBuilder();
        sb.Append("드론 편대 [온라인 : " + droneManager.AvailableDrone + "대 | 작업 중 : " + droneManager.WorkingDrone.ToString() + "대 | 전체 : " + droneManager.TotalDrone.ToString() + "]");
        droneFormationInfoHeader.text = sb.ToString();
        sb.Clear();

        int count = 0;
        try
        {
            count = int.Parse(droneFormationCountInput.text);
        }
        catch
        {
            droneFormationCheckResultText.text = "<color=\"red\">입력 오류</color>";
            droneFormationLogText.text = "[<color=\"red\">입력 오류!</color>] 드론 수 입력란을 확인하세요!";
            return -1;
        }

        if (count <= droneManager.AvailableDrone)
        {
            droneFormationCheckResultText.text = "<color=\"green\">출동 가능</color>";
        }
        else
        {
            droneFormationCheckResultText.text = "<color=\"red\">드론 부족</color>";
            droneFormationLogText.text = "[<color=\"red\">가용 불가!</color>] 가용 가능한 드론 수가 입력한 값보다 부족합니다!";
            return -1;
        }

        return count;
    }
    private List<Vector3> CheckPathInput()
    {
        List<Vector3> nodes = new List<Vector3>();
        try
        {
            string[] plain = droneFormationPathInput.text.Trim().Split(';');
            foreach (string pos in plain)
            {
                string[] value = pos.Trim().Split(',');
                float x = float.Parse(value[0]);
                float y = float.Parse(value[1]);
                float z = float.Parse(value[2]);
                nodes.Add(new Vector3(x, y, z));
            }
        }
        catch
        {
            droneFormationLogText.text = "[<color=\"red\">입력 오류!</color>] 드론 경로 입력란을 확인하세요! (EX 1,1,1 ; 10,5,10 ; 3,3,3)";
            return null;
        }

        return nodes;
    }
    public void OnFormationCheckButtonDown()
    {
        CheckCountInput();
    }
    public void OnFomrationOverviewButtonDown()
    {
        currentOverviewNodes = CheckPathInput();

        if (currentOverviewNodes.Count < 2)
        {
            droneFormationLogText.text = "[<color=\"red\">입력 오류!</color>] 최소 2개 이상의 노드가 필요합니다! (EX 1,1,1 ; 10,5,10 ; 3,3,3)";
            return;
        }

        droneManager.OverviewDroneFormation(currentOverviewNodes, (bool success) =>
        {
            if (success)
            {
                OpenOverviewWindow(0);
                return;
            }
            else
            {
                droneFormationLogText.text = "[<color=\"red\">미리보기 실패</color>]";
                return;
            }
        });
    }
    public void OnFormationExecuteButtonDown()
    {
        int count = CheckCountInput();

        if (count == -1)
        {
            return;
        }

        List<Vector3> nodes = CheckPathInput();

        if (nodes.Count < 2)
        {
            droneFormationLogText.text = "[<color=\"red\">입력 오류!</color>] 최소 2개 이상의 노드가 필요합니다! (EX 1,1,1 ; 10,5,10 ; 3,3,3)";
            return;
        }

        droneManager.DefineDroneFormation(count, nodes, (bool success) =>
        {
            if (success)
            {
                droneFormationLogText.text = "[<color=\"green\">편대 구성 성공</color>]";
                ClearFormationInput();
                return;
            }
            else
            {
                droneFormationLogText.text = "[<color=\"red\">편대 구성 실패</color>]";
                return;
            }
        });
    }

    // Need to remove
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

            // droneManager.BuildDroneFormation(pos, int.Parse(droneIdInput.text.ToString()));
            return;
        }

        if (dronePosInput.text.Length == 0) // 선택된 드론 폭파
        {
            droneManager.DestroyDrone(droneIdInput.text);
            return;
        }
    }
    // --- x

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
