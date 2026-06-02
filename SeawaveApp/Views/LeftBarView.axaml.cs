using System;
using Avalonia.Controls;
using SeawaveApp.ViewModels;

namespace SeawaveApp.Views;

public partial class LeftBarView : UserControl
{
    public LeftBarView()
    {
        InitializeComponent();
    }

    private void Flyout_OnClosed(object? sender, EventArgs e)
    {
        if (sender is not Flyout)
        {
            return;
        }

        if (DataContext is not LeftBarViewModel vm)
        {
            return;
        }

        if (vm.ResetWizardCommand.CanExecute(null))
        {
            vm.ResetWizardCommand.Execute(null);
        }
    }
}