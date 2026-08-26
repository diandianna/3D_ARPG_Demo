using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMgr : MonoBehaviour
{
    private static UIMgr instance;
    public static UIMgr Instance => instance;

    private Dictionary<string, BasePanel> panels = new Dictionary<string, BasePanel>();

    public Transform CanvasTrans;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        ShowPanel<BeginPanel>();
    }

    public BasePanel ShowPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (panels.ContainsKey(panelName) && panels[panelName] != null)
        {
            panels[panelName].gameObject.SetActive(true);
            StartCoroutine(ShowPanelRoutine(panels[panelName]));
            return panels[panelName];
        }
        if(panels.ContainsKey(panelName))panels.Remove(panelName);
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Panel/" + panelName);
        if (prefab == null)
        {
            Debug.LogError($"UIMgr: Prefab not found at Resources/Prefabs/Panel/{panelName}");
            prefab = Resources.Load<GameObject>("Prefabs/UI/" + panelName);
            if(prefab == null)
            {
                Debug.LogError($"UIMgr: Prefab not found at Resources/Prefabs/UI/{panelName}");
                return null;
            }
        }

        GameObject go = Instantiate(prefab);
        T panelobj = go.GetComponent<T>();
        if (panelobj == null)
        {
            Debug.LogError($"UIMgr: {typeof(T).Name} component not found on prefab {panelName}");
            Destroy(go);
            return null;
        }

        go.transform.SetParent(CanvasTrans, false);
        panels.Add(panelName, panelobj);
        //StartCoroutine(ShowPanelRoutine(panelobj));
        return panels[panelName];
    }

    public BasePanel GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (panels.ContainsKey(panelName))
        {
            return panels[panelName];
        }
        else
        {
            Debug.LogWarning($"UIMgr: Panel [{panelName}] not found.");
            return null;
        }
    }

    public void HidePanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (panels.ContainsKey(panelName))
        {
            Destroy(panels[panelName].gameObject);
            panels.Remove(panelName);
        }
    }

    private IEnumerator ShowPanelRoutine(BasePanel panel)
    {
        yield return StartCoroutine(panel.ShowPanel());

    }

    private IEnumerator HidePanelRoutine(BasePanel panel, string panelName)
    {
        yield return StartCoroutine(panel.HidePanel());
        Debug.Log($"UIMgr: Destroying panel [{panelName}]");
        panels.Remove(panelName);
        Destroy(panel.gameObject);
    }
}
