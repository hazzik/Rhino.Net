using Aga.Controls.Tree;

namespace Rhino.Tools.Debugger
{
    public partial class ContextWindow
    {
        public ContextWindow()
        {
            InitializeComponent();
        }

        public ITreeModel Model
        {
            get { return Tree.Model; }
            set { Tree.Model = value; }
        }
    }
}
