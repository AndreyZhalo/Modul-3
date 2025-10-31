using Modul_3.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Modul_3.ViewModels
{
    public class ControlViewModel : INotifyPropertyChanged
    {
        private Product _currentProduct;
        private Connector _selectedConnector;
        private ContactMarker _selectedMarker;
        private string _imagePath;
        private Size _imageSize = new Size(800, 600);
        public static bool _hasUnsavedChanges;

        public Product CurrentProduct
        {
            get => _currentProduct;
            set
            {
                if (_currentProduct == value) return;

                // Проверяем изменения перед сменой продукта
               // if (!CheckUnsavedChanges()) return;

                _currentProduct = value;
                OnPropertyChanged(nameof(CurrentProduct));
                OnPropertyChanged(nameof(Connectors));

                SelectedConnector = Connectors.FirstOrDefault();
                _hasUnsavedChanges = false;
            }
        }

        public ObservableCollection<Connector> Connectors =>
           new ObservableCollection<Connector>(_currentProduct?.Connectors ?? Enumerable.Empty<Connector>());

        public ObservableCollection<ContactMarker> Markers { get; } = new ObservableCollection<ContactMarker>();

        public Connector SelectedConnector
        {
            get => _selectedConnector;
            set
            {
                if (_selectedConnector == value) return;

                // Проверяем изменения перед переключением разъема
              //  if (!CheckUnsavedChanges()) return;

                _selectedConnector = value;
                OnPropertyChanged(nameof(SelectedConnector));
                LoadConnectorData();
                _hasUnsavedChanges = false;
            }
        }


        private void LoadConnectorData()
        {
            if (_selectedConnector == null)
            {
                Markers.Clear();
                ImagePath = null;
                return;
            }

            // Всегда загружаем из файла или инициализируем
            if (!LoadLayoutFromFile())
            {
                MessageBox.Show($"Отсутствует файл программы"); ;
            }
        }


        private bool LoadLayoutFromFile()
        {
            string layoutFilePath = GetLayoutFilePath();
            if (!File.Exists(layoutFilePath)) return false;

            try
            {
                string json = File.ReadAllText(layoutFilePath);
                var layoutData = JsonSerializer.Deserialize<LayoutData>(json);

                ImagePath = layoutData.ImagePath;
                ImageSize = layoutData.ImageSize;

                Markers.Clear();
                foreach (var position in layoutData.ContactPositions)
                {
                    Markers.Add(new ContactMarker
                    {
                        ContactNumber = position.ContactNumber,
                        ContactTag = position.ContactTag,
                        RelativeX = position.RelativeX,
                        RelativeY = position.RelativeY,
                        Diameter = position.Diameter
                    });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        private string GetLayoutFilePath()
        {
            if (_selectedConnector == null) return string.Empty;

            string connectorDir = Path.GetDirectoryName(_selectedConnector.ImagePath);
            string connectorName = Path.GetFileNameWithoutExtension(_selectedConnector.ImagePath);

            if (string.IsNullOrEmpty(connectorDir))
            {
                connectorDir = _currentProduct?.ConnectorsFolderPath;
                connectorName = _selectedConnector.Name;
            }

            return Path.Combine(connectorDir, $"{connectorName}.layout.json");
        }


        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath == value) return;
                _imagePath = value;
                OnPropertyChanged(nameof(ImagePath));
                _hasUnsavedChanges = true;
            }
        }

        public Size ImageSize
        {
            get => _imageSize;
            set
            {
                if (_imageSize == value) return;
                _imageSize = value;
                OnPropertyChanged(nameof(ImageSize));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
