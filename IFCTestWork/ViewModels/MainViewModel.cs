using HelixToolkit.Wpf;
using IFCTestWork.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;
using Xbim.Common.XbimExtensions;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using NLog;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace IFCTestWork.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public IfcStore model;
        public AsyncRelayCommand OpenCommand { get; set; }

        public ObservableCollection<TableItem> Properties { get; set; } = new ObservableCollection<TableItem>(); 
        private Dictionary<int, List<GeometryModel3D>> GeometryModels = new Dictionary<int, List<GeometryModel3D>>();
        private Dictionary<GeometryModel3D, IIfcProduct> IfcProducts = new Dictionary<GeometryModel3D, IIfcProduct>();
        private Dictionary<GeometryModel3D, Material> Materials = new Dictionary<GeometryModel3D, Material>();
        private Dictionary<int, NodeItem> NodesByLabel = new Dictionary<int, NodeItem>();
        private HashSet<GeometryModel3D> HighlightedObjects = new HashSet<GeometryModel3D>();

        private static readonly Material HighlightMaterial =
            MaterialHelper.CreateMaterial(Colors.OrangeRed);

        private Model3DGroup model3D = new Model3DGroup();
        public Model3DGroup Model3D
        {
            get => model3D;
            set => SetProperty(ref model3D, value);
        }

        private ObservableCollection<NodeItem> nodes = new ObservableCollection<NodeItem>();
        public ObservableCollection<NodeItem> Nodes
        {
            get => nodes;
            set => SetProperty(ref nodes, value);
        }

        private NodeItem selectedNode;
        public NodeItem SelectedNode
        {
            get => selectedNode;
            set => SetProperty(ref selectedNode, value);
        }

        public event Action ModelLoaded;
        public event Action<string> ErrorOccurred;

        public MainViewModel()
        {
            OpenCommand = new AsyncRelayCommand(OnOpen);
        }
        public async Task OnOpen()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Ifc файлы (*.ifc*)|*.ifc*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var name = openFileDialog.FileName;

                try
                {
                    IfcStore model = IfcStore.Open(openFileDialog.FileName);
                    this.model = model;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Не удалось открыть IFC-файл:{ex.Message}");
                }

                try
                {
                    await LoadIfc(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Не удалось открыть IFC-файл:{ex.Message}");
                }
            }
        }
        private async Task LoadIfc(string path)
        {
            model?.Dispose();
            GeometryModels.Clear();
            IfcProducts.Clear();
            Materials.Clear();
            NodesByLabel.Clear();
            HighlightedObjects.Clear();
            SelectedNode = null;

            try
            {
                model = IfcStore.Open(path);
                Log.Info("Файл прочитан, схема {Schema}", model.SchemaVersion);

                var (store, context) = await Task.Run(() =>
                {
                    var ifcModel = IfcStore.Open(path);
                    try
                    {
                        var ifcContext = new Xbim3DModelContext(ifcModel);
                        ifcContext.CreateContext();
                        return (ifcModel, ifcContext);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Ошибка триангуляции геометрии, файл: {Path}", path);
                        ifcModel.Dispose();
                        throw;
                    }
                });

                model = store;

                double scale = 1.0 / model.ModelFactors.OneMetre;
                var group = new Model3DGroup();

                foreach (var instance in context.ShapeInstances())
                {
                    var geometry = context.ShapeGeometry(instance);
                    if (geometry.Format != XbimGeometryType.PolyhedronBinary) continue;

                    var product = model.Instances[instance.IfcProductLabel] as IIfcProduct;
                    if (product == null) continue;

                    using (var ms = new MemoryStream(((IXbimShapeGeometryData)geometry).ShapeData))
                    {
                        using (var br = new BinaryReader(ms))
                        {
                            var tri = br.ReadShapeTriangulation().Transform(instance.Transformation);

                            var mesh = new MeshGeometry3D();
                            foreach (var v in tri.Vertices)
                                mesh.Positions.Add(new Point3D(v.X * scale, v.Y * scale, v.Z * scale));
                            foreach (var face in tri.Faces)
                                foreach (var idx in face.Indices)
                                    mesh.TriangleIndices.Add(idx);

                            var material = MaterialHelper.CreateMaterial(Colors.LightGray);
                            var gm = new GeometryModel3D(mesh, material) { BackMaterial = material };

                            IfcProducts[gm] = product;
                            Materials[gm] = material;
                            if (!GeometryModels.TryGetValue(product.EntityLabel, out var list))
                                GeometryModels[product.EntityLabel] = list = new List<GeometryModel3D>();
                            list.Add(gm);

                            group.Children.Add(gm);
                        }
                    }
                }

                Model3D = group;

                var project = model.Instances.OfType<IIfcProject>().FirstOrDefault();

                if (project == null)
                {
                    Nodes = new ObservableCollection<NodeItem>();
                    Log.Warn("В файле нет IfcProject, дерево будет пустым");
                }
                else
                {
                    Nodes = new ObservableCollection<NodeItem> { AddNode(project, null) };
                }

                ModelLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка загрузки модели IFC: {Message}", $"{ex.Message}");
            }

            
        }
        private NodeItem AddNode(IIfcObjectDefinition ifcObject, NodeItem parent)
        {
            var node = new NodeItem(ifcObject, parent, OnNodeSelectionChanged);
            NodesByLabel[ifcObject.EntityLabel] = node;

            foreach (var rel in ifcObject.IsDecomposedBy)
            {
                foreach (var child in rel.RelatedObjects)
                {
                    node.Children.Add(AddNode(child, parent));
                }  
            }    

            if (ifcObject is IIfcSpatialStructureElement spatial)
            {
                foreach (var relation in spatial.ContainsElements)
                {
                    foreach (var element in relation.RelatedElements)
                    {
                        node.Children.Add(AddNode(element, parent));
                    }   
                }    
            }

            return node;
        }

        private void OnNodeSelectionChanged(NodeItem node, bool isSelected)
        {
            if (isSelected)
            {
                SelectedNode = node;
                var meshes = node.CollectLabels()
                    .Where(GeometryModels.ContainsKey)
                    .SelectMany(g => GeometryModels[g]);
                SetHighlight(meshes);
            }
            else if (SelectedNode == node)
            {
                SelectedNode = null;
                SetHighlight(new List<GeometryModel3D>());
            }
        }

        public void SelectObject(GeometryModel3D selectObject)
        {
            if (selectObject != null &&
                IfcProducts.TryGetValue(selectObject, out var product) &&
                NodesByLabel.TryGetValue(product.EntityLabel, out var node))
            {
                node.IsSelected = true;
                node.ExpandParents();
            }
            else if (SelectedNode != null)
            {
                SelectedNode.IsSelected = false;
            }
        }

        private void SetHighlight(IEnumerable<GeometryModel3D> models)
        {
            foreach (var model in HighlightedObjects)
            {
                model.Material = model.BackMaterial = Materials[model];
            }

            HighlightedObjects.Clear();

            foreach (var model in models)
            {
                model.Material = model.BackMaterial = HighlightMaterial;
                HighlightedObjects.Add(model);
            }
        }
    }
}
