using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LoadingManager : Singleton<LoadingManager>
{

    [SerializeField] private Animation curtain = default;

    // Coroutines
    private Coroutine loadSceneRoutine = default;
    private string previousSceneName = default;


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string current = SceneManager.GetActiveScene().name;
        try
        {
            if (previousSceneName != current)
            {
                AnimationPlaySafe(curtain, "FadeOut_Canvas");
            }
            else
            {
                return;
            }
        }
        catch
        {
            return;
        }

        previousSceneName = current;
    }

    private void LoadScene(string name)
    {
        if (loadSceneRoutine != null)
        {
            Debug.LogError("이미 로딩중인 씬이 있습니다. 작성 코드를 확인하고 수정하시오!!");
            return;
        }
        else
        {
            loadSceneRoutine = StartCoroutine(LoadSceneRoutine(name));
        }
    }
    private IEnumerator LoadSceneRoutine(string name)
    {
        // 커튼
        AnimationPlaySafe(curtain, "FadeIn_Canvas");
        yield return new WaitForSeconds(1f);

        // 씬 로더 정의
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(name);
        asyncOperation.allowSceneActivation = false;

        // 씬 로딩 시작
        while (asyncOperation.progress >= 0.9f)
        {
            // print(asyncOperation.progress);

            yield return null;
        }

        // Load the actual scene
        asyncOperation.allowSceneActivation = true;
        yield return new WaitForSeconds(.4f);

        //커튼
        AnimationPlaySafe(curtain, "FadeOut_Canvas");

        loadSceneRoutine = null;
        yield break;
    }

    private void AnimationPlaySafe(Animation animation, string name)
    {
        animation.Stop();
        animation.Play(name);
    }
}