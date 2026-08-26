using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class SyncMgr : MonoBehaviour
{
    public Button test;


    public static SyncMgr Instance { get; private set; }


    //房间号
    public int roomId = 0;
    //是否为房主
    public bool isHost = false;
    //是否加入成功
    public bool isRoom = false;

    private Coroutine syncCoroutine;


    public Dictionary<string, GameObject> visitors = new Dictionary<string, GameObject>();
    Dictionary<string, Vector3> targetPos = new Dictionary<string, Vector3>();
    Dictionary<string, float> targetYaw = new Dictionary<string, float>();
    Dictionary<string, Vector3> lastPos = new Dictionary<string, Vector3>();

    Dictionary<string, PlayerData> visitorsData = new Dictionary<string, PlayerData>();



    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        test.onClick.AddListener(() =>
        {
            SocketMgr.Instance.Send(99, BitConverter.GetBytes(1000));
        });
    }

    // Update is called once per frame
    void Update()
    {
        float k = 1f - Mathf.Exp(-10f * Time.deltaTime); // 帧率无关的平滑系数，永远不到 1

        foreach(var kv in visitors)
        {
            string name = kv.Key;
            GameObject obj = kv.Value;

            if (targetPos.TryGetValue(name, out Vector3 tp))
                if ((obj.transform.position - tp).sqrMagnitude < 0.035f * 0.035f)
                {
                    obj.transform.position = tp;
                }
                else obj.transform.position = Vector3.Lerp(obj.transform.position, tp, k);

            if(targetYaw.TryGetValue(name,out float yaw))
                obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, Quaternion.Euler(0, yaw, 0), k);

            float speed = 0;
            if(lastPos.TryGetValue(name,out Vector3 prev))
                speed = (obj.transform.position - prev).magnitude / Time.deltaTime;
            lastPos[name] = obj.transform.position;
            
            Animator animator = obj.GetComponent<Animator>();
            if (animator != null)
                animator.SetFloat("Speed", speed > 0.08f ? 1f : 0f);
        }
    }

    public void CreateVisitor(string name ,Vector3 vector3)
    {
        if(visitors.ContainsKey(name))
        {
            return;
        }
        GameObject player = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Character/Lu"), vector3, Quaternion.identity);
       // print("Creating");
       visitors.Add(name ,player);
        lastPos[name] = vector3;
        //StartCoroutine(LerpPos(name));
        //StartCoroutine(LerpRot(name));
    }

    public void StartSyncLoop(Socket socket)
    {
        if(syncCoroutine != null)
        {
            return ;
        }
        isRoom = true;
        syncCoroutine = StartCoroutine(SyncLoop(socket));
    }
    public void OnRecvPos(string name,Vector3 p)
    {
        targetPos[name] = p;
    }
    public void OnRecvRot(string name,float r)
    {
        targetYaw[name] = r;
    }
    public IEnumerator SyncLoop(Socket socket)
    {
        while (isRoom && socket != null && socket.Connected) 
        {
            SendMyState(socket);
            yield return new WaitForSecondsRealtime(0.1f);
        }
        syncCoroutine = null;
    }

    public void SendMyState(Socket socket)
    {
        if (socket == null || !socket.Connected) return;
        byte[] response = new byte[4 + 4 + 4 + 12 + 4];
        BitConverter.GetBytes(4 + 4 + 4 + 12 + 4).CopyTo(response, 0);//消息长度
        BitConverter.GetBytes(98).CopyTo(response, 4);//消息类型
        BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(response, 8);//房间号
                                                                           
        BitConverter.GetBytes(GameDataMgr.Instance.mainCharacter.transform.position.x).CopyTo(response, 12);//位置
        BitConverter.GetBytes(GameDataMgr.Instance.mainCharacter.transform.position.y).CopyTo(response, 16);
        BitConverter.GetBytes(GameDataMgr.Instance.mainCharacter.transform.position.z).CopyTo(response, 20);
        BitConverter.GetBytes(GameDataMgr.Instance.mainCharacter.transform.eulerAngles.y).CopyTo(response, 24);//旋转
        socket.Send(response);

        if (isHost)
        {
            
            List<GameObject> monsters = GameDataMgr.Instance.monsters;
            response = new byte[4 + 4 + 4 + 4 + 32 * monsters.Count];
            BitConverter.GetBytes(4 + 4 + 4 + 4 + 32 * monsters.Count).CopyTo(response, 0);//总长度
            BitConverter.GetBytes(100).CopyTo(response, 4);//类型
            BitConverter.GetBytes(roomId).CopyTo(response, 8);//房间号
            BitConverter.GetBytes(monsters.Count).CopyTo(response, 12);//怪物数量
            int index = 16;
            foreach(GameObject monsobj in monsters)
            {
                MonsterData monsterData = monsobj.GetComponent<MonsterData>();
                BitConverter.GetBytes(monsterData.monsterid).CopyTo(response, index);index += 4;//怪物id
                BitConverter.GetBytes(monsobj.GetComponent<MonsterData>().monsterTypeid).CopyTo(response, index);index += 4;//怪物类型，暂时写死
                BitConverter.GetBytes(monsterData.transform.position.x).CopyTo(response, index);index += 4;//位置x
                BitConverter.GetBytes(monsterData.transform.position.y).CopyTo(response, index);index += 4;//y
                BitConverter.GetBytes(monsterData.transform.position.z).CopyTo(response, index);index += 4;//z
                BitConverter.GetBytes(monsterData.transform.eulerAngles.y).CopyTo(response, index);index += 4;//旋转
                BitConverter.GetBytes(monsterData.hp).CopyTo(response, index);index += 4;//怪物血量
                BitConverter.GetBytes(0).CopyTo(response, index);index += 4;//怪物状态，0正常，1受击，暂时写死
            }
            socket.Send(response);
        }
    }

    //public IEnumerator LerpPos(string acountName)
    //{
    //    float k = 1f - Mathf.Exp(-10f * Time.deltaTime); // 帧率无关的平滑系数，永远不到 1
    //    GameObject visitor = visitors[acountName];
    //    Animator animator = visitor.GetComponent<Animator>();
    //    float duration = 0.1f;
    //    Vector3 startPos = visitor.transform.position;
    //    float t = duration;
    //    while (true)
    //    {
    //        //t += Time.deltaTime;
    //        //p = Mathf.Clamp01(t / duration);
    //        visitor.transform.position = Vector3.Lerp(startPos, targetPos[acountName], k);
    //        animator.SetFloat("Speed", ((targetPos[acountName]-visitor.transform.position).magnitude / Time.deltaTime)<0.05?0f:1f);
    //        yield return null;
    //    }
    //}
    //public IEnumerator LerpRot(string acountName)
    //{
    //    GameObject visitor = visitors[acountName];
    //    float duration = 0.1f;
    //    float t = duration;
    //    float p;
    //    Quaternion startRot = visitor.transform.rotation;
    //    while (true)
    //    {
    //        t += Time.deltaTime;
    //        p = Mathf.Clamp01(t / duration);
    //        visitor.transform.rotation = Quaternion.Slerp(startRot, Quaternion.Euler(0,targetYaw[acountName],0), p);
    //        yield return null;
    //    }
    //}
}
