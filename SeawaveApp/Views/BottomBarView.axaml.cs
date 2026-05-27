using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SeawaveApp.ViewModels;

namespace SeawaveApp.Views;

public partial class BottomBarView : UserControl
{
    public BottomBarView()
    {
        InitializeComponent();
        TrackSlider.AddHandler(PointerPressedEvent, Slider_OnPointerPressed, RoutingStrategies.Tunnel);
        TrackSlider.AddHandler(PointerReleasedEvent, TrackSlider_OnPointerReleased, RoutingStrategies.Tunnel);
    }

    private void Slider_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Slider || DataContext is not BottomBarViewModel vm)
        {
            return;
        }
        
        vm.StartDragCommand.Execute(null);
    }

    private void Slider_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        var currentPoint = e.GetCurrentPoint(slider);
        var mouseX = currentPoint.Position.X;

        var trackWidth = slider.Bounds.Width;
        if (!(trackWidth > 0))
        {
            return;
        }

        var percentage = mouseX / trackWidth;
        percentage = Math.Clamp(percentage, 0.0, 1.0);

        var targetSeconds = percentage * slider.Maximum;
        var hoverTime = TimeSpan.FromSeconds(targetSeconds);

        var timeString = hoverTime.ToString(@"mm\:ss");
        ToolTip.SetTip(slider, timeString);

        var centerOffset = mouseX - (trackWidth / 2);
        ToolTip.SetHorizontalOffset(slider, centerOffset);
    }

    private void TrackSlider_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Slider slider && DataContext is BottomBarViewModel vm)
        {
            vm.SeekToTimeCommand.Execute(slider.Value);
        }
    }
}