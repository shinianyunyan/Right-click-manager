using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RightClickManager.Helpers
{
    public class ContextMenuInterceptor
    {
        private static ContextMenuInterceptor? _instance;
        public static ContextMenuInterceptor Instance => _instance ??= new ContextMenuInterceptor();

        private HashSet<Guid> _baselineClsids = new();
        private HashSet<string> _baselineVerbs = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<Guid> _notifiedClsids = new();
        private HashSet<string> _notifiedVerbs = new(StringComparer.OrdinalIgnoreCase);
        private bool _isStarted;
        private CancellationTokenSource? _cts;

        // Triggered when a new CLSID-based extension is intercepted
        public event EventHandler<Guid>? OnItemIntercepted;

        // Triggered when a new shell verb is intercepted
        public event EventHandler<string>? OnVerbIntercepted;

        public void Start()
        {
            if (_isStarted) return;
            _isStarted = true;
            _cts = new CancellationTokenSource();

            UpdateBaseline();
            Task.Run(async () => await MonitorLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _isStarted = false;
        }

        private void UpdateBaseline()
        {
            _baselineClsids = ShellMenuScanner.ScanAllExtensionClsids();
            _baselineVerbs = ShellMenuScanner.ScanAllVerbPaths();
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);

                try
                {
                    var currentClsids = ShellMenuScanner.ScanAllExtensionClsids();
                    foreach (var clsid in currentClsids)
                    {
                        if (!_baselineClsids.Contains(clsid))
                        {
                            PackagedComHelper.SetBlockedClsid(
                                clsid,
                                PackagedComHelper.BlockedClsidType.CurrentUser,
                                blocked: true,
                                isPending: true);
                            _baselineClsids.Add(clsid);
                            // Only notify for genuinely new items, never re-notify
                            if (_notifiedClsids.Add(clsid))
                                OnItemIntercepted?.Invoke(this, clsid);
                        }
                    }
                    _baselineClsids.IntersectWith(currentClsids);

                    var currentVerbs = ShellMenuScanner.ScanAllVerbPaths();
                    foreach (var verbPath in currentVerbs)
                    {
                        if (!_baselineVerbs.Contains(verbPath))
                        {
                            ShellMenuScanner.BlockVerb(verbPath, isPending: true);
                            _baselineVerbs.Add(verbPath);
                            if (_notifiedVerbs.Add(verbPath))
                                OnVerbIntercepted?.Invoke(this, verbPath);
                        }
                    }
                    _baselineVerbs.IntersectWith(currentVerbs);
                }
                catch { }
            }
        }
    }
}
