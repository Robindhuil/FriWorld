using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    private List<RoomData> allRooms = new List<RoomData>();
    public List<RoomData> SectorA { get; private set; } = new List<RoomData>();
    public List<RoomData> SectorB { get; private set; } = new List<RoomData>();
    public List<RoomData> SectorC { get; private set; } = new List<RoomData>();

    private Dictionary<string, RoomJsonData> jsonRoomDataDict = new Dictionary<string, RoomJsonData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadRoomDataFromJson();
            LoadRooms();
        }
        else
        {
            DestroyImmediate(gameObject);
            return;
        }
    }

    private void LoadRoomDataFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Rooms");
        if (jsonFile == null)
        {
            Debug.LogError("[RoomManager] JSON súbor s miestnosťami neexistuje v Resources!");
            return;
        }

        RoomJsonData[] jsonRooms = JsonHelper.FromJson<RoomJsonData>(jsonFile.text);

        if (jsonRooms == null || jsonRooms.Length == 0)
        {
            Debug.LogError("[RoomManager] Načítaný JSON je prázdny alebo nesprávne formátovaný!");
            return;
        }

        foreach (var room in jsonRooms)
        {
            jsonRoomDataDict[room.name] = room;
        }

        Debug.Log($"Načítaných {jsonRoomDataDict.Count} miestností z JSON.");
    }

    private void LoadRooms()
    {
        GameObject[] roomObjects = GameObject.FindGameObjectsWithTag("Room");
        Dictionary<string, int> roomIdMap = new Dictionary<string, int>();

        int idCounter = 0;
        foreach (GameObject room in roomObjects)
        {
            string roomName = room.name;

            if (!roomIdMap.ContainsKey(roomName))
            {
                roomIdMap[roomName] = idCounter++;
            }

            RoomData roomData = new RoomData(roomIdMap[roomName], roomName, room.transform);
            allRooms.Add(roomData);
            AssignRoomToSector(roomName, roomData);

            if (jsonRoomDataDict.TryGetValue(roomName, out RoomJsonData jsonData))
            {
                roomData.SetAdditionalData(jsonData.department, jsonData.original_code, jsonData.function, jsonData.professors, jsonData.url);
                //Debug.Log($"[RoomManager] Dáta načítané pre miestnosť: {roomName}");
            }
            else
            {
                Debug.LogWarning($"[RoomManager] Miestnosť {roomName} nebola nájdená v JSON.");
            }
        }

        Debug.Log($"[RoomManager] Načítanie miestností dokončené. Celkový počet: {allRooms.Count}");
    }

    private void AssignRoomToSector(string roomName, RoomData roomData)
    {
        if (roomName.StartsWith("RA"))
            SectorA.Add(roomData);
        else if (roomName.StartsWith("RB"))
            SectorB.Add(roomData);
        else if (roomName.StartsWith("RC"))
            SectorC.Add(roomData);
        else
            Debug.LogWarning($"[RoomManager] Miestnosť {roomName} nebola priradená do žiadneho sektora.");
    }
}