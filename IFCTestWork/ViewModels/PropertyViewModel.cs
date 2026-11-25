using IFCTestWork.Models;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc4.Interfaces;

namespace IFCTestWork.ViewModels
{
    public class PropertyViewModel
    {
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
                            Value = ((IfcPropertySingleValue)property).NominalValue.ToString()
                        });
                    }
                }
            }
            return properties;
        }
    }
}
