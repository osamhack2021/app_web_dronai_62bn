using TMPro;
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class DroneGridElementUI : MonoBehaviour, IPointerClickHandler
{

    [SerializeField, ReadOnly] private string id = string.Empty;
    [SerializeField] private TMP_Text displayText = default;
    [SerializeField] private Image displayImage = default;


    // Action and Events
    private Action<string> OnClick = default;


    public void Initialize(string id, string prefix, Color color, Action<string> OnClick)
    {
        this.id = id;
        this.OnClick += OnClick;

        // Update the display text
        displayText.text = prefix + id;
        displayImage.color = color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(id);
    }
}
