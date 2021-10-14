using UnityEngine;

namespace Dronai.Procedural
{
	[CreateAssetMenu(menuName ="ProceduralCity/Rule")]
	public class Rule : ScriptableObject
	{
		public string letter;
		[SerializeField]
		private string[] results = null;
		[SerializeField]
		private bool randomResult = false;

		public string GetResult()
		{
			if (randomResult)
			{
				int randomIndex = UnityEngine.Random.Range(0, results.Length);
				return results[randomIndex];
			}
			return results[0];
		}

	}
}

