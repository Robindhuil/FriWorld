using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Replaces copy-pasted mesh objects (e.g. scanned lamps) under the selected object(s)
/// with instances of a chosen prefab, aligned to the originals.
///
/// Align: Copy transform / Match mesh center / Match geometry (Kabsch-Horn best-fit over
/// all vertices, requires same source mesh). "Group under original parent" groups + renames
/// {parent}_{prefabBase}_{index}, reusing an existing root-level group and continuing its
/// numbering. "Diagnose" reports why geometry-align fails without changing anything.
/// Tools > Prefab Replacer.
/// </summary>
public class PrefabReplacerWindow : EditorWindow
{
    private enum AlignMode { CopyTransform, MatchMeshCenter, MatchGeometry }

    private GameObject prefab;
    private string keyword = "lamp";
    private AlignMode align = AlignMode.MatchGeometry;
    private float geomTolerance = 0.05f;
    private bool skipNonMatching = false;
    private Vector3 rotationOffset = Vector3.zero;
    private bool groupUnderParent = true;

    [MenuItem("Tools/Prefab Replacer")]
    private static void Open() => GetWindow<PrefabReplacerWindow>("Prefab Replacer");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Select parent object(s), set prefab + keyword, Replace. Undoable (Ctrl+Z).",
            MessageType.Info);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        keyword = EditorGUILayout.TextField("Name contains", keyword);
        align = (AlignMode)EditorGUILayout.EnumPopup(new GUIContent("Align"), align);

        if (align == AlignMode.MatchGeometry)
        {
            geomTolerance = EditorGUILayout.Slider(new GUIContent("Geometry tolerance"), geomTolerance, 0.01f, 0.30f);
            skipNonMatching = EditorGUILayout.Toggle(new GUIContent("Skip non-matching mesh",
                "Leave objects whose mesh doesn't match the prefab (a different variant) untouched, instead of replacing them anyway."),
                skipNonMatching);
        }

        rotationOffset = EditorGUILayout.Vector3Field(
            new GUIContent("Rotation offset (deg)", "Extra rotation applied to placed instances (fallback). Dial in e.g. (0,90,0) to fix a consistent 90° error."),
            rotationOffset);

        groupUnderParent = EditorGUILayout.Toggle(
            new GUIContent("Group under original parent",
                "Group instances under a parent named after the replaced object's parent and rename them " +
                "{parent}_{prefabBase}_{index}. Reuses an existing root-level group. Off = scene root, keep names."),
            groupUnderParent);

        EditorGUILayout.LabelField("Selected roots", Selection.gameObjects.Length.ToString());

        using (new EditorGUI.DisabledScope(prefab == null || Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Replace in selection"))
                Replace();
            if (GUILayout.Button("Diagnose (no changes)"))
                Diagnose();
        }
    }

    private void Replace()
    {
        string kw = keyword.ToLowerInvariant();

        var targets = new List<Transform>();
        foreach (var sel in Selection.gameObjects)
            foreach (var t in sel.GetComponentsInChildren<Transform>(true))
                if (t.name.ToLowerInvariant().Contains(kw) && t.GetComponentInChildren<Renderer>() != null)
                    targets.Add(t);

        if (targets.Count == 0)
        {
            Debug.LogWarning($"[PrefabReplacer] No objects containing '{keyword}' (with a Renderer) found in selection.");
            return;
        }

        string prefabBase = Regex.Replace(prefab.name, @"_\d+$", "");
        var groups = new Dictionary<string, Transform>();
        var counters = new Dictionary<string, int>();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName($"Replace {targets.Count} with {prefab.name}");

        int done = 0, geomFail = 0, skipped = 0;
        foreach (var orig in targets)
        {
            if (orig == null)
                continue;

            var origRend = orig.GetComponentInChildren<Renderer>();
            string parentName = orig.parent != null ? orig.parent.name : "Group";

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var instRend = inst.GetComponentInChildren<Renderer>();
            inst.transform.localScale = orig.lossyScale;

            bool aligned = false;
            if (align == AlignMode.MatchGeometry)
            {
                var iMF = inst.GetComponentInChildren<MeshFilter>();
                var oMF = orig.GetComponentInChildren<MeshFilter>();
                if (iMF != null && oMF != null &&
                    ComputeFit(iMF.transform, iMF.sharedMesh, oMF.transform, oMF.sharedMesh, out var R, out var t, out float rr) &&
                    rr <= geomTolerance)
                {
                    inst.transform.rotation = R * inst.transform.rotation;
                    inst.transform.position = R * inst.transform.position + t;
                    aligned = true;
                }
                else if (skipNonMatching)
                {
                    UnityEngine.Object.DestroyImmediate(inst); // different variant -> leave the original
                    skipped++;
                    continue;
                }
            }

            Undo.RegisterCreatedObjectUndo(inst, "Replace with prefab");

            if (!aligned)
            {
                if (align == AlignMode.MatchGeometry) geomFail++;

                if ((align == AlignMode.MatchMeshCenter || align == AlignMode.MatchGeometry) &&
                    origRend != null && instRend != null)
                {
                    Quaternion delta = origRend.transform.rotation * Quaternion.Inverse(instRend.transform.rotation);
                    inst.transform.rotation = delta * inst.transform.rotation;
                    inst.transform.rotation *= Quaternion.Euler(rotationOffset); // manual correction (fix 90° etc.)
                    inst.transform.position += origRend.bounds.center - instRend.bounds.center;
                }
                else
                {
                    inst.transform.rotation = orig.rotation * Quaternion.Euler(rotationOffset);
                    inst.transform.position = orig.position;
                }
            }

            if (groupUnderParent)
            {
                if (!groups.TryGetValue(parentName, out var grp) || grp == null)
                {
                    grp = FindRootObject(parentName);
                    if (grp == null)
                    {
                        var grpGO = new GameObject(parentName);
                        Undo.RegisterCreatedObjectUndo(grpGO, "Create group");
                        grp = grpGO.transform;
                    }
                    groups[parentName] = grp;
                    counters[parentName] = NextIndex(grp, $"{parentName}_{prefabBase}_");
                }
                int idx = counters[parentName]++;
                inst.transform.SetParent(grp, worldPositionStays: true);
                inst.name = $"{parentName}_{prefabBase}_{idx}";
            }
            else
            {
                inst.name = orig.name;
            }

            Undo.DestroyObjectImmediate(orig.gameObject);
            done++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        if (geomFail > 0)
            Debug.LogWarning($"[PrefabReplacer] {geomFail} object(s) fell back from geometry-align (fit above tolerance).");
        Debug.Log($"[PrefabReplacer] Replaced {done} object(s) with '{prefab.name}'" +
                  (groupUnderParent ? $" into {groups.Count} group(s)." : " at scene root."));
    }

    /// <summary>Reports (no changes) why geometry-align fails: vertex count, mirror, and best-fit error.</summary>
    private void Diagnose()
    {
        var pmf = prefab.GetComponentInChildren<MeshFilter>();
        int pc = (pmf != null && pmf.sharedMesh != null) ? pmf.sharedMesh.vertexCount : -1;
        Debug.Log($"[Diagnose] Prefab '{prefab.name}' mesh vertexCount = {pc}");

        string kw = keyword.ToLowerInvariant();
        int shown = 0;
        foreach (var sel in Selection.gameObjects)
            foreach (var tr in sel.GetComponentsInChildren<Transform>(true))
            {
                if (!tr.name.ToLowerInvariant().Contains(kw)) continue;
                var oMF = tr.GetComponentInChildren<MeshFilter>();
                if (oMF == null || oMF.sharedMesh == null) continue;
                int vc = oMF.sharedMesh.vertexCount;
                if (vc != pc) { if (shown++ < 20) Debug.Log($"  [MISMATCH] '{tr.name}' verts={vc} (prefab {pc})"); continue; }

                // temp instance to measure the best-fit rotation error, plus a reflection test
                var tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var iMF = tmp.GetComponentInChildren<MeshFilter>();
                string res = "no mesh";
                if (iMF != null && iMF.sharedMesh != null)
                {
                    tmp.transform.localScale = tr.lossyScale;
                    ComputeFit(iMF.transform, iMF.sharedMesh, oMF.transform, oMF.sharedMesh, out _, out _, out float rr);
                    tmp.transform.localScale = Vector3.Scale(tr.lossyScale, new Vector3(-1, 1, 1)); // reflected
                    ComputeFit(iMF.transform, iMF.sharedMesh, oMF.transform, oMF.sharedMesh, out _, out _, out float rm);
                    res = $"fit(rotation)={rr * 100f:F1}%  fit(mirror)={rm * 100f:F1}%";
                }
                UnityEngine.Object.DestroyImmediate(tmp);

                if (shown++ < 20)
                    Debug.Log($"  [MATCH] '{tr.name}' verts={vc} | {res}");
            }

        Debug.Log("[Diagnose] Read fit%: rotation small => should align (raise tolerance). " +
                  "mirror small but rotation big => baked mirror. both big => different vertex order.");
    }

    private static Transform FindRootObject(string name)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name)
                return root.transform;
        return null;
    }

    private static int NextIndex(Transform group, string prefix)
    {
        int max = 0;
        for (int i = 0; i < group.childCount; i++)
        {
            string n = group.GetChild(i).name;
            if (n.StartsWith(prefix) && int.TryParse(n.Substring(prefix.Length), out int num) && num > max)
                max = num;
        }
        return max + 1;
    }

    /// <summary>
    /// Kabsch/Horn best-fit rotation+translation mapping mesh mi (under ti) onto mesh mo
    /// (under to) by vertex index correspondence. Outputs the relative RMS residual (fraction
    /// of mesh size). Returns false if vertex counts differ or the fit is degenerate.
    /// </summary>
    private static bool ComputeFit(Transform ti, Mesh mi, Transform to, Mesh mo,
                                   out Quaternion R, out Vector3 t, out float relResidual)
    {
        R = Quaternion.identity; t = Vector3.zero; relResidual = float.MaxValue;
        if (mi == null || mo == null) return false;
        Vector3[] vi = mi.vertices, vo = mo.vertices;
        if (vi.Length != vo.Length || vi.Length < 3) return false;

        int n = vi.Length;
        int stride = Mathf.Max(1, n / 300);

        Vector3 cP = Vector3.zero, cQ = Vector3.zero;
        int m = 0;
        for (int i = 0; i < n; i += stride) { cP += ti.TransformPoint(vi[i]); cQ += to.TransformPoint(vo[i]); m++; }
        cP /= m; cQ /= m;

        double Sxx=0,Sxy=0,Sxz=0, Syx=0,Syy=0,Syz=0, Szx=0,Szy=0,Szz=0;
        for (int i = 0; i < n; i += stride)
        {
            Vector3 p = ti.TransformPoint(vi[i]) - cP;
            Vector3 q = to.TransformPoint(vo[i]) - cQ;
            Sxx+=p.x*q.x; Sxy+=p.x*q.y; Sxz+=p.x*q.z;
            Syx+=p.y*q.x; Syy+=p.y*q.y; Syz+=p.y*q.z;
            Szx+=p.z*q.x; Szy+=p.z*q.y; Szz+=p.z*q.z;
        }

        var N = new double[4, 4];
        N[0,0]=Sxx+Syy+Szz; N[0,1]=Syz-Szy;    N[0,2]=Szx-Sxz;    N[0,3]=Sxy-Syx;
        N[1,0]=N[0,1];      N[1,1]=Sxx-Syy-Szz; N[1,2]=Sxy+Syx;   N[1,3]=Szx+Sxz;
        N[2,0]=N[0,2];      N[2,1]=N[1,2];      N[2,2]=-Sxx+Syy-Szz; N[2,3]=Syz+Szy;
        N[3,0]=N[0,3];      N[3,1]=N[1,3];      N[3,2]=N[2,3];     N[3,3]=-Sxx-Syy+Szz;

        double c = 0;
        for (int i = 0; i < 4; i++) { double r = 0; for (int j = 0; j < 4; j++) r += Math.Abs(N[i,j]); if (r > c) c = r; }
        for (int i = 0; i < 4; i++) N[i,i] += c;

        double[] v = { 0.5, 0.5, 0.5, 0.5 };
        for (int it = 0; it < 200; it++)
        {
            double w0=N[0,0]*v[0]+N[0,1]*v[1]+N[0,2]*v[2]+N[0,3]*v[3];
            double w1=N[1,0]*v[0]+N[1,1]*v[1]+N[1,2]*v[2]+N[1,3]*v[3];
            double w2=N[2,0]*v[0]+N[2,1]*v[1]+N[2,2]*v[2]+N[2,3]*v[3];
            double w3=N[3,0]*v[0]+N[3,1]*v[1]+N[3,2]*v[2]+N[3,3]*v[3];
            double nrm = Math.Sqrt(w0*w0+w1*w1+w2*w2+w3*w3);
            if (nrm < 1e-15) return false;
            v[0]=w0/nrm; v[1]=w1/nrm; v[2]=w2/nrm; v[3]=w3/nrm;
        }

        R = new Quaternion((float)v[1], (float)v[2], (float)v[3], (float)v[0]);
        if (R.x == 0 && R.y == 0 && R.z == 0 && R.w == 0) return false;
        R = R.normalized;
        t = cQ - (R * cP);

        double se = 0;
        for (int i = 0; i < n; i += stride)
        {
            Vector3 d = (R * ti.TransformPoint(vi[i]) + t) - to.TransformPoint(vo[i]);
            se += d.sqrMagnitude;
        }
        float rms = Mathf.Sqrt((float)(se / m));
        float size = Mathf.Max(mo.bounds.size.magnitude, 1e-3f);
        relResidual = rms / size;
        return true;
    }
}
