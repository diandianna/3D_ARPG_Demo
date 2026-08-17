using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
public class AcountMgr : MonoBehaviour
{
    private static AcountMgr instance;
    public static AcountMgr Instance => instance;

    enum MsgType
    {
        Login = 0,
        Register = 1,

    }

    List<AcountData> acountList = new List<AcountData>();
    void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async Task Check(AcountData acountData)
    {
        //foreach (AcountData a in acountList)
        //{
        //    if (a.acountName == acountData.acountName && a.password == acountData.password)
        //    {
        //        Debug.Log("登录成功");
        //        SceneManager.LoadScene("GameScene");
        //        return true;
        //    }
        //}
        //Debug.Log("登录失败");
        //return false;
        if (SocketMgr.Instance.Check() == false)
        {
            TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
            t.SetTipsText("tag: 无法连接服务器，请检查网络连接");
            return;
        }
        await SocketMgr.Instance.SendLogin_Register(acountData, (int)MsgType.Login);
    }

    public async Task RegisterAcount(AcountData acountData)
    {
        //if(acountList.Exists(a => a.acountName == acountData.acountName))
        //{
        //    Debug.Log("注册失败，账号已存在");
        //    return false;
        //}
        //else
        //{
        //    acountList.Add(acountData);
        //    Debug.Log("注册成功");
        //    return true;
        //}
        if (SocketMgr.Instance.Check() == false)
        {
            TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
            t.SetTipsText("tag: 无法连接服务器，请检查网络连接");
            return;
        }
        await SocketMgr.Instance.SendLogin_Register(acountData, (int)MsgType.Register);
    }
}
