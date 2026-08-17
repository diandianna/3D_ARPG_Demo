using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PoolMgr : MonoBehaviour
{
    private static PoolMgr instance;
    public static PoolMgr Instance => instance;

    //伤害飘字
    public List<Text> DamegeFontPool;
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
    
    }

    public T PopObj<T>(List<T> obj) where T : Component,new()
    { 
        foreach (T t in obj)
        {
            if(!t.gameObject.activeSelf)
                return t;
        }
        return PushObj<T>(obj);
    }

    public T PushObj<T>(List<T> obj) where T: Component,new()
    {
        obj.Add(new T());
        return PopObj<T>(obj);
    }
}
