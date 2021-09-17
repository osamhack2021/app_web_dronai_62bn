using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UI : MonoBehaviour
{
    [SerializeField] private DroneManager droneManager = default;

    [SerializeField] private TMP_InputField droneIdInput = default;
    [SerializeField] private TMP_InputField positionInput = default;


    public void AutoFill(string text)
    {
        if(positionInput.text.Length==0)
        {
            if (droneManager.ContainsDrone(text))
            {
                Vector3 pos = droneManager.GetDronePositionById(text);
                positionInput.text = pos.x + "," + pos.y + "," + pos.z;
            }
        }
    }
    public void OnButtonDown()
    {
        if (positionInput.text.Length == 0) {
            droneManager.PickDrones();
            return;
        }

        string[] position = positionInput.text.Split(',');
        Vector3 pos = new Vector3(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]));
        droneManager.MoveSingleDrone(droneIdInput.text, pos);
    }
}
