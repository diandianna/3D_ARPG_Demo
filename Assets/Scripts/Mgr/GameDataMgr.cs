using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class GameDataMgr:MonoBehaviour
{
    private static GameDataMgr instance;
    public static GameDataMgr Instance=>instance;

    public string acountName;

    public PlayerData playerData;/* = new PlayerData();*/
    public PlayerData playerData2;
    public PlayerData playerData3;
    public PlayerData playerData4;

    public GameObject mainCharacter;
    public GameObject possessionCharacter;
    public bool isPossession = false;

    public bool playerDeath = false;
    public bool playerHurting = false;
    public List<GameObject> monsters = new List<GameObject>();
    public List<GameObject> possessedMonsters = new List<GameObject>();

    public bool playerIsAtking = false;
    public List<GameObject> monsterIsAtking = new List<GameObject>();

    public int soulIntegrity = 100;  // 灵魂完整度 0~100
    public float sanity = 100;         // 理智值 0~100

    public int nextMonsterid = 0;

    //玩家出场动画是否播放完毕
    public bool borned = false;
    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        monsters = new List<GameObject>();
        playerData = new PlayerData();
        //playerData.Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //判断当前对象是否是主角色 或者为 附身角色
    public bool IsCurrentControl(GameObject obj)
    {
        if(!isPossession && obj == mainCharacter)
        {
            return true;
        }
        else if (isPossession && obj == possessionCharacter)
        {
            return true;
        }
        return false;
    }

    public bool CanPossess() => soulIntegrity >= 10;

    public float PossessionTimeMult()
    {
        if (soulIntegrity >= 70) return 5f;
        if (soulIntegrity >= 30) return 10f;
        return 20f;
    }

    public void ClearAllMonster()
    {
        foreach(GameObject mons in new List<GameObject>(monsters))
        {
            Destroy(mons);
        }
        monsters.Clear();
    }

    public GameObject GetMonsterByID(int id)
    {
        foreach(var m in monsters)
            if (m != null && m.GetComponent<MonsterData>().monsterid == id) 
                return m;
        return null;
    }
}
