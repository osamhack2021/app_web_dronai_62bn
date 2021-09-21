using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;



public class DroneGridElementUI : MonoBehaviour, IPointerClickHandler
{

    [SerializeField, ReadOnly] private string id = string.Empty;
    [SerializeField] private TMP_Text displayText = default;


    // Action and Events
    private Action<string> OnClick = default;
    [SerializeField] private UnityEvent OnClickEvent = default;


    public void Initialize(string id, Action<string> OnClick)
    {
        this.id = id;
        this.OnClick += OnClick;

        // Update the display text
        displayText.text = id;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(id);
        OnClickEvent?.Invoke();
    }
}
