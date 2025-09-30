using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class RoomManagerComplete : MonoBehaviour
{
    public static RoomManagerComplete Instance { get; private set; }

    private List<RoomData> allRooms = new List<RoomData>();
    private Dictionary<string, RoomJsonData> jsonRoomDataDict = new Dictionary<string, RoomJsonData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRoomDataFromJson();
            SaveProfessorNames();
            SaveRoomNamesBySector();
        }
        else
        {
            DestroyImmediate(gameObject);
            return;
        }
    }

    private void LoadRoomDataFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Rooms.json");
        if (!File.Exists(path))
        {
            Debug.LogError("[RoomManagerComplete] JSON súbor s miestnosťami neexistuje!");
            return;
        }

        string jsonText = File.ReadAllText(path);
        RoomJsonData[] jsonRooms = JsonHelper.FromJson<RoomJsonData>(jsonText);

        if (jsonRooms == null || jsonRooms.Length == 0)
        {
            Debug.LogError("[RoomManagerComplete] Načítaný JSON je prázdny alebo nesprávne formátovaný!");
            return;
        }

        foreach (var room in jsonRooms)
        {
            jsonRoomDataDict[room.name] = room;
        }

        Debug.Log($"[RoomManagerComplete] Načítaných {jsonRoomDataDict.Count} miestností z JSON.");
    }

    private void SaveProfessorNames()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "Professors.txt");
        HashSet<string> professorNames = new HashSet<string>();

        foreach (var room in jsonRoomDataDict.Values)
        {
            if (room.professors != null)
            {
                foreach (var professor in room.professors)
                {
                    professorNames.Add(professor);
                }
            }
        }

        File.WriteAllLines(filePath, professorNames);
        Debug.Log("[RoomManagerComplete] Zoznam profesorov bol uložený do Professors.txt");
    }

    private void SaveRoomNamesBySector()
    {
        Dictionary<string, List<string>> sectorRooms = new Dictionary<string, List<string>>
        {
            { "A", new List<string>() },
            { "B", new List<string>() },
            { "C", new List<string>() }
        };

        foreach (var room in jsonRoomDataDict.Keys)
        {
            if (room.StartsWith("RA"))
                sectorRooms["A"].Add(room);
            else if (room.StartsWith("RB"))
                sectorRooms["B"].Add(room);
            else if (room.StartsWith("RC"))
                sectorRooms["C"].Add(room);
        }

        foreach (var sector in sectorRooms.Keys)
        {
            string filePath = Path.Combine(Application.persistentDataPath, $"Rooms_Sector_{sector}.txt");
            File.WriteAllLines(filePath, sectorRooms[sector]);
            Debug.Log($"[RoomManagerComplete] Zoznam miestností pre sektor {sector} bol uložený do Rooms_Sector_{sector}.txt");
        }
    }
}
