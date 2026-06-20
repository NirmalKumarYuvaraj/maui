# Plan

Goal: Refactor the Android window inset handling architecture so that a CoordinatorLayout owns exactly one MauiWindowInsetListener and child views do not register their own Android OnApplyWindowInsetsListener instances.

Current State:

- MauiWindowInsetListener can be attached to many child views.
- Child views locate a listener through:
    - RegisterView
    - UnregisterView
    - FindListenerForView
    - RegisterParentForChildViews
    - \_registeredViews static registry

- Each child ultimately receives Android inset callbacks even when the same listener instance is reused.
- This creates listener management complexity, hierarchy walking, and static state.

Target Architecture:

- One MauiWindowInsetListener per CoordinatorLayout.
- Listener attached only to CoordinatorLayout.
- LayoutViewGroup, ContentViewGroup, and NestedScrollView should not have Android inset listeners attached.
- CoordinatorLayout receives WindowInsets once and dispatches them internally to interested children.
- Remove the need for child listener discovery and listener registration lookup.
- Preserve existing IME animation support through WindowInsetsAnimationCompat.Callback.

Requirements:

1. Introduce an inset consumer abstraction:

```csharp
internal interface IInsetConsumer
{
    void ApplyInsets(WindowInsetsCompat insets);
    void ResetInsets();
}
```

2. MauiWindowInsetListener should maintain a collection of inset consumers.

Example:

```csharp
readonly HashSet<IInsetConsumer> _consumers;
```

3. When CoordinatorLayout receives OnApplyWindowInsets:
    - Store the latest WindowInsetsCompat.
    - Dispatch the insets to all registered consumers.
    - Continue existing CoordinatorLayout-specific inset logic.
    - Preserve IME animation behavior.

4. Support late registration:
    - If a consumer registers after insets were already received,
      immediately apply the last known insets.

Example:

```csharp
WindowInsetsCompat? _lastInsets;
```

5. Remove or deprecate:
    - \_registeredViews
    - ViewEntry
    - RegisterView
    - UnregisterView
    - FindListenerForView
    - RegisterParentForChildViews
    - SetMauiWindowInsetListenerForChildView
    - RemoveMauiWindowInsetListenerForChildView

6. Child view responsibilities:
    - LayoutViewGroup implements IInsetConsumer.
    - ContentViewGroup implements IInsetConsumer.
    - NestedScrollView (or Maui wrapper) implements IInsetConsumer.
    - Children apply padding/margin adjustments based on dispatched insets.
    - Children no longer receive Android OnApplyWindowInsets callbacks directly.

7. Preserve cleanup:
    - Existing ResetView / ResetAllViews behavior should be adapted for consumer reset.
    - Avoid memory leaks.
    - Consumers must unregister when disposed or detached.

8. Investigate inset propagation semantics:
   Determine whether current behavior depends on inset consumption ordering.

Questions:

- Does LayoutViewGroup consume top insets before ContentViewGroup sees them?
- Do all consumers need the original WindowInsetsCompat?
- Is current behavior effectively a broadcast model or a consumption pipeline?

If consumption ordering is required, propose a chained dispatcher:

```csharp
WindowInsetsCompat current = insets;

foreach (var consumer in consumers)
{
    current = consumer.ApplyInsets(current);
}
```

Otherwise use a broadcast dispatcher:

```csharp
foreach (var consumer in consumers)
{
    consumer.ApplyInsets(insets);
}
```

Deliverables:

1. Proposed class diagram.
2. Refactoring steps.
3. API changes.
4. Migration strategy.
5. Example implementation snippets.
6. Risks and compatibility concerns.
7. Recommendation on broadcast vs consumption pipeline based on current code.
