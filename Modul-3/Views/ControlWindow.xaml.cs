
using Modul_3.Models;
using Modul_3.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows.Media;

namespace Modul_3.Views
{
    public partial class  ControlWindow : Window
    {
        private ControlViewModel ViewModel => (ControlViewModel)DataContext;
        private Dictionary<ContactMarker, ContactMarkerControl> _markerControls = new Dictionary<ContactMarker, ContactMarkerControl>();

        // Фиксированные размеры изображения
        private const double ImageWidth = 800;
        private const double ImageHeight = 600;

        public ControlWindow(Product product)
        {
            InitializeComponent();

            var viewModel = new ControlViewModel();
            viewModel.CurrentProduct = product;
            DataContext = viewModel;

            // Устанавливаем фиксированный размер изображения в ViewModel
            viewModel.ImageSize = new Size(ImageWidth, ImageHeight);

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.Markers.CollectionChanged += Markers_CollectionChanged;
            Dispatcher.BeginInvoke(new Action(() => UpdateMarkers()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
            if (e.PropertyName == nameof(ControlViewModel.SelectedConnector))
            {
                UpdateMarkers();
            }
        }
        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"CollectionChanged: {e.Action}");
            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            try
            {
                
                MarkersCanvas.Children.Clear();
                _markerControls.Clear();

                // Добавляем новые маркеры
                foreach (var marker in ViewModel.Markers)
                {
                    var markerControl = new ContactMarkerControl();
                    markerControl.DataContext = marker;

                    // Устанавливаем позицию на основе относительных координат
                    UpdateMarkerPosition(marker, markerControl);

                    MarkersCanvas.Children.Add(markerControl);
                    _markerControls[marker] = markerControl;

                }

                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении маркеров: {ex.Message}");
            }
        }

        private void UpdateMarkerPosition(ContactMarker marker, ContactMarkerControl control)
        {
            // Вычисляем абсолютные координаты на основе относительных и фиксированного размера изображения
            double absoluteX = (marker.RelativeX * ImageWidth) - (control.Width / 2);
            double absoluteY = (marker.RelativeY * ImageHeight) - (control.Height / 2);

            // Ограничиваем в пределах Canvas
            absoluteX = Math.Max(0, Math.Min(ImageWidth - control.Width, absoluteX));
            absoluteY = Math.Max(0, Math.Min(ImageHeight - control.Height, absoluteY));

            Canvas.SetLeft(control, absoluteX);
            Canvas.SetTop(control, absoluteY);
                        
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            base.OnClosed(e);
        }
    }
}
