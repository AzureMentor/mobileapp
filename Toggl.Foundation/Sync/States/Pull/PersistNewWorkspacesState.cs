using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.Pull
{
    public class PersistNewWorkspacesState : ISyncState<IEnumerable<IWorkspace>>
    {
        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource;
        private readonly ISinceParameterRepository sinceParameterRepository;

        public StateResult FinishedPersisting { get; } = new StateResult();

        public PersistNewWorkspacesState(
            IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource,
            ISinceParameterRepository sinceParameterRepository)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(sinceParameterRepository, nameof(sinceParameterRepository));

            this.dataSource = dataSource;
            this.sinceParameterRepository = sinceParameterRepository;
        }

        public IObservable<ITransition> Start(IEnumerable<IWorkspace> workspaces)
            => Observable.Return(workspaces)
                .Do(sinceParameterRepository.Reset)
                .SelectMany(CommonFunctions.Identity)
                .Select(Workspace.Clean)
                .SelectMany(createOrUpdate)
                .ToList()
                .SelectValue(FinishedPersisting.Transition());

        private IObservable<IThreadSafeWorkspace> createOrUpdate(IThreadSafeWorkspace workspace)
            => dataSource
                .GetAll(ws => ws.Id == workspace.Id, includeInaccessibleEntities: true)
                .SelectMany(stored => stored.None()
                    ? dataSource.Create(workspace)
                    : dataSource.Update(workspace));
    }
}
