using UnityEngine;
using System.Collections.Generic;

public class RoomData
{
    public int ID { get; }
    public string Name { get; }
    public Transform RoomTransform { get; }
    public string Department { get; private set; }
    public string OriginalCode { get; private set; }
    public string Function { get; private set; }
    public List<string> Professors { get; private set; }
    public string URL { get; private set; }

    public RoomData(int id, string name, Transform transform)
    {
        ID = id;
        Name = name;
        RoomTransform = transform;
    }

    public void SetAdditionalData(string department, string originalCode, string function, List<string> professors, string url)
    {
        Department = department;
        OriginalCode = originalCode;
        Function = function;
        Professors = professors ?? new List<string>();
        URL = url;
    }
}
