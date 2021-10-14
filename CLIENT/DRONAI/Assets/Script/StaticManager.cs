using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticManager : MonoBehaviour
{
    public static StaticManager Instance;

    private void Awake()
    {
        // 글로벌 오브젝트 중복성 확인
        if (StaticManager.Instance != null) Destroy(gameObject);

        // 싱글톤 등록
        Instance = this;

        // 글로벌 오브젝트
        DontDestroyOnLoad(gameObject);
    }
}
