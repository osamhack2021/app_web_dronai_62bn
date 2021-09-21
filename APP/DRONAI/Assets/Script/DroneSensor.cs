using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;



public class DroneSensor : SerializedMonoBehaviour, IDetectable
{
    [SerializeField] private Drone drone = default;
    [SerializeField, Tooltip("센서 이름")] private string additionalString = default;
    [SerializeField] private UnityEvent<GameObject> OnTriggerEnterEvent = default;
    [SerializeField, Tooltip("딕셔너리 : TARGET ID - NAME")] private Dictionary<string, string> whitelist = new Dictionary<string, string>();

    public string ParentID
    {
        get
        {
            if (drone == null) return string.Empty;
            else return drone.GetID();
        }
    }

    [SerializeField, Button(ButtonSizes.Medium)]
    private void AutomaticallyAssign()
    {
        if (drone == null) drone = transform.parent.GetComponent<Drone>();
    }


    public void Initialize(Drone parent)
    {
        // Assign parent module
        if (drone == null) drone = parent;

        // Assign id
        string creatorId = drone.GetID();

        // Checking the whitelist
        if (!whitelist.ContainsKey(creatorId))
        {
            whitelist.Add(creatorId, drone.name);
        }

        // Change the object name
        name = additionalString.Length > 0 ? drone.name + " - " + additionalString : drone.name + " - Sensor";
    }

    private void OnTriggerEnter(Collider other)
    {
        IDetectable target = other.GetComponent<IDetectable>();
        if (target != null)
        {
            if (whitelist.ContainsKey(target.GetID()))
            {
                return;
            }
        }
        if (other.tag.Equals("Ignore"))
        {
            return;
        }

        OnTriggerEnterEvent?.Invoke(other.gameObject);

        return;
    }

    public Drone GetDrone()
    {
        return drone;
    }
    public string GetID()
    {
        return ParentID;
    }
}
