using SutherlandScript.ViewModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;


namespace SutherlandScript
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModelMain vm = new ViewModelMain();
            DataContext = vm;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            tbx_reloc.Text = "";
        }

        private void Tbx_reloc_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (tbx_reloc.Text == "")
                tbx_reloc.Text = "Record Locator";
        }
    }
}
