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
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDamageFont(Vector3 vector3, int damage ,Transform WorldCanvasTrans,int enemyIndex)
    {
        Debug.Log("伤害数字显示");
        GameObject obj = Instantiate(Resources.Load<GameObject>("Prefabs/DamageFont"), vector3, Quaternion.identity);
        Text objText = obj.GetComponent<Text>();

        obj.transform.forward = Camera.main.transform.forward;
        obj.transform.SetParent(WorldCanvasTrans);
        objText.text = damage.ToString();
        Auto monsterInfo = GameDataMgr.Instance.monsters[enemyIndex].GetComponent<Auto>();
        monsterInfo.monsterData.DamageTaken(damage);
    }
}
