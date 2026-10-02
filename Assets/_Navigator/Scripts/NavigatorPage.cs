using System.Runtime.InteropServices;

namespace FriWorld.Navigator
{
    /// <summary>
    /// Tells the web page hosting the Navigator what the flight does, through
    /// <c>Plugins/WebGL/NavigatorPage.jslib</c>, as window events the page listens to:
    /// <c>navigator:ready</c> {duration}, <c>navigator:time</c> {time, playing},
    /// <c>navigator:ended</c> and <c>navigator:error</c> {code}. The page answers through
    /// <c>SendMessage("Navigator", …)</c>. Anywhere but a web build it does nothing.
    /// </summary>
    public static class NavigatorPage
    {
        /// <summary>Codes of <see cref="Error"/>, for the page to show its own message.</summary>
        public const string UnknownRoom = "unknown-room";
        public const string NoPath = "no-path";
        public const string StartOffNavMesh = "start-off-navmesh";
        public const string NotSetUp = "not-set-up";

        public static void Ready(float duration) => NavigatorPageReady(duration);

        public static void Progress(float time, bool playing) => NavigatorPageTime(time, playing ? 1 : 0);

        public static void Ended() => NavigatorPageEnded();

        public static void Error(string code) => NavigatorPageError(code);

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void NavigatorPageReady(float duration);
        [DllImport("__Internal")] private static extern void NavigatorPageTime(float time, int playing);
        [DllImport("__Internal")] private static extern void NavigatorPageEnded();
        [DllImport("__Internal")] private static extern void NavigatorPageError(string code);
#else
        private static void NavigatorPageReady(float duration) { }
        private static void NavigatorPageTime(float time, int playing) { }
        private static void NavigatorPageEnded() { }
        private static void NavigatorPageError(string code) { }
#endif
    }
}
