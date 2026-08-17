using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PossessionCosts : MonoBehaviour
{
    // Start is called before the first frame update
    public bool QuitPossession = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameDataMgr.Instance.isPossession)
        {
            if (GameDataMgr.Instance.sanity > 0)
            {
                QuitPossession = false;
                GameDataMgr.Instance.sanity -= (Time.deltaTime * GameDataMgr.Instance.PossessionTimeMult()); // 每秒减少5点理智值
            }
            else if(!QuitPossession)
            {
                Debug.Log("理智值为0，结束附身状态");
                GameDataMgr.Instance.sanity = 0;
                // 理智值为0时，结束附身状态
                QuitPossession = true;

                StartCoroutine(BulletTimeMgr.Instance.BackSphereMove());
                BulletTimeMgr.Instance.ClearState();
                //GameDataMgr.Instance.isPossession = false;
                //// 这里可以添加其他逻辑，比如播放动画、触发事件等
            }
        }
        else
        {
            if(GameDataMgr.Instance.sanity < 100)
                GameDataMgr.Instance.sanity += Time.unscaledDeltaTime;
            if(GameDataMgr.Instance.sanity >100)
            {
                GameDataMgr.Instance.sanity = 100;
            }
        }
    }
}
