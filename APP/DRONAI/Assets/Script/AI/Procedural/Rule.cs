using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Dronai.Procedural
{

    [CreateAssetMenu(menuName = "ProceduralCity/Rule")]
    public class Rule : ScriptableObject
    {
        public string Letter = default;
        [SerializeField] private string[] results = null;




        public string GetResult()
        {
            return results[0];
        }
    }

}