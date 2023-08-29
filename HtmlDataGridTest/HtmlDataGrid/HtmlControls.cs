using System;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows;

using Interop = OpenSilver.Interop;

namespace Combine.Common.Silverlight.Controls.UserControls
{
    public abstract class HtmlElement : Panel
    {
        private static int _id = 0;
        private readonly int uniqueIdentifier = Interlocked.Increment(ref _id);
        public HtmlElement()
        {
            Loaded += (o, s) =>
            {
                this.Visibility = Visibility.Collapsed;
            };
        }
        public string UniqueIdentifier
        {
            get { return "html-element-" + uniqueIdentifier.ToString(); }
        }
        public abstract string Html { get; }
        public virtual void AttachEvents() { }
        public virtual void DetachEvents() { }

        public static readonly DependencyProperty TooltipProperty = DependencyProperty.Register("Tooltip", typeof(string), typeof(HtmlElement), null);

        public string Tooltip
        {
            get { return (string)GetValue(TooltipProperty); }
            set { SetValue(TooltipProperty, value); }
        }

        internal static string EscapeStringForUseInJavaScript(string s)
        {
            if (s == null || s.Length == 0)
            {
                return string.Empty;
            }

            int i;
            int len = s.Length;
            StringBuilder sb = new StringBuilder();
            string t;

            for (i = 0; i < len; i += 1)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '`':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append($"\\u{(int)c:x4}");
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }

    public class HtmlRadioButton : HtmlElement
    {
        private IDisposable _checkedCallback;
        private IDisposable _uncheckedCallback;

        public HtmlRadioButton()
        {
            this.IsEnabledChanged += HtmlRadioButton_IsEnabledChanged;
            this.BindingValidationError += HtmlRadioButton_BindingValidationError;
        }

        private void HtmlRadioButton_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                Interop.ExecuteJavaScriptVoid($@"
var el = document.getElementById('{UniqueIdentifier}');
if (el)
{{
    el.classList.add('rb-validation-error');
    el.title = '{e.Error.ErrorContent}';
}}
");
            }
            else if (e.Action == ValidationErrorEventAction.Removed)
            {
                Interop.ExecuteJavaScriptVoid($@"
var el = document.getElementById('{UniqueIdentifier}');
if (el)
{{
    el.classList.remove('rb-validation-error');
    el.removeAttribute('title');
}}
");
            }
        }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked", typeof(bool?), typeof(HtmlRadioButton), new PropertyMetadata(false, IsCheckedChanged));

        public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
            "GroupName", typeof(string), typeof(HtmlRadioButton), new PropertyMetadata(string.Empty));

        public bool? IsChecked
        {
            get { return (bool?)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public string GroupName
        {
            get { return (string)GetValue(GroupNameProperty); }
            set { SetValue(GroupNameProperty, value); }
        }

        private static void IsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HtmlRadioButton rb = (HtmlRadioButton)d;
            bool? isChecked = (bool?)e.NewValue;
            if (isChecked.HasValue)
            {
                Interop.ExecuteJavaScriptVoid($@"
var el = document.getElementById('{rb.UniqueIdentifier}');
if (el)
{{
    el.checked = {isChecked.Value.ToString().ToLower()};
}}
");
            }
        }

        private void HtmlRadioButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isEnabled = e.NewValue == null ? true : !(bool)e.NewValue;
            Interop.ExecuteJavaScriptVoid(
                $"var el = document.getElementById('{UniqueIdentifier}'); if (el) el.disabled = {isEnabled.ToString().ToLower()};");
        }

        public override void AttachEvents()
        {
            _checkedCallback?.Dispose();
            _checkedCallback = Interop.CreateJavascriptCallback((Action)(() =>
            {
                Interop.ExecuteJavaScriptVoid($@"
var el = document.getElementById('{UniqueIdentifier}');
if (el)
{{
var name = el.getAttribute('name');
var els = document.querySelectorAll(""[name='"" + name + ""']"");
for (var i = 0; i < els.length; i++)
{{
    if (els[i] != el)
    {{
        if(!els[i].checked)
        {{
            const event = document.createEvent(""Event"");
            event.initEvent(""unchecked"", true, true);
            els[i].dispatchEvent(event);
        }}
    }}
}}
}}
");
                this.IsChecked = true;
            }));

            _uncheckedCallback?.Dispose();
            _uncheckedCallback = Interop.CreateJavascriptCallback((Action)(() => this.IsChecked = false));

            Interop.ExecuteJavaScriptVoidAsync($@"(function (el) {{
el.addEventListener('change', $0);
el.addEventListener('unchecked', $1);

el.addEventListener('mousedown', (event) => {{ event.stopPropagation(); }});

el.parentElement.addEventListener('keydown', (event) => 
{{
    if (event.code == 'Space' && !el.disabled) el.checked = true;
}});

if ({this.IsChecked.ToString().ToLower()})
{{
    el.checked = true;

    const event = document.createEvent(""Event"");
    event.initEvent(""change"", true, true);
    el.dispatchEvent(event);
}}
}})(document.getElementById('{UniqueIdentifier}'));", _checkedCallback, _uncheckedCallback);
        }

        public override void DetachEvents()
        {
            _checkedCallback?.Dispose();
            _checkedCallback = null;
            _uncheckedCallback?.Dispose();
            _uncheckedCallback = null;
        }

