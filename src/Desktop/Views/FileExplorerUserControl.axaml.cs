using Avalonia.Controls;
using Desktop.ViewModels;

namespace Desktop.Views;

public partial class FileExplorerUserControl : UserControl
{
    public FileExplorerUserControl()
    {
        InitializeComponent();
    }

    public FileExplorerUserControl(FileExplorerViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}