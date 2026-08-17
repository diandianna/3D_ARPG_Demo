using System.Collections;
using UnityEngine;


public class BasePanel : MonoBehaviour
{
    public bool showPanel = true;
    public bool isAnimating = false;

    protected CanvasGroup canvasGroup;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        if (canvasGroup.alpha == 0 && showPanel && !isAnimating)
        {
            isAnimating = true;
            StartCoroutine(ShowPanel());
        }
        else if (canvasGroup.alpha == 1 && !showPanel && !isAnimating)
        {
            isAnimating = true;
            StartCoroutine(HidePanel());
        }
    }

    public virtual IEnumerator ShowPanel()
    {
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1;
        isAnimating = false;
    }

    public virtual IEnumerator HidePanel()
    {
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0;
        isAnimating = false;
    }
}
