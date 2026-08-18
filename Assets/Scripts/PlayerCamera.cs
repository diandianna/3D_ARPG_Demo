using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform target;           // 角色
    public Vector3 offset = new Vector3(0, 2, -5);  // 相机相对角色的偏移
    public float rotateSpeed = 150f;
    public float distance = 5f;

    private float angleY;   // 水平旋转角度

    public Camera camera;
    void Start()
    {
        camera = Camera.main;
        // 初始位置：角色正后方
        Vector3 pos = target.position + target.TransformDirection(offset);
        camera.transform.position = pos;
        camera.transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public Transform lockedTarget = null;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            if(lockedTarget == null)
            {
                foreach (GameObject t in GameDataMgr.Instance.monsters)
                {
                    lockedTarget = t.transform;
                    break;
                }
            }
            else
            {
                lockedTarget =null;
            }
        }
    }

    void LateUpdate()
    {
        if(BulletTimeMgr.Instance.IsBulletTime||BulletTimeMgr.Instance.sphereIsMoving) return;
        // 鼠标输入
        float mouseX = Input.GetAxis("Mouse X");
            angleY += mouseX * rotateSpeed * Time.deltaTime;
        if(target == null)
        {
            Debug.Log("当前没有目标");
            return;
        }


        // 从角色位置 + 角度计算相机位置
        Quaternion rot = Quaternion.Euler(0, angleY, 0);
        Vector3 desiredPos = target.position  + rot * offset;
        camera.transform.position = Vector3.Lerp(camera.transform.position, desiredPos, Time.deltaTime * 2f);
        if (lockedTarget == null)
        {
            camera.transform.LookAt(target.position + Vector3.up * 1.5f);
        }

        else
        {
            camera.transform.LookAt(lockedTarget.position);
        }
    }

    public static void SetTarget(Transform newTarget)
    {
        PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
        if (playerCamera != null)
        {
            playerCamera.target = newTarget;
        }
    }
}
