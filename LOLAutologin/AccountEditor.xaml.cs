using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace LOLAutologin
{
    /// <summary>
    /// Interaction logic for AccountEditor.xaml
    /// </summary>
    public partial class AccountEditor : Window
    {
        public static readonly DependencyProperty AccountProperty = DependencyProperty.Register(
            "Account",
            typeof(UserAccount),
            typeof(AccountEditor),
            new UIPropertyMetadata(null)
        );

        public UserAccount Account
        {
            get { return (UserAccount)base.GetValue(AccountEditor.AccountProperty); }
            set { base.SetValue(AccountEditor.AccountProperty, value); }
        }
        
        public AccountEditor() : this(new UserAccount())
        {
        }

        public AccountEditor(UserAccount account)
        {
            if (account == null)
            {
                throw new ArgumentNullException("account", "account is null.");
            }
            this.InitializeComponent();
            this.Account = account;
            base.DataContext = this.Account;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Riot Games\\League of Legends", "Path", null);
            openFileDialog.Filter = "LOL Launcher|*lol.launcher.admin.exe;*lol.launcher.exe";
            if (openFileDialog.ShowDialog() ?? false)
            {
                this.Account.LauncherPath = openFileDialog.FileName;
            }
        }

        private void Button_Click_OK(object sender, RoutedEventArgs e)
        {
            base.DialogResult = new bool?(true);
        }

        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            base.DialogResult = new bool?(false);
        }
    }
}
