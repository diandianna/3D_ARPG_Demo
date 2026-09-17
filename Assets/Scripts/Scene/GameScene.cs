using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameScene : MonoBehaviour
{
    public Button Setting;
    public Button Shop;      // 商店入口（HUD 按钮，编辑器拖引用）
    public Button Bag;       // 背包入口
    public Button Growth;    // 养成入口
    public Button Task;    // 养成入口
    public Button Charac;

    public Text money1;
    public Text money2;

    float t1;
    float t2;

    public Transform CanvasTrans;
    public Transform WorldCanvasTrans;
    private void Awake()
    {
        GameDataMgr.Instance.mainCharacter = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Character/Lu"),Vector3.zero,Quaternion.identity);
        GameDataMgr.Instance.spawnPos = GameDataMgr.Instance.mainCharacter.transform.position;   // 记录出生点，死亡复活回这里
        PlayerCamera.SetTarget(GameDataMgr.Instance.mainCharacter.transform);
        SocketMgr.Instance.WorldCanvasTrans = WorldCanvasTrans;

        UIMgr.Instance.CanvasTrans = CanvasTrans;
        Setting.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            UIMgr.Instance.ShowPanel<SettingPanel>();
        });
        if (Shop != null) Shop.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            UIMgr.Instance.ShowPanel<ShopPanel>();
        });
        if (Bag != null) Bag.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            UIMgr.Instance.ShowPanel<BackpackPanel>();
        });
        if (Growth != null) Growth.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            UIMgr.Instance.ShowPanel<GrowthPanel>();
        });     
        if (Task != null) Task.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            UIMgr.Instance.ShowPanel<TaskPanel>();
        });     
        if (Charac != null) Charac.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            UIMgr.Instance.ShowPanel<CharacterPanel>();
        });

    }

    private void Start()
    {
        BulletTimeMgr.Instance.PlayerTrans = GameDataMgr.Instance.mainCharacter.transform;
        AudioManager.Instance.PlayBGM(AudioManager.Instance.gameBgm);

        t1 = Time.time;
        t2 = Time.time;
    }

    private void Update()
    {
        t2 = Time.time;
        if (t2 - t1 >= 0.1)
        {
            t1 = t2;
            money1.text = GameDataMgr.Instance.playerData.money1.ToString();
            money2.text = GameDataMgr.Instance.playerData.money2.ToString();
        }
    }
}
