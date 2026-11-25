using IFCTestWork.Models;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace IFCTestWork.ViewModels
{
    public class PropertyViewModel
    {
        public List<TableItem> GetAttributes(IIfcObject ifcObject)
        {
            List<TableItem> attributes = new List<TableItem>();
            attributes.Add(new TableItem
            {
                Parameter = "Класс",
                Value = ifcObject.ExpressType.ExpressName
            });
            attributes.Add(new TableItem
            {
                Parameter = "Метка (#)",
                Value = ifcObject.EntityLabel.ToString()
            });
            attributes.Add(new TableItem
            {
                Parameter = "GlobalId",
                Value = ifcObject.GlobalId.ToString()
            });
            attributes.Add(new TableItem
            {
                Parameter = "Имя",
                Value = ifcObject.Name
            });
            attributes.Add(new TableItem
            {
                Parameter = "Описание",
                Value = ifcObject.Description
            });
            return attributes;
        }
        public List<TableItem> GetProperties(MainViewModel mainViewModel, IIfcObject ifcObject)
        {
            List<TableItem> properties = new List<TableItem>();
            var propertyRelations = mainViewModel.model.Instances.OfType<IIfcRelDefinesByProperties>().Where(x => x.RelatedObjects.Contains(ifcObject));

            foreach (var rel in propertyRelations)
            {
                var propertySet = rel.RelatingPropertyDefinition;

                if (propertySet is IIfcPropertySet set)
                {
                    foreach (var property in set.HasProperties)
                    {
                        properties.Add(new TableItem()
                        {
                            Parameter = property.Name,
                            Value = ((IIfcPropertySingleValue)property).NominalValue.ToString()
                        });
                    }
                }
            }
            return properties;
        }
        public List<TableItem> GetMaterials(IIfcObject ifcObject)
        {
            List<TableItem> materials = new List<TableItem>();

            foreach (var rel in ifcObject.HasAssociations.OfType<IIfcRelAssociatesMaterial>())
            {
                AddMaterialSelect(materials, rel.RelatingMaterial);
            }
            return materials;
        }
        private static void AddMaterialSelect(List<TableItem> rows, IIfcMaterialSelect material)
        {
            switch (material)
            {
                case IIfcMaterial ifcMaterial:
                    rows.Add(new TableItem()
                        {
                        Parameter = "Материал",
                        Value = ifcMaterial.Name.ToString()
                    });
                    break;

                case IIfcMaterialLayerSetUsage usage:
                    AddLayerSet(rows, usage.ForLayerSet);
                    break;

                case IIfcMaterialLayerSet layerSet:
                    AddLayerSet(rows, layerSet);
                    break;

                case IIfcMaterialConstituentSet constituentSet:
                    foreach (var constituent in constituentSet.MaterialConstituents)
                    {
                        rows.Add(new TableItem() 
                        { 
                            Parameter = constituent.Name?.ToString() ?? "Составляющая", 
                            Value = constituent.Material?.Name.ToString() ?? "" 
                        });
                    }
                    break;

                case IIfcMaterialProfileSetUsage profileSetUsage:
                    foreach (var profile in profileSetUsage.ForProfileSet.MaterialProfiles)
                    {
                        rows.Add(new TableItem() 
                        { 
                            Parameter = profile.Name?.ToString() ?? "Профиль", 
                            Value = profile.Material?.Name.ToString() ?? "" 
                        });
                    }  
                    break;

                case IIfcMaterialProfileSet profileSet:
                    foreach (var profile in profileSet.MaterialProfiles)
                    {
                        rows.Add(new TableItem() 
                        { 
                            Parameter = profile.Name?.ToString() ?? "Профиль", 
                            Value = profile.Material?.Name.ToString() ?? "" 
                        });
                    } 
                    break;

                case IIfcMaterialList list:
                    foreach (var materialList in list.Materials)
                    {
                        rows.Add(new TableItem() 
                        { 
                            Parameter = "Материал", 
                            Value = materialList.Name.ToString() 
                        });
                    }   
                    break;
            }
        }

        private static void AddLayerSet(List<TableItem> rows, IIfcMaterialLayerSet set)
        {
            if (set == null)
            {
                return;
            }

            if (set.LayerSetName != null)
                rows.Add(new TableItem() { Parameter = "Набор слоёв", Value = set.LayerSetName.ToString() });

            int i = 1;
            foreach (var layer in set.MaterialLayers)
                rows.Add(new TableItem()
                {
                    Parameter = $"Слой {i++}",
                    Value = $"{layer.Material?.Name.ToString() ?? "(без материала)"}, толщина {(layer.LayerThickness)}"
                });
        }
    }
}
