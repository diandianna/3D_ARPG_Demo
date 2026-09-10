using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 挂角色根节点，两端常驻
public class SkillVFXEvent : MonoBehaviour
{
    // 动画 Event 里配：调用 SkillVFX("slash_hit")，参数是 key
    public void SkillVFX(string key)
    {
        VFXMgr.Instance.Play(key, transform.position, transform.rotation);
    }
    public void SkillVFX_F(string key)
    {
        VFXMgr.Instance.Play(key, transform.position+transform.forward*1f, transform.rotation);
    }
    public void SkillVFX_B(string key)
    {
        VFXMgr.Instance.Play(key, transform.position+transform.forward*-1f, transform.rotation);
    }
}
