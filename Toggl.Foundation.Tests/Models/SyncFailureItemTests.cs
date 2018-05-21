﻿using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.SyncModel
{
    public sealed class SyncFailureItemTests
    {
        public sealed class TheConstructor
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new SyncFailureItem(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void SetsProjectTypeIfConstructedWithAProject()
            {
                IDatabaseProject project = Substitute.For<IDatabaseProject>();
                project.Name.Returns("My Project");

                SyncFailureItem syncFailure = new SyncFailureItem(project);

                syncFailure.Type.Should().Be(ItemType.Project);
            }

            [Fact, LogIfTooSlow]
            public void SetsTagTypeIfConstructedWithATag()
            {
                IDatabaseTag tag = Substitute.For<IDatabaseTag>();
                tag.Name.Returns("My Tag");

                SyncFailureItem syncFailure = new SyncFailureItem(tag);

                syncFailure.Type.Should().Be(ItemType.Tag);
            }

            [Fact, LogIfTooSlow]
            public void SetsClientTypeIfConstructedWithAClient()
            {
                IDatabaseClient client = Substitute.For<IDatabaseClient>();
                client.Name.Returns("My Client");

                SyncFailureItem syncFailure = new SyncFailureItem(client);

                syncFailure.Type.Should().Be(ItemType.Client);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheCorrectProperties()
            {
                IDatabaseClient tag = Substitute.For<IDatabaseClient>();
                tag.Name.Returns("My Client");
                tag.SyncStatus.Returns(SyncStatus.SyncFailed);
                tag.LastSyncErrorMessage.Returns("Something bad happened");

                SyncFailureItem syncFailure = new SyncFailureItem(tag);

                syncFailure.Name.Should().Be("My Client");
                syncFailure.SyncStatus.Should().Be(SyncStatus.SyncFailed);
                syncFailure.SyncErrorMessage.Should().Be("Something bad happened");
            }
        }
    }
}