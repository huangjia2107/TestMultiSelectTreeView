using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace TestMultiSelectTreeView
{
    public class TestSource : ViewModelBase
    {
        private bool _itemsChangedFlag;
        public bool ItemsChangedFlag
        {
            get { return _itemsChangedFlag; }
            set { _itemsChangedFlag = value; InvokePropertyChanged("ItemsChangedFlag"); }
        }

        public ObservableCollection<TestModel> ModelCollection { get; set; }
    }

    public class TestModel : ViewModelBase
    {
        public TestModel()
        {
            ModelCollection = new ObservableCollection<TestModel>();
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; InvokePropertyChanged("Name"); }
        }

        private bool _isGroup;
        public bool IsGroup
        {
            get { return _isGroup; }
            set { _isGroup = value; InvokePropertyChanged("IsGroup"); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; InvokePropertyChanged("IsSelected"); }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; InvokePropertyChanged("IsSelected"); }
        }

        public ObservableCollection<TestModel> ModelCollection { get; set; }
    }
}
