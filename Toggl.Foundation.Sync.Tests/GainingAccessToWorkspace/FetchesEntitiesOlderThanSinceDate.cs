﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Sync.Tests.Extensions;
using Toggl.Foundation.Sync.Tests.Helpers;
using Toggl.Foundation.Sync.Tests.State;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using System.Collections.Generic;
using Toggl.Multivac;
using Toggl.Ultrawave.Helpers;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.Tests.GainingAccessToWorkspace
{
    public class FetchesEntitiesOlderThanSinceDate : BaseComplexSyncTest
    {
        protected override ServerState ArrangeServerState(ServerState initialServerState)
            => initialServerState.With(
                clients: new[] { new MockClient { Id = -1, WorkspaceId = -2, Name = "c1" } },
                tags: new[] {
                    new MockTag { Id = -1, WorkspaceId = -2, Name = "t1" },
                    new MockTag { Id = -2, WorkspaceId = -2, Name = "t2" }
                },
                projects: new[] { 
                    new MockProject { 
                        Id = -1,
                        WorkspaceId = -2,
                        ClientId = -1,
                        Name = "p1",
                        Color = Helper.Color.DefaultProjectColors[0],
                        Active = true } },
                timeEntries: new[] {
                    new MockTimeEntry { 
                        Id = -1,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(2),
                        Duration = 10 * 60,
                        WorkspaceId = -2,
                        ProjectId = -1,
                        TagIds = new long[] { -1, -2 },
                        Description = "te1" },
                    new MockTimeEntry { 
                        Id = -2,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(1),
                        Duration = 10 * 60,
                        WorkspaceId = -2,
                        ProjectId = -1,
                        TagIds = new long[] { -1 },
                        Description = "te2" }
                },
                workspaces: new[] {
                    initialServerState.Workspaces.Single(),
                    new MockWorkspace { Id = -2, Name = "ws2" }
                },
                pricingPlans: New<IDictionary<long, PricingPlans>>.Value(
                    new Dictionary<long, PricingPlans>
                    {
                        [-2] = PricingPlans.StarterAnnual
                    })
            );

        protected override DatabaseState ArrangeDatabaseState(ServerState serverState)
        {
            var defaultWorkspace = serverState.Workspaces.Single(ws => ws.Id == serverState.User.DefaultWorkspaceId.Value);
            var regainedAccessWorkspace = serverState.Workspaces.Single(ws => ws.Name == "ws2");
            var regainedAccessClient = serverState.Clients.Single(c => c.Name == "c1");
            var regainedAccessTag1 = serverState.Tags.Single(t => t.Name == "t1");
            var regainedAccessTag2 = serverState.Tags.Single(t => t.Name == "t2");
            var regainedAccessProject = serverState.Projects.Single(p => p.Name == "p1");
            var regainedAccessTE1 = serverState.TimeEntries.Single(te => te.Description == "te1");
            var regainedAccessTE2 = serverState.TimeEntries.Single(te => te.Description == "te2");

            return new DatabaseState(
                user: serverState.User.ToSyncable(),
                preferences: serverState.Preferences.ToSyncable(),
                workspaces: new[]
                {
                    defaultWorkspace.ToSyncable(),
                    new MockWorkspace {
                        Id = regainedAccessWorkspace.Id,
                        Name = "ws2", IsInaccessible = true,
                        SyncStatus = SyncStatus.SyncNeeded }
                },
                clients: new[]
                {
                    new MockClient { Id = regainedAccessClient.Id, WorkspaceId = regainedAccessWorkspace.Id, SyncStatus = SyncStatus.SyncFailed }
                },
                tags: new[]
                {
                    new MockTag { Id = regainedAccessTag1.Id, WorkspaceId = regainedAccessWorkspace.Id, SyncStatus = SyncStatus.SyncNeeded },
                    new MockTag { Id = regainedAccessTag2.Id, WorkspaceId = regainedAccessWorkspace.Id, SyncStatus = SyncStatus.SyncNeeded }
                },
                projects: new[]
                {
                    new MockProject {
                        Id = regainedAccessProject.Id,
                        WorkspaceId = regainedAccessWorkspace.Id,
                        SyncStatus = SyncStatus.InSync,
                        ClientId = regainedAccessClient.Id }
                },
                timeEntries: new[]
                {
                    new MockTimeEntry {
                        Id = regainedAccessTE1.Id,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(2),
                        Duration = 10 * 60,
                        WorkspaceId = regainedAccessWorkspace.Id,
                        ProjectId = regainedAccessProject.Id,
                        TagIds = new long[] { regainedAccessTag1.Id },
                        SyncStatus = SyncStatus.InSync },
                    new MockTimeEntry {
                        Id = regainedAccessTE2.Id,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(1),
                        Duration = 10 * 60,
                        WorkspaceId = regainedAccessWorkspace.Id,
                        ProjectId = regainedAccessProject.Id,
                        TagIds = new long[] { regainedAccessTag1.Id },
                        SyncStatus = SyncStatus.SyncFailed,
                },
                    new MockTimeEntry {
                        Id = -5,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(1),
                        Duration = 10 * 60,
                        WorkspaceId = regainedAccessWorkspace.Id,
                        ProjectId = regainedAccessProject.Id,
                        TagIds = new long[] { regainedAccessTag2.Id },
                        SyncStatus = SyncStatus.SyncNeeded }
                },
                sinceParameters: new Dictionary<Type, DateTimeOffset?>
                {
                    [typeof(IDatabaseWorkspace)] = DateTimeOffset.Now.AddDays(1),
                    [typeof(IDatabaseClient)] = DateTimeOffset.Now.AddDays(1),
                    [typeof(IDatabaseProject)] = DateTimeOffset.Now.AddDays(1),
                    [typeof(IDatabaseTag)] = DateTimeOffset.Now.AddDays(1),
                    [typeof(IDatabaseTimeEntry)] = DateTimeOffset.Now.AddDays(1)
                });
        }

        protected override void AssertFinalState(AppServices services, ServerState finalServerState, DatabaseState finalDatabaseState)
        {
            if (!finalServerState.User.DefaultWorkspaceId.HasValue)
                throw new NoDefaultWorkspaceException();

            var defaultWorkspaceId = finalServerState.User.DefaultWorkspaceId.Value;
            var workspace = finalServerState.Workspaces.Single(w => w.Name == "ws2");

            finalServerState.Workspaces.Should().HaveCount(2)
                .And
                .Contain(ws => ws.Id == defaultWorkspaceId);

            finalDatabaseState.Workspaces.Should().HaveCount(2)
                .And
                .Contain(ws => ws.Id == defaultWorkspaceId && ws.SyncStatus == SyncStatus.InSync && !ws.IsInaccessible)
                .And
                .Contain(ws => ws.Id == workspace.Id && ws.SyncStatus == SyncStatus.InSync && !ws.IsInaccessible);

            finalDatabaseState.Clients.Should().HaveCount(1).And.OnlyContain(
                client => !client.IsInaccessible && client.SyncStatus == SyncStatus.InSync);
            finalDatabaseState.Tags.Should().HaveCount(2).And.OnlyContain(
                tag => !tag.IsInaccessible && tag.SyncStatus == SyncStatus.InSync);
            finalDatabaseState.Projects.Should().HaveCount(1).And.OnlyContain(
                project => !project.IsInaccessible && project.SyncStatus == SyncStatus.InSync);
            finalDatabaseState.TimeEntries.Should().HaveCount(3).And.OnlyContain(
                timeEntry => !timeEntry.IsInaccessible && timeEntry.SyncStatus == SyncStatus.InSync);
        }
    }
}
