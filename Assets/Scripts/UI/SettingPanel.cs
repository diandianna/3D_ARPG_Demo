using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public Dropdown resolutionDropdown;
    public Dropdown frameRateDropdown;
    public Dropdown vertical_SynchronizationDropdown;
    public Dropdown musicDropdown;
    public Dropdown soundDropdown;

    public Button back_mainBtn;
    public Button contineGame;

    void Start()
    {
        resolutionDropdown.onValueChanged.AddListener((OnResolutionChanged) =>
        {
            switch (OnResolutionChanged)
            {
                case 0:
                    Screen.SetResolution(1920, 1080, Screen.fullScreen);
                    break;
                case 1:
                    Screen.SetResolution(1280, 720, Screen.fullScreen);
                    break;
                case 2:
                    Screen.SetResolution(800, 600, Screen.fullScreen);
                    break;
                case 3:
                    Screen.SetResolution(2560,1440, Screen.fullScreen);
                    break;
                case 4:
                    Screen.SetResolution(3840,2160, Screen.fullScreen);
                    break;
            }
        });
        frameRateDropdown.onValueChanged.AddListener((OnFrameRateChanged) =>
        {
            QualitySettings.vSyncCount = 0;
            switch (OnFrameRateChanged)
            {
                case 0:
                    FrameRateManager.Instance.SetFrameRate(30);
                    break;
                case 1:
                    FrameRateManager.Instance.SetFrameRate(60);
                    break;
                case 2:
                    FrameRateManager.Instance.SetFrameRate(120);
                    break;
                case 3:
                    FrameRateManager.Instance.SetFrameRate(144);
                    break;
                case 4:
                    FrameRateManager.Instance.SetFrameRate(240);
                    break;
                case 5:
                    FrameRateManager.Instance.SetFrameRate(-1);
                    break;
            }
        });
        vertical_SynchronizationDropdown.onValueChanged.AddListener((OnVerticalSynchronizationChanged) =>
        {
            switch (OnVerticalSynchronizationChanged)
            {
                case 0:
                    QualitySettings.vSyncCount = 0;
                    break;
                case 1:
                    QualitySettings.vSyncCount = 1;
                    break;
            }
        });
        musicDropdown.onValueChanged.AddListener((OnMusicChanged) =>
        {

        });
        soundDropdown.onValueChanged.AddListener((OnSoundChanged) =>
        {

        });
        back_mainBtn.onClick.AddListener(() =>
        {
            if(SceneManager.GetActiveScene().name == "MainScene")
            {
                UIMgr.Instance.HidePanel<SettingPanel>();
                return;
            }
            SceneLoaderMgr.Instance.targetSceneName = "MainScene";
            SceneManager.LoadScene("LoadingScene");
            Time.timeScale = 1;
        });
        contineGame.onClick.AddListener(() =>
        {
            UIMgr.Instance.HidePanel<SettingPanel>();
            Time.timeScale = 1;
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIMgr.Instance.HidePanel<SettingPanel>();
        }
    }
}
