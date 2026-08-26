using UnityEngine;

// 访客端 Boss 的动画事件空壳接收器。
// 访客端会销毁 BossAuto（由房主权威驱动，本地不需要 AI），
// 但 Boss 攻击动画仍带动画事件（MarkAttackingState 等），没有接收器就会刷
// "AnimationEvent ... has no receiver" 报错。挂这个空组件接住即可。
// 方法体留空：伤害判定 / 冲刺检测 / 转阶段都由房主权威处理，访客端只播动画。
public class BossAnimEventStub : MonoBehaviour
{
    public bool isAttacking = false;
    public void MarkAttackingState() { isAttacking = true; }
    public void MarkAttackingStateEnd() { isAttacking = false; }
    public void MarkDashStart() { }
    public void MarkDashEnd() { }
    public void ChangePhase() 
    {

    }
}
