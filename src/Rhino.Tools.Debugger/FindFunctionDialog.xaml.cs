using System.Windows;
using System.Windows.Input;

namespace Rhino.Tools.Debugger
{
	public partial class FindFunctionDialog
	{
		public FindFunctionDialog()
		{
			InitializeComponent();
		}

		public string[] FunctionNames { get; set; }

		public string SelectedFunctionName
		{
			get { return (string) FunctionNamesListBox.SelectedItem; }
		}

		private void WindowLoaded(object sender, RoutedEventArgs e)
		{
			FunctionNamesListBox.ItemsSource = FunctionNames;
		}

		private void SelectClick(object sender, RoutedEventArgs e)
		{
			DialogResult = SelectedFunctionName != null;
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			DialogResult = null;
		}

		private void FunctionNamesListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			DialogResult = SelectedFunctionName != null;
		}
	}
}
