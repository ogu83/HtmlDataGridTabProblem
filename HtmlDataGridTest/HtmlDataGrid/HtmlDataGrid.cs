using CSHTML5.Internal;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using System.Reflection;
using System.Collections.Generic;

namespace Combine.Common.Silverlight.Controls.UserControls
{
    public static class ExecutingAssembly
    {
        static string _name = Assembly.GetExecutingAssembly().GetName().Name;
        public static string Name => _name;
    }

    public abstract class HtmlDataGridColumn
    {
        public string Header { get; set; }
        public double Width { get; set; }
    }

    public class HtmlDataGridTemplateColumn : HtmlDataGridColumn
    {
        public DataTemplate CellTemplate { get; set; }
    }

    public class HtmlDataGridRow : HtmlElement
    {
        public string Header { get; set; }
        public override string Html
        {
            get
            {
                string img = string.Empty;
                if (Tooltip != null)
                {
                    img = $"<img title='{Tooltip}' width=\"13\" src=\"resources/{ExecutingAssembly.Name}/Images/Icons/information.png\">";
                }
                string _html = $@"<div style=""display: flex; align-items: center; padding: 2px;""> 
                                    <div style=""padding: 2px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;"">{Header}</div>
                                    {img}
                                  </div>";
                return _html;
            }
        }
    }

    public class HtmlDataGrid : Control
    {
        private static bool _initialized = false;
        INTERNAL_HtmlDomElementReference _domElement = null;
        public double ColumnHeaderHeight { get; set; } = 20;
        public double RowHeaderWidth { get; set; } = 100;
        public double CellHeight { get; set; } = 0;
        public event RoutedEventHandler GridLoaded;
        bool HasFocus = false;
        private const string LAST_ELEMENT = "LastElement";

        private IDisposable _resizedCallback;
        private readonly List<HtmlElement> _children = new List<HtmlElement>();

        public HtmlDataGrid()
        {
            Columns = new ObservableCollection<HtmlDataGridColumn>();
            Rows = new ObservableCollection<HtmlDataGridRow>();

            if (!_initialized)
            {
                OpenSilver.Interop.ExecuteJavaScriptVoid($"document.head.insertAdjacentHTML('beforeend', `{CssStyle}`);");
                OpenSilver.Interop.ExecuteJavaScriptVoid($@"
                const styles = document.createElement('script');
                styles.type = 'text/javascript';
                styles.text = `{Scripts}`;
                document.body.appendChild(styles);");
                OpenSilver.Interop.ExecuteJavaScriptVoid($@"
document.createResizeDiv = function(id, callback) {{
    var dragStart = false;
    var positionX = 0;
    var elIndex = 0;
    var elWidth = 0;
    var elNextWidth = 0;
    var table = document.getElementById(id);
    var th = table.getElementsByTagName('th');
    for (var i = 0; i < th.length; i++) {{
        var div = document.createElement(""div"");
        div.className = ""y_resize tb_resize"";
        div.setAttribute(""data-resizecol"", i);

        div.style.right = '-4px';
        div.style.top = '0';

        div.style.position = 'absolute';

        div.style.width = '7px';
        div.style.height = th[i].offsetHeight + 'px';
        div.style.zIndex = 100;
        div.style.cursor = 'col-resize';
            th[i].append(div);

        div.addEventListener(""mousedown"", (event) => {{
            dragStart = true;
            positionX = event.pageX;
            elIndex = parseInt(event.srcElement.getAttribute('data-resizecol'));
            elWidth = th[elIndex].clientWidth;
            if (elIndex < th.length - 1) {{
                elNextWidth = th[elIndex + 1].clientWidth;
            }}
        }});
    }};

    table.addEventListener(""mousemove"", (event) => {{
        if (dragStart) {{
                var diff = event.pageX - positionX - 7;
                var width = elWidth + diff;

                if (width < 10) return;

                var widthPx = width + 'px';
                th[elIndex].style.width = widthPx;
                th[elIndex].style.minWidth = widthPx;
                th[elIndex].children[0].style.width = widthPx;

                var tr = table.getElementsByTagName('tr');
                for (var i = 1; i < tr.length; i++) {{
                    var td = tr[i].getElementsByTagName('td')[elIndex];
                    for (var j = 0; j < td.children.length; j++) {{
                        if (td.children[j].tagName.toLowerCase() === 'img') continue;

                        td.children[j].style.width = widthPx;
                    }}
                }}
                callback();
        }}
    }});

    table.addEventListener(""mouseup"", () => {{
        dragStart = false;
    }});
}}");
                _initialized = true;
            }
            this.GotFocus += HtmlDataGrid_GotFocus;
            this.LostFocus += HtmlDataGrid_LostFocus;
            this.Unloaded += HtmlDataGrid_Unloaded;
        }

        private void HtmlDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            HasFocus = false;
        }

        private void HtmlDataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!HasFocus)
            {
                HasFocus = true;
                OpenSilver.Interop.ExecuteJavaScriptVoid($"htmlDataGridHelpers.gotFocus('{GetTableIndex()}');");
            }
        }

