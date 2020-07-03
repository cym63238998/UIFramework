
//C#单例
public class Singleton<T> where T : class, new()
{
    private static T _instance;
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
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }

        set { _instance = value; }
    }
}