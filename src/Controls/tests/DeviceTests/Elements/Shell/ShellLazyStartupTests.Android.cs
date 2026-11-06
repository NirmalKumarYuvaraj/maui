#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AndroidX.Lifecycle;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class ShellLazyStartupTests : ControlsHandlerTestBase
	{
		void SetupBuilder()
		{
			EnsureHandlerCreated(builder => builder.ConfigureMauiHandlers(handlers =>
			{
				SetupShellHandlers(handlers);
				handlers.AddHandler<Shell, ShellHandler>();
				handlers.AddHandler<ShellItem, ShellItemHandler>();
				handlers.AddHandler<ShellSection, ShellSectionHandler>();
			}));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(2, 1)]
		[InlineData(4, 2)]
		public async Task StartupCreatesOnlyTheSelectedContent(int selectedSection, int selectedContent)
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() =>
				CreateShell(5, 3, selectedSection, selectedContent));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var selected = contents[selectedSection][selectedContent];
				await WaitForPage(selected, selected.CreatedPage!);

				foreach (var content in contents.SelectMany(section => section))
				{
					Assert.Equal(content == selected ? 1 : 0, content.CreationCount);
					if (content != selected)
					{
						Assert.Null(content.CreatedPage);
						Assert.Empty(GetFragment(content).ChildFragmentManager.Fragments);
					}
				}

				Assert.Equal(1, selected.CreatedPage!.AppearingCount);
			});
		}

		[Fact]
		public async Task FirstSelectionCreatesOnceAndRevisitsRetainTabStacks()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(3, 1));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var first = contents[0][0];
				var second = contents[1][0];
				var firstRoot = Assert.IsType<TrackingPage>(first.CreatedPage);
				var firstDetail = new ContentPage { Title = "First detail" };
				await shell.Navigation.PushAsync(firstDetail, false);
				await WaitForPage(first, firstDetail);
				var firstManager = GetNavigationManager(first);
				var firstHandler = firstRoot.Handler;

				Assert.Equal(0, second.CreationCount);
				tabs.CurrentItem = tabs.Items[1];
				await AssertEventually(() => second.CreatedPage is not null);
				var secondRoot = Assert.IsType<TrackingPage>(second.CreatedPage);
				await WaitForPage(second, secondRoot);
				var secondDetail = new ContentPage { Title = "Second detail" };
				await shell.Navigation.PushAsync(secondDetail, false);
				await WaitForPage(second, secondDetail);
				var secondManager = GetNavigationManager(second);

				for (int i = 0; i < 3; i++)
				{
					tabs.CurrentItem = tabs.Items[0];
					await WaitForPage(first, firstDetail);
					Assert.Same(firstDetail, shell.CurrentPage);
					Assert.Same(firstManager, GetNavigationManager(first));
					Assert.Equal(new IView[] { firstRoot, firstDetail }, firstManager.NavigationStack);
					Assert.Same(firstHandler, firstRoot.Handler);

					tabs.CurrentItem = tabs.Items[1];
					await WaitForPage(second, secondDetail);
					Assert.Same(secondDetail, shell.CurrentPage);
					Assert.Same(secondManager, GetNavigationManager(second));
					Assert.Equal(new IView[] { secondRoot, secondDetail }, secondManager.NavigationStack);
				}

				await shell.Navigation.PopAsync(false);
				await WaitForPage(second, secondRoot);
				Assert.Single(secondManager.NavigationStack);
				Assert.Equal(1, first.CreationCount);
				Assert.Equal(1, second.CreationCount);
				Assert.Equal(0, contents[2][0].CreationCount);
			});
		}

		[Fact]
		public async Task TopTabSelectionIsLazyAndCancelledSelectionDoesNotCreateContent()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 3, 0, 1));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var section = tabs.Items[0];
				var handler = Assert.IsType<ShellSectionHandler>(section.Handler);
				var selected = contents[0][1];
				await WaitForPage(selected, selected.CreatedPage!);
				Assert.Equal(0, contents[0][2].CreationCount);

				void CancelNavigation(object? sender, ShellNavigatingEventArgs args) => args.Cancel();
				shell.Navigating += CancelNavigation;
				try
				{
					handler.ContentTabLayout!.GetTabAt(2)!.Select();
					await AssertEventually(() => handler.ViewPager!.CurrentItem == 1);
					Assert.Same(selected, section.CurrentItem);
					Assert.Equal(0, contents[0][2].CreationCount);
				}
				finally
				{
					shell.Navigating -= CancelNavigation;
				}

				handler.ContentTabLayout!.GetTabAt(2)!.Select();
				var next = contents[0][2];
				await AssertEventually(() => next.CreatedPage is not null);
				await WaitForPage(next, next.CreatedPage!);
				handler.ContentTabLayout.GetTabAt(1)!.Select();
				await WaitForPage(selected, selected.CreatedPage!);
				handler.ContentTabLayout.GetTabAt(2)!.Select();
				await WaitForPage(next, next.CreatedPage!);

				Assert.Equal(0, contents[0][0].CreationCount);
				Assert.Equal(1, selected.CreationCount);
				Assert.Equal(1, next.CreationCount);
			});
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task TopContentSelectionReconcilesTheSharedSectionStack(bool previouslyVisited)
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 2));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var section = tabs.Items[0];
				var first = contents[0][0];
				var second = contents[0][1];
				await WaitForPage(first, first.CreatedPage!);

				if (previouslyVisited)
				{
					section.CurrentItem = second;
					await AssertEventually(() => second.CreatedPage is not null);
					await WaitForPage(second, second.CreatedPage!);
					section.CurrentItem = first;
					await WaitForPage(first, first.CreatedPage!);
				}

				var detail = new ContentPage { Title = "Shared detail" };
				await shell.Navigation.PushAsync(detail, false);
				await WaitForPage(first, detail);

				section.CurrentItem = second;
				await WaitForPage(second, detail);
				Assert.Same(detail, shell.CurrentPage);
				Assert.Equal(new Page[] { null!, detail }, section.Stack);
				Assert.Equal(new IView[] { second.CreatedPage!, detail },
					GetNavigationManager(second).NavigationStack);
				await AssertPageHostedBy(second, detail);

				section.CurrentItem = first;
				await WaitForPage(first, detail);
				await AssertPageHostedBy(first, detail);
				section.CurrentItem = second;
				await WaitForPage(second, detail);
				await AssertPageHostedBy(second, detail);

				Assert.Same(detail, await shell.Navigation.PopAsync(false));
				await WaitForPage(second, second.CreatedPage!);
				Assert.Same(second.CreatedPage, shell.CurrentPage);
				Assert.Single(section.Stack);
				Assert.Single(GetNavigationManager(second).NavigationStack);

				section.CurrentItem = first;
				await WaitForPage(first, first.CreatedPage!);
				Assert.Same(first.CreatedPage, shell.CurrentPage);
				Assert.Single(GetNavigationManager(first).NavigationStack);
				await AssertPageHostedBy(first, first.CreatedPage!);
				Assert.Equal(1, first.CreationCount);
				Assert.Equal(1, second.CreationCount);
			});
		}

		static Task AssertPageHostedBy(ShellContent content, Page page) =>
			AssertEventually(() =>
				page.Handler?.PlatformView is global::Android.Views.View view &&
				view.Parent == GetNavigationManager(content).NavHost.View,
				timeout: 3000, message: "The selected content does not own the displayed native page.");

		[Fact]
		public async Task RapidPushAndPopRequestsEachAwaitTheirOwnNativeCompletion()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 1));
			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var content = contents[0][0];
				var root = content.CreatedPage!;
				await WaitForPage(content, root);
				var pages = new[]
				{
					new ContentPage { Title = "First queued page" },
					new ContentPage { Title = "Second queued page" },
					new ContentPage { Title = "Third queued page" }
				};
				var pushes = pages.Select(page => shell.Navigation.PushAsync(page, false)).ToArray();
				Assert.All(pushes, task => Assert.False(task.IsCompleted));
				await Task.WhenAll(pushes).WaitAsync(TimeSpan.FromSeconds(10));
				await WaitForPage(content, pages[2]);
				Assert.Equal(new IView[] { root, pages[0], pages[1], pages[2] },
					GetNavigationManager(content).NavigationStack);

				var pops = new[]
				{
					shell.Navigation.PopAsync(false),
					shell.Navigation.PopAsync(false),
					shell.Navigation.PopAsync(false)
				};
				Assert.All(pops, task => Assert.False(task.IsCompleted));
				var popped = await Task.WhenAll(pops).WaitAsync(TimeSpan.FromSeconds(10));
				Assert.Equal(new Page[] { pages[2], pages[1], pages[0] }, popped);
				await WaitForPage(content, root);
				Assert.False(GetNavigationManager(content).HasPendingNavigation);
			});
		}

		[Fact]
		public async Task AppearingCanQueueAnotherPushWithoutLosingEitherCompletion()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 1));
			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var content = contents[0][0];
				await WaitForPage(content, content.CreatedPage!);
				var first = new ContentPage { Title = "Reentrant first" };
				var second = new ContentPage { Title = "Reentrant second" };
				Task? nestedPush = null;
				first.Appearing += (_, _) => nestedPush ??= shell.Navigation.PushAsync(second, false);

				var firstPush = shell.Navigation.PushAsync(first, false);
				Assert.NotNull(nestedPush);
				await Task.WhenAll(firstPush, nestedPush!).WaitAsync(TimeSpan.FromSeconds(10));
				await WaitForPage(content, second);
				Assert.Equal(new IView[] { content.CreatedPage!, first, second },
					GetNavigationManager(content).NavigationStack);
				Assert.False(GetNavigationManager(content).HasPendingNavigation);
			});
		}

		[Fact]
		public async Task SwitchingContentDuringPushCancelsTheOldHostAndReconcilesTheNewOne()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 2));
			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var first = contents[0][0];
				var second = contents[0][1];
				await WaitForPage(first, first.CreatedPage!);
				var detail = new ContentPage { Title = "Transferred pending detail" };
				var push = shell.Navigation.PushAsync(detail, false);

				tabs.Items[0].CurrentItem = second;

				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => push);
				await WaitForPage(second, detail);
				await AssertPageHostedBy(second, detail);
				Assert.Same(detail, shell.CurrentPage);
				await shell.Navigation.PopAsync(false);
				await WaitForPage(second, second.CreatedPage!);
			});
		}

		[Fact]
		public async Task DisconnectCancelsActiveAndQueuedNavigationWithoutReportingSuccess()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 1));
			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var content = contents[0][0];
				var root = content.CreatedPage!;
				await WaitForPage(content, root);
				var manager = GetNavigationManager(content);
				var first = new ContentPage { Title = "Interrupted first" };
				var second = new ContentPage { Title = "Interrupted second" };
				var active = manager.RequestNavigationAsync(new NavigationRequest(new IView[] { root, first }, false));
				var queued = manager.RequestNavigationAsync(new NavigationRequest(new IView[] { root, first, second }, false));
				Assert.False(active.IsCompleted);
				Assert.False(queued.IsCompleted);

				manager.Disconnect();

				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active);
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
				Assert.False(manager.HasPendingNavigation);
				Assert.Null(manager.OnResumeRequestedArgs);
				Assert.Empty(manager.NavigationStack);
			});
		}

		[Fact]
		public async Task CompletionCallbacksCannotOvertakeAlreadyQueuedRequests()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 1));
			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var content = contents[0][0];
				await WaitForPage(content, content.CreatedPage!);
				var manager = GetNavigationManager(content);
				var container = Assert.IsType<AndroidX.Fragment.App.FragmentContainerView>(GetFragment(content).View);
				var navigationView = new TrackingNavigationView { Title = "Queue root" };
				manager.Disconnect();
				manager.Connect(navigationView, container);
				try
				{
					await manager.RequestNavigationAsync(new NavigationRequest(new IView[] { navigationView }, false))
						.WaitAsync(TimeSpan.FromSeconds(10));
					var first = new ContentPage { Title = "First operation" };
					var second = new ContentPage { Title = "Already queued operation" };
					var third = new ContentPage { Title = "Reentrant operation" };
					var completed = new List<IView>();
					Task? reentrant = null;
					navigationView.Completed = stack =>
					{
						completed.Add(stack[stack.Count - 1]);
						if (stack[stack.Count - 1] == first)
							reentrant = manager.RequestNavigationAsync(
								new NavigationRequest(new IView[] { navigationView, first, second, third }, false));
					};

					var firstTask = manager.RequestNavigationAsync(
						new NavigationRequest(new IView[] { navigationView, first }, false));
					var secondTask = manager.RequestNavigationAsync(
						new NavigationRequest(new IView[] { navigationView, first, second }, false));
					await Task.WhenAll(firstTask, secondTask).WaitAsync(TimeSpan.FromSeconds(10));
					Assert.NotNull(reentrant);
					await reentrant!.WaitAsync(TimeSpan.FromSeconds(10));

					Assert.Equal(new IView[] { first, second, third }, completed);
					Assert.Same(third, manager.CurrentPage);
					Assert.False(manager.HasPendingNavigation);
				}
				finally
				{
					manager.Disconnect();
					navigationView.DisconnectHandlers();
				}
			});
		}

		[Fact]
		public async Task SectionViewRecreationReleasesAdaptersAndRetainsManagedPages()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(1, 2));
			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var content = contents[0][0];
				var root = content.CreatedPage!;
				await WaitForPage(content, root);
				var sectionHandler = Assert.IsType<ShellSectionHandler>(tabs.Items[0].Handler);
				var oldAdapter = sectionHandler.ViewPager!.Adapter;
				var wrapper = GetFragment(content).ParentFragment!;
				var fragmentManager = wrapper.ParentFragmentManager;

				fragmentManager.BeginTransaction().Detach(wrapper).CommitNow();
				Assert.Null(sectionHandler.ViewPager.Adapter);
				Assert.Null(sectionHandler.ContentTabLayout);
				fragmentManager.BeginTransaction().Attach(wrapper).CommitNow();

				try
				{
					await WaitForFragments(tabs);
				}
				catch (Xunit.Sdk.XunitException)
				{
					Console.WriteLine($"Recreated section: added={wrapper.IsAdded}, detached={wrapper.IsDetached}, resumed={wrapper.IsResumed}, view={wrapper.View?.Id}, pagerAttached={sectionHandler.ViewPager.IsAttachedToWindow}, pagerSize={sectionHandler.ViewPager.Width}x{sectionHandler.ViewPager.Height}, items={sectionHandler.ViewPager.Adapter?.ItemCount}");
					foreach (var fragment in wrapper.ChildFragmentManager.Fragments)
						Console.WriteLine($"Content: tag={fragment.Tag}, added={fragment.IsAdded}, detached={fragment.IsDetached}, resumed={fragment.IsResumed}, view={fragment.View?.Id}");
					throw;
				}
				await WaitForPage(content, root);
				Assert.NotSame(oldAdapter, sectionHandler.ViewPager.Adapter);
				Assert.Equal(1, content.CreationCount);
				Assert.Equal(0, contents[0][1].CreationCount);
				Assert.Same(root, shell.CurrentPage);
			});
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task FirstSelectionInitializesContentBeforePagerAnimationSettles(bool topTab)
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(2, 2));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var selected = topTab ? contents[0][1] : contents[1][0];
				Assert.Equal(0, selected.CreationCount);

				if (topTab)
					tabs.Items[0].CurrentItem = selected;
				else
					tabs.CurrentItem = tabs.Items[1];

				// The destination must already have a host, not wait for VP2's resumed lifecycle.
				var host = Assert.IsType<MauiNavHostFragment>(
					Assert.Single(GetFragment(selected).ChildFragmentManager.Fragments));
				Assert.NotNull(host.StackNavigationManager);
				Assert.Equal(1, selected.CreationCount);
				await WaitForPage(selected, selected.CreatedPage!);
			});
		}

		[Fact]
		public async Task ChangingContentInAnInactiveSectionDoesNotInitializeItsPages()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(2, 2));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var inactive = contents[1];
				Assert.All(inactive, content => Assert.Equal(0, content.CreationCount));
				tabs.Items[1].CurrentItem = inactive[1];
				await AssertEventually(() =>
					((ShellSectionHandler)tabs.Items[1].Handler!).ViewPager!.CurrentItem == 1);
				Assert.All(inactive, content =>
				{
					Assert.Equal(0, content.CreationCount);
					Assert.Empty(GetFragment(content).ChildFragmentManager.Fragments);
				});
			});
		}

		[Fact]
		public async Task NavigationBeforeFirstSelectionIsReconciledWithoutDuplicatePushes()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(2, 1));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var second = contents[1][0];
				var firstDetail = new ContentPage { Title = "First queued detail" };
				var secondDetail = new ContentPage { Title = "Second queued detail" };
				await tabs.Items[1].Navigation.PushAsync(firstDetail, false);
				await tabs.Items[1].Navigation.PushAsync(secondDetail, false);
				Assert.Equal(0, second.CreationCount);

				tabs.CurrentItem = tabs.Items[1];
				await WaitForPage(second, secondDetail);
				var root = Assert.IsType<TrackingPage>(second.CreatedPage);
				Assert.Equal(new IView[] { root, firstDetail, secondDetail },
					GetNavigationManager(second).NavigationStack);

				await shell.Navigation.PopAsync(false);
				await WaitForPage(second, firstDetail);
				await shell.Navigation.PopAsync(false);
				await WaitForPage(second, root);
				Assert.Equal(1, second.CreationCount);
			});
		}

		[Fact]
		public async Task InactiveTabChangesAndOverflowDoNotCreatePages()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(5, 1));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var inactive = tabs.Items[2];
				inactive.IsVisible = false;
				await WaitForFragments(tabs);
				inactive.IsVisible = true;
				var added = new LazyContent("Added");
				var addedTab = new Tab { Title = "Added tab", Route = "added", Items = { added } };
				tabs.Items.Insert(1, addedTab);
				var extraContent = new LazyContent("Extra");
				tabs.Items[3].Items.Add(extraContent);
				await WaitForFragments(tabs);

				inactive.Title = "Renamed inactive tab";
				inactive.BadgeText = "7";
				inactive.IsEnabled = false;
				var itemHandler = Assert.IsType<ShellItemHandler>(tabs.Handler);
				var bottomTabs = itemHandler.BottomNavigationView!;
				Assert.Equal(inactive.Title, bottomTabs.Menu.GetItem(3)!.TitleFormatted!.ToString());
				Assert.Equal("7", bottomTabs.GetBadge(3)!.Text);
				Assert.False(bottomTabs.Menu.GetItem(3)!.IsEnabled);
				inactive.IsEnabled = true;

				Assert.All(contents.Skip(1).SelectMany(section => section),
					content => Assert.Equal(0, content.CreationCount));
				Assert.Equal(0, added.CreationCount);
				Assert.Equal(0, extraContent.CreationCount);

				var overflow = contents[4][0];
				await shell.GoToAsync("//tabs/tab4/content4_0", false);
				await AssertEventually(() => overflow.CreatedPage is not null);
				await WaitForPage(overflow, overflow.CreatedPage!);
				Assert.Equal(1, overflow.CreationCount);

				tabs.Items.Remove(addedTab);
				tabs.Items[2].Items.Remove(extraContent);
				await WaitForFragments(tabs);
				Assert.Equal(0, added.CreationCount);
				Assert.Equal(0, extraContent.CreationCount);
				Assert.Same(overflow.CreatedPage, shell.CurrentPage);
			});
		}

		[Fact]
		public async Task InactiveViewRecreationAndActiveResumePreserveLazyInitialization()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(2, 1));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var first = contents[0][0];
				var second = contents[1][0];
				Assert.Equal(0, second.CreationCount);
				await WaitForPage(first, first.CreatedPage!);
				var manager = GetNavigationManager(first);
				var activeFragment = GetFragment(first);
				activeFragment.ParentFragmentManager.BeginTransaction()
					.SetMaxLifecycle(activeFragment, Lifecycle.State.Started!)
					.CommitNow();
				activeFragment.ParentFragmentManager.BeginTransaction()
					.SetMaxLifecycle(activeFragment, Lifecycle.State.Resumed!)
					.CommitNow();
				await WaitForPage(first, first.CreatedPage!);
				Assert.Same(manager, GetNavigationManager(first));

				var inactiveFragment = GetFragment(second);
				second.IsVisible = false;
				await WaitForFragments(tabs);
				second.IsVisible = true;
				await WaitForFragments(tabs);
				var recreatedFragment = GetFragment(second);
				Assert.NotSame(inactiveFragment, recreatedFragment);
				Assert.Equal(0, second.CreationCount);
				Assert.Empty(recreatedFragment.ChildFragmentManager.Fragments);

				tabs.CurrentItem = tabs.Items[1];
				await AssertEventually(() => second.CreatedPage is not null);
				await WaitForPage(second, second.CreatedPage!);
				Assert.Equal(1, first.CreationCount);
				Assert.Equal(1, second.CreationCount);
			});
		}

		[Fact]
		public async Task RecreatingVisitedContentRetainsItsRootAndNavigationStack()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(2, 1));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var first = contents[0][0];
				var second = contents[1][0];
				Assert.Equal(0, second.CreationCount);
				tabs.CurrentItem = tabs.Items[1];
				await AssertEventually(() => second.CreatedPage is not null);
				var root = second.CreatedPage!;
				await WaitForPage(second, root);
				var detail = new ContentPage { Title = "Retained detail" };
				await shell.Navigation.PushAsync(detail, false);
				await WaitForPage(second, detail);
				var manager = GetNavigationManager(second);

				tabs.CurrentItem = tabs.Items[0];
				await WaitForPage(first, first.CreatedPage!);
				var fragment = GetFragment(second);
				second.IsVisible = false;
				await WaitForFragments(tabs);
				second.IsVisible = true;
				await WaitForFragments(tabs);
				Assert.NotSame(fragment, GetFragment(second));

				tabs.CurrentItem = tabs.Items[1];
				await WaitForPage(second, detail);
				Assert.NotSame(manager, GetNavigationManager(second));
				Assert.Equal(new IView[] { root, detail }, GetNavigationManager(second).NavigationStack);
				Assert.Same(detail, shell.CurrentPage);
				Assert.Equal(1, second.CreationCount);

				await shell.Navigation.PopAsync(false);
				await WaitForPage(second, root);
			});
		}

		[Fact]
		public async Task ReconnectingShellPreservesStackWithoutCreatingInactiveContent()
		{
			SetupBuilder();
			var (shell, tabs, contents) = await InvokeOnMainThreadAsync(() => CreateShell(2, 1));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await WaitForFragments(tabs);
				var first = contents[0][0];
				var second = contents[1][0];
				Assert.Equal(0, second.CreationCount);
				var root = first.CreatedPage!;
				var detail = new ContentPage { Title = "Reconnected detail" };
				await shell.Navigation.PushAsync(detail, false);
				await WaitForPage(first, detail);

				var window = shell.Window!;
				var handler = shell.Handler!;
				var placeholder = new ContentPage { Content = new Label { Text = "Detached Shell" } };
				window.Page = placeholder;
				await OnLoadedAsync(placeholder);
				handler.DisconnectHandler();
				window.Page = shell;
				await WaitForFragments(tabs);
				await WaitForPage(first, detail);

				Assert.NotSame(handler, shell.Handler);
				Assert.Equal(new IView[] { root, detail }, GetNavigationManager(first).NavigationStack);
				Assert.Equal(1, first.CreationCount);
				Assert.Equal(0, second.CreationCount);

				tabs.CurrentItem = tabs.Items[1];
				await AssertEventually(() => second.CreatedPage is not null);
				await WaitForPage(second, second.CreatedPage!);
				Assert.Equal(1, second.CreationCount);
			});
		}

		static (Shell Shell, TabBar Tabs, LazyContent[][] Contents) CreateShell(
			int sectionCount, int contentCount, int selectedSection = 0, int selectedContent = 0)
		{
			var tabs = new TabBar { Route = "tabs" };
			var contents = new LazyContent[sectionCount][];
			for (int i = 0; i < sectionCount; i++)
			{
				var section = new Tab { Title = $"Tab {i}", Route = $"tab{i}" };
				contents[i] = new LazyContent[contentCount];
				for (int j = 0; j < contentCount; j++)
				{
					var content = new LazyContent($"content{i}_{j}");
					contents[i][j] = content;
					section.Items.Add(content);
				}
				tabs.Items.Add(section);
			}
			tabs.CurrentItem = tabs.Items[selectedSection];
			tabs.CurrentItem.CurrentItem = contents[selectedSection][selectedContent];
			return (new Shell { Items = { tabs } }, tabs, contents);
		}

		static Task WaitForFragments(TabBar tabs) => AssertEventually(() =>
		{
			var sections = ((IShellItemController)tabs).GetItems();
			if (tabs.Handler is not ShellItemHandler itemHandler || itemHandler._adapter is null)
				return false;

			for (int i = 0; i < sections.Count; i++)
			{
				if (itemHandler._adapter.GetRenderer(i) is not IShellObservableFragment renderer)
					return false;
				var expected = ((IShellSectionController)sections[i]).GetItems().Count;
				var fragments = renderer.Fragment.ChildFragmentManager.Fragments
					.OfType<ShellContentNavigationFragment>().ToList();
				if (fragments.Count(fragment => fragment.View is not null) != expected)
					return false;
				foreach (var fragment in fragments)
				{
					var host = fragment.ChildFragmentManager.Fragments.OfType<MauiNavHostFragment>().SingleOrDefault();
					if (host?.StackNavigationManager?.NavigationStack.LastOrDefault() is Page page && !page.IsLoaded)
						return false;
				}
			}
			return true;
		}, timeout: 3000, message: "Shell did not create its cached content fragment views.");

		static ShellContentNavigationFragment GetFragment(ShellContent content)
		{
			var section = Assert.IsAssignableFrom<ShellSection>(content.Parent);
			var item = Assert.IsAssignableFrom<ShellItem>(section.Parent);
			var itemHandler = Assert.IsType<ShellItemHandler>(item.Handler);
			var sectionIndex = ((IShellItemController)item).GetItems().IndexOf(section);
			var renderer = Assert.IsAssignableFrom<IShellObservableFragment>(
				itemHandler._adapter!.GetRenderer(sectionIndex));
			var sectionHandler = Assert.IsType<ShellSectionHandler>(section.Handler);
			var adapter = Assert.IsType<ShellContentFragmentAdapter>(sectionHandler.ViewPager!.Adapter);
			var contentIndex = ((IShellSectionController)section).GetItems().IndexOf(content);
			return Assert.IsType<ShellContentNavigationFragment>(
				renderer.Fragment.ChildFragmentManager.FindFragmentByTag($"f{adapter.GetItemId(contentIndex)}"));
		}

		static StackNavigationManager GetNavigationManager(ShellContent content) =>
			Assert.IsType<MauiNavHostFragment>(GetFragment(content).ChildFragmentManager.Fragments.Single())
				.StackNavigationManager!;

		async Task WaitForPage(ShellContent content, Page page)
		{
			await AssertEventually(() =>
			{
				var fragment = GetFragment(content);
				var host = fragment.ChildFragmentManager.Fragments.OfType<MauiNavHostFragment>().SingleOrDefault();
				var manager = host?.StackNavigationManager;
				return fragment.IsResumed && manager is not null && !manager.IsNavigating &&
					manager.NavigationStack.LastOrDefault() == page;
			}, timeout: 3000, message: "Selected Shell content did not finish native navigation.");
			await OnLoadedAsync(page);
			await OnFrameSetToNotEmpty(page);
		}

		sealed class LazyContent : ShellContent
		{
			public int CreationCount { get; private set; }
			public TrackingPage? CreatedPage { get; private set; }

			public LazyContent(string name)
			{
				Title = Route = name;
				ContentTemplate = new DataTemplate(() =>
				{
					CreationCount++;
					return CreatedPage = new TrackingPage(name);
				});
			}
		}

		sealed class TrackingPage : ContentPage
		{
			public int AppearingCount { get; private set; }

			public TrackingPage(string title)
			{
				Title = title;
				Content = new Label { Text = title };
			}

			protected override void OnAppearing()
			{
				base.OnAppearing();
				AppearingCount++;
			}
		}

		sealed class TrackingNavigationView : ContentPage, IStackNavigationView
		{
			internal Action<IReadOnlyList<IView>>? Completed { get; set; }

			void IStackNavigation.RequestNavigation(NavigationRequest request) =>
				throw new NotSupportedException("Tests submit requests directly to the native manager.");

			void IStackNavigation.NavigationFinished(IReadOnlyList<IView> newStack) =>
				Completed?.Invoke(newStack);
		}
	}
}
