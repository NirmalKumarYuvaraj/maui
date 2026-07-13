#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// Sets a property value within a <see cref="Style"/> or <see cref="TriggerBase"/>.
	/// </summary>
	[ContentProperty(nameof(Value))]
	[ProvideCompiled("Microsoft.Maui.Controls.XamlC.SetterValueProvider")]
	[RequireService(
		[typeof(IValueConverterProvider),
		 typeof(IXmlLineInfoProvider)])]
	public sealed class Setter : IValueProvider
	{
		/// <summary>
		/// Gets or sets the name of the element to which the setter applies.
		/// </summary>
		public string TargetName { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="BindableProperty"/> to set.
		/// </summary>
		public BindableProperty Property { get; set; }

		/// <summary>
		/// Gets or sets the value to apply to the property.
		/// </summary>
		public object Value { get; set; }

		// A single Setter (and therefore its Value) is shared across every target that applies it -- e.g. all
		// VisualElements using a given VisualState share the very same Setter instances (see VisualState.Clone(),
		// which reuses Setters rather than cloning them). When Value is itself an Element, handing the same shared
		// instance to multiple targets is unsafe: Element.Parent is a 1:1 relationship, and any live DynamicResource
		// resolution on that instance silently moves to whichever target most recently applied this Setter,
		// corrupting the state of every other target that previously received it (dotnet/maui#28606). This table
		// gives each target its own clone, created lazily and reused across repeated Apply/UnApply cycles.
		ConditionalWeakTable<BindableObject, Element> _clonedElementValues;

		object IValueProvider.ProvideValue(IServiceProvider serviceProvider)
		{
			if (Property == null)
				throw new XamlParseException("Property not set", serviceProvider);
			var valueconverter = serviceProvider.GetService(typeof(IValueConverterProvider)) as IValueConverterProvider;

			MemberInfo minforetriever()
			{
				MemberInfo minfo = null;
				try
				{
					minfo = Property.DeclaringType.GetRuntimeProperty(Property.PropertyName);
				}
				catch (AmbiguousMatchException e)
				{
					throw new XamlParseException($"Multiple properties with name '{Property.DeclaringType}.{Property.PropertyName}' found.", serviceProvider, innerException: e);
				}
				if (minfo != null)
					return minfo;
				try
				{
					return Property.DeclaringType.GetRuntimeMethod("Get" + Property.PropertyName, new[] { typeof(BindableObject) });
				}
				catch (AmbiguousMatchException e)
				{
					throw new XamlParseException($"Multiple methods with name '{Property.DeclaringType}.Get{Property.PropertyName}' found.", serviceProvider, innerException: e);
				}
			}

			object value = valueconverter.Convert(Value, Property.ReturnType, minforetriever, serviceProvider);
			Value = value;
			return this;
		}

		internal void Apply(BindableObject target, SetterSpecificity specificity)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			var targetObject = target;

			if (!string.IsNullOrEmpty(TargetName) && target is Element element)
				targetObject = FindTargetByName(element, TargetName) ?? throw new XamlParseException($"Cannot resolve '{TargetName}' as Setter Target for '{target}'.");

			if (Property == null)
				return;

			if (Value is BindingBase binding)
				targetObject.SetBinding(Property, binding.Clone(), specificity);
			else if (Value is DynamicResource dynamicResource)
				targetObject.SetDynamicResource(Property, dynamicResource.Key, specificity);
			else if (Value is IList<VisualStateGroup> visualStateGroupCollection)
				targetObject.SetValue(Property, visualStateGroupCollection.Clone(), specificity);
			else if (Value is Style style && (Property == StyleableElement.StyleProperty || Property == Span.StyleProperty))
			{
				// When setting a Style through a Setter (e.g., in VisualStateManager),
				// we need to call the Style's Apply method to ensure all its setters are applied
				((IStyle)style).Apply(targetObject, specificity);
			}
			else if (Value is Element sharedElement)
				targetObject.SetValue(Property, GetOrCreateClonedElementValue(sharedElement, targetObject), specificity: specificity);
			else
				targetObject.SetValue(Property, Value, specificity: specificity);
		}

		Element GetOrCreateClonedElementValue(Element sharedElement, BindableObject target)
		{
			_clonedElementValues ??= new ConditionalWeakTable<BindableObject, Element>();

			if (_clonedElementValues.TryGetValue(target, out var existingClone))
				return existingClone;

			var clone = CloneElementValue(sharedElement);
			_clonedElementValues.Add(target, clone);
			return clone;
		}

		[UnconditionalSuppressMessage("TrimAnalysis", "IL2072:UnrecognizedReflectionPattern",
			Justification = "Only used to clone a shared Element Setter value; if the value's type isn't preserved by " +
				"the trimmer (e.g. no parameterless constructor survives), we fall back to sharing the original instance.")]
		static Element CloneElementValue(Element source)
		{
			Element clone;
			try
			{
				clone = (Element)Activator.CreateInstance(source.GetType());
			}
			catch (Exception)
			{
				// The value type doesn't support being cloned (e.g. no parameterless constructor); fall back to the
				// previous (shared-instance) behavior rather than throwing.
				return source;
			}

			var dynamicResourceProperties = new HashSet<BindableProperty>(source.GetDynamicResourceProperties());

			using var enumerator = source.GetLocalValueEnumerator();
			while (enumerator.MoveNext())
			{
				var entry = enumerator.Current;
				var property = entry.Property;

				// DynamicResource-bound properties are re-wired below so the clone gets its own live
				// subscription, instead of just copying over the (possibly stale) last-resolved value.
				if (dynamicResourceProperties.Contains(property))
					continue;

				if (entry.Value is BindingBase binding)
				{
					clone.SetBinding(property, binding.Clone());
				}
				else
				{
					clone.SetValue(property, entry.Value);
				}
			}

			foreach (var property in dynamicResourceProperties)
			{
				if (source.TryGetDynamicResourceKey(property, out var key))
					clone.SetDynamicResource(property, key);
			}

			return clone;
		}

		internal void UnApply(BindableObject target, SetterSpecificity specificity)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			var targetObject = target;

			if (!string.IsNullOrEmpty(TargetName) && target is Element element)
				targetObject = FindTargetByName(element, TargetName) ?? throw new ArgumentNullException(nameof(targetObject));

			if (Property == null)
				return;
			if (Value is BindingBase binding)
				targetObject.RemoveBinding(Property, specificity);
			else if (Value is DynamicResource dynamicResource)
				targetObject.RemoveDynamicResource(Property, specificity);
			else if (Value is Style style && (Property == StyleableElement.StyleProperty || Property == Span.StyleProperty))
			{
				// When un-applying a Style that was set through a Setter,
				// we need to call the Style's UnApply method to properly clean up
				((IStyle)style).UnApply(targetObject);
				return;
			}
			targetObject.ClearValue(Property, specificity);
		}

		static BindableObject FindTargetByName(Element element, string name)
		{
			// Try standard lookup first (works for same or child namescopes)
			if (element.FindByName(name) is BindableObject target)
				return target;

			// Walk up parent tree to handle ControlTemplate namescope boundaries
			var current = element.Parent;
			while (current != null)
			{
				var namescope = current.GetNameScope();
				if (namescope?.FindByName(name) is BindableObject parentTarget)
					return parentTarget;

				current = current.Parent;
			}

			return null;
		}
	}
}
