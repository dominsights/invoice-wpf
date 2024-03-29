﻿using DgSystems.NFe.ViewModels.Commands;
using EmissorNFe.Imposto;
using EmissorNFe.Model;
using EmissorNFe.Produto;
using EmissorNFe.View.Destinatario;
using EmissorNFe.View.NotaFiscal;
using EmissorNFe.ViewModel;
using NFe.Core.Messaging;
using NFe.WPF.Commands;
using NFe.WPF.View.NotaFiscal;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DgSystems.NFe.ViewModels;

namespace EmissorNFe.NotaFiscal
{
    /// <summary>
    /// Interaction logic for NotaFiscalMainWindow.xaml
    /// </summary>
    public partial class NotaFiscalMainWindow : UserControl
    {
        public NotaFiscalMainWindow()
        {
            this.DataContext = (Application.Current.Resources["Locator"] as ViewModelLocator).NotaFiscalMain;
            InitializeComponent();


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current;
            var mainWindow = app.MainWindow;

            new NFCeWindow { Owner = mainWindow }.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var app = Application.Current;
            var mainWindow = app.MainWindow;

            new NFeWindow { Owner = mainWindow }.ShowDialog();
        }
    }
}
