using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Foundation.Sync.Tests.Extensions;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Tests.Integration.Helper;
using Xunit;

namespace Toggl.Foundation.Sync.Tests.Helpers.Tests
{
    public sealed class ServerTests
    {
        [Fact]
        public async Task SetsUserDataCorrectly()
        {
            var server = await Server.Create();
            var randomEmail = Email.From($"custom-random-email-{Guid.NewGuid().ToString()}@random.emails.com");
            var updatedServerState = server.InitialServerState.With(
                user: New<IUser>.Value(server.InitialServerState.User.With(email: randomEmail)));

            await server.Push(updatedServerState);

            var finalServerState = await server.PullCurrentState();
            finalServerState.User.Email.Should().NotBe(server.InitialServerState.User.Email);
            finalServerState.User.Email.Should().Be(randomEmail);
        }

        [Fact]
        public async Task SetsPreferencesCorrectly()
        {
            var server = await Server.Create();
            var differentDurationFormat =
                server.InitialServerState.Preferences.DurationFormat == DurationFormat.Classic
                    ? DurationFormat.Improved
                    : DurationFormat.Classic;
            var preferences = server.InitialServerState.Preferences.With(durationFormat: differentDurationFormat);
            var updatedServerState = server.InitialServerState.With(preferences: New<IPreferences>.Value(preferences));

            await server.Push(updatedServerState);

            var finalServerState = await server.PullCurrentState();
            finalServerState.Preferences.DurationFormat.Should()
                .NotBe(server.InitialServerState.Preferences.DurationFormat);
            finalServerState.Preferences.DurationFormat.Should().Be(differentDurationFormat);
        }

        [Fact]
        public async Task CorrectlySetsIdsOfConnectedEntities()
        {
            var server = await Server.Create();
            var arrangedState = server.InitialServerState.With(
                timeEntries: new[]
                {
                    new MockTimeEntry
                    {
                        Id = -1,
                        Description = "Time Entry",
                        ProjectId = -2,
                        TagIds = new long[] { -5, -6 },
                        WorkspaceId = -7,
                        Start = DateTimeOffset.Now,
                        Duration = 123
                    }
                },
                projects: new[]
                {
                    new MockProject
                    {
                        Id = -2,
                        Name = "Project",
                        Color = Helper.Color.DefaultProjectColors[0],
                        ClientId = -3,
                        WorkspaceId = -7,
                        Active = true
                    }
                },
                clients: new[] { new MockClient { Id = -3, Name = "Client", WorkspaceId = -7 } },
                tags: new[] { new MockTag { Id = -5, Name = "Tag 1", WorkspaceId = -7 }, new MockTag { Id = -6, Name = "Tag 2", WorkspaceId = -7 } },
                workspaces: new[] { new MockWorkspace { Id = -7, Name = "Workspace" } });

            await server.Push(arrangedState);
            var finalServerState = await server.PullCurrentState();

            finalServerState.TimeEntries.Should().HaveCount(1);
            finalServerState.Projects.Should().HaveCount(1);
            finalServerState.Tags.Should().HaveCount(2);
            finalServerState.Workspaces.Should().HaveCount(2);
            var te = finalServerState.TimeEntries.First();
            var project = finalServerState.Projects.First();
            var client = finalServerState.Clients.First();
            var tags = finalServerState.Tags;
            var workspaces = finalServerState.Workspaces;
            var workspace = workspaces.Single(ws => ws.Id != finalServerState.User.DefaultWorkspaceId.Value);
            te.WorkspaceId.Should().Be(workspace.Id);
            te.ProjectId.Should().Be(project.Id);
            project.ClientId.Should().Be(client.Id);
            project.WorkspaceId.Should().Be(workspace.Id);
            client.WorkspaceId.Should().Be(workspace.Id);
            tags.ForEach(tag => tag.WorkspaceId.Should().Be(workspace.Id));
        }

        [Fact]
        public async Task WorksWithPaidFeatures()
        {
            var server = await Server.Create();
            var pricingPlanActivator = new SubscriptionPlanActivator();
            await pricingPlanActivator.EnsureDefaultWorkspaceIsOnPlan(
                server.InitialServerState.User,
                PricingPlans.StarterAnnual);

            var arrangedState = server.InitialServerState.With(
                projects: new[] { new MockProject { Color = "#abcdef" } });
            Func<Task> pushingProjectWithCustomColor = () => server.Push(arrangedState);

            pushingProjectWithCustomColor.Should().NotThrow<PaymentRequiredException>();
        }
    }
}
