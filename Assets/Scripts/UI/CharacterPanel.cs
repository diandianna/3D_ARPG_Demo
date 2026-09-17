using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPanel : BasePanel
{


    private void Start()
    {
        
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = 1;
            UIMgr.Instance.HidePanel<CharacterPanel>();
        }
    }
}
