using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : BasePanel
{
    public InputField acountInput;
    public InputField passwordInput;
    public Button confirmBtn;
    public Button loginBtn;
    public Button backBtn;
    void Start()
    {
        confirmBtn.onClick.AddListener(async () =>
        {
           await AcountMgr.Instance.Check(new AcountData(acountInput.text, passwordInput.text));
        });
        loginBtn.onClick.AddListener(() =>
        {
            UIMgr.Instance.HidePanel<LoginPanel>();
            UIMgr.Instance.ShowPanel<RegisterPanel>();
        });
        backBtn.onClick.AddListener(() =>
        {
            UIMgr.Instance.HidePanel<LoginPanel>();
            UIMgr.Instance.ShowPanel<BeginPanel>();
        });
    }

    //注册成功后一键填充
    public void FillInputField(string acountName, string password)
    {
        acountInput.text = acountName;
        passwordInput.text = password;
    }


}
