using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopPanel : BasePanel
{
    private void Start()
    {

    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIMgr.Instance.HidePanel<ShopPanel>();
        }
    }
}
