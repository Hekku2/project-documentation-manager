using Avalonia.Controls;
using Desktop.ViewModels;

namespace Desktop.Views;

public partial class BuildConfirmationDialog : Window
{
    public BuildConfirmationDialog()
    {
        InitializeComponent();
    }

    public BuildConfirmationDialog(BuildConfirmationDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.DialogClosed += (_, _) => Close();
    }
}