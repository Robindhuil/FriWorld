using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomDisplay : MonoBehaviour
{
    [SerializeField] private string nameRoom;
    private Canvas canvas;
    private RawImage qrImage;
    private TextMeshProUGUI department;
    private TextMeshProUGUI roomNameText;
    private TextMeshProUGUI originalName;
    private TextMeshProUGUI function;
    private TextMeshProUGUI profeList;
    private QRCodeGenerator qrCodeGenerator;

    void Start()
    {
        if (string.IsNullOrEmpty(nameRoom))
            nameRoom = gameObject.name;

        InitializeComponents();
        Invoke("FindAndDisplayRoom", 0.1f);
    }

    private void InitializeComponents()
    {
        canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[RoomDisplay] Canvas component not found!", this);
            return;
        }

        department = FindText("Department");
        roomNameText = FindText("RoomName");
        originalName = FindText("OriginalName");
        function = FindText("Function");
        profeList = FindText("ProfeList");
        qrImage = canvas.transform.Find("Background/QRImage")?.GetComponent<RawImage>();

        if (qrImage != null)
        {
            qrCodeGenerator = new QRCodeGenerator(qrImage);
        }
    }

    private TextMeshProUGUI FindText(string name)
    {
        var tf = canvas.transform.Find($"Background/{name}");
        if (tf == null)
        {
            Debug.LogWarning($"[RoomDisplay] Text '{name}' not found under Background!");
            return null;
        }

        return tf.GetComponent<TextMeshProUGUI>();
    }

    public void FindAndDisplayRoom()
    {
        //Debug.Log($"[RoomDisplay] Hľadám miestnosť pre: {nameRoom}");

        if (RoomManager.Instance == null)
        {
            Debug.LogError("[RoomDisplay] RoomManager instance is NULL!", this);
            return;
        }

        var allRoomsField = typeof(RoomManager).GetField("allRooms", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allRooms = allRoomsField?.GetValue(RoomManager.Instance) as List<RoomData>;

        if (allRooms == null)
        {
            Debug.LogError("[RoomDisplay] allRooms je null!", this);
            return;
        }

        Debug.Log($"[RoomDisplay] Počet načítaných miestností: {allRooms.Count}");

        foreach (var room in allRooms)
        {
            //Debug.Log($"[RoomDisplay] ROOM: {room.Name} – Dept: {room.Department}");
        }

        var foundRoom = allRooms.FirstOrDefault(r => r.Name == nameRoom);

        if (foundRoom == null)
        {
            Debug.LogError($"[RoomDisplay] Miestnosť '{nameRoom}' NEBOLA nájdená!");
            return;
        }

        UpdateRoomInfo(foundRoom);
    }


    public void UpdateRoomInfo(RoomData room)
    {
        if (room == null) return;

        SetTextIfNotNull(department, room.Department);
        SetTextIfNotNull(roomNameText, room.Name);
        SetTextIfNotNull(originalName, $"{room.OriginalCode} - pôvodné označenie");
        SetTextIfNotNull(function, room.Function);

        if (profeList != null)
        {
            profeList.text = FormatProfessorsList(room.Professors);
        }

        if (qrCodeGenerator != null && !string.IsNullOrEmpty(room.URL))
        {
            qrCodeGenerator.GenerateQRCode(room.URL);
        }
    }

    private void SetTextIfNotNull(TextMeshProUGUI textComponent, string text)
    {
        if (textComponent != null)
            textComponent.text = string.IsNullOrEmpty(text) ? "Nezadané" : text;
    }

    private string FormatProfessorsList(List<string> professors)
    {
        if (professors == null || professors.Count == 0)
            return "—";

        var formatted = professors.Select(prof =>
        {
            int index = prof.IndexOf('(');
            return index > 0
                ? $"{prof.Substring(0, index).Trim()}\n<size=80%>{prof.Substring(index).Trim()}</size>"
                : prof;
        });

        return $"\n{string.Join("\n", formatted)}";
    }

    private void OnValidate()
    {
        if (Application.isPlaying && RoomManager.Instance != null)
        {
            FindAndDisplayRoom();
        }
    }
}
