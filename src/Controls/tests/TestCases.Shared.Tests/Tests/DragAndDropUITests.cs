﻿using Microsoft.Maui.Graphics;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests
{
	public class DragAndDropUITests : UITest
	{
		const string DragAndDropGallery = "Drag and Drop Gallery";
		public DragAndDropUITests(TestDevice device)
			: base(device)
		{
		}

		protected override void FixtureSetup()
		{
			base.FixtureSetup();
			App.NavigateToGallery(DragAndDropGallery);
		}

		// https://github.com/dotnet/maui/issues/24914
		
		[Test]
		[Category(UITestCategories.Gestures)]
		public void DragEvents()
		{
			App.WaitForElement("TargetView");
			App.EnterText("TargetView", "DragAndDropEvents");
			App.Tap("GoButton");

			App.WaitForElement("LabelDragElement");
			App.DragAndDrop("LabelDragElement", "DragTarget");

			App.WaitForElement("DragStartEventsLabel");
			var textAfterDragstart = App.FindElement("DragStartEventsLabel").GetText();

			if (string.IsNullOrEmpty(textAfterDragstart))
			{
				Assert.Fail("Text was expected: Drag start event");
			}
			else
			{
				Assert.That(textAfterDragstart, Is.EqualTo("DragStarting"));
			}

			App.WaitForElement("DragOverEventsLabel");
			var textAfterDragover = App.FindElement("DragOverEventsLabel").GetText();
			if (string.IsNullOrEmpty(textAfterDragover))
			{
				Assert.Fail("Text was expected: Drag over event");
			}
			else
			{
				Assert.That(textAfterDragover, Is.EqualTo("DragOver"));
			}

			App.WaitForElement("DragCompletedEventsLabel");
			var textAfterDragcomplete = App.FindElement("DragCompletedEventsLabel").GetText();
			if (string.IsNullOrEmpty(textAfterDragcomplete))
			{
				Assert.Fail("Text was expected: Drag complete event");
			}
			else
			{
				Assert.That(textAfterDragcomplete, Is.EqualTo("DropCompleted"));
			}
		}

		[Test]
		[Category(UITestCategories.Gestures)]
		public void DragAndDropBetweenLayouts()
		{
			App.WaitForElement("TargetView");
			App.EnterText("TargetView", "DragAndDropBetweenLayouts");
			App.Tap("GoButton");

			App.WaitForElement("ResetButton");
			App.Tap("ResetButton");

			App.WaitForElement("Red");
			App.WaitForElement("Green");
			App.DragAndDrop("Red", "Green");

			App.WaitForElement("DragStartEventsLabel");
			var textAfterDragstart = App.FindElement("DragStartEventsLabel").GetText();

			if (string.IsNullOrEmpty(textAfterDragstart))
			{
				Assert.Fail("Text was expected: Drag start event");
			}
			else
			{
				Assert.That(textAfterDragstart, Is.EqualTo("DragStarting"));
			}

			App.WaitForElement("DragOverEventsLabel");
			var textAfterDragover = App.FindElement("DragOverEventsLabel").GetText();
			if (string.IsNullOrEmpty(textAfterDragover))
			{
				Assert.Fail("Text was expected: Drag over event");
			}
			else
			{
				Assert.That(textAfterDragover, Is.EqualTo("DragOver"));
			}

			App.WaitForElement("DragCompletedEventsLabel");
			var textAfterDragcomplete = App.FindElement("DragCompletedEventsLabel").GetText();
			if (string.IsNullOrEmpty(textAfterDragcomplete))
			{
				Assert.Fail("Text was expected: Drag complete event");
			}
			else
			{
				Assert.That(textAfterDragcomplete, Is.EqualTo("DropCompleted"));
			}

			App.WaitForElement("RainBowColorsLabel");
			var rainbowColorText = App.FindElement("RainBowColorsLabel").GetText();
			if (string.IsNullOrEmpty(rainbowColorText))
			{
				Assert.Fail("Text was expected");
			}
			else
			{
				Assert.That(rainbowColorText, Is.EqualTo("RainbowColorsAdd:Red"));
			}
		}

		[Test]
		[Category(UITestCategories.Gestures)]
		public void PlatformDragEventArgs()
		{
			App.WaitForElement("TargetView");
			App.EnterText("TargetView", "DragAndDropEventArgs");
			App.Tap("GoButton");

			App.WaitForElement("LabelDragElement");
			App.DragAndDrop("LabelDragElement", "DragTarget");

			App.WaitForElement("DragStartEventsLabel");
			var textAfterDragstart = App.FindElement("DragStartEventsLabel").GetText();

			if (string.IsNullOrEmpty(textAfterDragstart))
			{
				Assert.Fail("Text was expected: Drag start event");
			}
			else
			{
				if (Device == TestDevice.iOS || Device == TestDevice.Mac)
				{
					Assert.That(textAfterDragstart.Contains("DragStarting:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragstart.Contains("DragStarting:DragInteraction", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragstart.Contains("DragStarting:DragSession", StringComparison.OrdinalIgnoreCase));
				}
				else if (Device == TestDevice.Android)
				{
					Assert.That(textAfterDragstart.Contains("DragStarting:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragstart.Contains("DragStarting:MotionEvent", StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					Assert.That(textAfterDragstart.Contains("DragStarting:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragstart.Contains("DragStarting:DragStartingEventArgs", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragstart.Contains("DragStarting:Handled", StringComparison.OrdinalIgnoreCase));
				}
			}

			App.WaitForElement("DragOverEventsLabel");
			var textAfterDragover = App.FindElement("DragOverEventsLabel").GetText();
			if (string.IsNullOrEmpty(textAfterDragover))
			{
				Assert.Fail("Text was expected: Drag over event");
			}
			else
			{
				if (Device == TestDevice.iOS || Device == TestDevice.Mac)
				{
					Assert.That(textAfterDragover.Contains("DragOver:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragover.Contains("DragOver:DropInteraction", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragover.Contains("DragOver:DropSession", StringComparison.OrdinalIgnoreCase));
				}
				else if (Device == TestDevice.Android)
				{
					Assert.That(textAfterDragover.Contains("DragOver:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragover.Contains("DragOver:DragEvent", StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					Assert.That(textAfterDragover.Contains("DragOver:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDragover.Contains("DragOver:DragEventArgs", StringComparison.OrdinalIgnoreCase));
				}
			}

			App.WaitForElement("DropCompletedEventsLabel");
			var textAfterDropcomplete = App.FindElement("DropCompletedEventsLabel").GetText();
			if (string.IsNullOrEmpty(textAfterDropcomplete))
			{
				Assert.Fail("Text was expected: Drop complete event");
			}
			else
			{
				if (Device == TestDevice.iOS || Device == TestDevice.Mac)
				{
					Assert.That(textAfterDropcomplete.Contains("DropCompleted:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDropcomplete.Contains("DropCompleted:DropInteraction", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDropcomplete.Contains("DropCompleted:DropSession", StringComparison.OrdinalIgnoreCase));
				}
				else if (Device == TestDevice.Android)
				{
					Assert.That(textAfterDropcomplete.Contains("DropCompleted:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDropcomplete.Contains("DropCompleted:DragEvent", StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					Assert.That(textAfterDropcomplete.Contains("DropCompleted:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDropcomplete.Contains("DropCompleted:DropCompletedEventArgs", StringComparison.OrdinalIgnoreCase));
				}
			}

			App.WaitForElement("DropEventsLabel");

			var textAfterDrop = App.FindElement("DropEventsLabel").GetText();

			if (string.IsNullOrEmpty(textAfterDrop))
			{
				Assert.Fail("Text was expected: drop event");
			}
			else
			{
				if (Device == TestDevice.iOS || Device == TestDevice.Mac)
				{
					Assert.That(textAfterDrop.Contains("Drop:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDrop.Contains("Drop:DropInteraction", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDrop.Contains("Drop:DropSession", StringComparison.OrdinalIgnoreCase));
				}
				else if (Device == TestDevice.Android)
				{
					Assert.That(textAfterDrop.Contains("Drop:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDrop.Contains("Drop:DragEvent", StringComparison.OrdinalIgnoreCase));

				}
				else
				{
					Assert.That(textAfterDrop.Contains("Drop:Sender", StringComparison.OrdinalIgnoreCase));
					Assert.That(textAfterDrop.Contains("Drop:DragEventArgs", StringComparison.OrdinalIgnoreCase));
				}
			}
		}

		[Test]
		[Category(UITestCategories.Gestures)]
		public void DragStartEventCoordinates()
		{
			App.WaitForElement("TargetView");
			App.EnterText("TargetView", "DragAndDropBetweenLayouts");
			App.Tap("GoButton");

			App.Tap("ResetButton");

			App.WaitForElement("Blue");
			App.WaitForElement("Green");
			App.DragAndDrop("Blue", "Green");

			var dragStartRelativeToSelf = GetCoordinatesFromLabel(App.FindElement("DragStartRelativeSelf").GetText());
			var dragStartRelativeToScreen = GetCoordinatesFromLabel(App.FindElement("DragStartRelativeScreen").GetText());
			var dragStartRelativeToLabel = GetCoordinatesFromLabel(App.FindElement("DragStartRelativeLabel").GetText());

			ClassicAssert.NotNull(dragStartRelativeToSelf);
			ClassicAssert.NotNull(dragStartRelativeToScreen);
			ClassicAssert.NotNull(dragStartRelativeToLabel);

			Assert.That(dragStartRelativeToSelf!.Value.X > 0 && dragStartRelativeToSelf!.Value.Y > 0);
			Assert.That(dragStartRelativeToScreen!.Value.X > 0 && dragStartRelativeToScreen!.Value.Y > 0);

			// The position of the drag relative to itself should be less than that relative to the screen
			// There are other elements in the screen, plus the ContentView of the test has some margin
			Assert.That(dragStartRelativeToSelf!.Value.X < dragStartRelativeToScreen!.Value.X);
			Assert.That(dragStartRelativeToSelf!.Value.Y < dragStartRelativeToScreen!.Value.Y);

			// Since the label is below the the box, the Y position of the drag relative to the label should be negative
			Assert.That(dragStartRelativeToLabel!.Value.Y < 0);
		}

		[Test]
		[Category(UITestCategories.Gestures)]
		public void DragEventCoordinates()
		{
			App.WaitForElement("TargetView");
			App.EnterText("TargetView", "DragAndDropBetweenLayouts");
			App.Tap("GoButton");

			App.Tap("ResetButton");

			App.WaitForElement("Blue");
			App.WaitForElement("Green");
			App.DragAndDrop("Blue", "Green");

			var dragRelativeToDrop = GetCoordinatesFromLabel(App.FindElement("DragRelativeDrop").GetText());
			var dragRelativeToScreen = GetCoordinatesFromLabel(App.FindElement("DragRelativeScreen").GetText());
			var dragRelativeToLabel = GetCoordinatesFromLabel(App.FindElement("DragRelativeLabel").GetText());
			var dragStartRelativeToScreen = GetCoordinatesFromLabel(App.FindElement("DragStartRelativeScreen").GetText());

			ClassicAssert.NotNull(dragRelativeToDrop);
			ClassicAssert.NotNull(dragRelativeToScreen);
			ClassicAssert.NotNull(dragRelativeToLabel);
			ClassicAssert.NotNull(dragStartRelativeToScreen);

			Assert.That(dragRelativeToDrop!.Value.X > 0 && dragRelativeToDrop!.Value.Y > 0);
			Assert.That(dragRelativeToScreen!.Value.X > 0 && dragRelativeToScreen!.Value.Y > 0);

			// The position of the drag relative to the drop location should be less than that relative to the screen
			// There are other elements in the screen, plus the ContentView of the test has some margin
			Assert.That(dragRelativeToDrop!.Value.X < dragRelativeToScreen!.Value.X);
			Assert.That(dragRelativeToDrop!.Value.Y < dragRelativeToScreen!.Value.Y);

			// Since the label is below the the box, the Y position of the drag relative to the label should be negative
			Assert.That(dragRelativeToLabel!.Value.Y < 0);

			// The drag is executed left to right, so the X value should be higher than where it started
			Assert.That(dragRelativeToScreen!.Value.X > dragStartRelativeToScreen!.Value.X);
		}

		[Test]
		[Category(UITestCategories.Gestures)]
		public void DropEventCoordinates()
		{
			App.WaitForElement("TargetView");
			App.EnterText("TargetView", "DragAndDropBetweenLayouts");
			App.Tap("GoButton");

			App.Tap("ResetButton");

			App.WaitForElement("Blue");
			App.WaitForElement("Green");
			App.DragAndDrop("Blue", "Green");

			var dropRelativeToLayout = GetCoordinatesFromLabel(App.FindElement("DropRelativeLayout").GetText());
			var dropRelativeToScreen = GetCoordinatesFromLabel(App.FindElement("DropRelativeScreen").GetText());
			var dropRelativeToLabel = GetCoordinatesFromLabel(App.FindElement("DropRelativeLabel").GetText());

			var dragRelativeToLabel = GetCoordinatesFromLabel(App.FindElement("DragRelativeLabel").GetText());
			var dragStartRelativeToScreen = GetCoordinatesFromLabel(App.FindElement("DragStartRelativeScreen").GetText());

			ClassicAssert.NotNull(dropRelativeToLayout);
			ClassicAssert.NotNull(dropRelativeToScreen);
			ClassicAssert.NotNull(dropRelativeToLabel);

			ClassicAssert.NotNull(dragRelativeToLabel);
			ClassicAssert.NotNull(dragStartRelativeToScreen);

			Assert.That(dropRelativeToLayout!.Value.X > 0 && dropRelativeToLayout!.Value.Y > 0);
			Assert.That(dropRelativeToScreen!.Value.X > 0 && dropRelativeToScreen!.Value.Y > 0);

			// The position of the drop relative the layout should be less than that relative to the screen
			// There are other elements in the screen, plus the ContentView of the test has some margin
			Assert.That(dropRelativeToLayout!.Value.X < dropRelativeToScreen!.Value.X);
			Assert.That(dropRelativeToLayout!.Value.Y < dropRelativeToScreen!.Value.Y);

			// Since the label is below the the box, the Y position of the drop relative to the label should be negative
			Assert.That(dropRelativeToLabel!.Value.Y < 0);

			// The drop is executed left to right, so the X value should be higher than where it started
			Assert.That(dropRelativeToScreen!.Value.X > dragStartRelativeToScreen!.Value.X);

			// The label receiving the coordinates of the drop is below that which receives the coordinates of the drag
			// Therefore, the label that receives the coordinates of the drop should have a smaller Y value (more negative)
			Assert.That(dropRelativeToLabel!.Value.Y < dragRelativeToLabel!.Value.Y);
		}

		// Helper function to parse out the X and Y coordinates from text labels 'Drag position: (x),(y)'
		Point? GetCoordinatesFromLabel(string? labelText)
		{
			if (labelText is null)
				return null;

			var i = labelText.IndexOf(':', StringComparison.Ordinal);

			if (i == -1)
				return null;

			var coordinates = labelText[(i + 1)..].Split(",");
			var x = int.Parse(coordinates[0]);
			var y = int.Parse(coordinates[1]);

			return new Point(x, y);
		}
	}
}