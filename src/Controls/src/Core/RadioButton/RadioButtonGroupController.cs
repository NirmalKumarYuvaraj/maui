#nullable disable
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Maui.Controls
{
	internal class RadioButtonGroupController
	{
		private static readonly ConditionalWeakTable<Element, RadioButtonGroupController> GroupControllers = new();
		private readonly Element _layout;
		private string _groupName;
		private object _selectedValue;

		public string GroupName { get => _groupName; set => SetGroupName(value); }
		public object SelectedValue { get => _selectedValue; set => SetSelectedValue(value); }

		public RadioButtonGroupController(Maui.ILayout layout)
		{
			if (layout is null)
			{
				throw new ArgumentNullException(nameof(layout));
			}

			_layout = (Element)layout;
			_layout.ChildAdded += ChildAdded;

			if (!string.IsNullOrEmpty(_groupName))
			{
				UpdateGroupNames(_layout, _groupName);
			}

			GroupControllers.Add(_layout, this);
		}

		public static RadioButtonGroupController GetController(Element layout)
		{
			return GroupControllers.TryGetValue(layout, out var controller) ? controller : null;
		}

		public void NotifySelectionChanged(RadioButton selected)
		{
			if (selected.GroupName != _groupName)
			{
				return;
			}

			_layout.SetValue(RadioButtonGroup.SelectedValueProperty, selected.Value);
		}

		void ChildAdded(object sender, ElementEventArgs e)
		{
			if (string.IsNullOrEmpty(_groupName))
			{
				return;
			}

			if (e.Element is RadioButton radioButton)
			{
				AddRadioButton(radioButton);
			}
			else
			{
				foreach (var element in e.Element.Descendants())
				{
					if (element is RadioButton radioButton1)
					{
						AddRadioButton(radioButton1);
					}
				}
			}
		}

		void AddRadioButton(RadioButton radioButton)
		{
			UpdateGroupName(radioButton, _groupName);

			if (radioButton.IsChecked)
			{
				_layout.SetValue(RadioButtonGroup.SelectedValueProperty, radioButton.Value);
			}

			if (object.Equals(radioButton.Value, this.SelectedValue))
			{
				radioButton.SetValue(RadioButton.IsCheckedProperty, true, specificity: SetterSpecificity.FromHandler);
			}
		}

		void UpdateGroupName(Element element, string name, string oldName = null)
		{
			if (!(element is RadioButton radioButton))
			{
				return;
			}

			var currentName = radioButton.GroupName;

			if (string.IsNullOrEmpty(currentName) || currentName == oldName)
			{
				radioButton.GroupName = name;
			}
		}

		void UpdateGroupNames(Element element, string name, string oldName = null)
		{
			foreach (Element descendant in element.Descendants())
			{
				UpdateGroupName(descendant, name, oldName);
			}
		}

		void SetSelectedValue(object radioButtonValue)
		{
			_selectedValue = radioButtonValue;
		}

		void SetGroupName(string groupName)
		{
			var oldName = _groupName;
			_groupName = groupName;
			UpdateGroupNames(_layout, _groupName, oldName);
		}
	}
}