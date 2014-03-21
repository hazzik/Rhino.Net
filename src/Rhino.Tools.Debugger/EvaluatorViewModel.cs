using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rhino.Tools.Debugger
{
    public class EvaluatorViewModel : INotifyPropertyChanged
    {
        private string _expression;
        private string _value;

        public string Expression
        {
            get { return _expression; }
            set
            {
                if (value == _expression) return;
                _expression = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}