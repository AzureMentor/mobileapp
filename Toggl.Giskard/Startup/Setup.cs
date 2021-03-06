﻿using System;
using System.Reactive.Concurrency;
using Android.Content;
using MvvmCross;
using MvvmCross.Binding;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Exceptions;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Presenters;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Giskard.BroadcastReceivers;
using Toggl.Giskard.Presenters;
using Toggl.Giskard.Services;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using ColorPlugin = MvvmCross.Plugin.Color.Platforms.Android.Plugin;
using VisibilityPlugin = MvvmCross.Plugin.Visibility.Platforms.Android.Plugin;

namespace Toggl.Giskard
{
    public sealed partial class Setup : MvxAppCompatSetup<App<LoginViewModel>>
    {
        private const int maxNumberOfSuggestions = 5;

        private IAnalyticsService analyticsService;
        private IForkingNavigationService navigationService;
        private PlatformInfo platformInfo;

#if USE_PRODUCTION_API
        private const ApiEnvironment environment = ApiEnvironment.Production;
#else
        private const ApiEnvironment environment = ApiEnvironment.Staging;
#endif

        protected override MvxBindingBuilder CreateBindingBuilder() => new TogglBindingBuilder();

        protected override IMvxNavigationService InitializeNavigationService(IMvxViewModelLocatorCollection collection)
        {
            analyticsService = new AnalyticsServiceAndroid();
            platformInfo = new PlatformInfo { Platform = Platform.Giskard };

            var loader = CreateViewModelLoader(collection);
            Mvx.RegisterSingleton<IMvxViewModelLoader>(loader);

            navigationService = new NavigationService(null, loader, analyticsService, platformInfo);

            Mvx.RegisterSingleton<IForkingNavigationService>(navigationService);
            Mvx.RegisterSingleton<IMvxNavigationService>(navigationService);
            return navigationService;
        }

        protected override IMvxAndroidViewPresenter CreateViewPresenter()
            => new TogglPresenter(AndroidViewAssemblies);

        protected override void InitializeApp(IMvxPluginManager pluginManager, IMvxApplication app)
        {
            const string clientName = "Giskard";
            var packageInfo = ApplicationContext.PackageManager.GetPackageInfo(ApplicationContext.PackageName, 0);
            var version = packageInfo.VersionName;
            var sharedPreferences = ApplicationContext.GetSharedPreferences(clientName, FileCreationMode.Private);
            var database = new Database();
            var scheduler = Scheduler.Default;
            var timeService = new TimeService(scheduler);
            var suggestionProviderContainer = new SuggestionProviderContainer(
                new MostUsedTimeEntrySuggestionProvider(database, timeService, maxNumberOfSuggestions)
            );

            var appVersion = Version.Parse(version);
            var userAgent = new UserAgent(clientName, version);
            var mailService = new MailServiceAndroid(ApplicationContext);
            var dialogService = new DialogServiceAndroid();
            var platformConstants = new PlatformConstants();
            var keyValueStorage = new SharedPreferencesStorageAndroid(sharedPreferences);
            var settingsStorage = new SettingsStorage(appVersion, keyValueStorage);
            var feedbackService = new FeedbackService(userAgent, mailService, dialogService, platformConstants);
            var schedulerProvider = new AndroidSchedulerProvider();
            var permissionsService = new PermissionsServiceAndroid();
            var calendarService = new CalendarServiceAndroid(permissionsService);

            ApplicationContext.RegisterReceiver(new TimezoneChangedBroadcastReceiver(timeService),
                new IntentFilter(Intent.ActionTimezoneChanged));

            var foundation =
                TogglFoundation
                    .ForClient(userAgent, appVersion)
                    .WithDatabase(database)
                    .WithScheduler(scheduler)
                    .WithTimeService(timeService)
                    .WithMailService(mailService)
                    .WithApiEnvironment(environment)
                    .WithGoogleService<GoogleServiceAndroid>()
                    .WithRatingService<RatingServiceAndroid>()
                    .WithLicenseProvider<LicenseProviderAndroid>()
                    .WithAnalyticsService(analyticsService)
                    .WithSchedulerProvider(schedulerProvider)
                    .WithPlatformConstants(platformConstants)
                    .WithNotificationService<NotificationServiceAndroid>()
                    .WithRemoteConfigService<RemoteConfigServiceAndroid>()
                    .WithApiFactory(new ApiFactory(environment, userAgent))
                    .WithBackgroundService(new BackgroundService(timeService))
                    .WithSuggestionProviderContainer(suggestionProviderContainer)
                    .WithApplicationShortcutCreator(new ApplicationShortcutCreator(ApplicationContext))
                    .WithPlatformInfo(platformInfo)
                    .WithStopwatchProvider<FirebaseStopwatchProviderAndroid>()
                    .WithIntentDonationService(new NoopIntentDonationServiceAndroid())
                    .WithPrivateSharedStorageService(new NoopPrivateSharedStorageServiceAndroid())

                    .StartRegisteringPlatformServices()
                    .WithDialogService(dialogService)
                    .WithFeedbackService(feedbackService)
                    .WithLastTimeUsageStorage(settingsStorage)
                    .WithBrowserService<BrowserServiceAndroid>()
                    .WithCalendarService(calendarService)
                    .WithKeyValueStorage(keyValueStorage)
                    .WithUserPreferences(settingsStorage)
                    .WithOnboardingStorage(settingsStorage)
                    .WithNavigationService(navigationService)
                    .WithPermissionsService(permissionsService)
                    .WithAccessRestrictionStorage(settingsStorage)
                    .WithErrorHandlingService(new ErrorHandlingService(navigationService, settingsStorage))
                    .Build();

            foundation.RevokeNewUserIfNeeded().Initialize();

            ensureDataSourceInitializationIfLoggedIn();

            base.InitializeApp(pluginManager, app);
        }

        protected override IMvxAndroidCurrentTopActivity CreateAndroidCurrentTopActivity()
        {
            var mvxApplication = MvxAndroidApplication.Instance;
            var activityLifecycleCallbacksManager = new QueryableMvxLifecycleMonitorCurrentTopActivity();
            mvxApplication.RegisterActivityLifecycleCallbacks(activityLifecycleCallbacksManager);
            return activityLifecycleCallbacksManager;
        }

        // Skip the sluggish and reflection-based manager and load our plugins by hand
        protected override IMvxPluginManager InitializePluginFramework()
        {
            LoadPlugins(null);
            return null;
        }

        public override void LoadPlugins(IMvxPluginManager pluginManager)
        {
            new ColorPlugin().Load();
            new VisibilityPlugin().Load();
        }

        protected override void PerformBootstrapActions()
        {
            // This method uses reflection to find classes that inherit from
            // IMvxBootstrapAction, creates instances of these classes and then
            // calls their Run method. We can skip it since we don't have such classes.
        }

        void ensureDataSourceInitializationIfLoggedIn()
        {
            /* Why? The ITogglDataSource is lazily initialized by the login manager
             * during some of it's methods calls.
             * The App.cs code that makes those calls don't have time to
             * do so during rehydration and on starup on some phones.
             * This call makes sure the ITogglDataSource singleton is registered
             * and ready to be injected during those times.
             */
            var loginManager = Mvx.Resolve<ILoginManager>();
            var dataSource = loginManager.GetDataSourceIfLoggedIn();
        }
    }
}
