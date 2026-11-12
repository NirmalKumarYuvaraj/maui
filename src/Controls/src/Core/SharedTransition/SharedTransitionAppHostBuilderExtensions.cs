using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;

namespace Microsoft.Maui.Controls.Hosting
{
	/// <summary>Extensions for configuring shared-element transitions.</summary>
	public static class SharedTransitionAppHostBuilderExtensions
	{
		/// <summary>Configures app-wide defaults for shared-element transitions.</summary>
		/// <param name="builder">The app builder.</param>
		/// <param name="configure">Delegate that updates the <see cref="SharedTransitionOptions"/>.</param>
		/// <returns>The same <paramref name="builder"/>, for chaining.</returns>
		public static MauiAppBuilder ConfigureSharedTransitions(this MauiAppBuilder builder, Action<SharedTransitionOptions> configure)
		{
			_ = builder ?? throw new ArgumentNullException(nameof(builder));
			_ = configure ?? throw new ArgumentNullException(nameof(configure));

			builder.Services.AddSingleton(new SharedTransitionOptionsConfiguration(configure));
			builder.Services.TryAddSingleton(services =>
			{
				var options = new SharedTransitionOptions();
				foreach (var configuration in services.GetServices<SharedTransitionOptionsConfiguration>())
					configuration.Configure(options);
				return options;
			});

			return builder;
		}

		internal sealed class SharedTransitionOptionsConfiguration
		{
			public SharedTransitionOptionsConfiguration(Action<SharedTransitionOptions> configure) => Configure = configure;

			public Action<SharedTransitionOptions> Configure { get; }
		}
	}
}
