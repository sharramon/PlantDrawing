using UnityEngine;
using System.Collections;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private bool m_isCreatedSingleton = false;
    private static T m_instance = null;
    public static T Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = (T)FindObjectOfType(typeof(T));
                if (m_instance == null)
                {
                    if (Application.isPlaying == true)
                    {
                        m_instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
                        m_instance.m_isCreatedSingleton = true;
                    }
                }
                if (m_instance != null)
                    m_instance.init();
            }
            return m_instance;
        }
    }
    protected virtual void init() { }

    public void getInstance() { }

    private void OnApplicationQuit()
    {
        if (m_isCreatedSingleton == true)
        {
            if (this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }
}