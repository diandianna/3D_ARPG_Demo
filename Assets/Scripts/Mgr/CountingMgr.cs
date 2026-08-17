using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CountingMgr : MonoBehaviour
{
    public static CountingMgr Instance { get; private set; }


    public Text atkCounting;
    private int atkCount = 0;
    private float atkCountTime = 0;
    void Start()
    {
        Instance = this;
    }

    
    void Update()
    {
        if (Time.time - atkCountTime > 3f)
        {
            atkCount = 0;
        }
        if (atkCount > 0)
        {
            atkCounting.gameObject.SetActive(true);
        }
        else atkCounting.gameObject.SetActive(false);
    }

    public void AtkCounting()
    {
        atkCounting.text = (++atkCount).ToString();
        atkCountTime = Time.time;
    }
}
