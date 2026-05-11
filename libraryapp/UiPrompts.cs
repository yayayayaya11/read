using System.Windows;
using System.Windows.Controls;

namespace libraryapp
{
    public static class UiPrompts
    {
        public static string AskMultiline(string title, string hint = null)
        {
            var w = new Window
            {
                Title = title,
                Width = 440,
                Height = 260,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            var root = new DockPanel();
            var tb = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(8),
                ToolTip = hint
            };
            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8) };
            string result = null;
            var ok = new Button { Content = "OK", Width = 90, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            ok.Click += (_, __) => { result = tb.Text; w.DialogResult = true; };
            var cancel = new Button { Content = "Отмена", Width = 90, IsCancel = true };
            bar.Children.Add(ok);
            bar.Children.Add(cancel);
            DockPanel.SetDock(bar, Dock.Bottom);
            root.Children.Add(bar);
            root.Children.Add(tb);
            w.Content = root;
            return w.ShowDialog() == true ? result : null;
        }

        public static string AskPassword(string title)
        {
            var w = new Window
            {
                Title = title,
                Width = 360,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            var sp = new StackPanel { Margin = new Thickness(12) };
            var pb = new PasswordBox { Margin = new Thickness(0, 0, 0, 12) };
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            string result = null;
            var ok = new Button { Content = "OK", Width = 80, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            ok.Click += (_, __) => { result = pb.Password; w.DialogResult = true; };
            var cancel = new Button { Content = "Отмена", Width = 80, IsCancel = true };
            row.Children.Add(ok);
            row.Children.Add(cancel);
            sp.Children.Add(pb);
            sp.Children.Add(row);
            w.Content = sp;
            return w.ShowDialog() == true ? result : null;
        }
    }
}
