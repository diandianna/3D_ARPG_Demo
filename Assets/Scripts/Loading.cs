using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    public Scrollbar progressBar;
    public Image progressHandle;

    public Image imgBk;

    void Start()
    {
        //随机1-4的数字，随机显示背景图
        int rand = 4;/*Random.Range(1, 5);*/
        imgBk.sprite = Resources.Load<Sprite>("Loading/Loading" + rand);
        progressHandle.sprite = Resources.Load<Sprite>("Loading/LoadingBar/LoadingBar" + rand);

        StartCoroutine(LoadAsync(SceneLoaderMgr.Instance.targetSceneName));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public IEnumerator LoadAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            progressBar.size = progress;
            if(op.progress >= 0.9f)
            {
                progressBar.size = 1f;
                yield return new WaitForSecondsRealtime(1f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
