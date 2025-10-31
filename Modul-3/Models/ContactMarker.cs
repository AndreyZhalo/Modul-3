using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Modul_3.Models
{
    public class ContactMarker : INotifyPropertyChanged
    {
        private int _contactNumber;
        private string _contactTag;
        private bool _isSelected;
        private double _relativeX;
        private double _relativeY;
        private double _diameter = 30;

        public int ContactNumber
        {
            get => _contactNumber;
            set
            {
                if (_contactNumber != value)
                {
                    _contactNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ContactTag
        {
            get => _contactTag;
            set
            {
                if (_contactTag != value)
                {
                    _contactTag = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public double RelativeX
        {
            get => _relativeX;
            set
            {
                if (_relativeX != value)
                {
                    _relativeX = value;
                    OnPropertyChanged();
                }
            }
        }

        public double RelativeY
        {
            get => _relativeY;
            set
            {
                if (_relativeY != value)
                {
                    _relativeY = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Diameter
        {
            get => _diameter;
            set
            {
                if (_diameter != value)
                {
                    _diameter = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"ContactMarker {ContactNumber}";
        }
    }
}