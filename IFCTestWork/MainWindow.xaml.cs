using HelixToolkit.Wpf;
using IFCTestWork.Models;
using IFCTestWork.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Xbim.Ifc4.Interfaces;

namespace IFCTestWork
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel;
        private PropertyViewModel PropertyViewModel;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            PropertyViewModel = new PropertyViewModel();
            DataContext = ViewModel;
            ViewModel.ModelLoaded += () => viewport.ZoomExtents();
            ViewModel.ErrorOccurred += msg => MessageBox.Show(this, msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Viewport_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectObject = viewport.Viewport.FindHits(e.GetPosition(viewport)).Select(h => h.Model as GeometryModel3D).FirstOrDefault(model => model != null);

            ViewModel.SelectObject(selectObject);
        }

        private void TreeItem_Selected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
                ((TreeViewItem)sender).BringIntoView();
        }
        private void IfcTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.Attributes.Clear();
            ViewModel.Properties.Clear();
            ViewModel.MaterialsIfc.Clear();

            if (DataContext is MainViewModel viewModel && e.NewValue is NodeItem selected)
            {
                viewModel.SelectedNode = selected;
            }

            if (e.NewValue is NodeItem item && item.Entity is IIfcObject ifcObject)
            {
                foreach (var attribute in PropertyViewModel.GetAttributes(ifcObject))
                {
                    ViewModel.Attributes.Add(attribute);
                }

                foreach (var property in PropertyViewModel.GetProperties(ViewModel, ifcObject))
                {
                    ViewModel.Properties.Add(property);
                }

                foreach (var material in PropertyViewModel.GetMaterials(ifcObject))
                {
                    ViewModel.MaterialsIfc.Add(material);
                }
            }
            else
            {
                return;
            }
        }
    }
}
