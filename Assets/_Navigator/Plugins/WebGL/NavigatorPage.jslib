// What the Navigator tells the page hosting it, as window events (see NavigatorPage.cs):
//   navigator:ready  {duration}   a flight is planned, this many seconds long
//   navigator:time   {time, playing}
//   navigator:ended               the flight reached the room
//   navigator:error  {code}       unknown-room, no-path, start-off-navmesh, not-set-up
mergeInto(LibraryManager.library, {
  NavigatorPageReady: function (duration) {
    window.dispatchEvent(new CustomEvent('navigator:ready', { detail: { duration: duration } }));
  },

  NavigatorPageTime: function (time, playing) {
    window.dispatchEvent(new CustomEvent('navigator:time', { detail: { time: time, playing: playing !== 0 } }));
  },

  NavigatorPageEnded: function () {
    window.dispatchEvent(new CustomEvent('navigator:ended'));
  },

  NavigatorPageError: function (code) {
    window.dispatchEvent(new CustomEvent('navigator:error', { detail: { code: UTF8ToString(code) } }));
  },
});
