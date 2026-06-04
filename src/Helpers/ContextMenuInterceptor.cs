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
                            _baselineClsids.Add(clsid);
                            _deferredClsids.Add(clsid);
                            OnItemIntercepted?.Invoke(this, clsid);
                            // Block after 3s delay so app has time to verify its registration succeeded
                            _ = DelayedClsidBlock(clsid, token);
                        }
                        else if (_deferredClsids.Remove(clsid))
                        {
                            // Already handled by delayed task, just clean up
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
                            _ = DelayedVerbBlock(verbPath, token);
                        }
                        else if (_deferredVerbs.Remove(verbPath))
                        {
                            // Already handled by delayed task, just clean up
                        }
                    }
                    _baselineVerbs.IntersectWith(currentVerbs);
                }
                catch { }
            }
        }

        private async Task DelayedClsidBlock(Guid clsid, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);
                if (_deferredClsids.Contains(clsid))
                {
                    PackagedComHelper.SetBlockedClsid(
                        clsid,
                        PackagedComHelper.BlockedClsidType.CurrentUser,
                        blocked: true,
                        isPending: true);
                }
            }
            catch { }
        }

        private async Task DelayedVerbBlock(string verbPath, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);
                if (_deferredVerbs.Contains(verbPath))
                {
                    ShellMenuScanner.BlockVerb(verbPath, isPending: true);
                }
            }
            catch { }
        }
    }
}
