using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Text;

public class FormationElementUI : MonoBehaviour
{

    // Basic info
    [Header("Basic Info")]
    [SerializeField] private TMP_Text basicInfoText = default;

    // State
    [Header("State")]
    [SerializeField] private Transform stateElementParent = default;
    [SerializeField] private GameObject stateElementPrefab = default;

    // Routes
    [Header("Routes")]
    [SerializeField] private TMP_Text previousPositionText = default;
    [SerializeField] private TMP_Text currentPositionText = default;
    [SerializeField] private TMP_Text nextPositionText = default;

    // Buttons
    [Header("Buttons")]
    [SerializeField] private GameObject actButton = default;


    // Identity
    private int code = default;

    // Components
    private DroneManager droneManager = default;
    private UI uiManager = default;
    private Formation formation = default;

    public void Initialize(int code, DroneManager droneManager, UI uiManager)
    {
        // 할당
        this.code = code;
        this.droneManager = droneManager;
        this.uiManager = uiManager;
        formation = droneManager.Formations[code];

        // Events
        droneManager.OnFormationUpdated += RefreshUI;

        // UI 최신화
        RefreshUI();
    }
    private void OnDisable()
    {
        // Events
        droneManager.OnFormationUpdated -= RefreshUI;
    }
    private void RefreshUI()
    {
        StringBuilder sb = new StringBuilder();

        // Basic info
        sb.AppendFormat("그룹 아이디 : {0} | 포트 키 : {1} | 편대 {2}", code, formation.PortKey, formation.DroneCount);
        basicInfoText.text = sb.ToString();
        sb.Clear();

        // State
        string[] states = formation.GetAllStateString();
        int index = formation.GetConditionIndex();
        for (int i = 0; i < states.Length; i++)
        {
            GameObject targetObject = Instantiate(stateElementPrefab, stateElementParent);
            TMP_Text targetText = targetObject.GetComponent<TMP_Text>();
            targetText.text = states[i];
            if (i == index)
            {
                targetObject.GetComponent<Animation>().Play("StateElement_Mark");
            }
        }

        // Route positions
        int currentRoutesIndex = formation.GetPathPosition();
        previousPositionText.text = formation.GetPathPositionString(currentRoutesIndex - 1, "PREVIOUS");
        currentPositionText.text = formation.GetPathPositionString(currentRoutesIndex, "CURRENT");
        nextPositionText.text = formation.GetPathPositionString(currentRoutesIndex + 1, "NEXT");

        // Buttons
        if(formation.GetConditionIndex() == 2)
        {
            sb.Append("구성 중...");
        }
        else if (formation.GetConditionIndex() == 6)
        {
            sb.Append("복귀 중...");
        }
        else
        {
            if (formation.GetConditionIndex() > 2)
            {
                sb.Append("복귀");
            }
            else
            {
                sb.Append("출격");
            }
        }

        actButton.transform.GetChild(0).GetComponent<TMP_Text>().text = sb.ToString();
        sb.Clear();
    }
    public void OnActButtonDown()
    {
        if (formation.GetConditionIndex() == 2)
        {
            // Nothing
        }
        else if (formation.GetConditionIndex() == 6)
        {
            // Nothing
        }
        else
        {
            if (formation.GetConditionIndex() > 2)
            {
                droneManager.CloseDroneFormation(code);
            }
            else
            {
                droneManager.BuildDroneFormation(code);

                // 출격 명령 후 자동 추적
                TraceTarget();
            }
        }

    }
    public void OnTraceButtonDown()
    {
        TraceTarget();
    }
    private void TraceTarget()
    {
        uiManager.OpenWindow(false);
        CameraManager.Instance.ChangeTarget(formation.HeadDrone.transform);
    }

}
