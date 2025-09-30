using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        return JsonUtility.FromJson<Wrapper<T>>($"{{\"array\":{json}}}").array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
