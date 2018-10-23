using System;
using Toggl.Multivac;
using Toggl.Multivac.Models;

namespace Toggl.Foundation.Sync.Tests.Extensions
{
    public static class IUserExtensions
    {
        private sealed class User : IUser
        {
            public long Id { get; set; }
            public DateTimeOffset At { get; set; }
            public string ApiToken { get; set; }
            public long? DefaultWorkspaceId { get; set; }
            public Email Email { get; set; }
            public string Fullname { get; set; }
            public BeginningOfWeek BeginningOfWeek { get; set; }
            public string Language { get; set; }
            public string ImageUrl { get; set; }
        }

        public static IUser With(
            this IUser user,
            New<long?> defaultWorkspaceId = default(New<long?>))
            => new User
            {
                Id = user.Id,
                At = user.At,
                ApiToken = user.ApiToken,
                DefaultWorkspaceId = defaultWorkspaceId.ValueOr(user.DefaultWorkspaceId),
                BeginningOfWeek = user.BeginningOfWeek,
                Email = user.Email,
                Fullname = user.Fullname,
                ImageUrl = user.ImageUrl,
                Language = user.Language
            };
    }
}