        private void HtmlDataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            ClearChildren();
        }

        private void ClearChildren()
        {
            _resizedCallback?.Dispose();
            _resizedCallback = null;

            foreach (HtmlElement child in _children)
            {
                child.DetachEvents();
            }

            _children.Clear();
        }

        public override object CreateDomElement(object parentRef, out object domElementWhereToPlaceChildren)
        {
            var obj = base.CreateDomElement(parentRef, out domElementWhereToPlaceChildren);
            _domElement = domElementWhereToPlaceChildren as INTERNAL_HtmlDomElementReference;
            OpenSilver.Interop.ExecuteJavaScriptVoid(
                $"htmlDataGridHelpers.createTable('{_domElement.UniqueIdentifier}', '{GetTableIndex()}');");
            Render();
            return obj;
        }

        public ObservableCollection<HtmlDataGridColumn> Columns { get; set; }
        public ObservableCollection<HtmlDataGridRow> Rows { get; set; }

        public static DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(HtmlDataGrid), new PropertyMetadata(OnItemsSourceChanged));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(HtmlDataGrid), null);


        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as HtmlDataGrid).Render();
        }

        private void CreateColumnHeaders()
        {
            if (Columns.Count > 0)
            {
                string html = "<thead><tr>";
                if (Rows.Count > 0)
                {
                    html += $"<th class='column-header-empty' style='min-width: {RowHeaderWidth}px; position: relative;'></th>";
                }

                foreach (var column in Columns)
                {
                    html += $"<th class='column-header' style='min-width: {column.Width}px; position: relative; white-space: nowrap; height: {ColumnHeaderHeight}px'><div style='overflow: hidden; text-overflow: ellipsis;'>{column.Header}</div></th>";
                }
                html += "</tr></thead>";
                AppendHtml(GetTableIndex(), html);
            }
        }

        private void AddRow(int index)
        {
            if (Columns.Count > 0)
            {
                var html = "<tr>";
                var isLastRow = index == Rows.Count - 1;
                if (index < Rows.Count)
                {
                    html += $"<td class='row-header' >{Rows[index].Html}</td>";
                }
                var cIndex = 0;
                foreach (var column in Columns)
                {
                    var isLastCol = false;
                    if (isLastRow)
                    {
                        isLastCol = Columns.IndexOf(column) == (Columns.Count - 1);
                    }

                    var style = CellHeight != 0 ? $"height: {CellHeight}px" : string.Empty;
                    if (!isLastCol)
                    {
                        html += $"<td id='{GetCellIndex(index, cIndex++)}' style='{style}'></td>";
                    }
                    else
                    {
                        html += $"<td id='{GetCellIndex(index, cIndex++)}' style='{style}' class='{LAST_ELEMENT}'></td>";
                    }
                }
                html += "</tr>";
                AppendHtml(GetTableIndex(), html);
            }
        }

        private void CreateEmptyRows()
        {
            var index = 0;
            foreach (var item in ItemsSource)
            {
                AddRow(index++);
            }
        }

        private void AppendHtml(string id, string html)
        {
            OpenSilver.Interop.ExecuteJavaScriptVoid("htmlDataGridHelpers.appendHtml($0, $1);", id, html);
        }

        private string GetCellIndex(int rIndex, int cIndex)
        {
            return $"{_domElement.UniqueIdentifier}-td-{rIndex}{cIndex}";
        }

        private string GetTableIndex()
        {
            return $"{_domElement.UniqueIdentifier}-table";
        }

        private void RemoveTable()
        {
            OpenSilver.Interop.ExecuteJavaScriptVoid($"htmlDataGridHelpers.removeHtml('{GetTableIndex()}')");
        }

        private void HandleTab()
        {
            OpenSilver.Interop.ExecuteJavaScriptVoid($"htmlDataGridHelpers.handleTab('{GetTableIndex()}');");
        }

        private void CreateResizeDivs()
        {
            _resizedCallback?.Dispose();
            _resizedCallback = OpenSilver.Interop.CreateJavascriptCallback((Action)(() => Width = GetTableSize().Width));
            
            OpenSilver.Interop.ExecuteJavaScriptVoid(
                $@"document.createResizeDiv('{GetTableIndex()}', $0);", _resizedCallback);
        }
        
        public void Render()
        {
            if (_domElement != null && ItemsSource != null)
            {
                bool res = OpenSilver.Interop.ExecuteJavaScriptGetResult<bool>($"htmlDataGridHelpers.getIfAttached('{GetTableIndex()}');");
                if (!res)
                {
                    // Element is not in visual tree anymore
                    return;
                }
                ClearChildren();
                RemoveTable();
                CreateColumnHeaders();
                CreateEmptyRows();

                var rowIndex = 0;
                foreach (var item in ItemsSource)
                {
                    var columnIndex = 0;
                    foreach (var column in Columns)
                    {
                        if (column is HtmlDataGridTemplateColumn tc)
                        {
                            if (tc.CellTemplate.LoadContent() is HtmlElement element)
                            {
                                element.DataContext = item;
                                if (column.Width != 0)
                                {
                                    element.MaxWidth = column.Width;
                                }
                                AppendHtml(GetCellIndex(rowIndex, columnIndex++), element.Html);
                                element.AttachEvents();

                                _children.Add(element);
                            }
                        }
                    }
                    rowIndex++;
                }
                HandleTab();
                CreateResizeDivs();
                GridLoaded?.Invoke(this, null);
            }
        }

        private Size GetSize()
        {
            var size = OpenSilver.Interop.ExecuteJavaScriptGetResult<string>($"htmlDataGridHelpers.getSize('{GetTableIndex()}');");
            int.TryParse(size.Split('|')[0], out var width);
            int.TryParse(size.Split('|')[1], out var height);

            return new Size(width + 10, height + 10);
        }

        private Size GetTableSize()
        {
            var size = OpenSilver.Interop.ExecuteJavaScriptGetResult<string>($"htmlDataGridHelpers.getSize('{GetTableIndex()}');");
            int width = 0;
            int height = 0;
            int.TryParse(size.Split('|')[0], out width);
            int.TryParse(size.Split('|')[1], out height);
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return GetSize();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return GetSize();
        }

        #region CssStyle and Scripts

        private const string Scripts = @"
