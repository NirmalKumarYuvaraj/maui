using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Platform.Compatibility;
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

		[Fact]
		public async Task ExplicitLegacyRendererKeepsPageFragmentFactory()
		{
			EnsureHandlerCreated(builder => builder.ConfigureMauiHandlers(handlers =>
			{
				SetupShellHandlers(handlers);
				handlers.AddHandler<Shell, PageFactoryTrackingShellRenderer>();
				handlers.AddAndroidShellHandlers(true);
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

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void RolloutSelectionPreservesExplicitRegistrations(bool useHandler)
		{
			var defaults = new MauiHandlersCollection();
			defaults.AddAndroidShellHandlers(useHandler);
			var factory = new MauiHandlersFactory(defaults);
			var shellHandler = factory.GetHandler(typeof(Shell));
			if (useHandler)
			{
				Assert.IsType<ShellHandler>(shellHandler);
				Assert.IsType<ShellItemHandler>(factory.GetHandler(typeof(ShellItem)));
				Assert.IsType<ShellSectionHandler>(factory.GetHandler(typeof(ShellSection)));
			}
			else
			{
				Assert.IsType<Controls.Handlers.Compatibility.ShellRenderer>(shellHandler);
				Assert.Null(factory.GetHandlerType(typeof(ShellItem)));
				Assert.Null(factory.GetHandlerType(typeof(ShellSection)));
			}

			var explicitRegistrations = new MauiHandlersCollection();
			explicitRegistrations.AddHandler<Shell, ItemFactoryTrackingShellHandler>();
			explicitRegistrations.AddHandler<ShellItem, FirstItemHandler>();
			explicitRegistrations.AddAndroidShellHandlers(useHandler);
			factory = new MauiHandlersFactory(explicitRegistrations);
			Assert.IsType<ItemFactoryTrackingShellHandler>(factory.GetHandler(typeof(Shell)));
			Assert.IsType<FirstItemHandler>(factory.GetHandler(typeof(ShellItem)));
		}

		public class FirstItem : FlyoutItem { }
		public class SecondItem : FlyoutItem { }
		public class FirstItemHandler : ShellItemHandler { }
		public class SecondItemHandler : ShellItemHandler { }

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
