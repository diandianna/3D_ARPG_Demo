using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            GameDataMgr.Instance.playerDeath = false;
            GameDataMgr.Instance.playerHurting = false;
            GameDataMgr.Instance.borned = false;
            GameDataMgr.Instance.playerData.hp = GameDataMgr.Instance.playerData.MaxHp;
            SocketMgr.Instance.Send(2, GameDataMgr.Instance.playerData.GetPlayerDataBytes());
            SceneLoaderMgr.Instance.targetSceneName = "Moon_scene";
            SceneManager.LoadScene("LoadingScene");
        }
    }
}
