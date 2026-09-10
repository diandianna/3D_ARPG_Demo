using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoonScene : MonoBehaviour
{
    public Button Setting;

    public Transform CanvasTrans;
    public Transform WorldCanvasTrans;

    public Transform BornPos;
    private void Awake()
    {
        GameDataMgr.Instance.mainCharacter = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Character/Lu"), BornPos.transform.position, BornPos.transform.rotation);
        SocketMgr.Instance.WorldCanvasTrans = WorldCanvasTrans;
        GameDataMgr.Instance.mainCharacter.GetComponent<BaseMove>().playerCamera = Camera.main.gameObject.GetComponent<PlayerCamera>();
        Camera.main.gameObject.GetComponent<PlayerCamera>().target = GameDataMgr.Instance.mainCharacter.transform;

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
