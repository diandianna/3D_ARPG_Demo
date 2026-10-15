using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DamageFontMgr : MonoBehaviour
{
    public static DamageFontMgr Instance { get; private set; }
    private Camera mainCam;
    private GameObject damageFontPrefab;
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        mainCam = Camera.main;
        damageFontPrefab = Resources.Load<GameObject>("Prefabs/DamageFont");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDamageFont(Vector3 vector3, int damage ,Transform WorldCanvasTrans,int enemyIndex)
    {
        Debug.Log("伤害数字显示");
        GameObject obj = Instantiate(damageFontPrefab, vector3, Quaternion.identity);
        Text objText = obj.GetComponent<Text>();
        if(mainCam != null) 
             obj.transform.forward = mainCam.transform.forward;
        obj.transform.SetParent(WorldCanvasTrans);
        objText.text = damage.ToString();

        // monsters 是 List，越界或目标已销毁时只飘字、不结算伤害，
        // 避免 ArgumentOutOfRange 打断调用方的消息处理循环
        GameObject monster = (enemyIndex >= 0 && enemyIndex < GameDataMgr.Instance.monsters.Count)
                             ? GameDataMgr.Instance.monsters[enemyIndex] : null;
        Auto monsterInfo = monster != null ? monster.GetComponent<Auto>() : null;
        if (monsterInfo != null && monsterInfo.monsterData != null)
            monsterInfo.monsterData.DamageTaken(damage);

        Destroy(obj, 2.5f);
    }
}
