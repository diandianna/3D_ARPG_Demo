using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PossessionCosts : MonoBehaviour
{
    private static PossessionCosts instance;
    public static PossessionCosts Instance => instance;

    // Start is called before the first frame update
    public bool QuitedPossession = false;
    int savedDef;
    float overDmgSum;
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameDataMgr.Instance.isPossession)
        {
            if (GameDataMgr.Instance.sanity > 0)
            {
                QuitedPossession = false;
                GameDataMgr.Instance.sanity -= (Time.deltaTime * GameDataMgr.Instance.PossessionTimeMult()); // 每秒减少x点理智值
            }
            else if(!QuitedPossession)
            {
                Debug.Log("理智值为0，结束附身状态");
                GameDataMgr.Instance.sanity = 0;
                // 理智值为0时，结束附身状态
                QuitedPossession = true;

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

    public void AddOverDamage(float dmg) { overDmgSum += dmg; }

    Collider body;
    public void OnPossessStart()
    {
        body = GameDataMgr.Instance.possessionCharacter.GetComponent<Collider>();
        if (body != null) body.isTrigger = true;
        savedDef = GameDataMgr.Instance.playerData.DefNum;
        GameDataMgr.Instance.playerData.DefNum = 0;   // ① 肉体腐化
        overDmgSum = 0;
    }

    public void OnPossessEnd()
    {
        if (body != null) body.isTrigger = false;
        GameDataMgr.Instance.playerData.DefNum = savedDef;        // ① 恢复防御
        GameDataMgr.Instance.playerData.ChangeHp(-(int)overDmgSum); // ③ 灵魂过载结算
        overDmgSum = 0;
    }
}
