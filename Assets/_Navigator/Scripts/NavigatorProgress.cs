namespace FriWorld.Navigator
{
    /// <summary>
    /// When the web page hears about the flight: the time a few times a second while it plays,
    /// at once when it pauses, resumes or jumps, and the end once each time the flight gets
    /// there. Only the bookkeeping — <see cref="NavigatorPage"/> does the telling.
    /// </summary>
    public sealed class NavigatorProgress
    {
        /// <summary>Real seconds between two reports of the time while the flight plays.</summary>
        public const float Interval = 0.2f;

        public struct Report
        {
            public bool time;
            public bool ended;
        }

        private float lastSent;
        private bool lastPlaying;
        private bool atEnd;
        private bool started;

        /// <summary>A new flight: it is reported from its start, and its end again.</summary>
        public void Reset()
        {
            started = false;
            atEnd = false;
        }

        /// <param name="now">Real time in seconds, unaffected by the playback speed.</param>
        /// <param name="jumped">The time was set since the last call, e.g. by Seek.</param>
        public Report Next(float now, float time, float duration, bool playing, bool jumped)
        {
            bool end = duration > 0f && time >= duration;
            var report = new Report { ended = end && !atEnd };
            atEnd = end;

            report.time = !started || jumped || report.ended || playing != lastPlaying
                          || playing && now - lastSent >= Interval;
            if (report.time)
            {
                started = true;
                lastSent = now;
                lastPlaying = playing;
            }

            return report;
        }
    }
}
