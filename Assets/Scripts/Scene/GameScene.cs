using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameScene : MonoBehaviour
{
    public Button Setting;

    public Transform CanvasTrans;
    public Transform WorldCanvasTrans;
    private void Awake()
    {
        GameDataMgr.Instance.mainCharacter = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Character/Lu"),Vector3.zero,Quaternion.identity);
        PlayerCamera.SetTarget(GameDataMgr.Instance.mainCharacter.transform);
        SocketMgr.Instance.WorldCanvasTrans = WorldCanvasTrans;

        UIMgr.Instance.CanvasTrans = CanvasTrans;
        Setting.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            UIMgr.Instance.ShowPanel<SettingPanel>();
        });
    }

    private void Start()
    {
        BulletTimeMgr.Instance.PlayerTrans = GameDataMgr.Instance.mainCharacter.transform;
        AudioManager.Instance.PlayBGM(AudioManager.Instance.gameBgm);
    }
}
