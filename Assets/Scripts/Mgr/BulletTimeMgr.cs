using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class BulletTimeMgr : MonoBehaviour
{
    public static BulletTimeMgr Instance { get; private set; }
    public bool IsBulletTime { get; private set; } = false;

    public CanvasGroup screenOverlay;  // Inspector 拖入
    public float overlayAlpha = 0.3f;  // 加深程度

    public Camera sphereCam;//灵体移动时摄像机
    public Camera mainCam;//玩家摄像机

    public float BulletSeconds;
    public float maxBulletSeconds = 15f;

    public Transform PlayerTrans;
    List<GameObject> monsters = new List<GameObject>();

    public GameObject LockImg;
    public int nowMonsterIndex = 0;

    //鼠标移动达到这个值时，切换锁定目标
    public int changeThreshold = 3;

    public GameObject currentSphere;
    public bool sphereIsMoving = false;
    public bool isIntercepted = false;

    GameObject sphere;
    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
        sphereCam.enabled = false;
        BulletSeconds = maxBulletSeconds;

        sphere = Resources.Load<GameObject>("Prefabs/Soul/Sphere");
    }

    float accumulator = 0;
    private void Update()
    {
        if (IsBulletTime)
        {
            BulletSeconds -= Time.unscaledDeltaTime * 1f;
        }
        else
        {
            BulletSeconds += Time.unscaledDeltaTime * 0.5f;
            if (BulletSeconds >= maxBulletSeconds)
            {
                BulletSeconds = maxBulletSeconds;
            }
        }
        if (Input.GetKey(KeyCode.E) && BulletSeconds > 0 )
        {
            if (GameDataMgr.Instance.isPossession&&!sphereIsMoving)
            {
                StartCoroutine(BackSphereMove());
                return;
            }
            monsters.Clear();
            List<GameObject> m = GameDataMgr.Instance.monsters;
            foreach (GameObject monster in m)
            {
                if (monster == gameObject) continue;
                if (Vector3.Distance(monster.gameObject.transform.position, PlayerTrans.position) <= 15f)
                {
                    if (GameDataMgr.Instance.possessedMonsters.Contains(monster)) continue;
                    monsters.Add(monster);
                }
            }
            if(monsters.Count > 0 &&!sphereIsMoving)
            {
                LockImg.SetActive(true);
            }
        }
        if (Input.GetKey(KeyCode.E) && BulletSeconds > 0 && !GameDataMgr.Instance.isPossession)
        {
            IsBulletTime = true;
            Time.timeScale = 0.1f;
            if (screenOverlay != null)
                screenOverlay.alpha = Mathf.Lerp(screenOverlay.alpha, overlayAlpha, 0.1f); // 淡入
        }
        if (Input.GetKeyUp(KeyCode.E) && BulletSeconds > 0)
        {

            if (sphereIsMoving||GameDataMgr.Instance.isPossession) return;
            ClearState();
        }
        if (IsBulletTime)
        {
           // print("子弹时间中");
            float mouseX = Input.GetAxisRaw("Mouse X");
           // print("mouseX: " + mouseX);
            accumulator += mouseX;// * Time.unscaledDeltaTime * 5f;
            if (monsters.Count > 0)
            {
                if(Input.GetKeyDown(KeyCode.Space)&&!sphereIsMoving)
                {
                    LockImg.SetActive(false);
                    //monsters[nowMonsterIndex].GetComponent<Monster>().TakeDamage(100);
                    StartCoroutine(SphereMove(nowMonsterIndex));
                    return;
                }

                if (accumulator > changeThreshold)
                {
                    nowMonsterIndex = (nowMonsterIndex + 1) % monsters.Count;
                    accumulator = 0;
                    //accumulator -= changeThreshold;
                }
                else if (accumulator < -changeThreshold)
                {
                    nowMonsterIndex =(nowMonsterIndex - 1 + monsters.Count) % monsters.Count;
                    accumulator = 0;
                    //accumulator += changeThreshold;
                }

                mainCam.transform.rotation = Quaternion.LookRotation(
                monsters[nowMonsterIndex].transform.position - mainCam.transform.position
                );
                //monsters[nowMonsterIndex]
                
            }
        }
        if (BulletSeconds <= 0.01f)
        {
            //if (GameDataMgr.Instance.isPossession)
            //{
            //    StartCoroutine(BackSphereMove());
            //    return;
            //}
            ClearState();
        }
    }

    public IEnumerator SphereMove(int MonsterIndex)
    {
        sphereIsMoving = true;
        GameObject obj;
        Vector3 startPos;
        Vector3 endPos;
        float t = 0;

        obj = GameObject.Instantiate(sphere, PlayerTrans.position + Vector3.up * 0.5f, Quaternion.identity);
        startPos = obj.transform.position;
        endPos = monsters[MonsterIndex].transform.position + Vector3.up * 0.5f;
        
        mainCam.enabled = false;
        sphereCam.transform.position = PlayerTrans.position + Vector3.up * 0.5f + Vector3.right + Vector3.forward *-1;
        sphereCam.enabled = true;
        sphereCam.transform.SetParent(obj.transform);
        currentSphere = obj;

        while (obj != null)
        {
            if (isIntercepted) break;
            t += Time.unscaledDeltaTime;
            obj.transform.position = Vector3.Lerp(startPos, endPos, t);
            if(t>=1)
            {
                break;
            }
            yield return null;
        }
        mainCam.enabled = true;
        sphereCam.enabled = false;
        sphereCam.transform.parent = null;
        currentSphere = null;
        if (isIntercepted)
        {
            isIntercepted = false;
            ClearState();
            sphereIsMoving = false;
            Destroy(obj);
            yield break;
        }

        GameDataMgr.Instance.possessionCharacter = monsters[MonsterIndex];
        GameDataMgr.Instance.isPossession = true;
        if (SyncMgr.Instance.isRoom && !GameDataMgr.Instance.possessedMonsters.Contains(monsters[MonsterIndex])&&!SyncMgr.Instance.isHost)
        {
            GameDataMgr.Instance.possessedMonsters.Add(monsters[MonsterIndex]);
            int id = -1;
            foreach(var m in MonsterSyncMgr.Instance.monsters)
            {
                if (m.Value == monsters[MonsterIndex])
                {
                    id=m.Key;
                    break;
                }
            }
            if (id < 0) yield break;
            byte[] buffer = new byte[12];
            BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(buffer, 0);
            BitConverter.GetBytes(id).CopyTo(buffer,4);
            BitConverter.GetBytes(1).CopyTo(buffer,8);
            SocketMgr.Instance.Send(101, buffer);
        }
        PlayerCamera.SetTarget(monsters[MonsterIndex].transform);
        AudioManager.Instance.PlayPossessIn();
        PossessionCosts.Instance.OnPossessStart();

        ClearState();

        sphereIsMoving = false;
        Destroy(obj);
    }

    public IEnumerator BackSphereMove()
    {
        sphereIsMoving = true;
        GameObject obj;
        Vector3 startPos;
        Vector3 endPos;
        float t = 0;

        obj = GameObject.Instantiate(sphere, GameDataMgr.Instance.possessionCharacter.transform.position + Vector3.up * 0.5f, Quaternion.identity);
        startPos = obj.transform.position;
        endPos = GameDataMgr.Instance.mainCharacter.transform.position + Vector3.up * 0.5f;
        
        mainCam.enabled = false;
        sphereCam.transform.position = PlayerTrans.position + Vector3.up * 0.5f + Vector3.right + Vector3.forward * -1;
        sphereCam.enabled = true;
        sphereCam.transform.SetParent(obj.transform);

        currentSphere = obj;
        while (obj != null)
        {
            if (isIntercepted) break;
            t += Time.unscaledDeltaTime;
            obj.transform.position = Vector3.Lerp(startPos, endPos, t);
            if (t >= 1)
            {
                break;
            }
            yield return null;
        }
        mainCam.enabled = true;
        sphereCam.enabled = false;
        sphereCam.transform.parent = null;
        currentSphere = null;
        if (isIntercepted)
        {
            isIntercepted = false;
            ClearState();
            sphereIsMoving = false;
            Destroy(obj);
            yield break;
        }

        if (SyncMgr.Instance.isRoom&&!SyncMgr.Instance.isHost)
        {
            if (GameDataMgr.Instance.possessedMonsters.Contains(GameDataMgr.Instance.possessionCharacter))
            {
                int id = -1;
                foreach (var m in MonsterSyncMgr.Instance.monsters)
                {
                    if (m.Value == GameDataMgr.Instance.possessionCharacter)
                    {
                        id = m.Key;
                        break;
                    }
                }
                if (id < 0) yield break;

                byte[] buffer = new byte[12];
                BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(buffer, 0);
                BitConverter.GetBytes(id).CopyTo(buffer, 4);
                BitConverter.GetBytes(0).CopyTo(buffer, 8);
                SocketMgr.Instance.Send(101, buffer);
                GameDataMgr.Instance.possessedMonsters.Remove(GameDataMgr.Instance.possessionCharacter);
            }
        }
        if(GameDataMgr.Instance.possessionCharacter != null)
        {
            Transform pc =GameDataMgr.Instance.possessionCharacter.transform;
            foreach(var m in GameDataMgr.Instance.monsters)
            {
                Auto a = m != null ? m.GetComponent<Auto>() : null;
                if (a != null && a.hateTarget == pc) 
                {
                    a.hateTarget = null;
                }
            }
        }

        GameDataMgr.Instance.possessionCharacter = null;
        GameDataMgr.Instance.isPossession = false;
        PlayerCamera.SetTarget(GameDataMgr.Instance.mainCharacter.transform);
        AudioManager.Instance.PlayPossessOut();
        PossessionCosts.Instance.OnPossessEnd();

        ClearState();

        sphereIsMoving = false;
        Destroy(obj);
    }


    public void RollbackPossession()
    {
        if (GameDataMgr.Instance.possessionCharacter != null)
            GameDataMgr.Instance.possessedMonsters.Remove(GameDataMgr.Instance.possessionCharacter);
        GameDataMgr.Instance.possessionCharacter.GetComponent<Auto>().hateTarget = null;
        if (GameDataMgr.Instance.possessionCharacter != null) 
        {
            Transform pc = GameDataMgr.Instance.possessionCharacter.transform;
            foreach(var m in GameDataMgr.Instance.monsters)
            {
                Auto a = m != null ? m.GetComponent<Auto>() : null;
                if (a != null && a.hateTarget == pc)
                {
                    a.hateTarget = null;
                }
            }
        }
        
        GameDataMgr.Instance.isPossession = false;
        PlayerCamera.SetTarget(GameDataMgr.Instance.mainCharacter.transform);
        AudioManager.Instance.PlayPossessOut();
        PossessionCosts.Instance.OnPossessEnd();   // 和 SphereMove 里的 OnPossessStart 配对
    }

    public void ClearState()
    {
        IsBulletTime = false;
        Time.timeScale = 1f;
        monsters.Clear();
        nowMonsterIndex = 0;
        if (screenOverlay != null)
            screenOverlay.alpha = 0f;  // 立刻消失
        LockImg.SetActive(false);
    }

    public IEnumerator Slow_Motion(float timescale,float waittime)
    {
        Time.timeScale = timescale;
        yield return new WaitForSecondsRealtime(waittime);
        Time.timeScale = 1;
    }

    public void OnIntercepted()
    {
        isIntercepted = true;
    }
}
