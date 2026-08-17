using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    private static FrameRateManager instance;
    public static FrameRateManager Instance => instance;

    private void Awake()
    {
        instance = this;
        //默认无限制帧
        Application.targetFrameRate = -1;
    }

    public void SetFrameRate(int frameRate)
    {
        Application.targetFrameRate = frameRate;
    }
}
