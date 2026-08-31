using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{



    public Button startButton;
    public Button setButton;
    public Button aboutButton;
    public Button exitButton;

    private void Start()
    {
        startButton.onClick.AddListener(() =>
        {

            UIMgr.Instance.ShowPanel<LoginPanel>();
            UIMgr.Instance.HidePanel<BeginPanel>();
        });
        setButton.onClick.AddListener(() => {
            //showPanel = false;
        });
        aboutButton.onClick.AddListener(() => {
            //showPanel = false;
        });
        exitButton.onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync("HomeScene").allowSceneActivation = true;

        }); 
    }

}
