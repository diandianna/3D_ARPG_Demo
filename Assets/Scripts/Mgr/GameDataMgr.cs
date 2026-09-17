using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public Vector3 spawnPos = Vector3.zero;   // 出生点：场景生成玩家时记录，死亡复活回这里
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

    /// <summary>玩家死亡统一入口。幂等：已在死亡流程里就忽略重复触发。</summary>
    public void OnPlayerDead()
    {
        if (playerDeath) return;
        playerDeath = true;
        if (mainCharacter != null)
        {
            Animator anim = mainCharacter.GetComponent<Animator>();
            if (anim != null) anim.SetBool("Death", true);
        }
        StartCoroutine(RespawnAfterDelay());
    }

    /// <summary>等死亡动画播完再复活（用 Realtime，防止暂停时卡住不复活）。</summary>
    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3f);
        RespawnPlayer();
    }

    /// <summary>复活：回出生点、回满血、解锁输入、退出死亡动画，并同步服务器。</summary>
    public void RespawnPlayer()
    {
        playerDeath = false;
        playerHurting = false;
        playerData.hp = playerData.MaxHp;

        // 满血同步服务器（客户端权威，死亡后服务器端 hp 也要拉回满血）
        if (SocketMgr.Instance != null && SocketMgr.Instance.Check())
            SocketMgr.Instance.Send(2, playerData.GetPlayerDataBytes());

        // 出生点在 GameScene：不在 GameScene（比如 Boss 的 Moon_scene）就先回城再复活
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            borned = false;   // 让新生成的玩家重新播出生动画
            SceneLoaderMgr.Instance.targetSceneName = "GameScene";
            SceneManager.LoadScene("LoadingScene");
            return;
        }

        if (mainCharacter == null) return;
        mainCharacter.transform.position = spawnPos;
        Animator anim = mainCharacter.GetComponent<Animator>();
        if (anim != null) anim.Rebind();   // 重置动画状态机，退出死亡回 Idle
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
