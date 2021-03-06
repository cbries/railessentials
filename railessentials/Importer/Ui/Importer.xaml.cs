// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Importer.xaml.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace railessentials.Importer.Ui
{
    public partial class Importer
    {
        private const string ImportSuffix = "RailEssentials_";

        private readonly Configuration _cfg;

        public string ImportedWorkspace { get; private set; }
        public bool IsCanceled { get; private set; }

        public Importer(Configuration cfg)
        {
            InitializeComponent();

            _cfg = cfg;

            CmdImport.IsEnabled = false;
        }

        private void CmdSelectInput_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "plan.xml |plan.xml";
            if (openFileDialog.ShowDialog() == true)
                TxtInputfile.Text = openFileDialog.FileName;

            CmdImport.IsEnabled = !string.IsNullOrEmpty(TxtInputfile.Text);
        }

        private void ShowError(string msg)
        {
            MessageBox.Show(msg, Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CmdImport_Click(object sender, RoutedEventArgs e)
        {
            var inputFile = TxtInputfile.Text;
            var directory = Path.GetDirectoryName(inputFile);
            var wsname = Path.GetFileName(directory);
            var newwsname = $"{ImportSuffix}{wsname}";
            var outputDirectory = Path.Combine(Globals.RootWorkspace, newwsname);

            outputDirectory = outputDirectory.Replace("/", "\\");

            var absOutputDirectory = new DirectoryInfo(outputDirectory).FullName;

            if (!Directory.Exists(absOutputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(absOutputDirectory);
                }
                catch(Exception ex)
                {
                    ShowError($"Import failed: {ex.Message}");
                    return;
                }
            }

            ImportedWorkspace = newwsname;

            var importer = new Rocrail.ImportRocrail {
                ThemePath = Path.Combine(_cfg.Theme.Root, _cfg.Theme.Name + ".json")
            };
            var res = importer.Execute(inputFile, absOutputDirectory, out var errorMessage);
            if (!res)
                ShowError(errorMessage);
            else
            {
                if(ChkOpenWorkspace.IsChecked != null && ChkOpenWorkspace.IsChecked.Value)
                    Process.Start("explorer.exe", $"{absOutputDirectory}");

                IsCanceled = false;

                Close();
            }
        }

        private void CmdCancel_OnClick(object sender, RoutedEventArgs e)
        {
            IsCanceled = true;

            Close();
        }
    }
}
