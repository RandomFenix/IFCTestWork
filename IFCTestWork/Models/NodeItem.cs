using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xbim.Ifc4.Interfaces;

namespace IFCTestWork.Models
{
    public class NodeItem : INotifyPropertyChanged
    {
        public IIfcObjectDefinition Entity { get; }
        public NodeItem Parent { get; }
        public ObservableCollection<NodeItem> Children { get; } = new ObservableCollection<NodeItem>();
        public string Name { get; set; }

        private bool _isSelected, _isExpanded;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                OnChanged(nameof(IsSelected));
                _onSelectionChanged?.Invoke(this, value);
            }
        }
        public bool IsExpanded
        {
            get => _isExpanded;
            set 
            { 
                _isExpanded = value; 
                OnChanged(nameof(IsExpanded)); 
            }
        }
        public void ExpandParents()
        {
            for (var p = Parent; p != null; p = p.Parent)
                p.IsExpanded = true;
        }

        public NodeItem(IIfcObjectDefinition entity, NodeItem parent, Action<NodeItem, bool> onSelectionChanged)
        {
            Entity = entity;
            Parent = parent;
            _onSelectionChanged = onSelectionChanged;
            Name = string.IsNullOrWhiteSpace(Entity.Name) ? "(без имени)" : Entity.Name.ToString();
        }

        public List<int> CollectLabels()
        {
            var result = new List<int>();
            Collect(result);
            return result;
        }

        private void Collect(List<int> result)
        {
            result.Add(Entity.EntityLabel);
            foreach (var child in Children)
            {
                child.Collect(result);
            }
        }

        private readonly Action<NodeItem, bool> _onSelectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
