﻿using System;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross.Platforms.Android.Binding.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class SettingsActivity : ReactiveActivity<SettingsViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SettingsActivity);

            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            InitializeViews();

            var adapter = new SimpleAdapter<SelectableWorkspaceViewModel>(
                Resource.Layout.SettingsActivityWorkspaceCell,
                WorkspaceSelectionViewHolder.Create
            );
            adapter.OnItemTapped = ViewModel.SelectDefaultWorkspace;
            workspacesRecyclerView.SetAdapter(adapter);
            workspacesRecyclerView.SetLayoutManager(new LinearLayoutManager(this));

            versionTextView.Text = ViewModel.Version;

            ViewModel.Name
                .Subscribe(nameTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Email
                .Subscribe(emailTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Workspaces
                .Subscribe(adapter.Rx().Items())
                .DisposedBy(DisposeBag);

            ViewModel.IsManualModeEnabled
                .Subscribe(manualModeSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.UseTwentyFourHourFormat
                .Subscribe(is24hoursModeSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.AreRunningTimerNotificationsEnabled
                .Subscribe(runningTimerNotificationsSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.AreStoppedTimerNotificationsEnabled
                .Subscribe(stoppedTimerNotificationsSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.BeginningOfWeek
                .Subscribe(beginningOfWeekTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.UserAvatar
                .Select(userImageFromBytes)
                .Subscribe(bitmap =>
                {
                    avatarView.SetImageBitmap(bitmap);
                    avatarContainer.Visibility = ViewStates.Visible;
                })
                .DisposedBy(DisposeBag);

            ViewModel.LoggingOut
                .VoidSubscribe(this.CancelAllNotifications)
                .DisposedBy(DisposeBag);

            ViewModel.IsFeedbackSuccessViewShowing
                .Subscribe(showFeedbackSuccessToast)
                .DisposedBy(DisposeBag);

            logoutView.Rx().Tap()
                .Subscribe(_ => ViewModel.TryLogout())
                .DisposedBy(DisposeBag);

            helpView.Rx().Tap()
                .Subscribe(ViewModel.OpenHelpView)
                .DisposedBy(DisposeBag);

            aboutView.Rx().Tap()
                .Subscribe(ViewModel.OpenAboutView)
                .DisposedBy(DisposeBag);

            feedbackView.Rx().Tap()
                .Subscribe(ViewModel.SubmitFeedback)
                .DisposedBy(DisposeBag);

            manualModeView.Rx().Tap()
                .VoidSubscribe(ViewModel.ToggleManualMode)
                .DisposedBy(DisposeBag);

            is24hoursModeView.Rx()
                .BindAction(ViewModel.ToggleTwentyFourHourSettings)
                .DisposedBy(DisposeBag);

            runningTimerNotificationsView.Rx().Tap()
                .VoidSubscribe(ViewModel.ToggleRunningTimerNotifications)
                .DisposedBy(DisposeBag);

            stoppedTimerNotificationsView.Rx().Tap()
                .VoidSubscribe(ViewModel.ToggleStoppedTimerNotifications)
                .DisposedBy(DisposeBag);

            beginningOfWeekView.Rx().Tap()
                .Subscribe(ViewModel.SelectBeginningOfWeek)
                .DisposedBy(DisposeBag);

            setupToolbar();
        }

        private void showFeedbackSuccessToast(bool succeeeded)
        {
            if (!succeeeded) return;

            var toast = Toast.MakeText(this, Resource.String.SendFeedbackSuccessMessage, ToastLength.Long);
            toast.SetGravity(GravityFlags.CenterHorizontal | GravityFlags.Bottom, 0, 0);
            toast.Show();
        }

        private Bitmap userImageFromBytes(byte[] imageBytes)
            => BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);

        private void setupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);

            toolbar.Title = ViewModel.Title;

            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            toolbar.NavigationClick += onNavigateBack;
        }

        private void onNavigateBack(object sender, Toolbar.NavigationClickEventArgs e)
        {
            ViewModel.Close().Execute();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        protected override void AttachBaseContext(Context @base)
        {
            base.AttachBaseContext(MvxContextWrapper.Wrap(@base, this));
        }
    }
}
