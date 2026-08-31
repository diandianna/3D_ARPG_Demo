using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public Image image;
    public float duration = 1f;
    public float timer = 0f;
    public bool hide = false;
    public bool once = false;
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hide)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void Show()
    {
            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);
            timer += Time.deltaTime;



        if (canvasGroup.alpha >= 1)
        {
            StartCoroutine(WaitSeconds());
        }

        //StartCoroutine(Hide());
    }

    public void Hide()
    {
            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            timer += Time.deltaTime;
        if (canvasGroup.alpha <= 0 && once)
        {
            SceneManager.LoadScene("HomeScene");
        }

        if (canvasGroup.alpha <= 0)
        {
            if (!once)
            {
                image.sprite = Resources.Load<Sprite>("StartUI/防沉迷");
                timer = 0;
                once = true;
                hide = false;
            }

        }

    }

    IEnumerator WaitSeconds()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        hide = true;
        timer = 0;
    }
}
