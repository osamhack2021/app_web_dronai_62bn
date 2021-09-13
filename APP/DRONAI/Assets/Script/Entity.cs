using System;
using UnityEngine;
using Sirenix.OdinInspector;



public class Entity : SerializedMonoBehaviour, IDetectable
{
    [BoxGroup("Entity"), SerializeField, ReadOnly] protected string id = Guid.NewGuid().ToString();

    public string ID
    {
        get { return id; }
    }

    protected void GenerateNewID()
    {
        id = Guid.NewGuid().ToString();
    }
 
    public string GetID()
    {
        return id;
    }
}
