using UnityEngine;
using System.Collections;


namespace Dronai.Procedural
{
    public class FallTween : MonoBehaviour
    {
        private Vector3 destination;
        public float timeToFall = 0.2f;

        private void Start()
        {
            destination = transform.position;
            gameObject.transform.position += Vector3.up * 10;
            StartCoroutine(Drop());
        }

        private IEnumerator Drop()
        {
            Vector3 position = gameObject.transform.position;
            float currentTime = 0f;
            
            while (currentTime <= timeToFall)
            {
                gameObject.transform.position = Vector3.Lerp(position, destination, currentTime / timeToFall);
                currentTime += Time.deltaTime;
                yield return null;
            }

            gameObject.transform.position = destination;
        }
    }
}

