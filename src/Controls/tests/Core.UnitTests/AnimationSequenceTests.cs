using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class AnimationSequenceTests : BaseTestFixture
	{
		[Fact]
		public void TotalDurationIsSumOfSequentialSegments()
		{
			var seq = new AnimationSequence()
				.Then(_ => { }, 300)
				.Then(_ => { }, 200);

			Assert.Equal(500u, seq.TotalDuration);
		}

		[Fact]
		public void ParallelExtendsTotalDurationWhenLonger()
		{
			var seq = new AnimationSequence()
				.Then(_ => { }, 200)
				.WithParallel(_ => { }, 500);

			// Parallel branch starts with the last segment (at 200ms - 200ms = 0)
			// and runs 500ms, so total is 0 + 500 = 500.
			Assert.Equal(500u, seq.TotalDuration);
		}

		[Fact]
		public void ParallelDoesNotShortenTotalDurationWhenShorter()
		{
			var seq = new AnimationSequence()
				.Then(_ => { }, 400)
				.WithParallel(_ => { }, 100);

			Assert.Equal(400u, seq.TotalDuration);
		}

		[Fact]
		public void DelayInsertsGapBetweenSegments()
		{
			var seq = new AnimationSequence()
				.Then(_ => { }, 100)
				.Delay(50)
				.Then(_ => { }, 100);

			Assert.Equal(250u, seq.TotalDuration);
		}

		[Fact]
		public void BuildAssignsCorrectFractionsToChildren()
		{
			double? lastA = null, lastB = null;
			var anim = new AnimationSequence()
				.Then(v => lastA = v, 250)
				.Then(v => lastB = v, 750)
				.Build();

			var cb = anim.GetCallback();

			// At parent fraction 0.1 only the first segment ([0.0, 0.25]) should fire.
			cb(0.1);
			Assert.NotNull(lastA);
			Assert.Null(lastB);

			// At parent fraction 0.5 the second segment ([0.25, 1.0]) should also fire.
			cb(0.5);
			Assert.NotNull(lastB);
			// Inside the second window: (0.5 - 0.25) / 0.75 = ~0.333
			Assert.InRange(lastB!.Value, 0.32, 0.34);
		}

		[Fact]
		public void BuildAssignsCorrectFractionsForParallel()
		{
			double? lastA = null, lastB = null, lastC = null;
			var anim = new AnimationSequence()
				.Then(v => lastA = v, 200)
				.WithParallel(v => lastB = v, 400)
				.Then(v => lastC = v, 100)
				.Build();

			// Total = max(0 + 400, 0 + 200) + 100 = 500.
			// Segment windows: A=[0, 0.4], B=[0, 0.8], C=[0.8, 1.0].

			var cb = anim.GetCallback();

			// f=0.2: A and B should fire, C should not.
			cb(0.2);
			Assert.NotNull(lastA);
			Assert.NotNull(lastB);
			Assert.Null(lastC);

			// f=0.9: C should now fire too. C's local value: (0.9 - 0.8) / 0.2 = 0.5
			cb(0.9);
			Assert.NotNull(lastC);
			Assert.InRange(lastC!.Value, 0.49, 0.51);
		}

		[Fact]
		public void BuildThrowsForEmptySequence()
		{
			var seq = new AnimationSequence();
			Assert.Throws<InvalidOperationException>(() => seq.Build());
		}

		[Fact]
		public void ThenThrowsForNullAnimation()
		{
			var seq = new AnimationSequence();
			Assert.Throws<ArgumentNullException>(() => seq.Then((Animation)null, 100));
		}

		[Fact]
		public void ThenThrowsForNullCallback()
		{
			var seq = new AnimationSequence();
			Assert.Throws<ArgumentNullException>(() => seq.Then((Action<double>)null, 100));
		}

		[Fact]
		public void ThenThrowsForZeroDuration()
		{
			var seq = new AnimationSequence();
			Assert.Throws<ArgumentOutOfRangeException>(() => seq.Then(_ => { }, 0));
		}

		[Fact]
		public void WithParallelThrowsBeforeAnyThen()
		{
			var seq = new AnimationSequence();
			Assert.Throws<InvalidOperationException>(() => seq.WithParallel(_ => { }, 100));
		}

		[Fact]
		public async Task CommitRunsAllSegmentsToCompletion()
		{
			var box = AnimationReadyHandler.Prepare(new BoxView());

			double a = 0, b = 0, c = 0;
			new AnimationSequence()
				.Then(v => a = v, 50)
				.WithParallel(v => b = v, 50)
				.Then(v => c = v, 50)
				.Commit(box, "seq");

			await Task.Delay(500);

			// Each callback's last value should be at or near 1.0 (end of its segment).
			Assert.InRange(a, 0.99, 1.0);
			Assert.InRange(b, 0.99, 1.0);
			Assert.InRange(c, 0.99, 1.0);
		}
	}
}
