using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DS18B20_1WireBus.Model
{
    class TemperatureModel : INotifyPropertyChanged
    {
        double _temperature = 0;
        bool _started = false;
        string _systemSymbol = "C";
        string _statusText = "";

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public double Temperature
        {
            get { return _temperature; }
            set
            {
                if (value != _temperature)
                {
                    _temperature = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool Started
        {
            get { return _started; }
            set
            {
                if (value != _started)
                {
                    _started = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("Stopped");
                }
            }
        }
        public bool Stopped
        {
            get { return !Started; }
        }
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (value != _statusText)
                {
                    _statusText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string SystemSymbol
        {
            get { return _systemSymbol; }
            set
            {
                if (value != _systemSymbol)
                {
                    _systemSymbol = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
