using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>What the table area shows: the loading indicator, the rows, the empty message or the error with Retry.</summary>
    public enum LeaderboardState
    {
        Loading,
        Content,
        Empty,
        Error
    }

    /// <summary>
    /// Filter state and paging of the leaderboard screen as pure C# (the view only draws it). Every filter change starts a
    /// new request id and clears the table; an answer carrying an old id is dropped, so a slow reply can never show
    /// rows of a filter that is no longer selected. Pages are merged by entry id (no duplicates); the server's rank is
    /// shown as it is (the client never derives a rank from a page number). Region "UK" is normalised to "GB".
    /// </summary>
    public sealed class LeaderboardPresenter
    {
        private readonly ILeaderboard _source;
        private readonly int _pageSize;
        private readonly List<LeaderboardRow> _rows = new List<LeaderboardRow>();
        private readonly HashSet<string> _ids = new HashSet<string>(StringComparer.Ordinal);
        private int _requestId;
        private int _nextOffset = -1;
        private bool _loadingMore;
        private bool _findMePending;
        private LeaderboardRow _own;

        public LeaderboardPresenter(ILeaderboard source, int pageSize)
        {
            _source = source;
            _pageSize = Math.Max(1, pageSize);
        }

        /// <summary>Anything the screen shows changed: redraw.</summary>
        public event Action Changed;

        /// <summary>The own row is loaded at this table index: scroll to it.</summary>
        public event Action<int> ScrollToRowRequested;

        public LeaderboardState State { get; private set; } = LeaderboardState.Loading;

        /// <summary>Country code filter; null = Global.</summary>
        public string CountryCode { get; private set; }

        public LeaderboardPeriod Period { get; private set; } = LeaderboardPeriod.AllTime;
        public RaceEventId EventId { get; private set; }
        public int RequestId => _requestId;
        public IReadOnlyList<LeaderboardRow> Rows => _rows;
        public LeaderboardRow OwnEntry => _own;
        public bool IsLoadingMore => _loadingMore;
        public bool CanLoadMore => State == LeaderboardState.Content && _nextOffset >= 0 && !_loadingMore;
        public bool CanFindMe => _own != null && State == LeaderboardState.Content;

        /// <summary>"UK" is the same country as "GB" (one backend code); empty means Global (null).</summary>
        public static string NormalizeCountry(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            string upper = code.Trim().ToUpperInvariant();
            return upper == "UK" ? "GB" : upper;
        }

        /// <summary>Opening the screen: Global, All Time, the race the player comes from.</summary>
        public void Open(RaceEventId eventId)
        {
            CountryCode = null;
            Period = LeaderboardPeriod.AllTime;
            EventId = eventId;
            Reload();
        }

        public void SetCountry(string code)
        {
            string normalized = NormalizeCountry(code);
            if (string.Equals(normalized, CountryCode, StringComparison.Ordinal)) return;
            CountryCode = normalized;
            Reload();
        }

        public void SetPeriod(LeaderboardPeriod period)
        {
            if (period == Period) return;
            Period = period;
            Reload();
        }

        public void ApplyEvent(RaceEventId eventId)
        {
            if (eventId == EventId) return;
            EventId = eventId;
            Reload();
        }

        /// <summary>Same filters, new request (the error screen's Retry; filters are not reset).</summary>
        public void Retry() => Reload();

        /// <summary>Next page when the list is scrolled near its end; nothing while a page is on its way or the list ended.</summary>
        public void LoadMore()
        {
            if (!CanLoadMore) return;
            _loadingMore = true;
            Changed?.Invoke();
            Request(_requestId, _nextOffset);
        }

        /// <summary>
        /// Brings the own row into view: scrolls to it when loaded, else loads pages until it arrives. Does nothing
        /// without an own entry (FIND ME is disabled then).
        /// </summary>
        public void FindMe()
        {
            if (!CanFindMe) return;
            int index = IndexOfOwn();
            if (index >= 0)
            {
                ScrollToRowRequested?.Invoke(index);
                return;
            }
            _findMePending = true;
            ContinueFindMe();
        }

        private void ContinueFindMe()
        {
            if (!_findMePending) return;
            int index = IndexOfOwn();
            if (index >= 0)
            {
                _findMePending = false;
                ScrollToRowRequested?.Invoke(index);
            }
            else if (CanLoadMore) LoadMore();
            else _findMePending = false;
        }

        private int IndexOfOwn()
        {
            if (_own == null) return -1;
            for (int i = 0; i < _rows.Count; i++)
                if (string.Equals(_rows[i].EntryId, _own.EntryId, StringComparison.Ordinal)) return i;
            return -1;
        }

        private void Reload()
        {
            int id = ++_requestId; // an answer to any older request is ignored from here on
            _rows.Clear();
            _ids.Clear();
            _own = null;
            _nextOffset = -1;
            _loadingMore = false;
            _findMePending = false;
            State = LeaderboardState.Loading;
            Changed?.Invoke();
            Request(id, 0);
        }

        private void Request(int id, int offset)
        {
            if (_source == null)
            {
                OnError(id);
                return;
            }
            if (EventId.IsEmpty)
            {
                OnPage(id, offset, null);
                return;
            }
            var query = new LeaderboardQuery(EventId, Period, CountryCode, offset, _pageSize);
            _source.Query(query, page => OnPage(id, offset, page), message => OnError(id));
        }

        private void OnPage(int id, int offset, LeaderboardPage page)
        {
            if (id != _requestId) return; // a reply for a filter that is no longer selected
            _loadingMore = false;

            if (page != null && page.Rows != null)
            {
                foreach (LeaderboardRow row in page.Rows)
                {
                    if (row == null || !_ids.Add(row.EntryId ?? string.Empty)) continue; // the same entry never shows twice
                    _rows.Add(row);
                }
                // A source that answers with the same cursor again would loop forever: treat it as the end.
                _nextOffset = page.NextOffset > offset ? page.NextOffset : -1;
                if (page.OwnEntry != null) _own = page.OwnEntry;
            }
            else
            {
                _nextOffset = -1;
            }

            State = _rows.Count > 0 ? LeaderboardState.Content : LeaderboardState.Empty;
            Changed?.Invoke();
            ContinueFindMe();
        }

        private void OnError(int id)
        {
            if (id != _requestId) return;
            _findMePending = false;
            if (_loadingMore)
            {
                _loadingMore = false; // the rows already shown stay; the next scroll tries again
                Changed?.Invoke();
                return;
            }
            State = LeaderboardState.Error;
            Changed?.Invoke();
        }
    }
}
