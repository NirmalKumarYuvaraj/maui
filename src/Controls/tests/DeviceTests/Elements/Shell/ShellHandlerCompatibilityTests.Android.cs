using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Hosting.Internal;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	public partial class ShellHandlerTests_Shell
	{
		[Fact]
		public Task AndroidBackCommandConsumesRootBackOnlyWhileEnabled() =>
			InvokeOnMainThreadAsync(() =>
			{
				var calls = 0;
				var page = new ContentPage();
				var shell = new Shell { Items = { page } };
				var behavior = new BackButtonBehavior { Command = new Command(() => calls++) };
				Shell.SetBackButtonBehavior(page, behavior);

				Assert.True(Controls.Window.CanConsumeBackNavigation(shell));
				Assert.True(shell.SendBackButtonPressed());
				Assert.Equal(1, calls);

				behavior.IsEnabled = false;
				Assert.False(Controls.Window.CanConsumeBackNavigation(shell));
				Assert.False(shell.SendBackButtonPressed());
				Assert.Equal(1, calls);
			});

		void SetupAndroidShellHandlers() =>
			EnsureHandlerCreated(builder => builder.ConfigureMauiHandlers(handlers =>
			{
				SetupShellHandlers(handlers);
				handlers.AddHandler<Shell, ShellHandler>();
				handlers.AddHandler<ShellItem, ShellItemHandler>();
				handlers.AddHandler<ShellSection, ShellSectionHandler>();
			}));

		[Fact]
		public async Task HandlerBackClosesPresentedFlyoutBeforePopping()
		{
			SetupAndroidShellHandlers();
			var root = new ContentPage();
			var shell = await CreateShellAsync(shell =>
			{
				shell.FlyoutBehavior = FlyoutBehavior.Flyout;
				shell.Items.Add(root);
			});

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await OnLoadedAsync(root);
				var detail = new ContentPage();
				await shell.Navigation.PushAsync(detail, false);
				await OnLoadedAsync(detail);
				shell.FlyoutIsPresented = true;
				var itemHandler = Assert.IsType<ShellItemHandler>(shell.CurrentItem.Handler);

				Assert.True(itemHandler.OnBackButtonPressed());
				Assert.False(shell.FlyoutIsPresented);
				await Task.Delay(250);
				Assert.Same(detail, shell.CurrentPage);
				Assert.Equal(2, shell.Navigation.NavigationStack.Count);

				Assert.True(itemHandler.OnBackButtonPressed());
				await AssertionExtensions.AssertEventually(() => shell.CurrentPage == root);
			});
		}

		[Fact]
		public async Task HandlerBackRunsEnabledCommandInsteadOfPopping()
		{
			SetupAndroidShellHandlers();
			var root = new ContentPage();
			var shell = await CreateShellAsync(shell => shell.Items.Add(root));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await OnLoadedAsync(root);
				var calls = 0;
				var detail = new ContentPage();
				var behavior = new BackButtonBehavior { Command = new Command(() => calls++) };
				Shell.SetBackButtonBehavior(detail, behavior);
				await shell.Navigation.PushAsync(detail, false);
				await OnLoadedAsync(detail);
				var itemHandler = Assert.IsType<ShellItemHandler>(shell.CurrentItem.Handler);

				Assert.True(itemHandler.OnBackButtonPressed());
				Assert.Equal(1, calls);
				await Task.Delay(250);
				Assert.Same(detail, shell.CurrentPage);

				behavior.IsEnabled = false;
				Assert.True(itemHandler.OnBackButtonPressed());
				await AssertionExtensions.AssertEventually(() => shell.CurrentPage == root);
				Assert.Equal(1, calls);
			});
		}

		[Fact]
		public async Task ItemRendererRejectsSharedOrIncompatibleRegisteredHandlers()
		{
			var shared = new FirstItemHandler();
			EnsureHandlerCreated(builder => builder.ConfigureMauiHandlers(handlers =>
			{
				SetupShellHandlers(handlers);
				handlers.AddHandler<Shell, RendererFactoryShellHandler>();
				handlers.AddHandler<ShellItem, ShellItemHandler>();
				handlers.AddHandler<ShellSection, ShellSectionHandler>();
				handlers.AddHandler<FirstItem>(_ => shared);
				handlers.AddHandler<SecondItem>(_ => shared);
				handlers.AddHandler<ThirdItem>(_ => new LabelHandler());
			}));

			var firstPage = new ContentPage();
			var first = new FirstItem { Items = { new ShellSection { Items = { new ShellContent { Content = firstPage } } } } };
			var second = new SecondItem { Items = { new ShellSection { Items = { new ShellContent { Content = new ContentPage() } } } } };
			var third = new ThirdItem { Items = { new ShellSection { Items = { new ShellContent { Content = new ContentPage() } } } } };
			var shell = await CreateShellAsync(shell =>
			{
				shell.Items.Add(first);
				shell.Items.Add(second);
				shell.Items.Add(third);
				shell.CurrentItem = first;
			});

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await OnLoadedAsync(firstPage);
				Assert.Same(shared, first.Handler);
				var shellHandler = Assert.IsType<RendererFactoryShellHandler>(shell.Handler);

				// Main silently re-targeted the connected handler or replaced the registration.
				Assert.Throws<InvalidOperationException>(() => shellHandler.CreateItemRenderer(second));
				Assert.Same(first, ((IElementHandler)shared).VirtualView);
				Assert.Throws<InvalidOperationException>(() => shellHandler.CreateItemRenderer(third));
			});
		}

		[Fact]
		public async Task ExplicitLegacyRendererKeepsPageFragmentFactory()
		{
			EnsureHandlerCreated(builder => builder.ConfigureMauiHandlers(handlers =>
			{
				SetupShellHandlers(handlers);
				handlers.AddHandler<Shell, PageFactoryTrackingShellRenderer>();
			}));
			var page = new ContentPage();
			var shell = await CreateShellAsync(shell => shell.Items.Add(page));

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await OnLoadedAsync(page);
				var renderer = Assert.IsType<PageFactoryTrackingShellRenderer>(shell.Handler);
				Assert.True(renderer.SectionRendererCreations > 0);
				var previousCreations = renderer.PageFragmentCreations;
				var detail = new ContentPage();
				await shell.Navigation.PushAsync(detail, false);
				await OnLoadedAsync(detail);
				Assert.True(renderer.PageFragmentCreations > previousCreations);
				Assert.Same(detail, renderer.LastPageFragmentPage);
				Assert.NotNull(renderer.LastPageFragment);
				Assert.True(renderer.LastPageFragment.Fragment.IsAdded);
			});
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task SwitchingItemsResolvesDestinationHandlerAndFactoryHook(bool useFactories)
		{
			var firstFactoryCalls = 0;
			var secondFactoryCalls = 0;
			EnsureHandlerCreated(builder => builder.ConfigureMauiHandlers(handlers =>
			{
				SetupShellHandlers(handlers);
				handlers.AddHandler<Shell, ItemFactoryTrackingShellHandler>();
				handlers.AddHandler<ShellItem, ShellItemHandler>();
				handlers.AddHandler<ShellSection, ShellSectionHandler>();
				if (useFactories)
				{
					handlers.AddHandler<FirstItem>(_ =>
					{
						firstFactoryCalls++;
						return new FirstItemHandler();
					});
					handlers.AddHandler<SecondItem>(_ =>
					{
						secondFactoryCalls++;
						return new SecondItemHandler();
					});
				}
				else
				{
					handlers.AddHandler<FirstItem, FirstItemHandler>();
					handlers.AddHandler<SecondItem, SecondItemHandler>();
				}
			}));

			var firstPage = new ContentPage();
			var secondPage = new ContentPage();
			var first = new FirstItem { Items = { new ShellSection { Items = { new ShellContent { Content = firstPage } } } } };
			var second = new SecondItem { Items = { new ShellSection { Items = { new ShellContent { Content = secondPage } } } } };
			var shell = await CreateShellAsync(shell =>
			{
				shell.Items.Add(first);
				shell.Items.Add(second);
				shell.CurrentItem = first;
			});

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await OnLoadedAsync(firstPage);
				Assert.IsType<FirstItemHandler>(first.Handler);
				var shellHandler = Assert.IsType<ItemFactoryTrackingShellHandler>(shell.Handler);
				Assert.Equal(1, shellHandler.ItemRendererCreations);

				shell.CurrentItem = second;
				await OnLoadedAsync(secondPage);
				Assert.IsType<SecondItemHandler>(second.Handler);
				Assert.Equal(2, shellHandler.ItemRendererCreations);

				shell.CurrentItem = first;
				await OnLoadedAsync(firstPage);
				Assert.IsType<FirstItemHandler>(first.Handler);
				Assert.Equal(3, shellHandler.ItemRendererCreations);
				if (useFactories)
				{
					Assert.Equal(2, firstFactoryCalls);
					Assert.Equal(1, secondFactoryCalls);
				}
			});
		}

		[Fact]
		public void DefaultShellHandlersAreRegisteredAndExplicitRegistrationsTakePrecedence()
		{
			var defaults = new MauiHandlersCollection();
			defaults.AddControlsHandlers();
			var factory = new MauiHandlersFactory(defaults);
			Assert.IsType<ShellHandler>(factory.GetHandler(typeof(Shell)));
			Assert.IsType<ShellItemHandler>(factory.GetHandler(typeof(ShellItem)));
			Assert.IsType<ShellSectionHandler>(factory.GetHandler(typeof(ShellSection)));

			var explicitRegistrations = new MauiHandlersCollection();
			explicitRegistrations.AddControlsHandlers();
			explicitRegistrations.AddHandler<Shell, ItemFactoryTrackingShellHandler>();
			explicitRegistrations.AddHandler<ShellItem, FirstItemHandler>();
			factory = new MauiHandlersFactory(explicitRegistrations);
			Assert.IsType<ItemFactoryTrackingShellHandler>(factory.GetHandler(typeof(Shell)));
			Assert.IsType<FirstItemHandler>(factory.GetHandler(typeof(ShellItem)));
		}

		public class FirstItem : FlyoutItem { }
		public class SecondItem : FlyoutItem { }
		public class FirstItemHandler : ShellItemHandler { }
		public class SecondItemHandler : ShellItemHandler { }
		public class ThirdItem : FlyoutItem { }

		public class RendererFactoryShellHandler : ShellHandler
		{
			public IShellItemRenderer CreateItemRenderer(ShellItem shellItem) => CreateShellItemRenderer(shellItem);
		}

		public class ItemFactoryTrackingShellHandler : ShellHandler
		{
			public int ItemRendererCreations { get; private set; }

			protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem)
			{
				ItemRendererCreations++;
				return base.CreateShellItemRenderer(shellItem);
			}
		}

		public class PageFactoryTrackingShellRenderer : Controls.Handlers.Compatibility.ShellRenderer
		{
			public int SectionRendererCreations { get; private set; }
			public int PageFragmentCreations { get; private set; }
			public Page LastPageFragmentPage { get; private set; }
			public IShellObservableFragment LastPageFragment { get; private set; }

			protected override IShellSectionRenderer CreateShellSectionRenderer(ShellSection shellSection)
			{
				SectionRendererCreations++;
				return base.CreateShellSectionRenderer(shellSection);
			}

			protected override IShellObservableFragment CreateFragmentForPage(Page page)
			{
				PageFragmentCreations++;
				LastPageFragmentPage = page;
				return LastPageFragment = base.CreateFragmentForPage(page);
			}
		}
	}
}
