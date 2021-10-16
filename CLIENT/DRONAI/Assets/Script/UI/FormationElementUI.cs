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


    // Identity
    private int code = default;

    // Components
    private DroneManager droneManager = default;

    public void Initialize(int code, DroneManager droneManager)
    {
        this.code = code;
        this.droneManager = droneManager;

        StringBuilder sb = new StringBuilder();

        Formation formation = droneManager.Formations[code];

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
        int currentRoutesIndex = formation.GetRouteIndex();
        previousPositionText.text = formation.GetRoutePositionString(currentRoutesIndex - 1, "PREVIOUS");
        currentPositionText.text = formation.GetRoutePositionString(currentRoutesIndex, "CURRENT");
        nextPositionText.text = formation.GetRoutePositionString(currentRoutesIndex + 1, "NEXT");
    }

}
