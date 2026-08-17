using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainPanel : BasePanel
{
    public Text money1;
    public Text money2;

    public Button setting;
    public Button start;
    public Button character;
    public Button bag;
    public Button shop;

    public Transform canvas;

    float t1;
    float t2;
    void Start()
    {
        UIMgr.Instance.CanvasTrans = canvas;

        setting.onClick.AddListener(() =>
        {
            UIMgr.Instance.ShowPanel<SettingPanel>();
        });
        start.onClick.AddListener(() =>
        {
            SceneLoaderMgr.Instance.targetSceneName = "GameScene";
            SceneManager.LoadScene("LoadingScene");
        });
        character.onClick.AddListener(() =>
        {
            UIMgr.Instance.ShowPanel<CharacterPanel>();
        });
        bag.onClick.AddListener(() =>
        {

        });
        shop.onClick.AddListener(() =>
        {
            UIMgr.Instance.ShowPanel<ShopPanel>();
        });

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
