using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using BackgroundTasks;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace BackgroundWork;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private static PeriodicBackgroundSyncHandler _periodicBackgroundSyncHandler = new PeriodicBackgroundSyncHandler();
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var finishedLaunching = base.FinishedLaunching(application, launchOptions);
        
        App.NetLog(TraceLevel.Info,  Environment.CurrentManagedThreadId, $"Finished Launching {finishedLaunching}");

        RegisterPeriodicBackgroundSyncHandler();
        
        App.NetLog(TraceLevel.Info,  Environment.CurrentManagedThreadId, "Finished registration of handler");
        return finishedLaunching;
    }

    public override void DidEnterBackground(UIApplication application)
    {
        var result = BGTaskScheduler.Shared.GetPendingAsync().GetAwaiter().GetResult();
        
        
        
        App.NetLog(TraceLevel.Info,  Environment.CurrentManagedThreadId, $"Background task entered: [{result.Length}]");
        _periodicBackgroundSyncHandler.SchedulePeriodicBackgroundSync();
        base.DidEnterBackground(application);
        
    }

    public override void WillEnterForeground(UIApplication application)
    {
        base.WillEnterForeground(application);
        
        _periodicBackgroundSyncHandler.CancelPeriodicBackgroundSync();
    }


    [SuppressMessage("Design", "CA1031:Do not catch general exception types",  Justification = "fine")]
    private static void RegisterPeriodicBackgroundSyncHandler()
    {
        try
        {
            // Get handler from DI container (cast to internal type is fine - same assembly)
            var handler = _periodicBackgroundSyncHandler;

            if (handler != null)
            {
                // Register background task handler
                BGTaskScheduler.Shared.Register(
                    PeriodicBackgroundSyncHandler.TaskIdentifier,
                    null,
                    handler.HandleBackgroundTask);
                App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId, "Registered periodic background sync handler");
            }
            else
            {
                App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId, "handler is already there, so not re-registering!");
            }
        }
        catch (Exception ex)
        {
            // Silently ignore if handler is not registered yet (during initial app setup)
            // This can happen if the app is not fully initialized yet
            App.NetLog(TraceLevel.Info, Environment.CurrentManagedThreadId, "failed to register periodic background sync handler", ex);
        }
    }
}