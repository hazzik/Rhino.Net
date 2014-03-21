using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Rhino.Tools.Debugger
{
    /// <summary>
    ///     Interaction logic for EvaluatorWindow.xaml
    /// </summary>
    public partial class EvaluatorWindow
    {
        public EvaluatorWindow()
        {
            InitializeComponent();
            DataGrid.ItemsSource = new ObservableCollection<EvaluatorViewModel>();
        }

        public Dim Debugger { get; set; }

        private void DataGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var model = (EvaluatorViewModel) e.Row.Item;
                switch ((string) e.Column.Header)
                {
                    case "Expression":
                        model.Value = Debugger.Eval(model.Expression);
                        break;
                    case "Value":
                        break;
                }
            }
        }
    }
}
