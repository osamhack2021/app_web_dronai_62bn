using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Dronai.Network;


public class LoadingUI : MonoBehaviour
{

    [SerializeField] private Image frontBar = default;
    [SerializeField] private TMP_Text detailText = default;

    [SerializeField] private Animation anim = default;
    [SerializeField] private Animation blackAnimation = default;


    // Coroutines
    private Coroutine loadingProgressBarRoutine = null;


    private void Start()
    {
        Initialize();
    }
    private void Initialize()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        yield return new WaitForSeconds(0.6f);

        anim.Stop();
        anim.Play("LoadingUI_Intro");


        // 씬 로딩 시작
        SetLoadingProgressBar(0);
        detailText.text = "Getting simulation data...";

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("SampleScene");
        asyncOperation.allowSceneActivation = false;

        while (asyncOperation.progress >= 0.9f)
        {
            detailText.text = "Getting simulation data... " + (asyncOperation.progress * 100) + "%";

            //Output the current progress
            SetLoadingProgressBar(asyncOperation.progress * 0.5f);

            yield return null;
        }


        // 좀 쉬었다 가자
        yield return new WaitForSeconds(1f);


        // 서버 접속 시도
        SetLoadingProgressBar(0.6f);
        detailText.text = "Connecting to server...";
        yield return new WaitForSeconds(1f);

        bool working = true, connection = false;
        NetworkManager.instance.TestConnection((bool result) =>
        {
            connection = result;
            working = false;
        });


        // 서버 응답 대기
        while (working)
        {
            yield return null;
        }

        SetLoadingProgressBar(0.8f);

        // 결과 제출
        if (!connection)
        {
            detailText.text = "Failed to connect server! [무시하고 진행을 원할시 Space키]";
            for (; ; )
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // 임시 로딩 완료
                    SetLoadingProgressBar(1f);
                    detailText.text = "Unstable client, but you are free to go!";
                    yield return new WaitForSeconds(3f);

                    blackAnimation.Play("FadeIn_Canvas");
                    yield return new WaitForSeconds(1f);

                    asyncOperation.allowSceneActivation = true;
                    yield break;
                }
                yield return null;
            }
        }

        // 로딩 완료
        SetLoadingProgressBar(1f);
        detailText.text = "Accepted, you are good to go!";
        yield return new WaitForSeconds(3f);

        blackAnimation.Play("FadeIn_Canvas");
        yield return new WaitForSeconds(1f);

        asyncOperation.allowSceneActivation = true;
    }

    private void SetLoadingProgressBar(float value)
    {
        if (loadingProgressBarRoutine != null)
        {
            StopCoroutine(loadingProgressBarRoutine);
            loadingProgressBarRoutine = null;
        }

        loadingProgressBarRoutine = StartCoroutine(LoadingProgressBarRoutine(value));
    }

    private IEnumerator LoadingProgressBarRoutine(float targetX)
    {
        Vector3 destination = Vector3.zero;

        for (; ; )
        {
            destination = frontBar.transform.localScale;
            destination.x = Mathf.Lerp(destination.x, targetX, Time.deltaTime);
            frontBar.transform.localScale = destination;
            yield return null;
        }
    }
}
