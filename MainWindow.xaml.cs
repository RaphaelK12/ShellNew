using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;

namespace ShellNew
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Title = "ShellNew - " + Assembly.GetEntryAssembly()?.GetName().Version;
            AddKeys();
        }

        void AddKeys()
        {
            foreach (string name in Registry.ClassesRoot.GetSubKeyNames())
            {
                if (!name.StartsWith("."))
                    continue;

                using (RegistryKey subKey = Registry.ClassesRoot.OpenSubKey(name))
                    AddKeysRecursive(subKey);
            }
        }

        void AddKeysRecursive(RegistryKey key)
        {
            if (key == null)
                return;

            if (key.Name.EndsWith("\\ShellNew") || key.Name.EndsWith("\\_ShellNew"))
            {
                Item item = new Item(key.Name.Substring(key.Name.IndexOf("\\") + 1));

                if (item.Text != "" && key.GetValueNames().Length > 0)
                    Items.Add(item);

                return;
            }

            foreach (string name in key.GetSubKeyNames())
                using (RegistryKey subKey = key.OpenSubKey(name))
                    AddKeysRecursive(subKey);
        }

        public class Item
        {
            public string Text { get; set; } = "";
            string Key = "";

            public Item(string key)
            {
                Key = key;
                string rootKeyName = key.Substring(0, key.IndexOf("\\"));

                using RegistryKey subKey = Registry.ClassesRoot.OpenSubKey(rootKeyName);
                string fileKeyName = (string)(subKey.GetValue(null) ?? "");

                if (fileKeyName != "")
                    using (RegistryKey fileKey = Registry.ClassesRoot.OpenSubKey(fileKeyName))
                        Text = (string)(fileKey?.GetValue(null) ?? "");
            }

            public bool IsChecked {
                get => Key.EndsWith("\\ShellNew");
                set {
                    using (RegistryKey parentKey = Registry.ClassesRoot.OpenSubKey(Path.GetDirectoryName(Key),
                        RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        string newKey = "";

                        if (value)
                            newKey = Key.Replace("\\_ShellNew", "\\ShellNew");
                        else
                            newKey = Key.Replace("\\ShellNew", "\\_ShellNew");

                        RegUtil.RenameSubKey(parentKey, Path.GetFileName(Key), Path.GetFileName(newKey));
                        Key = newKey;
                    }
                }
            }
        }

        public class RegUtil
        {
            public static bool RenameSubKey(RegistryKey parentKey, string subKeyName, string newSubKeyName)
            {
                CopyKey(parentKey, subKeyName, newSubKeyName);
                parentKey.DeleteSubKeyTree(subKeyName);
                return true;
            }

            public static bool CopyKey(RegistryKey parentKey, string keyNameToCopy, string newKeyName)
            {
                RegistryKey destinationKey = parentKey.CreateSubKey(newKeyName);
                RegistryKey sourceKey = parentKey.OpenSubKey(keyNameToCopy);
                RecurseCopyKey(sourceKey, destinationKey);
                return true;
            }

            static void RecurseCopyKey(RegistryKey sourceKey, RegistryKey destinationKey)
            {
                foreach (string valueName in sourceKey.GetValueNames())
                {
                    object objValue = sourceKey.GetValue(valueName);
                    RegistryValueKind valKind = sourceKey.GetValueKind(valueName);
                    destinationKey.SetValue(valueName, objValue, valKind);
                }

                foreach (string sourceSubKeyName in sourceKey.GetSubKeyNames())
                {
                    RegistryKey sourceSubKey = sourceKey.OpenSubKey(sourceSubKeyName);
                    RegistryKey destSubKey = destinationKey.CreateSubKey(sourceSubKeyName);
                    RecurseCopyKey(sourceSubKey, destSubKey);
                }
            }
        }
    }
}