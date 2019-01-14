using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using LOLAutologin.Properties;
using Microsoft.Win32;
using IWshRuntimeLibrary;
using System.Xml;
using System.Xml.Linq;

namespace LOLAutologin
{
    internal partial class MainWindow : Window
    {
        #region Accounts

        private ObservableCollection<UserAccount> _Accounts = new ObservableCollection<UserAccount>();
        public ObservableCollection<UserAccount> Accounts
        {
            get { return _Accounts; }
        }

        #endregion

        #region DependencyProperty SelectedAccount

        public static readonly DependencyProperty SelectedAccountProperty = DependencyProperty.Register(
            "SelectedAccount",
            typeof(UserAccount),
            typeof(MainWindow),
            new FrameworkPropertyMetadata(null)
        );

        public UserAccount SelectedAccount
        {
            get { return (UserAccount)GetValue(SelectedAccountProperty); }
            set { SetValue(SelectedAccountProperty, value); }
        }

        #endregion

        private UserAccount __SelectedAccount
        {
            get { return list.SelectedItem as UserAccount; }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            base.Loaded += delegate (object _, RoutedEventArgs e)
            {
                this.Load();

                // focus the list, fix the grey menu
                this.list.Focus();
            };

            base.Closed += delegate (object _, EventArgs e)
            {
                this.Save();
            };
        }

        private void SelectedAccountCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (__SelectedAccount != null);
        }

        private void AddCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AccountEditor accountEditor = new AccountEditor();
            accountEditor.Owner = this;
            if (accountEditor.ShowDialog() ?? false)
            {
                this.Accounts.Add(accountEditor.Account);
            }
        }

        private void EditCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UserAccount selectedAccount = this.SelectedAccount;
            if (selectedAccount != null)
            {
                if (new AccountEditor(selectedAccount)
                {
                    Owner = this
                }.ShowDialog() ?? false)
                {
                }
            }
        }

        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UserAccount selectedAccount = this.SelectedAccount;
            if (selectedAccount != null)
            {
                this.Accounts.Remove(selectedAccount);
            }
        }

        private void LoginCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UserAccount selectedAccount = this.SelectedAccount;
            if (selectedAccount != null)
            {
                new LolAutoLogger().Login(selectedAccount.Username, selectedAccount.Password, selectedAccount.LauncherPath);
            }
        }
        private void ShortcutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UserAccount selectedAccount = this.SelectedAccount;
            if (selectedAccount != null)
            {
                WshShell wshShellClass = new WshShell();
                IWshShortcut wshShortcut = wshShellClass.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + selectedAccount.Username + ".lnk") as IWshShortcut;
                wshShortcut.Arguments = string.Concat(new string[]
                {
                    "-u ",
                    selectedAccount.Username,
                    " -p ",
                    selectedAccount.Password,
                    " -l \"",
                    selectedAccount.LauncherPath,
                    "\""
                });
                wshShortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
                wshShortcut.WindowStyle = 1;
                wshShortcut.Description = "Login with " + selectedAccount.DisplayName;
                wshShortcut.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                wshShortcut.IconLocation = selectedAccount.LauncherPath;
                wshShortcut.Save();
            }
        }



        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.AddCommand_Executed(null, null);
        }
        private void list_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ListBox)sender).UnselectAll();
        }


        private void Save()
        {
            XmlWriter xmlWriter = XmlWriter.Create("settings.xml", new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t"
            });
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Settings");
            foreach (UserAccount current in this.Accounts)
            {
                xmlWriter.WriteStartElement("UserAccount");
                xmlWriter.WriteStartElement("Username");
                xmlWriter.WriteCData(current.Username);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteStartElement("Password");
                xmlWriter.WriteCData(current.Password);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteElementString("Region", current.Region.ToString());
                xmlWriter.WriteStartElement("Note");
                xmlWriter.WriteCData(current.Note);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteStartElement("LauncherPath");
                xmlWriter.WriteCData(current.LauncherPath);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        private void Load()
        {
            if (!System.IO.File.Exists("settings.xml"))
                return;

            this.Accounts.Clear();

            XDocument xDocument = XDocument.Load("settings.xml");
            foreach (XElement account in xDocument.Descendants("UserAccount"))
            {
                try
                {
                    this.Accounts.Add(new UserAccount
                    {
                        Username = account.Element("Username").Value,
                        Password = account.Element("Password").Value,
                        Region = (RegionUI)Enum.Parse(typeof(RegionUI), account.Element("Region").Value),
                        Note = account.Element("Note").Value,
                        LauncherPath = account.Element("LauncherPath").Value
                    });
                }
                catch
                {

                }
            }
        }
    }
}
