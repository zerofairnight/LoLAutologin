using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using LOLAutologin.Properties;

namespace LOLAutologin
{
    public static class LocalCommands
    {
        public static readonly RoutedUICommand Add = new RoutedUICommand(StringResources.Add, "Add", typeof(LocalCommands));

        public static readonly RoutedUICommand Edit = new RoutedUICommand(StringResources.Edit, "Edit", typeof(LocalCommands));

        public static readonly RoutedUICommand Delete = new RoutedUICommand(StringResources.Delete, "Delete", typeof(LocalCommands));

        public static readonly RoutedUICommand Login = new RoutedUICommand(StringResources.Login, "Login", typeof(LocalCommands));

        public static readonly RoutedUICommand Shortcut = new RoutedUICommand(StringResources.CreateShortcut, "Shortcut", typeof(LocalCommands));
    }
}
