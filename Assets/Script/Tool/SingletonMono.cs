using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//MonoBehavior单例
public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    protected static T _instance = null;
    private static readonly object syslock = new object();

    public static T Instance
    {
        get
        { //线程安全锁
            if (_instance == null)
            {
                lock (syslock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();
                    }

                    return _instance;
                }
            }
            return _instance;
        }

        set { _instance = value; }
    }


    protected virtual void OnDestroy()
    {
        _instance = null;
    }
}