window.htmlDataGridHelpers = {
    gotFocus: function (id) {
        const el = document.getElementById(id);
        el.setAttribute('tabindex', '0');
        el.focus();
    },
    createTable: function (elementId, tableId) {
        const el = document.getElementById(elementId);
        el.innerHTML += ""<table class='custom-table' id='"" + tableId + ""' style='pointer-events: auto;'></table>"";
    },
    appendHtml: function (id, html) {
        const el = document.getElementById(id); 
        if (el) {
            el.innerHTML += html;
        }
    },
    removeHtml: function (id) {
        const el = document.getElementById(id); 
        if (el) {
            el.innerHTML = '';
        }
    },
    handleTab: function (id) {
        const el = document.getElementById(id);
        let tabSwitch = false;
        el?.addEventListener('keydown', (event) => {
            var elem = event.srcElement;
            elem = $(elem);
            if (elem && elem.hasClass('custom-table')) {
                console.log('tabSwitch:', tabSwitch);
                if (!tabSwitch)
                    event.stopPropagation();
                tabSwitch = false;
            }
            else {
                var isLastElem = $(elem).parent().parent().hasClass('LastElement') || $(elem).parent().hasClass('LastElement');
                console.log('isLastElem:', isLastElem);
                if (!isLastElem) {
                    event.stopPropagation();
                } 
                else {
                    el.focus();
                    tabSwitch = true;
                }
            }
        });
    },
    getSize: function (id) {
        const el = document.getElementById(id);
        if (el) {
            return el.offsetWidth + '|' + el.offsetHeight;
        }
        else {
            return '0|0';
        }
    },
    getIfAttached: function (id) {
        return !!document.getElementById(id);
    },
};
";
        private const string CssStyle = @"
<style>
    .custom-table {
        font-family: Tahoma;
        font-size: 10px;
        border-collapse: collapse;
        color: #17407e;
        border: 0.5px solid #617584;
    }

    .custom-table th {
        font-weight: normal;
        background-color: #dae7f8;
        text-align: left;
    }

    .row-header {
        background-color: #dae7f8;
        text-align: left;
    }

    .custom-table tbody:nth-child(even) {
        background-color: #eef2f8;
    }

    .custom-table tr:hover {
        background-color: #fbec5d;
    }

    .custom-table td {
        border-right: 1px solid #c9caca;
        padding: 0 4px 0 4px;
        color: black;
    }

    .custom-table .row-header {
        color: #17407e;
    }

    .custom-table td:last-child {
        border-right: 1px solid #6c90ba;
    }

    .custom-table th {
        border-right: 1px solid #6c90ba;
        padding: 4px;
    }

    .rb-validation-error:after {
        width: 15px;
        height: 15px;
        border-radius: 15px;
        top: -1px;
        left: -1px;
        position: relative;
        content: '';
        display: inline-block;
        visibility: visible;
        border: 2px solid red;
    }

    .custom-table div:focus{
        outline: none;
        border:1px solid black;
    }
</style>
";
        #endregion
    }
}
