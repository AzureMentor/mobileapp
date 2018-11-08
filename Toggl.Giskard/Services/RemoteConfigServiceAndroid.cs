﻿using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Gms.Tasks;
using Firebase.RemoteConfig;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using GmsTask = Android.Gms.Tasks.Task;

namespace Toggl.Giskard.Services
{
    public class RemoteConfigServiceAndroid : IRemoteConfigService
    {
        public IObservable<RatingViewConfiguration> RatingViewConfiguration
            => Observable.Create<RatingViewConfiguration>(observer =>
            {
                try
                {
                    var remoteConfig = FirebaseRemoteConfig.Instance;

                    var settings = new FirebaseRemoteConfigSettings
                        .Builder()
                        .SetDeveloperModeEnabled(true)
                        .Build();

                    remoteConfig.SetConfigSettings(settings);
                    remoteConfig.Fetch(error =>
                    {
                        if (error != null)
                            return;

                        remoteConfig.ActivateFetched();
                        var configuration = new RatingViewConfiguration(
                            (int)remoteConfig.GetValue("day_count").AsLong(),
                            remoteConfig.GetString("criterion").ToRatingViewCriterion()
                        );
                        observer.OnNext(configuration);
                        observer.OnCompleted();
                    });
                }
                catch (Exception ex)
                {

                }

                return Disposable.Empty;
            });

        public IObservable<bool> IsCalendarFeatureEnabled
            => Observable.Return(false);
    }

    public class RatingViewCompletionHandler : Java.Lang.Object, IOnCompleteListener
    {
        private FirebaseRemoteConfig remoteConfig;
        private Action<Exception> action;

        public RatingViewCompletionHandler(FirebaseRemoteConfig remoteConfig, Action<Exception> action)
        {
            this.remoteConfig = remoteConfig;
            this.action = action;
        }

        public void OnComplete(GmsTask task)
        {
            action(task.IsSuccessful ? null : task.Exception);
        }
    }

    public static class FirebaseExtensions
    {
        public static void Fetch(this FirebaseRemoteConfig remoteConfig, Action<Exception> action)
        {
            remoteConfig.Fetch().AddOnCompleteListener(new RatingViewCompletionHandler(remoteConfig, action));
        }
    }
}
