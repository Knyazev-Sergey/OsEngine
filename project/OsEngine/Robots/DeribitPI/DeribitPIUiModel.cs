using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OsEngine.Robots.DeribitPI
{
    public class DeribitPIUiModel : INotifyPropertyChanged
    {
        private string _lastPrice;

        public event PropertyChangedEventHandler PropertyChanged;

        /*public DeribitPIUiModel()
        {
            _lastPrice = "0";
        }*/

        public string LastPrice
        {
            get { return _lastPrice; }
            set
            {
                _lastPrice = value;
                //OnPropertyChanged();
                PropertyChanged(this, new PropertyChangedEventArgs(LastPrice));
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
