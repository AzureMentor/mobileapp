﻿using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Activities
{
    public sealed partial class MainActivity
    {
        private View runningEntryCardFrame;
        private FloatingActionButton playButton;
        private FloatingActionButton stopButton;
        private CoordinatorLayout coordinatorLayout;
        private ImageView reportsView;
        private ImageView settingsView;
        private View timeEntryCard;
        private TextView timeEntryCardTimerLabel;
        private TextView timeEntryCardDescriptionLabel;
        private TextView timeEntryCardAddDescriptionLabel;
        private View timeEntryCardDotContainer;
        private View timeEntryCardDotView;
        private TextView timeEntryCardProjectLabel;
        private TextView timeEntryCardClientLabel;
        private SwipeRefreshLayout refreshLayout;
        private RecyclerView mainRecyclerView;

        protected override void InitializeViews()
        {
            mainRecyclerView = FindViewById<RecyclerView>(Resource.Id.MainRecyclerView);
            runningEntryCardFrame = FindViewById(Resource.Id.MainRunningTimeEntryFrame);
            playButton = FindViewById<FloatingActionButton>(Resource.Id.MainPlayButton);
            stopButton = FindViewById<FloatingActionButton>(Resource.Id.MainStopButton);
            coordinatorLayout = FindViewById<CoordinatorLayout>(Resource.Id.MainCoordinatorLayout);
            reportsView = FindViewById<ImageView>(Resource.Id.ToolbarReportsImageView);
            settingsView = FindViewById<ImageView>(Resource.Id.ToolbarSettingsImageView);
            timeEntryCard = FindViewById(Resource.Id.MainContentArea);
            timeEntryCardTimerLabel = FindViewById<TextView>(Resource.Id.MainRunningTimeEntryTimerLabel);
            timeEntryCardDescriptionLabel = FindViewById<TextView>(Resource.Id.MainRunningTimeEntryDescription);
            timeEntryCardAddDescriptionLabel = FindViewById<TextView>(Resource.Id.MainRunningTimeEntryAddDescriptionLabel);
            timeEntryCardDotContainer = FindViewById(Resource.Id.MainRunningTimeEntryProjectDotContainer);
            timeEntryCardDotView = FindViewById(Resource.Id.MainRunningTimeEntryProjectDotView);
            timeEntryCardProjectLabel = FindViewById<TextView>(Resource.Id.MainRunningTimeEntryProjectLabel);
            timeEntryCardClientLabel = FindViewById<TextView>(Resource.Id.MainRunningTimeEntryClientLabel);
            refreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.MainSwipeRefreshLayout);
        }
    }
}
