﻿using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Extensions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Xunit;
using static Toggl.Multivac.Extensions.EmailExtensions;
using static Toggl.Multivac.Extensions.PasswordExtensions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public class TokenResetViewModelTests
    {
        public abstract class TokenResetViewModelTest : BaseViewModelTests<TokenResetViewModel>
        {
            protected static readonly Email ValidEmail = "susancalvin@psychohistorian.museum".ToEmail();
            protected static readonly Email InvalidEmail = "foo@".ToEmail();

            protected static readonly Password ValidPassword = "123456".ToPassword();
            protected static readonly Password InvalidPassword = Password.Empty;

            protected ITestableObserver<T> Observe<T>(IObservable<T> observable)
            {
                var observer = TestScheduler.CreateObserver<T>();
                observable.Subscribe(observer);
                return observer;
            }

            protected override TokenResetViewModel CreateViewModel()
                => new TokenResetViewModel(
                    LoginManager,
                    DataSource,
                    DialogService,
                    NavigationService,
                    UserPreferences,
                    OnboardingStorage,
                    AnalyticsService,
                    SchedulerProvider);
        }

        public sealed class TheConstructor : TokenResetViewModelTest
        {
            [Theory, LogIfTooSlow, ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool userLoginManager,
                bool userNavigationService,
                bool useDataSource,
                bool useDialogService,
                bool useUserPreferences,
                bool useOnboardingStorage,
                bool useAnalyticsService,
                bool useSchedulerProvider
            )
            {
                var loginManager = userLoginManager ? LoginManager : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var dataSource = useDataSource ? DataSource : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TokenResetViewModel(
                        loginManager,
                        dataSource,
                        dialogService,
                        navigationService,
                        userPreferences,
                        onboardingStorage,
                        analyticsService,
                        schedulerProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheNextIsEnabledProperty : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseWhenThePasswordIsNotValid()
            {
                var nextIsEnabledObserver = Observe(ViewModel.NextIsEnabled);

                await ViewModel.SetPassword.Execute(InvalidPassword.ToString());

                TestScheduler.Start();
                nextIsEnabledObserver.LastValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTrueIfThePasswordIsValid()
            {
                var nextIsEnabledObserver = Observe(ViewModel.NextIsEnabled);

                await ViewModel.SetPassword.Execute(ValidPassword.ToString());

                TestScheduler.Start();
                nextIsEnabledObserver.LastValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseWheThePasswordIsValidButTheViewIsLoading()
            {
                var scheduler = new TestScheduler();
                var never = Observable.Never<ITogglDataSource>();
                LoginManager.RefreshToken(Arg.Any<Password>()).Returns(never);
                await ViewModel.SetPassword.Execute(ValidPassword.ToString());
                var nextIsEnabledObserver = Observe(ViewModel.NextIsEnabled);

                ViewModel.Done.Execute();

                TestScheduler.Start();
                nextIsEnabledObserver.LastValue().Should().BeFalse();
            }
        }

        public sealed class TheSetPasswordCommand : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task IsSuccessfullyEmmited()
            {
                var passwordObserver = Observe(ViewModel.Password);
                await ViewModel.SetPassword.Execute(ValidPassword.ToString());

                TestScheduler.Start();
                passwordObserver.LastValue().Should().Be(ValidPassword);
            }
        }

        public sealed class TheDoneCommand : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task DoesNotAttemptToLoginWhileThePasswordIsNotValid()
            {
                await ViewModel.SetPassword.Execute(InvalidPassword.ToString());
                var executionObserver = TestScheduler.CreateObserver<Unit>();

                ViewModel.Done.Execute().Subscribe(executionObserver);

                TestScheduler.Start();
                executionObserver.Messages.Last().Value.Kind.Should().Be(NotificationKind.OnError);
                await LoginManager.DidNotReceive().RefreshToken(Arg.Any<Password>());
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheLoginManagerWhenThePasswordIsValid()
            {
                await ViewModel.SetPassword.Execute(ValidPassword.ToString());

                await ViewModel.Done.Execute();

                await LoginManager.Received().RefreshToken(Arg.Is(ValidPassword));
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheMainViewModelModelWhenTheTokenRefreshSucceeds()
            {
                await ViewModel.SetPassword.Execute(ValidPassword.ToString());
                LoginManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                await ViewModel.Done.Execute();

                await NavigationService.Received().ForkNavigate<MainTabBarViewModel, MainViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task StopsTheViewModelLoadStateWhenItCompletes()
            {
                await ViewModel.Initialize();
                await ViewModel.SetPassword.Execute(ValidPassword.ToString());
                var isLoadingObserver = Observe(ViewModel.IsLoading);

                LoginManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                await ViewModel.Done.Execute();

                TestScheduler.Start();
                isLoadingObserver.LastValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task StopsTheViewModelLoadStateWhenItErrors()
            {
                await ViewModel.SetPassword.Execute(ValidPassword.ToString());
                LoginManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Throw<ITogglDataSource>(new Exception()));
                var isLoadingObserver = Observe(ViewModel.IsLoading);

                ViewModel.Done.Execute();

                TestScheduler.Start();
                var messages = isLoadingObserver.Messages.ToList();
                isLoadingObserver.LastValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotNavigateWhenTheLoginFails()
            {
                await ViewModel.SetPassword.Execute(ValidPassword.ToString());
                LoginManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                ViewModel.Done.Execute();

                await NavigationService.DidNotReceive().Navigate<MainViewModel>();
            }
        }

        public sealed class TheSignOutCommand : TokenResetViewModelTest
        {
            private async Task setup(bool hasUnsyncedData = false, bool userConfirmsSignout = true) 
            {
                DialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                             .Returns(Observable.Return(userConfirmsSignout));
                DataSource.HasUnsyncedData().Returns(Observable.Return(hasUnsyncedData));

                await ViewModel.Initialize();
            }

            [Fact, LogIfTooSlow]
            public async Task LogsTheUserOut()
            {
                await setup();

                await ViewModel.SignOut.Execute();

                await DataSource.Received().Logout();
            }

            [Fact, LogIfTooSlow]
            public async Task ResetsUserPreferences()
            {
                await setup();

                await ViewModel.SignOut.Execute();

                UserPreferences.Received().Reset();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginViewModel()
            {
                await setup();

                await ViewModel.SignOut.Execute();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task AsksForPermissionIfThereIsUnsyncedData()
            {
                await setup(hasUnsyncedData: true);

                await ViewModel.SignOut.Execute();

                await DialogService.Received().Confirm(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotLogTheUserOutIfPermissionIsDenied()
            {
                await setup(hasUnsyncedData: true, userConfirmsSignout: false);

                await ViewModel.SignOut.Execute();

                await DataSource.DidNotReceive().Logout();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksLogoutEvent()
            {
                await setup();

                await ViewModel.SignOut.Execute();

                AnalyticsService.Logout.Received().Track(LogoutSource.TokenReset);
            }
        }
    }
}
