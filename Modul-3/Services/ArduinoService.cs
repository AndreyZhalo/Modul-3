using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Modul_3.Services
{
    public class ArduinoService : INotifyPropertyChanged
    {
        private SerialPort _serialPort;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;

        public event Action<bool[]> ContactsStateChanged;
        public event Action<string> MessageReceived;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string[] AvailablePorts => SerialPort.GetPortNames();

        public async Task<bool> ConnectAsync(string portName)
        {
            try
            {
                Disconnect();

                _serialPort = new SerialPort(portName, 115200)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.Open();
                IsConnected = true;

                // Ждем инициализации Arduino
                await Task.Delay(2000);

                // Запускаем чтение данных
                _cancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(() => ReadDataAsync(_cancellationTokenSource.Token));

                // Запускаем непрерывный опрос
                StartContinuousReading();

                return true;
            }
            catch (Exception ex)
            {
                MessageReceived?.Invoke($"Connection error: {ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            _cancellationTokenSource?.Cancel();

            if (_serialPort?.IsOpen == true)
            {
                StopContinuousReading();
                _serialPort.Close();
            }

            _serialPort?.Dispose();
            _serialPort = null;
            IsConnected = false;
        }

        public void StartContinuousReading()
        {
            if (IsConnected)
            {
                _serialPort?.WriteLine("11"); // Команда непрерывного опроса
            }
        }

        public void StopContinuousReading()
        {
            if (IsConnected)
            {
                _serialPort?.WriteLine("13"); // Команда сброса (выключает непрерывный опрос)
            }
        }

        private async Task ReadDataAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _serialPort?.IsOpen == true)
            {
                try
                {
                    string data = _serialPort.ReadLine().Trim();
                    MessageReceived?.Invoke($"Received: {data}");

                    ProcessReceivedData(data);
                }
                catch (TimeoutException)
                {
                    // Игнорируем таймауты - это нормально
                }
                catch (Exception ex)
                {
                    MessageReceived?.Invoke($"Read error: {ex.Message}");
                    break;
                }

                await Task.Delay(10);
            }
        }

        private void ProcessReceivedData(string data)
        {
            if (data.StartsWith("STATUS:"))
            {
                // Формат: STATUS:1,0,1,0,0,0,0,0,0,0
                string statesString = data.Substring(7);
                string[] stateValues = statesString.Split(',');

                if (stateValues.Length == 10)
                {
                    bool[] states = new bool[10];
                    for (int i = 0; i < 10; i++)
                    {
                        // INPUT_PULLUP: 0 = замкнут, 1 = разомкнут
                        // Преобразуем: true = контакт замкнут, false = разомкнут
                        states[i] = stateValues[i] == "0";
                    }

                    ContactsStateChanged?.Invoke(states);
                }
            }
            else if (data.StartsWith("PIN"))
            {
                // Формат: PIN0:1 или PIN5:0
                // Можно обработать при необходимости
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}