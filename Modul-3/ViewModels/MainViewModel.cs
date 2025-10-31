using Modul_3.Models;
using Modul_3.Services;
using Modul_3.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Modul_3.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ProductCatalogLoader _loader;
        private Product _selectedProduct;
        private string _productNumber;
        private string _selectedOperator;

        public ProductCatalog Catalog { get; private set; }
        public ObservableCollection<Product> Products { get; private set; }
        public ObservableCollection<string> Operators { get; private set; }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
            }
        }

        public string ProductNumber
        {
            get => _productNumber;
            set
            {
                _productNumber = value;
                OnPropertyChanged();
            }
        }

        public string SelectedOperator
        {
            get => _selectedOperator;
            set
            {
                _selectedOperator = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartCommand { get; private set; }
        public ICommand EditorCommand { get; private set; }

        public MainViewModel()
        {
            _loader = new ProductCatalogLoader();
            LoadCatalog();
            InitializeOperators();
            InitializeCommands();
        }

        private void LoadCatalog()
        {
            Catalog = _loader.LoadCatalog();
            Products = new ObservableCollection<Product>(Catalog.Products);
            OnPropertyChanged(nameof(Products));

            if (Products.Count > 0)
            {
                SelectedProduct = Products[0];
            }
        }

        private void InitializeOperators()
        {
            // Здесь можно загружать операторов из базы данных или файла конфигурации
            Operators = new ObservableCollection<string>
            {
                "Иванов А.С.",
                "Петров В.И.",
                "Сидорова М.К.",
                "Козлов Д.П."
            };
            OnPropertyChanged(nameof(Operators));

            if (Operators.Count > 0)
            {
                SelectedOperator = Operators[0];
            }
        }

        private void InitializeCommands()
        {
            StartCommand = new RelayCommand(
                execute: () => StartTesting(),
                canExecute: () => CanStartTesting());

            EditorCommand = new RelayCommand(
                execute: () => OpenEditor());
        }

        private void StartTesting()
        {
           
            var controlWindow = new Views.ControlWindow(SelectedProduct);//ProductNumber, SelectedOperator - добавить для статистики
            controlWindow.Owner = Application.Current.MainWindow; // Делаем главное окно владельцем
            controlWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            controlWindow.ShowDialog();
        }

        private bool CanStartTesting()
        {
            return SelectedProduct != null &&
                   !string.IsNullOrWhiteSpace(ProductNumber) &&
                   !string.IsNullOrWhiteSpace(SelectedOperator);
        }

        private void OpenEditor()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Выберите изделие для редактирования", "Внимание",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editorWindow = new Views.EditorWindow(SelectedProduct);
                editorWindow.Owner = Application.Current.MainWindow; // Делаем главное окно владельцем
                editorWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                editorWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии редактора: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}