using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// One area and which build it belongs to. A missing platform means "not decided yet" — the
    /// appliers leave such an area exactly as it is and report it.
    /// </summary>
    public class RoomEntry
    {
        /// <summary>
        /// Full container name, e.g. "ra100_corridor_2". Deliberately NOT the stripped prefix:
        /// corridor 1 and corridor 2 are different places and decide for themselves.
        /// </summary>
        public string room;

        // Omitted from the file entirely when unset. A null written into the file would be
        // indistinguishable from "all", and those two need opposite behaviour — one leaves a
        // gate alone, the other removes it.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string platform;   // all | desktopOnly | webOnly

        [JsonIgnore]
        public bool IsDecided => platform != null;
    }

    /// <summary>What one Reconcile call added, and which entries it could no longer place.</summary>
    public class ReconcileResult
    {
        public readonly List<string> added = new List<string>();
        public readonly List<string> orphans = new List<string>();
    }

    public class RoomPlatforms
    {
        public const string All = "all";
        public const string DesktopOnly = "desktopOnly";
        public const string WebOnly = "webOnly";

        public List<RoomEntry> rooms = new List<RoomEntry>();

        [JsonIgnore]
        readonly Dictionary<string, RoomEntry> index = new Dictionary<string, RoomEntry>();

        public static bool IsValidPlatform(string platform)
            => platform == All || platform == DesktopOnly || platform == WebOnly;

        public static RoomPlatforms FromJson(string json)
        {
            var file = JsonConvert.DeserializeObject<RoomPlatforms>(json) ?? new RoomPlatforms();
            if (file.rooms == null) file.rooms = new List<RoomEntry>();
            file.Reindex();
            return file;
        }

        public static RoomPlatforms Load(string path)
            => File.Exists(path) ? FromJson(File.ReadAllText(path)) : new RoomPlatforms();

        /// <summary>
        /// Undecided areas float to the top, so a freshly synced area is the first thing in the
        /// file rather than something to scroll for. Once decided it settles into the
        /// alphabetical body, which keeps diffs readable — plain insertion order would leave the
        /// file permanently unsorted.
        /// </summary>
        public string ToJson()
        {
            rooms.Sort((a, b) =>
            {
                bool aDecided = a.IsDecided, bDecided = b.IsDecided;
                if (aDecided != bDecided) return aDecided ? 1 : -1;
                return string.CompareOrdinal(a.room, b.room);
            });
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public void Save(string path) => File.WriteAllText(path, ToJson());

        /// <summary>Exact name only — substring matching is the bug the type registry removed.</summary>
        public RoomEntry Find(string room)
        {
            if (string.IsNullOrEmpty(room)) return null;
            return index.TryGetValue(room, out var entry) ? entry : null;
        }

        /// <summary>The area's platform, or null when the area is unknown or undecided.</summary>
        public string PlatformOf(string room)
        {
            var entry = Find(room);
            return entry == null ? null : entry.platform;
        }

        /// <summary>
        /// Brings the file in line with the areas that actually exist. New areas get an
        /// undecided entry; entries whose container is gone are kept and reported. Existing
        /// decisions are never touched — re-deciding three hundred rows by hand is exactly what
        /// this file exists to prevent, and a container is briefly missing every time someone
        /// renames one in Blender.
        /// </summary>
        public ReconcileResult Reconcile(IReadOnlyList<string> areasInHierarchy)
        {
            var result = new ReconcileResult();
            var live = new HashSet<string>();

            if (areasInHierarchy != null)
            {
                foreach (var area in areasInHierarchy)
                {
                    if (string.IsNullOrEmpty(area)) continue;
                    live.Add(area);
                    if (index.ContainsKey(area)) continue;

                    var entry = new RoomEntry { room = area };
                    rooms.Add(entry);
                    index[area] = entry;
                    result.added.Add(area);
                }
            }

            foreach (var entry in rooms)
                if (entry != null && !string.IsNullOrEmpty(entry.room) && !live.Contains(entry.room))
                    result.orphans.Add(entry.room);

            result.added.Sort(string.CompareOrdinal);
            result.orphans.Sort(string.CompareOrdinal);
            return result;
        }

        void Reindex()
        {
            index.Clear();
            foreach (var entry in rooms)
                if (entry != null && !string.IsNullOrEmpty(entry.room))
                    index[entry.room] = entry;
        }
    }
}
