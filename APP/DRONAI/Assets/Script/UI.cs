using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    [SerializeField] private DroneManager droneManager = default;

    [SerializeField] private TMP_InputField droneNameInput = default;
    [SerializeField] private TMP_InputField positionInput = default;


    public void OnButtonDown()
    {
        string[] position = positionInput.text.Split(',');
        Vector3 pos = new Vector3(float.Parse(position[0]),float.Parse(position[1]), float.Parse(position[2]));
        droneManager.MoveSingleDrone(droneNameInput.text, pos);
    }
}
