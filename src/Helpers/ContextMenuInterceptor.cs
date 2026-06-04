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
        private HashSet<Guid> _deferredClsids = new();
        private HashSet<string> _deferredVerbs = new(StringComparer.OrdinalIgnoreCase);
        private bool _isStarted;
        private CancellationTokenSource? _cts;

        public event EventHandler<Guid>? OnItemIntercepted;
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
                            // New CLSID: defer block to next cycle so the app verifies
                            // its registration succeeded first, breaking the retry loop.
                            _baselineClsids.Add(clsid);
                            _deferredClsids.Add(clsid);
                            OnItemIntercepted?.Invoke(this, clsid);
                        }
                        else if (_deferredClsids.Remove(clsid))
                        {
                            // Previously deferred: now safe to block (app already verified success)
                            PackagedComHelper.SetBlockedClsid(
                                clsid,
                                PackagedComHelper.BlockedClsidType.CurrentUser,
                                blocked: true,
                                isPending: true);
                        }
                    }
                    _baselineClsids.IntersectWith(currentClsids);

                    var currentVerbs = ShellMenuScanner.ScanAllVerbPaths();
                    foreach (var verbPath in currentVerbs)
                    {
                        if (!_baselineVerbs.Contains(verbPath))
                        {
                            _baselineVerbs.Add(verbPath);
                            _deferredVerbs.Add(verbPath);
                            OnVerbIntercepted?.Invoke(this, verbPath);
                        }
                        else if (_deferredVerbs.Remove(verbPath))
                        {
                            ShellMenuScanner.BlockVerb(verbPath, isPending: true);
                        }
                    }
                    _baselineVerbs.IntersectWith(currentVerbs);
                }
                catch { }
            }
        }
    }
}
