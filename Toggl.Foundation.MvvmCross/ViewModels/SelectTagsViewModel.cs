﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectTagsViewModel : MvxViewModel<(long[] tagIds, long workspaceId), long[]>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IStopwatchProvider stopwatchProvider;

        private readonly Subject<string> textSubject = new Subject<string>();
        private readonly BehaviorSubject<bool> hasTagsSubject = new BehaviorSubject<bool>(false);
        private readonly HashSet<long> selectedTagIds = new HashSet<long>();

        private long[] defaultResult;
        private long workspaceId;
        private IStopwatch navigationFromEditTimeEntryStopwatch;

        public string Text { get; set; } = "";

        public bool SuggestCreation
        {
            get
            {
                var text = Text.Trim();
                return !string.IsNullOrEmpty(text)
                       && Tags.None(tag => tag.Name.IsSameCaseInsensitiveTrimedTextAs(text))
                       && text.IsAllowedTagByteSize();
            }
        }

        public bool IsFilterEmpty => string.IsNullOrWhiteSpace(Text);

        public MvxObservableCollection<SelectableTagViewModel> Tags { get; }
            = new MvxObservableCollection<SelectableTagViewModel>();

        public bool IsEmpty { get; set; } = false;

        [DependsOn(nameof(IsEmpty))]
        public string PlaceholderText
            => IsEmpty
            ? Resources.EnterTag
            : Resources.AddFilterTags;

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand SaveCommand { get; }

        public IMvxAsyncCommand CreateTagCommand { get; }

        public IMvxCommand ClearTextCommand { get; }

        public IMvxCommand<SelectableTagViewModel> SelectTagCommand { get; }

        public SelectTagsViewModel(
            ITogglDataSource dataSource,
            IMvxNavigationService navigationService,
            IInteractorFactory interactorFactory,
            IStopwatchProvider stopwatchProvider)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));

            this.dataSource = dataSource;
            this.navigationService = navigationService;
            this.interactorFactory = interactorFactory;
            this.stopwatchProvider = stopwatchProvider;

            CloseCommand = new MvxAsyncCommand(close);
            SaveCommand = new MvxAsyncCommand(save);
            CreateTagCommand = new MvxAsyncCommand(createTag);
            SelectTagCommand = new MvxCommand<SelectableTagViewModel>(selectTag);
            ClearTextCommand = new MvxCommand(clearText);
        }

        public override void Prepare((long[] tagIds, long workspaceId) parameter)
        {
            workspaceId = parameter.workspaceId;
            defaultResult = parameter.tagIds;
            selectedTagIds.AddRange(parameter.tagIds);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            navigationFromEditTimeEntryStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenSelectTagsView);
            stopwatchProvider.Remove(MeasuredOperation.OpenSelectTagsView);

            var initialHasTags = dataSource.Tags
               .GetAll()
               .Select(tags => tags.Where(tag => tag.WorkspaceId == workspaceId).Any());

            hasTagsSubject.AsObservable()
                          .Merge(initialHasTags)
                          .Subscribe(hasTags => IsEmpty = !hasTags);

            textSubject.AsObservable()
                       .StartWith(Text)
                       .Select(text => text.SplitToQueryWords())
                       .SelectMany(wordsToQuery => interactorFactory.GetTagsAutocompleteSuggestions(wordsToQuery).Execute())
                       .Select(suggestions => suggestions.Cast<TagSuggestion>())
                       .Select(suggestions => suggestions.Where(s => s.WorkspaceId == workspaceId))
                       .Subscribe(onTags);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromEditTimeEntryStopwatch?.Stop();
            navigationFromEditTimeEntryStopwatch = null;
        }

        private void OnTextChanged()
        {
            textSubject.OnNext(Text.Trim());
        }

        private void onTags(IEnumerable<TagSuggestion> tags)
        {
            Tags.Clear();

            var sortedTags = tags.Select(createSelectableTag)
                                 .OrderByDescending(tag => tag.Selected)
                                 .ThenBy(tag => tag.Name);

            Tags.AddRange(sortedTags);
        }

        private SelectableTagViewModel createSelectableTag(TagSuggestion tagSuggestion)
            => new SelectableTagViewModel(tagSuggestion, selectedTagIds.Contains(tagSuggestion.TagId));

        private Task close()
            => navigationService.Close(this, defaultResult);

        private Task save() => navigationService.Close(this, selectedTagIds.ToArray());

        private void selectTag(SelectableTagViewModel tag)
        {
            tag.Selected = !tag.Selected;

            if (tag.Selected)
                selectedTagIds.Add(tag.Id);
            else
                selectedTagIds.Remove(tag.Id);
        }

        private async Task createTag()
        {
            var createdTag = await interactorFactory.CreateTag(Text.Trim(), workspaceId).Execute();
            selectedTagIds.Add(createdTag.Id);
            Text = "";

            hasTagsSubject.OnNext(true);
        }

        private void clearText()
        {
            Text = "";
        }
    }
}
