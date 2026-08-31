using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class HomeCharacter : MonoBehaviour
{
    private Animator animator;
    public HomeCamera homeCamera;


    public float rotateSpeed;
    float horizontal;
    float vertical;

    Transform mainCamTrans;

    public bool isTurning = false;
    float targetRot = 0;
    float deltaRot = 0;
    // Start is called before the first frame update
    private void Awake()
    {
        animator=GetComponent<Animator>();
        homeCamera=GetComponent<HomeCamera>();
        mainCamTrans = Camera.main.transform;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        targetRot = horizontal == -1 ? -90+ mainCamTrans.eulerAngles.y : 
                    horizontal ==  1 ? 90 + mainCamTrans.eulerAngles.y : 
                    vertical   ==  1 ? 0 + mainCamTrans.eulerAngles.y  : 
                    vertical   == -1 ? 180 + mainCamTrans.eulerAngles.y : transform.eulerAngles.y;
        deltaRot = Mathf.DeltaAngle(transform.eulerAngles.y, targetRot);
        if(!isTurning)
        if (deltaRot >= -100 && deltaRot <= -80)
        {
            isTurning = true;
            animator.SetInteger("TurnStep", 1);
            animator.SetInteger("IdleStep", -1);
            animator.SetInteger("WalkStep", -1);
        }
        else if (deltaRot >= 80 && deltaRot <= 100)
        {
            isTurning = true;
            animator.SetInteger("TurnStep", 2);
            animator.SetInteger("IdleStep", -1);
            animator.SetInteger("WalkStep", -1);
        }
        else if (deltaRot >= -190 && deltaRot <= -170)
        {
            isTurning = true;
            animator.SetInteger("TurnStep", 3);
            animator.SetInteger("IdleStep", -1);
            animator.SetInteger("WalkStep", -1);
        }
        else if (deltaRot >= 170 && deltaRot <= 190)
        {
            isTurning = true;
            animator.SetInteger("TurnStep", 4);
            animator.SetInteger("IdleStep", -1);
            animator.SetInteger("WalkStep", -1);
        }
        else Rotate();

        if(!isTurning) 
        if (horizontal != 0 || vertical != 0) 
        {
            animator.SetInteger("WalkStep", 1);
            animator.SetInteger("IdleStep", -1);
            animator.SetInteger("TurnStep", -1);
        }
        else
        {
            animator.SetInteger("WalkStep", -1);
            animator.SetInteger("TurnStep", -1);
            animator.SetInteger("IdleStep", 1);
        }
    }

    //private void OnAnimatorMove()
    //{
    //    transform.position += animator.deltaPosition;
    //    transform.rotation *= animator.deltaRotation;
    //    transform.rotation  = new Quaternion(transform.rotation.x,
    //                                         Mathf.Round(transform.rotation.y / 90) * 90,
    //                                         transform.rotation.z,
    //                                         transform.rotation.w);
    //}

    public void Rotate()
    {
        // 死区：摇杆几乎在中心时不旋转，避免微小漂移导致角色抖动
        if (Mathf.Abs(vertical) < 0.1f && Mathf.Abs(horizontal) < 0.1f)
        {
            return;
        }

        // 获取相机的世界空间方向
        Vector3 cameraForword = mainCamTrans.forward;
        Vector3 cameraRight = mainCamTrans.right;

        // 剔除 Y 轴分量，投影到水平面（XZ 平面）
        // 防止摄像机俯仰角导致角色朝上/朝下旋转
        cameraForword.y = 0;
        cameraRight.y = 0;
        cameraForword.Normalize();
        cameraRight.Normalize();

        // 合成相机相对方向的移动向量
        // vertical   = W/S 或摇杆上下 → 沿相机前方
        // horizontal = A/D 或摇杆左右 → 沿相机右方
        Vector3 moveDir = cameraForword * vertical + cameraRight * horizontal;

        // 方向向量 → 四元数旋转 → Slerp 平滑过渡
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    public void EndTurning()
    {
        isTurning = false;
    }
}
