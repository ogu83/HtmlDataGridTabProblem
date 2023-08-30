using Combine.Common.Silverlight.Controls.UserControls;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using OpenSilver.Internal.Xaml;
using RuntimeHelpers = OpenSilver.Internal.Xaml.RuntimeHelpers;
using System.Windows.Data;

namespace HtmlDataGridTest
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;

            var itemSource = new List<string>() { "hello", "bye" };
            var htmlColumns = new List<HtmlDataGridTemplateColumn>();

            var htmlCellDataTemplate = new DataTemplate();
            RuntimeHelpers.SetTemplateContent(
                htmlCellDataTemplate,
                RuntimeHelpers.Create_XamlContext(),
                (owner, context) =>
                {
                    var radioButton = new HtmlRadioButton()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    RuntimeHelpers.SetTemplatedParent(radioButton, owner);
                    return radioButton;
                });

            var htmlColumn1 = new HtmlDataGridTemplateColumn
            {
                Header = "Col1",
                Width = 200,
                CellTemplate = htmlCellDataTemplate
            };
            htmlColumns.Add(htmlColumn1);

            var htmlColumn2 = new HtmlDataGridTemplateColumn
            {
                Header = "Col2",
                Width = 200,
                CellTemplate = htmlCellDataTemplate
            };
            htmlColumns.Add(htmlColumn2);
            
            htmlDataGrid.Columns.AddRange(htmlColumns);

            var htmlRows = new List<HtmlDataGridRow> { new HtmlDataGridRow { Header = "Row1" }, new HtmlDataGridRow { Header = "Row2" } };
            htmlDataGrid.Rows.AddRange(htmlRows);

            htmlDataGrid.ItemsSource = itemSource;

            htmlDataGrid1.Columns.AddRange(htmlColumns);
            htmlDataGrid1.Rows.AddRange(htmlRows);
            htmlDataGrid1.ItemsSource = itemSource;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await OpenSilver.Interop.LoadJavaScriptFile($"ms-appx:///HtmlDataGridTest/Scripts/jquery-3.6.3.min.js");
        }
    }
}