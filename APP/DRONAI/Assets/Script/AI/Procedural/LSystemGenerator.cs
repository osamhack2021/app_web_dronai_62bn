using System.Text;
using UnityEngine;


namespace Dronai.Procedural
{
    public class LSystemGenerator : MonoBehaviour
    {
        public Rule[] Rules;
        public string RootSentence = string.Empty;
        [Range(0, 10)] public int IterationLimit = 1;

        [SerializeField] private bool randomIgnoreRuleModifier = true;
        [SerializeField, Range(0, 1)] private float changeToIgnoreRule = 0.3f;



        public string GenerateSentence(string word = null)
        {
            if (word == null)
            {
                word = RootSentence;
            }
            return GrowRecursive(word);
        }

        private string GrowRecursive(string word, int iterationIndex = 0)
        {
            if (iterationIndex >= IterationLimit)
            {
                return word;
            }

            StringBuilder newWord = new StringBuilder();

            foreach (var c in word)
            {
                newWord.Append(c);
                ProcessRulesRecursivelly(newWord, c, iterationIndex);
            }

            return newWord.ToString();
        }

        private void ProcessRulesRecursivelly(StringBuilder newWord, char c, int iterationIndex)
        {
            foreach (var rule in Rules)
            {
                if (rule.Letter == c.ToString())
                {
                    if(randomIgnoreRuleModifier && iterationIndex > 1)
                    {
                        if(Random.value < changeToIgnoreRule)
                        {
                            return;
                        }
                    }
                    newWord.Append(GrowRecursive(rule.GetResult(), iterationIndex + 1));
                }
            }
        }
    }
}
