using Modul_3.Models;
using Modul_3.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;

namespace Modul_3.ViewModels
{
    public class ControlViewModel : INotifyPropertyChanged, IDisposable
    {
        private Product _currentProduct;
        private Connector _selectedConnector;
        private ContactMarker _selectedMarker;
        private string _imagePath;
        private Size _imageSize = new Size(800, 600);
        public static bool _hasUnsavedChanges;

        // Arduino сервис и связанные свойства
        private readonly ArduinoService _arduinoService;
        private string _selectedPort;
        private bool _isArduinoConnected;
        private string _arduinoStatus;

        // Команды
        private ICommand _connectArduinoCommand;
        private ICommand _disconnectArduinoCommand;
        private ICommand _refreshPortsCommand;

        public ControlViewModel()
        {
            _arduinoService = new ArduinoService();
            _arduinoService.ContactsStateChanged += OnContactsStateChanged;
            _arduinoService.MessageReceived += OnMessageReceived;
            _arduinoService.PropertyChanged += OnArduinoServicePropertyChanged;
        }

        public Product CurrentProduct
        {
            get => _currentProduct;
            set
            {
                if (_currentProduct == value) return;

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

                _selectedConnector = value;
                OnPropertyChanged(nameof(SelectedConnector));
                LoadConnectorData();
                _hasUnsavedChanges = false;
            }
        }

        // Arduino свойства
        public ObservableCollection<string> AvailablePorts => new ObservableCollection<string>(_arduinoService.AvailablePorts);

        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                if (_selectedPort != value)
                {
                    _selectedPort = value;
                    OnPropertyChanged(nameof(SelectedPort));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsArduinoConnected
        {
            get => _isArduinoConnected;
            private set
            {
                if (_isArduinoConnected != value)
                {
                    _isArduinoConnected = value;
                    OnPropertyChanged(nameof(IsArduinoConnected));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string ArduinoStatus
        {
            get => _arduinoStatus;
            private set
            {
                if (_arduinoStatus != value)
                {
                    _arduinoStatus = value;
                    OnPropertyChanged(nameof(ArduinoStatus));
                }
            }
        }

        // Команды
        public ICommand ConnectArduinoCommand
        {
            get
            {
                if (_connectArduinoCommand == null)
                {
                    _connectArduinoCommand = new RelayCommand(
                        async () => await ConnectArduinoAsync(),
                        () => !IsArduinoConnected && !string.IsNullOrEmpty(SelectedPort)
                    );
                }
                return _connectArduinoCommand;
            }
        }

        public ICommand DisconnectArduinoCommand
        {
            get
            {
                if (_disconnectArduinoCommand == null)
                {
                    _disconnectArduinoCommand = new RelayCommand(
                        DisconnectArduino,
                        () => IsArduinoConnected
                    );
                }
                return _disconnectArduinoCommand;
            }
        }

        public ICommand RefreshPortsCommand
        {
            get
            {
                if (_refreshPortsCommand == null)
                {
                    _refreshPortsCommand = new RelayCommand(RefreshPorts);
                }
                return _refreshPortsCommand;
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

            if (!LoadLayoutFromFile())
            {
                MessageBox.Show($"Отсутствует файл программы");
            }
            else
            {
                // После загрузки маркеров, обновляем их состояние по данным от Arduino
                RefreshMarkersFromArduino();
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
                        Diameter = position.Diameter,
                        IsActive = false // По умолчанию неактивны
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки файла разметки: {ex.Message}");
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

        // Arduino методы
        public async Task ConnectArduinoAsync()
        {
            if (string.IsNullOrEmpty(SelectedPort))
            {
                ArduinoStatus = "Порт не выбран";
                return;
            }

            ArduinoStatus = "Подключение...";
            try
            {
                bool success = await _arduinoService.ConnectAsync(SelectedPort);

                if (success)
                {
                    IsArduinoConnected = true;
                    ArduinoStatus = "Подключено";

                    // Запускаем непрерывный опрос контактов
                    _arduinoService.StartContinuousReading();
                }
                else
                {
                    ArduinoStatus = "Ошибка подключения";
                }
            }
            catch (Exception ex)
            {
                ArduinoStatus = $"Ошибка: {ex.Message}";
            }
        }

        public void DisconnectArduino()
        {
            _arduinoService.Disconnect();
            IsArduinoConnected = false;
            ArduinoStatus = "Отключено";

            // Сбрасываем все маркеры в неактивное состояние
            foreach (var marker in Markers)
            {
                marker.IsActive = false;
            }
        }

        public void RefreshPorts()
        {
            OnPropertyChanged(nameof(AvailablePorts));
            if (AvailablePorts.Any() && string.IsNullOrEmpty(SelectedPort))
            {
                SelectedPort = AvailablePorts.First();
            }
        }

        private void OnContactsStateChanged(bool[] states)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < states.Length && i < Markers.Count; i++)
                {
                    var marker = Markers.FirstOrDefault(m => m.ContactNumber == i + 1);
                    if (marker != null)
                    {
                        marker.IsActive = states[i];

                        if (states[i])
                        {
                            Debug.WriteLine($"Контакт {marker.ContactNumber} активирован");
                        }
                    }
                }
            });
        }

        private void OnMessageReceived(string message)
        {
            Debug.WriteLine($"Arduino: {message}");

            if (message.Contains("Ready") || message.Contains("Hello"))
            {
                ArduinoStatus = message;
            }
        }

        private void OnArduinoServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ArduinoService.IsConnected))
            {
                IsArduinoConnected = _arduinoService.IsConnected;
            }
        }

        private void RefreshMarkersFromArduino()
        {
            // Если Arduino подключено, запрашиваем текущее состояние
            if (IsArduinoConnected)
            {
                _arduinoService.StartContinuousReading();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _arduinoService?.Disconnect();
            
        }
    }
}