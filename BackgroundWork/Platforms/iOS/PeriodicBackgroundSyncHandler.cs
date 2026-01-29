using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using BackgroundTasks;
using CoreFoundation;
using Foundation;

namespace BackgroundWork;

internal class PeriodicBackgroundSyncHandler
{
    public const string TaskIdentifier = "com.mycompany.backgroundwork.background-sync-test";


    public PeriodicBackgroundSyncHandler()
    {
    }

    public void SchedulePeriodicBackgroundSync()
    {
        // Cancel existing
        BGTaskScheduler.Shared.Cancel(TaskIdentifier);

        // Create BGProcessingTaskRequest
        using var request = new BGProcessingTaskRequest(TaskIdentifier);
        request.RequiresNetworkConnectivity = true;
        request.RequiresExternalPower = false;
        request.EarliestBeginDate = NSDate.Now.AddSeconds(1 * 60);

        // Submit request
        BGTaskScheduler.Shared.Submit(request, out var error);

        if (error != null)
        {
            App.NetLog(TraceLevel.Error, Environment.CurrentManagedThreadId,
                $"Failed to schedule periodic sync: NsError: Domain = {error.Domain} - Code = {error.Code}");
            error.Dispose();
        }
        else
        {
            App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId,
                "Periodic background sync scheduled (iOS will decide when to run)");
        }
    }

    public void CancelPeriodicBackgroundSync()
    {
        BGTaskScheduler.Shared.Cancel(TaskIdentifier);
        App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId, "Periodic background sync cancelled");
    }

    /// <summary>
    /// Called by iOS when the background task executes.
    /// This method is registered in AppDelegate.
    /// </summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "fine")]
    public void HandleBackgroundTask(BGTask task)
    {
        App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId, "app - HandleBackgroundTask called");

        // Schedule next execution BEFORE starting work (method checks feature flag internally)
        SchedulePeriodicBackgroundSync();

#pragma warning disable CA2000
        // Disposed when job is done
        CancellationTokenSource cts = new CancellationTokenSource();
#pragma warning restore CA2000

        // Set expiration handler - iOS gives limited time for background tasks
        task.ExpirationHandler = () =>
        {
            App.NetLog(TraceLevel.Warning, Environment.CurrentManagedThreadId, "Periodic sync expiring - iOS terminating task");

            cts.Cancel();

            DisposeCts(cts);
        };

        // Perform full sync asynchronously
        Task.Run(
            async () =>
            {
                try
                {
                    // Trigger full sync (push + pull)
                    await Task.Delay(15000, cts.Token).ConfigureAwait(false);

                    App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId, "Periodic background sync completed successfully");
                    
                    task.SetTaskCompleted(true);
                }
                catch (OperationCanceledException)
                {
                    App.NetLog(TraceLevel.Warning, Environment.CurrentManagedThreadId, "Periodic sync cancelled due to iOS timeout");
                    
                    task.SetTaskCompleted(false);
                }
                catch (Exception ex)
                {
                    App.NetLog(TraceLevel.Error, Environment.CurrentManagedThreadId, "Periodic sync failed", ex);
                    
                    task.SetTaskCompleted(false);
                }
                finally
                {
                    DisposeCts(cts);
                }
            },
            cts.Token);
    }

    private void DisposeCts(CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            cancellationTokenSource.Dispose();
        }
        catch (Exception ex)
        {
            App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId, "failed to dispose", ex);
        }
    }
}