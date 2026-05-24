using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RightClickManager.Helpers
{
    public class ContextMenuInterceptor
    {
        private static ContextMenuInterceptor? _instance;
        public static ContextMenuInterceptor Instance => _instance ??= new ContextMenuInterceptor();

        private readonly HashSet<Guid> _baselineClsids = new();
        private bool _isStarted;
        private CancellationTokenSource? _cts;

        // Custom event triggered when a new context menu is intercepted
        public event EventHandler<Guid>? OnItemIntercepted;

        public void Start()
        {
            if (_isStarted) return;
            _isStarted = true;
            _cts = new CancellationTokenSource();

            // 1. Establish the baseline
            UpdateBaseline();

            // 2. Start monitoring task
            Task.Run(async () => await MonitorLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _isStarted = false;
        }

        private void UpdateBaseline()
        {
            var packages = PackagedComHelper.GetAllComPackages();
            _baselineClsids.Clear();
            foreach (var package in packages)
            {
                foreach (var info in package.Clsids)
                {
                    _baselineClsids.Add(info.Clsid);
                }
            }
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Poll every 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(5), token);

                try
                {
                    var packages = PackagedComHelper.GetAllComPackages();
                    var currentClsids = new HashSet<Guid>();

                    foreach (var package in packages)
                    {
                        foreach (var info in package.Clsids)
                        {
                            currentClsids.Add(info.Clsid);

                            // Detect New CLSID
                            if (!_baselineClsids.Contains(info.Clsid))
                            {
                                // Block it immediately (Quarantine) - Marked as Pending Approval
                                PackagedComHelper.SetBlockedClsid(info.Clsid, PackagedComHelper.BlockedClsidType.CurrentUser, true, true);
                                
                                // Update baseline to prevent repeated blocking loops
                                _baselineClsids.Add(info.Clsid);

                                // Trigger event for UI notification
                                OnItemIntercepted?.Invoke(this, info.Clsid);
                            }
                        }
                    }

                    // Remove items from baseline that have been uninstalled
                    _baselineClsids.IntersectWith(currentClsids);
                }
                catch
                {
                    // Ignore transient registry reading errors like access denied
                }
            }
        }
    }
}
