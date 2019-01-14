using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Win32;

namespace LOLAutologin
{
    public enum RegionUI
    {
        [Description("NA")]
        NorthAmerica,
        [Description("EUW")]
        EUWest,
        [Description("EUNE")]
        EUNordicAndEast,
        [Description("BR")]
        Brazil,
        [Description("TR")]
        Turkey,
        [Description("RU")]
        Russia,
        [Description("LAN")]
        LatinAmericaNorth,
        [Description("LAS")]
        LatinAmericaSouth,
        [Description("AUS & NZ")]
        Oceania,
        [Description("PBE")]
        PBE
    }

    public class UserAccount : DependencyObject
    {
        #region Username

        public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register(
            "Username",
            typeof(string),
            typeof(UserAccount),
            new UIPropertyMetadata(null, new PropertyChangedCallback(ReevalutateDisplayName)),
            new ValidateValueCallback(ValidateLoginString)
       );

        public string Username
        {
            get { return (string)GetValue(UsernameProperty); }
            set { SetValue(UsernameProperty, value); }
        }

        #endregion

        #region Password

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            "Password",
            typeof(string),
            typeof(UserAccount),
            new UIPropertyMetadata(null, new PropertyChangedCallback(ReevalutateDisplayName)),
            new ValidateValueCallback(ValidateLoginString)
        );

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        #endregion

        #region Region

        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register(
            "Region",
            typeof(RegionUI),
            typeof(UserAccount),
            new UIPropertyMetadata(RegionUI.EUWest, new PropertyChangedCallback(ReevalutateDisplayName))
        );

        public RegionUI Region
        {
            get { return (RegionUI)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        #endregion

        #region Note

        public static readonly DependencyProperty NoteProperty = DependencyProperty.Register(
            "Note",
            typeof(string),
            typeof(UserAccount),
            new UIPropertyMetadata(null, new PropertyChangedCallback(ReevalutateDisplayName))
        );

        public string Note
        {
            get { return (string)GetValue(NoteProperty); }
            set { SetValue(NoteProperty, value); }
        }

        #endregion

        #region LauncherPath

        public static readonly DependencyProperty LauncherPathProperty = DependencyProperty.Register(
            "LauncherPath",
            typeof(string),
            typeof(UserAccount),
            new UIPropertyMetadata(ReadLauncherPath()),
            new ValidateValueCallback(ValidateLauncherPath)
        );

        public string LauncherPath
        {
            get { return (string)GetValue(LauncherPathProperty); }
            set { SetValue(LauncherPathProperty, value); }
        }

        #endregion

        #region DisplayName

        private static readonly DependencyPropertyKey DisplayNamePropertyKey = DependencyProperty.RegisterReadOnly(
            "DisplayName",
            typeof(string),
            typeof(UserAccount), new UIPropertyMetadata(null, null, new CoerceValueCallback(OnCoerceDisplayName))
        );

        public static readonly DependencyProperty DisplayNameProperty = DisplayNamePropertyKey.DependencyProperty;

        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
        }

        #endregion
        
        private static string ReadLauncherPath()
        {
            string text = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Riot Games\\League of Legends", "Path", null);
            string result;
            if( text != null )
            {
                result = text + "\\lol.launcher.admin.exe";
            }
            else
            {
                result = null;
            }
            return result;
        }

        private static bool ValidateLoginString(object value)
        {
            string text = (string)value;
            return text == null || (!string.IsNullOrWhiteSpace(text) && !text.Contains(' '));
        }

        private static bool ValidateLauncherPath(object value)
        {
            string text = (string)value;
            return text == null || string.IsNullOrEmpty(text) || text.EndsWith("\\lol.launcher.admin.exe") || text.EndsWith("\\lol.launcher.exe");
        }

        private static void ReevalutateDisplayName(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UserAccount)d).CoerceValue(UserAccount.DisplayNameProperty);
        }

        private static object OnCoerceDisplayName(DependencyObject d, object baseValue)
        {
            UserAccount userAccount = (UserAccount)d;
            string str = string.IsNullOrEmpty(userAccount.Note) ? "" : (" (" + userAccount.Note + ")");
            Type typeFromHandle = typeof(RegionUI);
            MemberInfo[] member = typeFromHandle.GetMember(userAccount.Region.ToString());
            object[] customAttributes = member[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            string description = ((DescriptionAttribute)customAttributes[0]).Description;
            string str2 = string.IsNullOrEmpty(description) ? "" : (", " + description);
            return userAccount.Username + str2 + str;
        }
    }
}
