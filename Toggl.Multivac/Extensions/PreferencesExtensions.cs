﻿using Toggl.Multivac.Models;

namespace Toggl.Multivac.Extensions
{
    public static class PreferencesExtensions
    {
        public static bool IsEqualTo(this IPreferences first, IPreferences second)
            => first.DateFormat.Localized == second.DateFormat.Localized
                && first.DurationFormat == second.DurationFormat
                && first.TimeOfDayFormat.Localized == second.TimeOfDayFormat.Localized
                && first.CollapseTimeEntries == second.CollapseTimeEntries;
    }
}
