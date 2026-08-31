using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeCamera : MonoBehaviour
{
    public Transform target;           // 角色
    public Vector3 offset = new Vector3(0, 2, -5);  // 相机相对角色的偏移
    public float rotateSpeed = 150f;
    public float distance = 5f;

    private float angleY;   // 水平旋转角度

    private Camera camera;
    void Start()
    {
        camera = Camera.main;
        // 初始位置：角色正后方
        Vector3 pos = target.position + target.TransformDirection(offset);
        camera.transform.position = pos;
        camera.transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    private void Update()
    {
    }

    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X");
        angleY += mouseX * rotateSpeed * Time.deltaTime;

        // 从角色位置 + 角度计算相机位置
        Quaternion rot = Quaternion.Euler(0, angleY, 0);
        Vector3 desiredPos = target.position + rot * offset;
        Vector3 from = transform.position + Vector3.up * 1.5f;
        Vector3 dir = (desiredPos - from).normalized;
        float dist = (desiredPos - from).magnitude;

        float camY = desiredPos.y;
        Vector3 hDir = dir;
        hDir.y = 0;
        hDir.Normalize();
        if (Physics.SphereCast(from, 0.25f, dir, out RaycastHit hit, dist, 1 << LayerMask.NameToLayer("Wall")))
        {
            desiredPos = from + dir * (hit.distance - 0.1f);
            //desiredPos.y = camY;
        }
        camera.transform.position = Vector3.Lerp(camera.transform.position, desiredPos, Time.deltaTime * 2f);
        camera.transform.LookAt(target.position + Vector3.up * 1.5f);

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
