using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Fragment.App;
using AndroidX.Navigation;
using AndroidX.Navigation.Fragment;
using Java.Lang;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Xunit;
using AInsets = AndroidX.Core.Graphics.Insets;
using ATextAlignment = Android.Views.TextAlignment;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	public partial class NavigationViewHandlerTests
	{
		[Theory]
		[InlineData(false, false, (int)NavigationLayoutRegion.Content, (int)NavigationLayoutRegion.Content)]
		[InlineData(true, false, (int)NavigationLayoutRegion.AppBar, (int)NavigationLayoutRegion.Content)]
		[InlineData(false, true, (int)NavigationLayoutRegion.Content, (int)NavigationLayoutRegion.BottomTabs)]
		[InlineData(true, true, (int)NavigationLayoutRegion.AppBar, (int)NavigationLayoutRegion.BottomTabs)]
		public void NavigationLayoutResolvesInsetOwners(
			bool appBarHasContent,
			bool bottomTabsHaveContent,
			int expectedTopOwner,
			int expectedBottomOwner)
		{
			var owners = NavigationLayoutWindowInsetListener.ResolveOwners(
				appBarHasContent,
				bottomTabsHaveContent);

			Assert.Equal((NavigationLayoutRegion)expectedTopOwner, owners.Top);
			Assert.Equal((NavigationLayoutRegion)expectedBottomOwner, owners.Bottom);
		}

		[Fact]
		public Task NavigationLayoutUsesVisibleStructureForOwnership()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var region = new FrameLayout(MauiProgram.DefaultContext);
				var child = new AView(MauiProgram.DefaultContext);

				Assert.False(NavigationLayoutWindowInsetListener.HasVisibleContent(region));

				region.AddView(child);
				Assert.True(NavigationLayoutWindowInsetListener.HasVisibleContent(region));

				child.Visibility = ViewStates.Gone;
				Assert.False(NavigationLayoutWindowInsetListener.HasVisibleContent(region));

				var fragmentContainer = new FragmentContainerView(MauiProgram.DefaultContext);
				region.RemoveAllViews();
				region.AddView(fragmentContainer);
				Assert.False(NavigationLayoutWindowInsetListener.HasVisibleContent(region));

				fragmentContainer.AddView(child);
				child.Visibility = ViewStates.Visible;
				Assert.True(NavigationLayoutWindowInsetListener.HasVisibleContent(region));

				child.Visibility = ViewStates.Visible;
				region.Visibility = ViewStates.Gone;
				Assert.False(NavigationLayoutWindowInsetListener.HasVisibleContent(region));
			});
		}

		[Fact]
		public Task ExplicitSafeAreaAncestorBlocksOnlyImplicitDescendants()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var context = MauiProgram.DefaultContext;
				var explicitParent = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(hasExplicitSafeAreaEdges: true)
				};
				var implicitChild = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(hasExplicitSafeAreaEdges: false)
				};
				var explicitChild = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(hasExplicitSafeAreaEdges: true)
				};

				explicitParent.AddView(implicitChild);
				implicitChild.AddView(explicitChild);

				Assert.False(SafeAreaExtensions.ShouldApplySafeAreaInsets(
					implicitChild,
					(ISafeAreaView2)implicitChild.CrossPlatformLayout));
				Assert.True(SafeAreaExtensions.ShouldApplySafeAreaInsets(
					explicitChild,
					(ISafeAreaView2)explicitChild.CrossPlatformLayout));
			});
		}

		[Fact]
		public Task ExplicitDescendantInsideScrollViewRemainsEligible()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var context = MauiProgram.DefaultContext;
				var scrollView = new MauiScrollView(context);
				var container = new FrameLayout(context);
				var implicitChild = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(hasExplicitSafeAreaEdges: false)
				};
				var explicitChild = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(hasExplicitSafeAreaEdges: true)
				};

				scrollView.AddView(container);
				container.AddView(implicitChild);
				container.AddView(explicitChild);

				Assert.False(MauiWindowInsetListener.ShouldSetMauiWindowInsetListener(implicitChild));
				Assert.True(MauiWindowInsetListener.ShouldSetMauiWindowInsetListener(explicitChild));
			});
		}

		[Fact]
		public Task ZeroInsetsResetPreviouslyAppliedPadding()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var context = MauiProgram.DefaultContext;
				var view = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(
						hasExplicitSafeAreaEdges: true,
						safeAreaRegions: SafeAreaRegions.All)
				};
				var listener = new MauiWindowInsetListener();
				var systemBars = new WindowInsetsCompat.Builder()
					.SetInsets(WindowInsetsCompat.Type.SystemBars(), AInsets.Of(0, 40, 0, 20))
					.Build();
				var zeroInsets = new WindowInsetsCompat.Builder().Build();

				listener.OnApplyWindowInsets(view, systemBars);
				Assert.Equal(40, view.PaddingTop);
				Assert.Equal(20, view.PaddingBottom);

				listener.OnApplyWindowInsets(view, zeroInsets);
				Assert.Equal(0, view.PaddingTop);
				Assert.Equal(0, view.PaddingBottom);
			});
		}

		[Fact]
		public void BottomTabsConsumeBottomOnlyForContentRegion()
		{
			var manager = new WindowInsetsManager();
			var insets = new WindowInsetsCompat.Builder()
				.SetInsets(WindowInsetsCompat.Type.SystemBars(), AInsets.Of(10, 40, 10, 20))
				.Build();

			manager.Update(insets);
			var remaining = manager.BuildRemaining(
				NavigationLayoutWindowInsetListener.GetContentConsumedEdges(bottomTabsHaveContent: true));
			var remainingSystemBars = remaining.GetInsets(WindowInsetsCompat.Type.SystemBars());

			Assert.Equal(10, remainingSystemBars.Left);
			Assert.Equal(40, remainingSystemBars.Top);
			Assert.Equal(10, remainingSystemBars.Right);
			Assert.Equal(0, remainingSystemBars.Bottom);
			Assert.Equal(
				WindowInsetEdges.None,
				NavigationLayoutWindowInsetListener.GetContentConsumedEdges(bottomTabsHaveContent: false));
		}

		[Fact]
		public void WindowInsetsManagerTracksImeIndependently()
		{
			var manager = new WindowInsetsManager();
			var insets = new WindowInsetsCompat.Builder()
				.SetInsets(WindowInsetsCompat.Type.SystemBars(), AInsets.Of(0, 40, 0, 20))
				.SetInsets(WindowInsetsCompat.Type.Ime(), AInsets.Of(0, 0, 0, 300))
				.SetVisible(WindowInsetsCompat.Type.SystemBars(), true)
				.SetVisible(WindowInsetsCompat.Type.Ime(), true)
				.Build();

			manager.Update(insets);

			Assert.Equal(40, manager.Current.SystemBars.Top);
			Assert.Equal(20, manager.Current.SystemBars.Bottom);
			Assert.Equal(300, manager.Current.Ime.Bottom);
			Assert.True(manager.Current.SystemBarsVisible);
			Assert.True(manager.Current.ImeVisible);
		}

		[Fact]
		public void WindowInsetsManagerStartsWithEmptySnapshot()
		{
			var manager = new WindowInsetsManager();

			Assert.Equal(0, manager.GetSafeAreaInset(WindowInsetEdges.Left));
			Assert.Equal(0, manager.GetSafeAreaInset(WindowInsetEdges.Top));
			Assert.Equal(0, manager.GetSafeAreaInset(WindowInsetEdges.Right));
			Assert.Equal(0, manager.GetSafeAreaInset(WindowInsetEdges.Bottom));
			Assert.False(manager.Current.ImeVisible);
		}

		[Fact]
		public void VisibleZeroImeRemainsVisibleWhenNotConsumed()
		{
			var insets = new WindowInsetsCompat.Builder()
				.SetInsets(WindowInsetsCompat.Type.Ime(), AInsets.None)
				.SetVisible(WindowInsetsCompat.Type.Ime(), true)
				.Build();
			var remaining = WindowInsetsManager.BuildRemaining(
				insets,
				WindowInsetConsumption.None);

			Assert.True(remaining.IsVisible(WindowInsetsCompat.Type.Ime()));
		}

		[Fact]
		public void ConsumingImePreservesContainerInsets()
		{
			var insets = new WindowInsetsCompat.Builder()
				.SetInsets(WindowInsetsCompat.Type.SystemBars(), AInsets.Of(0, 40, 0, 20))
				.SetInsets(WindowInsetsCompat.Type.Ime(), AInsets.Of(0, 0, 0, 300))
				.SetVisible(WindowInsetsCompat.Type.SystemBars(), true)
				.SetVisible(WindowInsetsCompat.Type.Ime(), true)
				.Build();
			var remaining = WindowInsetsManager.BuildRemaining(
				insets,
				new WindowInsetConsumption(
					WindowInsetEdges.None,
					WindowInsetEdges.None,
					WindowInsetEdges.Bottom));

			Assert.Equal(
				AInsets.Of(0, 40, 0, 20),
				remaining.GetInsets(WindowInsetsCompat.Type.SystemBars()));
			Assert.Equal(
				AInsets.None,
				remaining.GetInsets(WindowInsetsCompat.Type.Ime()));
			Assert.True(remaining.IsVisible(WindowInsetsCompat.Type.SystemBars()));
			Assert.False(remaining.IsVisible(WindowInsetsCompat.Type.Ime()));
		}

		[Fact]
		public void ConsumingContainerPreservesIme()
		{
			var insets = new WindowInsetsCompat.Builder()
				.SetInsets(WindowInsetsCompat.Type.SystemBars(), AInsets.Of(0, 40, 0, 20))
				.SetInsets(WindowInsetsCompat.Type.Ime(), AInsets.Of(0, 0, 0, 300))
				.SetVisible(WindowInsetsCompat.Type.SystemBars(), true)
				.SetVisible(WindowInsetsCompat.Type.Ime(), true)
				.Build();
			var remaining = WindowInsetsManager.BuildRemaining(
				insets,
				WindowInsetConsumption.Container(WindowInsetEdges.Bottom));

			Assert.Equal(
				AInsets.Of(0, 40, 0, 0),
				remaining.GetInsets(WindowInsetsCompat.Type.SystemBars()));
			Assert.Equal(
				AInsets.Of(0, 0, 0, 300),
				remaining.GetInsets(WindowInsetsCompat.Type.Ime()));
			Assert.True(remaining.IsVisible(WindowInsetsCompat.Type.Ime()));
		}

		[Theory]
		[InlineData(20, 300, 300)]
		[InlineData(40, 20, 40)]
		public void AllUsesLargerContainerOrImeBottom(
			double containerBottom,
			double imeBottom,
			double expectedBottom)
		{
			var padding = SafeAreaExtensions.GetSafeAreaForEdge(
				SafeAreaRegions.All,
				containerBottom,
				edge: 3,
				isKeyboardShowing: true,
				new SafeAreaPadding(0, 0, 0, imeBottom));

			Assert.Equal(expectedBottom, padding);
		}

		[Fact]
		public Task OnlySoftInputOwnersParticipateInImeAnimation()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var context = MauiProgram.DefaultContext;
				var softInputView = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(
						hasExplicitSafeAreaEdges: true,
						safeAreaRegions: SafeAreaRegions.SoftInput)
				};
				var containerView = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(
						hasExplicitSafeAreaEdges: true,
						safeAreaRegions: SafeAreaRegions.Container)
				};

				Assert.True(SafeAreaExtensions.CanApplyImeInsets(softInputView));
				Assert.False(SafeAreaExtensions.CanApplyImeInsets(containerView));
			});
		}

		[Fact]
		public Task TopmostExplicitSoftInputViewOwnsImeAnimation()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var context = MauiProgram.DefaultContext;
				var parent = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(
						hasExplicitSafeAreaEdges: true,
						safeAreaRegions: SafeAreaRegions.All)
				};
				var child = new LayoutViewGroup(context)
				{
					CrossPlatformLayout = new SafeAreaLayoutStub(
						hasExplicitSafeAreaEdges: true,
						safeAreaRegions: SafeAreaRegions.SoftInput)
				};

				parent.AddView(child);

				Assert.True(SafeAreaExtensions.CanApplyImeInsets(parent));
				Assert.False(SafeAreaExtensions.CanApplyImeInsets(child));
			});
		}

		[Theory]
		[InlineData(126, 872, 0, 126)]
		[InlineData(126, 872, 0.5f, 499)]
		[InlineData(126, 872, 1, 872)]
		[InlineData(872, 126, 0, 872)]
		[InlineData(872, 126, 0.5f, 499)]
		[InlineData(872, 126, 1, 126)]
		public void ImePaddingInterpolationSupportsShowAndHide(
			int startPadding,
			int targetPadding,
			float fraction,
			int expectedPadding)
		{
			Assert.Equal(
				expectedPadding,
				ImeWindowInsetsCoordinator.CalculateAnimatedPadding(
					startPadding,
					targetPadding,
					fraction));
		}

		[Fact]
		public Task InsetListenerLifecycleClearsCoordinatorTag()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var view = new LayoutViewGroup(MauiProgram.DefaultContext);

				MauiWindowInsetListener.SetupViewWithLocalListener(view);
				Assert.IsType<MauiWindowInsetListener>(
					view.GetTag(Resource.Id.maui_window_inset_listener));

				MauiWindowInsetListener.RemoveViewWithLocalListener(view);
				Assert.Null(view.GetTag(Resource.Id.maui_window_inset_listener));
			});
		}

		sealed class SafeAreaLayoutStub : ICrossPlatformLayout, ISafeAreaView2
		{
			readonly SafeAreaRegions _safeAreaRegions;

			internal SafeAreaLayoutStub(
				bool hasExplicitSafeAreaEdges,
				SafeAreaRegions safeAreaRegions = SafeAreaRegions.None)
			{
				HasExplicitSafeAreaEdges = hasExplicitSafeAreaEdges;
				_safeAreaRegions = safeAreaRegions;
			}

			public bool HasExplicitSafeAreaEdges { get; }

			Thickness ISafeAreaView2.SafeAreaInsets { set { } }

			public SafeAreaRegions GetSafeAreaRegionsForEdge(int edge) => _safeAreaRegions;

			public Graphics.Size CrossPlatformMeasure(double widthConstraint, double heightConstraint) =>
				Graphics.Size.Zero;

			public Graphics.Size CrossPlatformArrange(Graphics.Rect bounds) => bounds.Size;
		}

		int GetNativeNavigationStackCount(NavigationViewHandler navigationViewHandler)
		{
			int i = 0;
			var navController = navigationViewHandler.StackNavigationManager.NavHost.NavController;
			navController.IterateBackStack(_ => i++);

			return i;
		}

		Task CreateNavigationViewHandlerAsync(IStackNavigationView navigationView, Func<NavigationViewHandler, Task> action)
		{
			return InvokeOnMainThreadAsync(async () =>
			{
				var context = MauiProgram.DefaultContext;

				var rootView = (context as AppCompatActivity).Window.DecorView as ViewGroup;
				var linearLayoutCompat = new LinearLayoutCompat(context);
				var fragmentManager = MauiContext.GetFragmentManager();
				var viewFragment = new NavViewFragment(MauiContext);

				try
				{
					linearLayoutCompat.Id = View.GenerateViewId();

					fragmentManager
						.BeginTransaction()

						.Add(linearLayoutCompat.Id, viewFragment)
						.Commit();

					rootView.AddView(linearLayoutCompat);
					await viewFragment.FinishedLoading;
					var handler = CreateHandler<NavigationViewHandler>(navigationView, viewFragment.ScopedMauiContext);

					if (navigationView is NavigationViewStub nvs && nvs.NavigationStack?.Count > 0)
					{
						navigationView.RequestNavigation(new NavigationRequest(nvs.NavigationStack, false));
						await nvs.OnNavigationFinished;
					}

					await action(handler);
				}
				finally
				{
					rootView.RemoveView(linearLayoutCompat);

					fragmentManager
						.BeginTransaction()
						.Remove(viewFragment)
						.Commit();
				}
			});
		}

		class NavViewFragment : Fragment
		{
			TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
			readonly IMauiContext _mauiContext;
			public IMauiContext ScopedMauiContext { get; set; }

			public Task FinishedLoading => _taskCompletionSource.Task;
			public NavViewFragment(IMauiContext mauiContext)
			{
				_mauiContext = mauiContext;
			}

			public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
			{
				ScopedMauiContext = _mauiContext.MakeScoped(layoutInflater: inflater, fragmentManager: ChildFragmentManager, registerNewNavigationRoot: true);
				return ScopedMauiContext.GetNavigationRootManager().RootView;
			}

			public override void OnResume()
			{
				base.OnResume();
				_taskCompletionSource.SetResult(true);
			}
		}
	}
}