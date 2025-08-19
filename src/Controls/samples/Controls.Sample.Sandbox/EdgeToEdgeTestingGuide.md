# Android Edge-to-Edge Testing Guide

This document provides comprehensive testing scenarios for Android Edge-to-Edge support in .NET MAUI.

## Overview

Edge-to-Edge support allows apps to extend their content behind the system bars (status bar and navigation bar) on Android devices. This requires proper handling of window insets to ensure content is not obscured by system UI.

## Test Scenarios

### Navigation Page Scenarios

1. **Default Navigation Page**

    - Tests basic NavigationPage behavior with edge-to-edge
    - Content should extend behind system bars but remain readable
    - Navigation bar should be properly positioned

2. **Navigation Page + BarBackgroundColor**

    - Tests navigation bar with custom background color
    - Verify color extends properly in edge-to-edge mode
    - Check color visibility behind status bar

3. **Navigation Page + BarBackground (Gradient)**

    - Tests navigation bar with gradient background
    - Verify gradient rendering in edge-to-edge environment
    - Check gradient positioning relative to system bars

4. **Navigation Page + ToolBar Items**

    - Tests toolbar items positioning with edge-to-edge
    - Verify items are clickable and not obscured
    - Check proper spacing from system bars

5. **Navigation Page + HasNavigationBar=False**
    - Tests full-screen content with no navigation bar
    - Content should utilize entire screen safely
    - Back button functionality should work

### Modal Navigation Page Scenarios

6. **Default Modal Navigation Page**

    - Tests modal presentation with edge-to-edge
    - Modal should cover full screen appropriately
    - Close functionality should be accessible

7. **Modal Navigation Page + BarBackgroundColor**

    - Tests modal with colored navigation bar
    - Verify modal overlay behavior
    - Check color consistency

8. **Modal Navigation Page + BarBackground (Gradient)**

    - Tests modal with gradient navigation bar
    - Verify gradient in modal context
    - Check proper rendering

9. **Modal Navigation Page + ToolBar Items**

    - Tests modal with toolbar items
    - Verify items accessibility in modal
    - Check proper positioning

10. **Modal Navigation Page + HasNavigationBar=False**
    - Tests full-screen modal
    - Verify proper edge-to-edge behavior
    - Check dismissal methods

### Page Type Scenarios

11. **Default Tabbed Page**

    -   Tests TabbedPage with edge-to-edge
    -   Tab bar should be properly positioned
    -   Tab content should respect insets

12. **Default Flyout Page**
    -   Tests FlyoutPage with edge-to-edge
    -   Flyout should slide properly
    -   Detail content should respect insets
    -   Menu should be accessible

### Shell Scenarios

13. **Shell with Flyout**

    -   Tests Shell flyout with edge-to-edge
    -   Flyout header should respect status bar
    -   Flyout items should be accessible
    -   Navigation should work properly

14. **Shell with TabBar**

    -   Tests Shell tab navigation
    -   Tab bar positioning with edge-to-edge
    -   Tab switching should work smoothly

15. **Shell with Bottom Tabs**

    -   Tests bottom tab bar with edge-to-edge
    -   Tab bar should not be obscured by navigation bar
    -   Proper spacing from bottom edge

16. **Shell with Top Tabs**

    -   Tests top tab placement
    -   Tabs should not be obscured by status bar
    -   Proper spacing from top edge

17. **Shell + NavBarIsVisible=False**

    -   Tests Shell without navigation bar
    -   Full-screen content utilization
    -   Alternative navigation methods

18. **Shell + NavBarHasShadow=True**

    -   Tests navigation bar shadow rendering
    -   Shadow should render properly with edge-to-edge
    -   Visual appearance verification

19. **Shell + Background Color**
    -   Tests Shell with custom background color
    -   Color should extend properly behind system bars
    -   Consistent color application

## Testing Checklist

### Visual Testing

-   [ ] Content extends behind status bar (top)
-   [ ] Content extends behind navigation bar (bottom)
-   [ ] No content is obscured by system UI
-   [ ] Proper padding/margins applied
-   [ ] Colors/gradients render correctly
-   [ ] Shadows appear as expected

### Interactive Testing

-   [ ] All buttons are clickable
-   [ ] Touch targets are not blocked
-   [ ] Scrolling works properly
-   [ ] Navigation gestures work
-   [ ] Text input fields are accessible
-   [ ] Modal presentation/dismissal works

### Device Variations

-   [ ] Test on devices with different screen sizes
-   [ ] Test on devices with/without notches
-   [ ] Test with gesture navigation
-   [ ] Test with button navigation
-   [ ] Test orientation changes (portrait/landscape)
-   [ ] Test on different Android versions

### Edge Cases

-   [ ] Keyboard appearance doesn't break layout
-   [ ] App switching/multitasking
-   [ ] Theme changes (light/dark)
-   [ ] Accessibility services enabled
-   [ ] High contrast mode
-   [ ] Font size scaling

## Expected Behavior

### Correct Edge-to-Edge Implementation

1. Content draws behind system bars
2. Interactive elements have proper safe area margins
3. Text remains readable with appropriate padding
4. Navigation remains functional
5. No visual glitches during transitions

### Common Issues to Watch For

1. Content obscured by status bar
2. Buttons not clickable due to navigation bar overlap
3. Inconsistent padding across screens
4. Color bleeding or incorrect rendering
5. Navigation stack corruption

## Test Pages Features

Each test page includes:

-   **Visual indicators**: Colored boxes to verify visibility
-   **Interactive elements**: Buttons to test touch responsiveness
-   **System information**: Display device and screen details
-   **Navigation controls**: Back and close modal buttons
-   **Comprehensive labeling**: Clear identification of test scenario

## Usage Instructions

1. Run the sample app on an Android device
2. Navigate through each test scenario systematically
3. Document any issues found with specific scenarios
4. Test across multiple device configurations
5. Verify fixes don't break other scenarios

## Future Enhancements

This test suite can be extended to include:

-   Tablet-specific scenarios
-   Foldable device testing
-   Performance monitoring
-   Automated visual testing
-   Additional UI controls testing
