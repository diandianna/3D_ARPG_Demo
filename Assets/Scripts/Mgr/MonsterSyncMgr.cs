using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Antlr3.Runtime.Tree.TreeWizard;

public class MonsterSyncMgr : MonoBehaviour
{
    public static MonsterSyncMgr Instance { get; private set; }

    public Dictionary<int, GameObject> monsters = new Dictionary<int, GameObject>();
    Dictionary<int, Vector3> targetPos = new Dictionary<int, Vector3>();
    Dictionary<int, float> targetYaw = new Dictionary<int, float>();
    Dictionary<int, Vector3> lastPos = new Dictionary<int, Vector3>();
    HashSet<int> seeded = new HashSet<int>();
    HashSet<int> dying = new HashSet<int>();
    Dictionary<int ,GameObject> hpObj = new Dictionary<int ,GameObject>();
    Dictionary<int ,Image> hpImg = new Dictionary<int , Image>();
    void Start()
    {
        Instance = this;
    }


    void Update()
    {
        float k = 1f - Mathf.Exp(-10f * Time.deltaTime); // 帧率无关的平滑系数，永远不到 1

        foreach (var kv in monsters)
        {
            int id = kv.Key;
            GameObject obj = kv.Value;
            if (dying.Contains(id)) continue;
            if(hpObj.TryGetValue(id, out GameObject hpobj))
            {
                hpObj[id].transform.LookAt(Camera.main.transform);
            }

            if (targetPos.TryGetValue(id, out Vector3 tp))
                if ((obj.transform.position - tp).sqrMagnitude < 0.035f * 0.035f)
                {
                    obj.transform.position = tp;
                }
                else obj.transform.position = Vector3.Lerp(obj.transform.position, tp, k);

            if (targetYaw.TryGetValue(id, out float yaw))
                obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, Quaternion.Euler(0, yaw, 0), k);

            float speed = 0;
            if (lastPos.TryGetValue(id, out Vector3 prev))
                speed = (obj.transform.position - prev).magnitude / Time.deltaTime;
            lastPos[id] = obj.transform.position;

            Animator animator = obj.GetComponent<Animator>();
            if (animator != null)
                animator.SetFloat("Speed", speed > 0.08f ? 1f : 0f);
        }
    }

    public void OnRecvMonster(int id, int type, Vector3 pos, float yaw, int hp, int state)
    {
        seeded.Add(id);

        if (hp <= 0)
        {
            MarkDead(id);
            return;
        }

        targetPos[id] = pos;
        targetYaw[id] = yaw;
        if(monsters.TryGetValue(id,out GameObject obj) && obj != null)
        {
            MonsterData monster = obj.GetComponent<MonsterData>();
            if (monster != null)
            {
                monster.hp = hp;
                hpImg[id].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1.5f * hp / 100);
            }
            return;
        }
        CreateMonster(id, type, pos, hp, state);
    }

    public void CreateMonster(int id,int type, Vector3 vector3,int hp,int state)
    {
        if (monsters.ContainsKey(id))
        {
            return;
        }
        print("CreatingMonster");
        GameObject monster = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Character/LanAI"), vector3, Quaternion.identity);
        monster.transform.SetParent(SocketMgr.Instance.WorldCanvasTrans,true);
        Auto auto = monster.GetComponent<Auto>();
        if (auto != null)
        {
            hpImg.Add(id,auto.hpImg);
            hpObj.Add(id,auto.hpbkImg);
            Destroy(auto);
        }
        monster.GetComponent<MonsterData>().monsterid = id;
        monster.GetComponent<MonsterData>().monsterType = "Normal";
        monster.GetComponent<MonsterData>().hp = hp;
        hpImg[id].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1.5f * hp / 100) ;
        monsters.Add(id, monster);

        //monsters[id] = vector3;

        //StartCoroutine(LerpPos(name));
        //StartCoroutine(LerpRot(name));
    }

    public void OnSnapShotEnd()
    {
        List<int> dead = new List<int>();
        foreach (int id in monsters.Keys)
            if(!seeded.Contains(id)) 
                dead.Add(id);
        foreach(int id in dead)
            MarkDead(id);
        seeded.Clear();
    }

    void MarkDead(int id)
    {
        if (dying.Contains(id)) return;
        if (!monsters.TryGetValue(id, out GameObject obj) || obj == null) return;
        dying.Add(id);
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null) animator.SetBool("Death", true);
        StartCoroutine(DestroyMonster(id, 3f));
    }

    IEnumerator DestroyMonster(int id, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (monsters.TryGetValue(id, out GameObject obj) && obj != null)
            Destroy(obj);
        monsters.Remove(id);
        targetPos.Remove(id);
        targetYaw.Remove(id);
        lastPos.Remove(id);
        hpImg.Remove(id);
        hpObj.Remove(id);
        dying.Remove(id);
    }
}
