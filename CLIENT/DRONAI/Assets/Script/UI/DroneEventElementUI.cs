using TMPro;
using Dronai.Data;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DroneEventElementUI : MonoBehaviour
{
    [SerializeField] private RawImage thumbnail = default;
    [SerializeField] private TMP_Text detailText = default;
    [SerializeField] private TMP_Text infoText = default;

    public void Initialize(DroneEvent droneEvent)
    {
        detailText.text = droneEvent.Detail;
        infoText.text = "드론 아이디 : " + droneEvent.DroneId;
        StartCoroutine(LoadImage(droneEvent.ImgPath));
    }
    private IEnumerator LoadImage(string path)
    {
        // read image and store in a byte array
        byte[] byteArray = File.ReadAllBytes(path);

        // Texture size does not matter 
        Texture2D sampleTexture = new Texture2D(2,2);

        // the size of the texture will be replaced by image size
        bool isLoaded = sampleTexture.LoadImage(byteArray);

        // apply this texure as per requirement on image or material
        if (isLoaded)
        {
            thumbnail.texture = sampleTexture;
        }

        yield break;
    }
}