        public override string Html
        {
            get
            {
                string align = "text-align: left;";
                if (this.HorizontalAlignment == HorizontalAlignment.Center)
                {
                    align = "text-align: center;";
                }
                else if (this.HorizontalAlignment == HorizontalAlignment.Right)
                {
                    align = "text-align: right;";
                }
                string html = $"<div tabindex='0' style='{align}'><input tabindex=-1 type=\"radio\" name=\"{GroupName}\" id=\"{UniqueIdentifier}\" ";
                if (!this.IsEnabled) { html += "disabled "; }
                html += " /></div>";

                return html;
            }
        }
    }

    public class HtmlImage : HtmlElement
    {
        private IDisposable _clickCallback;

        public string Source { get; set; }
        public event RoutedEventHandler Click;

        public override void AttachEvents()
        {
            if (Click != null)
            {
                _clickCallback?.Dispose();
                _clickCallback = Interop.CreateJavascriptCallback((Action)(() => Click?.Invoke(this, null)));

                Interop.ExecuteJavaScriptVoidAsync($@"(function (img) {{
img.addEventListener(""click"", $0);
img.style.cursor = 'pointer';
}})(document.getElementById('{UniqueIdentifier}'));", _clickCallback);
            }
        }

        public override void DetachEvents()
        {
            _clickCallback?.Dispose();
            _clickCallback = null;
        }

        public override string Html
        {
            get
            {
                string title = Tooltip != null ? $"title='{Tooltip}'" : string.Empty;
                string display = this.Visibility == Visibility.Collapsed ? "display: none" : "";
                return $"<img {title} id=\"{UniqueIdentifier}\" style=\"{display}\" src=\"resources/{ExecutingAssembly.Name}{Source}\">";
            }
        }
    }

    public class HtmlStackPanel : HtmlElement
    {
        public Orientation Orientation { get; set; }
        public override string Html
        {
            get
            {
                string html = String.Empty;
                foreach (var c in Children)
                {
                    html += (c as HtmlElement).Html;
                }

                if (Orientation == Orientation.Horizontal)
                {
                    html = "<div style=\"display: flex;\">" + html + "</div>";
                }
                return html;
            }
        }
    }

    public class HtmlTextBlock : HtmlElement
    {
        private IDisposable _inputCallback;
        private IDisposable _keyDownCallback;
        private IDisposable _clickCallback;

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(TextBlock), new PropertyMetadata(string.Empty));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public bool IsEditable { get; set; } = false;

        public override void AttachEvents()
        {
            if (IsEditable)
            {
                _inputCallback?.Dispose();
                _inputCallback = Interop.CreateJavascriptCallback((Action<string>)((text) => Text = text));

                _keyDownCallback?.Dispose();
                _keyDownCallback = Interop.CreateJavascriptCallback((Action<object>)((o) =>
                {
                    if (o.ToString() == "Space")
                    {
                        if (IsEnabled)
                        {
                            EnterEditMode();
                        }
                    }
                }));

                _clickCallback?.Dispose();
                _clickCallback = Interop.CreateJavascriptCallback((Action)(() => EnterEditMode()));
                
                Interop.ExecuteJavaScriptVoidAsync(
                    $"document.getElementById('{UniqueIdentifier}').parentElement.addEventListener('click', $0);", _clickCallback);
                Interop.ExecuteJavaScriptVoidAsync(
                    $"document.getElementById('{UniqueIdentifier}').parentElement.addEventListener('keydown', (event) => {{ $0(event.code); }});", _keyDownCallback);
            }
        }
        
        public override void DetachEvents()
        {
            _inputCallback?.Dispose();
            _inputCallback = null;
            _keyDownCallback?.Dispose();
            _keyDownCallback = null;
            _clickCallback?.Dispose();
            _clickCallback = null;
        }

        private void EnterEditMode()
        {
            if (_inputCallback == null) return;

            Interop.ExecuteJavaScriptVoidAsync($@"(function (el) {{
var enabled = {IsEnabled.ToString().ToLower()};
// Create input element if enabled and not created yet
if (el.parentElement.children.length === 1 && enabled)
{{
    var input = document.createElement('input');
    input.setAttribute('type', 'text');
    input.setAttribute('value', ""{EscapeStringForUseInJavaScript(Text)}"");
    input.setAttribute('spellcheck', 'false');
    input.style.width = el.clientWidth + 'px';
    input.style.fontSize = '10px';
    input.style.outline = 'none';
    input.addEventListener('keydown', (event) => {{ if (event.code != 'Tab') event.stopPropagation(); }});
    input.addEventListener('mousedown', (event) => {{ event.stopPropagation(); }});
    input.addEventListener('mouseup', (event) => {{ event.stopPropagation(); }});
    input.addEventListener('input', (event) => {{ 
        el.innerText = input.value;
        if ({IsAutoTooltip.ToString().ToLower()}) el.title = input.value;
        $0(input.value);
    }});
    input.addEventListener('focusout', (event) => {{
        el.style.display = 'block';
        input.remove();
    }});
    el.style.display = 'none';
    el.parentElement.appendChild(input);
    input.focus();
    var end = input.value.length;
    input.setSelectionRange(end, end);
}}
}})(document.getElementById('{UniqueIdentifier}'));", _inputCallback);
        }

        public bool IsAutoTooltip { get; set; } = false;

        public override string Html
        {
            get
            {
                string tooltip = Tooltip;
                if (IsAutoTooltip) tooltip = Text;
                string title = tooltip != null ? $"title='{EscapeStringForUseInJavaScript(tooltip)}'" : string.Empty;
                return $"<div tabindex=0 {title} id=\"{UniqueIdentifier}\" style=\"white-space: nowrap; overflow: hidden; text-overflow: ellipsis; width: {this.MaxWidth}px;\">{EscapeStringForUseInJavaScript(Text)}</div>";
            }
        }
    }
}
