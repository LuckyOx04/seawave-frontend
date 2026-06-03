using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using SeawaveApp.Models;
using SeawaveApp.ViewModels;

namespace SeawaveApp.Views;

public partial class CenterAreaView : UserControl
{
    public CenterAreaView()
    {
        InitializeComponent();
    }

    private void Border_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border)
        {
            return;
        }

        if (border.DataContext is not UnifiedTrack selectedTrack)
        {
            return;
        }
        
        var userControl = border.FindAncestorOfType<UserControl>();

        if (userControl?.DataContext is not CenterAreaViewModel vm)
        {
            return;
        }

        if (vm.PlayTrackCommand.CanExecute(selectedTrack))
        {
            vm.PlayTrackCommand.Execute(selectedTrack);
        }
    }
}