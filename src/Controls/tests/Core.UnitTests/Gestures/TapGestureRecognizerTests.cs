using System;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{

	public class TapGestureRecognizerTests : BaseTestFixture
	{
		[Fact]
		public void Constructor()
		{
			var tap = new TapGestureRecognizer();

			Assert.Null(tap.Command);
			Assert.Null(tap.CommandParameter);
			Assert.Equal(1, tap.NumberOfTapsRequired);
		}

		[Fact]
		public void CallbackPassesParameter()
		{
			var view = new View();
			var tap = new TapGestureRecognizer();
			tap.CommandParameter = "Hello";

			object result = null;
			tap.Command = new Command(o => result = o);

			tap.SendTapped(view);
			Assert.Equal(result, tap.CommandParameter);
		}

		[Fact]
		public void TapGestureRecognizerImplementsITapGestureController()
		{
			var tap = new TapGestureRecognizer();
			Assert.IsAssignableFrom<ITapGestureController>(tap);
		}

		[Fact]
		public void ITapGestureControllerSendTappedFiresTappedEvent()
		{
			var view = new View();
			var tap = new TapGestureRecognizer();
			bool tappedFired = false;
			tap.Tapped += (s, e) => tappedFired = true;

			((ITapGestureController)tap).SendTapped(view);

			Assert.True(tappedFired);
		}

		[Fact]
		public void ITapGestureControllerSendTappedExecutesCommand()
		{
			var view = new View();
			var tap = new TapGestureRecognizer();
			tap.CommandParameter = "test";
			object executedParam = null;
			tap.Command = new Command(o => executedParam = o);

			((ITapGestureController)tap).SendTapped(view);

			Assert.Equal("test", executedParam);
		}

		[Fact]
		public void ITapGestureControllerSendTappedPassesGetPosition()
		{
			var view = new View();
			var tap = new TapGestureRecognizer();
			TappedEventArgs capturedArgs = null;
			tap.Tapped += (s, e) => capturedArgs = e;

			var expectedPoint = new Point(10, 20);
			((ITapGestureController)tap).SendTapped(view, _ => expectedPoint);

			Assert.NotNull(capturedArgs);
			Assert.Equal(expectedPoint, capturedArgs.GetPosition(null));
		}
	}
}
