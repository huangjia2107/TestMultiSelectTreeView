using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TestMultiSelectTreeView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TestSource _testSource { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _testSource = new TestSource
            {
                ModelCollection = new ObservableCollection<TestModel> 
                {
                    new TestModel { Name = "AAA" },
                    new TestModel 
                    { 
                        Name = "BBB", 
                        IsGroup = true,
                        ModelCollection = new ObservableCollection<TestModel>
                        {
                            new TestModel { Name = "Group_BBB_111" },
                            new TestModel { Name = "Group_BBB_222" },
                            new TestModel { Name = "Group_BBB_333" },
                        }
                    },
                    new TestModel { Name = "CCC" },
                    new TestModel { Name = "DDD" },
                    new TestModel { 
                        Name = "EEE", 
                        IsGroup = true,
                        ModelCollection = new ObservableCollection<TestModel>
                        {
                            new TestModel { Name = "Group_EEE_111" },
                            new TestModel { Name = "Group_EEE_222" },
                            new TestModel { Name = "Group_EEE_333" },
                        }
                    }
                }
            };

            this.DataContext = _testSource;
        }

        int j = 0;
        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            _testSource.ModelCollection[0].ModelCollection.Add(new TestModel { Name = "Test Add " + j, IsSelected = true });
            j++;

            _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
        }

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_testSource.ModelCollection[0].ModelCollection.Count > 0)
            {
                _testSource.ModelCollection[0].ModelCollection.RemoveAt(0);
                _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
            }
        }

        private void Move_Button_Click(object sender, RoutedEventArgs e)
        {
            _testSource.ModelCollection[1].ModelCollection.Move(0, 2);

            _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;

            ShowSelectedItems();
        }

        int i = 0;
        private void Replace_Button_Click(object sender, RoutedEventArgs e)
        {
            _testSource.ModelCollection[1].ModelCollection[0] = new TestModel { Name = "Test Replace " + i, IsSelected = true };
            i++;

            _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
        }

        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {

            this.DataContext = null;

            this.DataContext = _testSource;
        }

        private void ShowSelectedItems()
        {
            if (treeView.SelectedItems != null)
            {
                string s = string.Empty;
                foreach (var d in treeView.SelectedItems)
                {
                    s += (d as TestModel).Name + " | ";
                }

                if (!string.IsNullOrEmpty(s))
                    System.Diagnostics.Trace.TraceInformation("Time = {0}, SelectedItems = {1}", DateTime.Now.ToString("HH:mm:ss,fff"), s.TrimEnd(" | ".ToCharArray()));
                else
                    System.Diagnostics.Trace.TraceInformation("Time = {0}, SelectedItems is empty", DateTime.Now.ToString("HH:mm:ss,fff"));
            }
        }

        private void treeView_SelectedItemsChanged(object sender, RoutedPropertyChangedEventArgs<System.Collections.IList> e)
        {
            ShowSelectedItems();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (treeView.ItemContainerGenerator.ContainerFromIndex(treeView.Items.Count -1) as FrameworkElement).BringIntoView();
        }
    }
}

