// Editor tool: bakes each RoomDisplay's world-space Canvas into a compressed texture on a
// quad, then disables the Canvas — so during gameplay the ~169 signs cost only a cheap,
// SRP-batched, occlusion-cullable quad each (no per-frame canvas rebuild / TMP parse / GC).
// Re-run after changing Resources/Rooms.json. Menu: FriWorld > Room Signs.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public static class RoomSignBaker
{
    const string FOLDER = "Assets/!/BakedSigns";
    const int BAKE_LAYER = 31;      // temporary layer used only during the offscreen render
    const int TEX_HEIGHT = 384;     // portrait; width derived from the sign's aspect
    const string QUAD_NAME = "BakedSignQuad";

    [MenuItem("FriWorld/Room Signs/Bake All Signs")]
    public static void BakeAll() => Bake(AllSigns());

    [MenuItem("FriWorld/Room Signs/Bake Selected Signs")]
    public static void BakeSelected()
    {
        var set = new List<RoomDisplay>();
        foreach (var go in Selection.gameObjects)
        {
            var rd = go.GetComponentInParent<RoomDisplay>();
            if (rd != null && !set.Contains(rd)) set.Add(rd);
        }
        if (set.Count == 0) { Debug.LogWarning("[RoomSignBaker] Select one or more RoomDisplay objects first."); return; }
        Bake(set.ToArray());
    }

    [MenuItem("FriWorld/Room Signs/Restore Canvases (undo bake)")]
    public static void Restore()
    {
        int n = 0;
        foreach (var s in AllSigns())
        {
            var q = s.transform.Find(QUAD_NAME);
            if (q != null) UnityEngine.Object.DestroyImmediate(q.gameObject);
            var c = s.GetComponentInChildren<Canvas>(true);
            if (c != null) c.enabled = true;
            n++;
        }
        Debug.Log($"[RoomSignBaker] Restored {n} signs (quads removed, canvases re-enabled). Save the scene.");
    }

    static RoomDisplay[] AllSigns() =>
        UnityEngine.Object.FindObjectsByType<RoomDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    static void Bake(RoomDisplay[] signs)
    {
        if (!AssetDatabase.IsValidFolder(FOLDER)) AssetDatabase.CreateFolder("Assets/!", "BakedSigns");
        var byName = LoadJson();
        int ok = 0;
        try
        {
            for (int i = 0; i < signs.Length; i++)
            {
                var s = signs[i];
                string room = RoomNameOf(s);
                if (EditorUtility.DisplayCancelableProgressBar("Baking room signs",
                        $"{room}  ({i + 1}/{signs.Length})", (float)i / signs.Length))
                    break;
                try { if (BakeSign(s, room, byName)) ok++; }
                catch (Exception e) { Debug.LogError($"[RoomSignBaker] '{room}' failed: {e}"); }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RoomSignBaker] Baked {ok}/{signs.Length} signs → {FOLDER}. SAVE THE SCENE to keep the quads.");
    }

    static string RoomNameOf(RoomDisplay s)
    {
        string n = new SerializedObject(s).FindProperty("nameRoom").stringValue;
        return string.IsNullOrEmpty(n) ? s.name : n;
    }

    static Dictionary<string, RoomJsonData> LoadJson()
    {
        var map = new Dictionary<string, RoomJsonData>();
        var json = Resources.Load<TextAsset>("Rooms");
        if (json == null) { Debug.LogError("[RoomSignBaker] Resources/Rooms not found."); return map; }
        var rooms = JsonHelper.FromJson<RoomJsonData>(json.text);
        if (rooms != null) foreach (var r in rooms) map[r.name] = r;
        return map;
    }

    static bool BakeSign(RoomDisplay sign, string room, Dictionary<string, RoomJsonData> byName)
    {
        var canvas = sign.GetComponentInChildren<Canvas>(true);
        if (canvas == null) { Debug.LogWarning($"[RoomSignBaker] '{room}' has no Canvas."); return false; }
        var rt = canvas.GetComponent<RectTransform>();

        // 1) populate texts + QR from JSON
        if (byName.TryGetValue(room, out var jd))
        {
            SetText(canvas, "Background/Department", jd.department);
            SetText(canvas, "Background/RoomName", room);
            SetText(canvas, "Background/OriginalName", jd.original_code + " - pôvodné označenie");
            SetText(canvas, "Background/Function", jd.function);
            SetText(canvas, "Background/ProfeList",
                (jd.professors != null && jd.professors.Count > 0) ? string.Join("\n", jd.professors) : "—");
            AssignQR(canvas, jd.url, room);
        }
        else Debug.LogWarning($"[RoomSignBaker] '{room}' not found in JSON.");

        canvas.enabled = true;
        Canvas.ForceUpdateCanvases();

        // 2) render the canvas to a texture via an offscreen ortho camera
        float wW = rt.rect.width * canvas.transform.lossyScale.x;
        float wH = rt.rect.height * canvas.transform.lossyScale.y;
        int H = TEX_HEIGHT, W = Mathf.RoundToInt(H * (wW / wH));

        var layers = new Dictionary<GameObject, int>();
        foreach (var tr in canvas.GetComponentsInChildren<Transform>(true))
        { layers[tr.gameObject] = tr.gameObject.layer; tr.gameObject.layer = BAKE_LAYER; }

        Vector3 center = rt.TransformPoint(rt.rect.center);
        Vector3 fwd = canvas.transform.forward;

        var camGO = new GameObject("__SignBakeCam");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = wH * 0.5f; cam.aspect = wW / wH;
        cam.cullingMask = 1 << BAKE_LAYER;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.transform.position = center - fwd * 0.5f;                 // readable side
        cam.transform.rotation = Quaternion.LookRotation(fwd, canvas.transform.up);
        cam.nearClipPlane = 0.45f; cam.farClipPlane = 0.53f;         // thin slab -> ignore the back face

        var rtex = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rtex; cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rtex;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev;

        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(camGO);
        UnityEngine.Object.DestroyImmediate(rtex);
        foreach (var kv in layers) if (kv.Key != null) kv.Key.layer = kv.Value;

        // 3) save texture asset (compressed) + material
        string texPath = $"{FOLDER}/{room}.png";
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
        imp.textureType = TextureImporterType.Default;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Compressed;
        imp.SaveAndReimport();
        var texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        string matPath = $"{FOLDER}/{room}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", texAsset);
        EditorUtility.SetDirty(mat);

        // 4) create/update the quad and disable the canvas
        var qt = sign.transform.Find(QUAD_NAME);
        GameObject quad;
        if (qt != null) quad = qt.gameObject;
        else
        {
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = QUAD_NAME;
            var col = quad.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }
        quad.transform.SetParent(sign.transform, true);
        quad.transform.position = center;
        quad.transform.rotation = canvas.transform.rotation;
        Vector3 pl = sign.transform.lossyScale;
        quad.transform.localScale = new Vector3(wW / pl.x, wH / pl.y, 1f);
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;

        canvas.enabled = false;
        EditorUtility.SetDirty(sign);
        return true;
    }

    static void AssignQR(Canvas canvas, string url, string room)
    {
        if (string.IsNullOrEmpty(url)) return;
        var t = canvas.transform.Find("Background/QRImage");
        var ri = t != null ? t.GetComponent<UnityEngine.UI.RawImage>() : null;
        if (ri == null) return;
        try
        {
            string q = "https://api.qrserver.com/v1/create-qr-code/?size=256x256&data=" + Uri.EscapeDataString(url);
            byte[] png;
            using (var wc = new System.Net.WebClient()) png = wc.DownloadData(q);
            var qr = new Texture2D(2, 2);
            qr.LoadImage(png);
            ri.texture = qr;
        }
        catch (Exception e) { Debug.LogWarning($"[RoomSignBaker] QR download failed for '{room}': {e.Message}"); }
    }

    static void SetText(Canvas c, string path, string txt)
    {
        var t = c.transform.Find(path);
        if (t == null) return;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = string.IsNullOrEmpty(txt) ? "Nezadané" : txt;
    }
}
