#nullable enable
using System;
using System.Collections.Generic;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// Provides a fluent builder for composing <see cref="Animation"/> objects sequentially and in parallel
	/// without having to manually calculate the normalized <c>beginAt</c>/<c>finishAt</c> fractions required
	/// by <see cref="Animation.Add(double, double, Animation)"/>.
	/// </summary>
	/// <remarks>
	/// Durations are specified in milliseconds. The total length of the resulting animation is the sum of
	/// the sequential segments (extended by any longer parallel branches). Call <see cref="Build"/> to obtain
	/// a single <see cref="Animation"/> or <see cref="Commit"/> to run it directly against an
	/// <see cref="IAnimatable"/>.
	/// </remarks>
	public sealed class AnimationSequence
	{
		readonly List<Entry> _entries = new();
		uint _lastSegmentStart;
		uint _lastSegmentEnd;
		bool _hasSegment;

		readonly struct Entry
		{
			public Entry(Animation animation, uint startMs, uint endMs)
			{
				Animation = animation;
				StartMs = startMs;
				EndMs = endMs;
			}

			public Animation Animation { get; }
			public uint StartMs { get; }
			public uint EndMs { get; }
		}

		/// <summary>
		/// Gets the total duration, in milliseconds, of the composed animation.
		/// </summary>
		public uint TotalDuration => _lastSegmentEnd;

		/// <summary>
		/// Appends <paramref name="animation"/> to the sequence so it runs immediately after the previous
		/// sequential segment for the given <paramref name="duration"/> in milliseconds.
		/// </summary>
		/// <param name="animation">The animation to add.</param>
		/// <param name="duration">The duration, in milliseconds, of this segment. Must be greater than zero.</param>
		/// <returns>The current <see cref="AnimationSequence"/> instance, for chaining.</returns>
		public AnimationSequence Then(Animation animation, uint duration)
		{
			if (animation is null)
				throw new ArgumentNullException(nameof(animation));
			if (duration == 0)
				throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");

			var start = _lastSegmentEnd;
			var end = start + duration;
			_entries.Add(new Entry(animation, start, end));
			_lastSegmentStart = start;
			_lastSegmentEnd = end;
			_hasSegment = true;
			return this;
		}

		/// <summary>
		/// Creates an <see cref="Animation"/> from <paramref name="callback"/> and appends it to the sequence.
		/// </summary>
		/// <param name="callback">An action that is called with successive animation values in the range [0, 1].</param>
		/// <param name="duration">The duration, in milliseconds, of this segment. Must be greater than zero.</param>
		/// <param name="easing">The easing function to apply to this segment.</param>
		/// <returns>The current <see cref="AnimationSequence"/> instance, for chaining.</returns>
		public AnimationSequence Then(Action<double> callback, uint duration, Easing? easing = null)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));
			return Then(new Animation(callback, 0.0, 1.0, easing), duration);
		}

		/// <summary>
		/// Runs <paramref name="animation"/> in parallel with the most recently added sequential segment.
		/// The parallel branch starts at the same time as that segment.
		/// </summary>
		/// <param name="animation">The animation to add in parallel.</param>
		/// <param name="duration">The duration, in milliseconds, of the parallel branch. Must be greater than zero.</param>
		/// <returns>The current <see cref="AnimationSequence"/> instance, for chaining.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no sequential segment has been added yet.</exception>
		public AnimationSequence WithParallel(Animation animation, uint duration)
		{
			if (animation is null)
				throw new ArgumentNullException(nameof(animation));
			if (duration == 0)
				throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");
			if (!_hasSegment)
				throw new InvalidOperationException("WithParallel can only be called after at least one Then().");

			var start = _lastSegmentStart;
			var end = start + duration;
			_entries.Add(new Entry(animation, start, end));
			if (end > _lastSegmentEnd)
				_lastSegmentEnd = end;
			return this;
		}

		/// <summary>
		/// Creates an <see cref="Animation"/> from <paramref name="callback"/> and runs it in parallel with the most
		/// recently added sequential segment.
		/// </summary>
		/// <param name="callback">An action that is called with successive animation values in the range [0, 1].</param>
		/// <param name="duration">The duration, in milliseconds, of the parallel branch. Must be greater than zero.</param>
		/// <param name="easing">The easing function to apply to the parallel branch.</param>
		/// <returns>The current <see cref="AnimationSequence"/> instance, for chaining.</returns>
		public AnimationSequence WithParallel(Action<double> callback, uint duration, Easing? easing = null)
		{
			if (callback is null)
				throw new ArgumentNullException(nameof(callback));
			return WithParallel(new Animation(callback, 0.0, 1.0, easing), duration);
		}

		/// <summary>
		/// Inserts an idle gap of <paramref name="duration"/> milliseconds before the next sequential segment.
		/// </summary>
		/// <param name="duration">The gap duration in milliseconds.</param>
		/// <returns>The current <see cref="AnimationSequence"/> instance, for chaining.</returns>
		public AnimationSequence Delay(uint duration)
		{
			_lastSegmentEnd += duration;
			_lastSegmentStart = _lastSegmentEnd;
			return this;
		}

		/// <summary>
		/// Builds a single <see cref="Animation"/> from all queued segments. The returned animation has
		/// child animations whose <c>StartDelay</c>/<c>Duration</c> are normalized fractions of
		/// <see cref="TotalDuration"/>.
		/// </summary>
		/// <returns>A composed <see cref="Animation"/> ready to be committed via <see cref="Animation.Commit"/>.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the sequence is empty.</exception>
		public Animation Build()
		{
			if (_entries.Count == 0)
				throw new InvalidOperationException("AnimationSequence is empty. Add at least one segment with Then() before building.");

			var parent = new Animation();
			double total = _lastSegmentEnd;
			foreach (var entry in _entries)
			{
				double beginAt = entry.StartMs / total;
				double finishAt = entry.EndMs / total;

				if (beginAt < 0.0)
					beginAt = 0.0;
				if (finishAt > 1.0)
					finishAt = 1.0;
				if (finishAt <= beginAt)
					finishAt = Math.Min(1.0, beginAt + 1e-9);

				parent.Add(beginAt, finishAt, entry.Animation);
			}
			return parent;
		}

		/// <summary>
		/// Builds the composed animation and commits it against <paramref name="owner"/>.
		/// </summary>
		/// <param name="owner">The owning <see cref="IAnimatable"/> that will run the animation.</param>
		/// <param name="name">The name (handle) used to track the animation.</param>
		/// <param name="rate">The time, in milliseconds, between frames.</param>
		/// <param name="easing">The easing function to apply on the parent timeline.</param>
		/// <param name="finished">A callback invoked when the composed animation finishes or is cancelled.</param>
		/// <param name="repeat">A predicate that returns <see langword="true"/> to repeat the animation.</param>
		public void Commit(IAnimatable owner, string name, uint rate = 16, Easing? easing = null, Action<double, bool>? finished = null, Func<bool>? repeat = null)
		{
			if (owner is null)
				throw new ArgumentNullException(nameof(owner));
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			var animation = Build();
			animation.Commit(owner, name, rate, TotalDuration, easing, finished, repeat);
		}
	}
}
