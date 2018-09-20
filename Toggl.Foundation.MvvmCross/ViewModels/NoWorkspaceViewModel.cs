﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class NoWorkspaceViewModel : MvxViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly Subject<bool> isLoading = new Subject<bool>();

        public IObservable<bool> IsLoading => isLoading.AsObservable();

        public NoWorkspaceViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IAccessRestrictionStorage accessRestrictionStorage)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.accessRestrictionStorage = accessRestrictionStorage;
            this.navigationService = navigationService;
            this.interactorFactory = interactorFactory;
        }

        public async Task TryAgain()
        {
            isLoading.OnNext(true);

            dataSource.CreateNewSyncManager();

            var anyWorkspaceIsAvailable = await dataSource
                .SyncManager
                .ForceFullSync()
                .Where(state => state == SyncState.Sleep)
                .SelectMany(dataSource.Workspaces.GetAll())
                .Any(workspaces => workspaces.Any());

            isLoading.OnNext(false);

            if (anyWorkspaceIsAvailable)
            {
                close();
            }
        }

        public async Task CreateWorkspaceWithDefaultName()
        {
            isLoading.OnNext(true);

            dataSource.CreateNewSyncManager();

            await interactorFactory.CreateDefaultWorkspace().Execute();

            await dataSource
                .SyncManager
                .ForceFullSync();

            isLoading.OnNext(false);
            close();
        }

        private void close()
        {
            accessRestrictionStorage.SetNoWorkspaceStateReached(false);
            navigationService.Close(this);
        }
    }
}
