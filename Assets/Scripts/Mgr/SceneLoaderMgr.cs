using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderMgr : MonoBehaviour
{
    private static SceneLoaderMgr instance;
    public static SceneLoaderMgr Instance => instance;

    public string targetSceneName;

    private void Awake()
    {
        instance = this;
    }


}
