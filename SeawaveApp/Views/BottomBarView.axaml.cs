using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SeawaveApp.ViewModels;
using Avalonia.Threading;

namespace SeawaveApp.Views;

public partial class BottomBarView : UserControl
{
    public BottomBarView()
    {
        InitializeComponent();
    }

    private void Slider_OnDragStarted(object? sender, VectorEventArgs e)
    {
        if (DataContext is BottomBarViewModel vm)
        {
            vm.StartDragCommand.Execute(null);
        }
    }

    private void Slider_OnDragCompleted(object? sender, VectorEventArgs e)
    {
        if (sender is Slider slider && DataContext is BottomBarViewModel vm)
        {
            vm.SeekToTimeCommand.Execute(slider.Value);
        }
    }

    private void Slider_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Console.WriteLine("Input element pressed");
        if (sender is not Slider slider || DataContext is not BottomBarViewModel vm || 
            e.Source is not Visual visualSource || visualSource.GetType().Name.Contains("Thumb"))
        {
            return;
        }

        vm.StartDragCommand.Execute(null);
        
        Dispatcher.UIThread.Post(() =>
        {
            vm.SeekToTimeCommand.Execute(slider.Value);
        }, DispatcherPriority.Input);
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
}