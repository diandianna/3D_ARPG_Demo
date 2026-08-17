using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RegisterPanel : BasePanel
{
    public InputField acountInput;
    public InputField passwordInput;
    public Button confirmBtn;

    public Button backBtn;
    void Start()
    {
        confirmBtn.onClick.AddListener(async () =>
        {
            await AcountMgr.Instance.RegisterAcount(new AcountData(acountInput.text, passwordInput.text));

        });

        backBtn.onClick.AddListener(() =>
        {
            UIMgr.Instance.HidePanel<RegisterPanel>();
            UIMgr.Instance.ShowPanel<LoginPanel>();
        });
    }

    public void FillRegisterInfo()
    {
        LoginPanel t = UIMgr.Instance.GetPanel<LoginPanel>() as LoginPanel;
        t.FillInputField(acountInput.text, passwordInput.text);
    }
}
