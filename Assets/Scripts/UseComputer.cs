using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UseComputer : MonoBehaviour
{
    Collider Collider;

    public Camera portalCam;
    public Camera mainCam;

    public Transform targetPos;
    AsyncOperation operation;
    Vector3 startPos;
    Vector3 endPos;
    float t=0;
    public bool isChanging = false;
    public bool isInside = false;
    public Material offMaterial;
    public Material onMaterial;

    public MeshRenderer ScreenRenderer;
    private void Awake()
    {
        Collider = GetComponent<Collider>();
        mainCam = Camera.main;
        Collider.isTrigger = true;
        portalCam.enabled = false;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isChanging)
        {
            portalCam.transform.position = Vector3.Lerp(startPos, endPos, t/1f);
            t += Time.deltaTime;
            if(Vector3.Distance(portalCam.transform.position,targetPos.position)<0.01f )
            {
                isChanging = false;
                operation.allowSceneActivation = true;
                operation = null;
            }
            return;
        }
        if (Input.GetKeyDown(KeyCode.E) && portalCam.enabled == false)
        {
            mainCam.enabled = false;
            portalCam.enabled = true;
            ScreenRenderer.material = onMaterial;
        }
        else if (Input.GetKeyDown(KeyCode.E) && portalCam.enabled == true)
        {
            isChanging = true;
            startPos = portalCam.transform.position;
            endPos = targetPos.position;
            t = 0;

            operation = SceneManager.LoadSceneAsync("BeginScene", LoadSceneMode.Additive);
            operation.allowSceneActivation = false;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && mainCam.enabled == false)
        {
            portalCam.enabled = false;
            mainCam.enabled = true;
            ScreenRenderer.material = offMaterial;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        isInside = true;
    }
    private void OnTriggerExit(Collider other)
    {
        isInside = false;
    }
}
