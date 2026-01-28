using UnityEngine;

public class PersistSingleton : MonoBehaviour
{
    private static PersistSingleton instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); 
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
