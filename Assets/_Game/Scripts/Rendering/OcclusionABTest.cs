using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editor-only A/B harness for the baked occlusion data.
///
/// Flips <see cref="Camera.useOcclusionCulling"/> on a fixed period and reports rendering
/// stats separately for each state, so the value of the bake is measured on the same walked
/// route instead of guessed. Alternating on a short period means both states see roughly the
/// same mix of corridors and rooms — comparing two separate play sessions would not.
///
/// Not attached to anything by default. Drop it on the player camera when you need the
/// numbers again — e.g. after changing occluder flags or the bake's smallestOccluder — then
/// take it off. Give it a couple of minutes of walking: the early windows are dominated by
/// scene load and read far off.
///
/// Stats come from UnityEditor.UnityStats, which only exists in the editor; in a player build
/// this component compiles to nothing and does nothing.
/// </summary>
[RequireComponent(typeof(Camera))]
public class OcclusionABTest : MonoBehaviour
{
    [Tooltip("How long to stay in each state before flipping.")]
    [SerializeField] private float switchInterval = 2f;

    [Tooltip("How often to log the running comparison.")]
    [SerializeField] private float logInterval = 12f;

#if UNITY_EDITOR
    private struct Sample
    {
        public int frames;
        public long drawCalls, tris, setPass;
        public double ms;

        public void Add(int dc, int t, int sp, double frameMs)
        {
            frames++; drawCalls += dc; tris += t; setPass += sp; ms += frameMs;
        }

        public string Format(string label)
        {
            if (frames == 0) return label + ": no samples";
            return string.Format("{0}: {1,7:F0} drawCalls  {2,9:F0} tris  {3,6:F0} setPass  {4,6:F2} ms/frame  ({5} frames)",
                label, (double)drawCalls / frames, (double)tris / frames,
                (double)setPass / frames, ms / frames, frames);
        }
    }

    private Camera cam;
    private Sample on, off;
    private bool occlusionOn = true;
    private float nextSwitch, nextLog;
    private int settleFrames;

    private void Start()
    {
        cam = GetComponent<Camera>();
        occlusionOn = cam.useOcclusionCulling;
        nextSwitch = Time.unscaledTime + switchInterval;
        nextLog = Time.unscaledTime + logInterval;
        StartCoroutine(SampleAtEndOfFrame());
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextSwitch)
        {
            nextSwitch = Time.unscaledTime + switchInterval;
            occlusionOn = !occlusionOn;
            cam.useOcclusionCulling = occlusionOn;
            // The first frames after a flip still carry the old visibility set; dropping them
            // keeps a frame from being credited to the wrong state.
            settleFrames = 2;
        }

        if (Time.unscaledTime >= nextLog)
        {
            nextLog = Time.unscaledTime + logInterval;
            Report();
        }
    }

    private IEnumerator SampleAtEndOfFrame()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (true)
        {
            yield return endOfFrame; // UnityStats now holds this frame's numbers
            if (settleFrames > 0) { settleFrames--; continue; }

            double frameMs = Time.unscaledDeltaTime * 1000.0;
            if (occlusionOn) on.Add(UnityStats.drawCalls, UnityStats.triangles, UnityStats.setPassCalls, frameMs);
            else            off.Add(UnityStats.drawCalls, UnityStats.triangles, UnityStats.setPassCalls, frameMs);
        }
    }

    private void Report()
    {
        if (on.frames == 0 || off.frames == 0)
        {
            Debug.Log("[OcclusionAB] still collecting…");
            return;
        }

        double drawCut = 100.0 * (1.0 - ((double)on.drawCalls / on.frames) / ((double)off.drawCalls / off.frames));
        double triCut  = 100.0 * (1.0 - ((double)on.tris      / on.frames) / ((double)off.tris      / off.frames));
        double msCut   = 100.0 * (1.0 - (on.ms / on.frames) / (off.ms / off.frames));

        Debug.Log("[OcclusionAB] cumulative\n"
                + "  " + off.Format("occlusion OFF") + "\n"
                + "  " + on.Format("occlusion ON ") + "\n"
                + string.Format("  Umbra removes: {0:F1}% of draw calls, {1:F1}% of triangles, {2:F1}% of frame time",
                    drawCut, triCut, msCut));
    }

    private void OnDisable()
    {
        if (cam != null) cam.useOcclusionCulling = true; // never leave it switched off
    }
#endif
}
