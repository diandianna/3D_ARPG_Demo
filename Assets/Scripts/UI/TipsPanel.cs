using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipsPanel : BasePanel
{
    public Text TipsText;

    // Update is called once per frame
    void Update()
    {
        if(Input.anyKeyDown)
        {
            UIMgr.Instance.HidePanel<TipsPanel>();
        }
    }

    public void SetTipsText(string str)
    {
        TipsText.text = str;
    }
}
