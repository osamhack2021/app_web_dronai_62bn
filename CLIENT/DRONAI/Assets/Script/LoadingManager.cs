using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LoadingManager : Singleton<LoadingManager>
{

    [SerializeField] private Animation curtain = default;

    // Coroutines
    private Coroutine loadSceneRoutine = default;


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        curtain.Stop();
        curtain.Play("FadeOut_Canvas");
    }

    private void LoadScene(string name)
    {
        if (loadSceneRoutine != null)
        {
            Debug.LogError("이미 로딩중인 씬이 있습니다. 작성 코드를 확인하고 수정하시오!!");
            return;
        }
    }
    private IEnumerator LoadSceneRoutine(string name)
    {
        // 커튼
        AnimationPlaySafe(curtain, "FadeOut_Canvas");

        // 씬 로더 정의
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(name);
        asyncOperation.allowSceneActivation = false;

        // 씬 로딩 시작
        while (asyncOperation.progress >= 0.9f)
        {
            // print(asyncOperation.progress);

            yield return null;
        }

        //커튼
        AnimationPlaySafe(curtain, "FadeIn_Canvas");

        // Load the actual scene
        asyncOperation.allowSceneActivation = true;
    }

    private void AnimationPlaySafe(Animation animation, string name)
    {
        animation.Stop();
        animation.Play(name);
    }
}