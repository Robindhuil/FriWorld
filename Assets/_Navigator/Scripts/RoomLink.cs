using System;

namespace FriWorld.Navigator
{
    /// <summary>
    /// The room a link opens: the <c>room</c> query parameter of the page URL, as in
    /// <c>…/index.html?room=RA101</c>. The web build reads it at start, so a plain link starts the
    /// flight with no script on the page.
    /// </summary>
    public static class RoomLink
    {
        public const string Parameter = "room";

        public static bool TryGetCode(string url, out string code)
        {
            code = null;
            if (string.IsNullOrEmpty(url))
                return false;

            int start = url.IndexOf('?');
            if (start < 0)
                return false;
            int end = url.IndexOf('#', start);
            string query = end < 0 ? url.Substring(start + 1) : url.Substring(start + 1, end - start - 1);

            foreach (string pair in query.Split('&'))
            {
                int equals = pair.IndexOf('=');
                string key = equals < 0 ? pair : pair.Substring(0, equals);
                if (!string.Equals(key, Parameter, StringComparison.OrdinalIgnoreCase))
                    continue;

                // '+' is a space in a query string; UnescapeDataString leaves it alone.
                string value = equals < 0 ? string.Empty : Uri.UnescapeDataString(pair.Substring(equals + 1).Replace('+', ' '));
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                code = value.Trim();
                return true;
            }

            return false;
        }
    }
}
