﻿using System;
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
using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    /// <summary>
    /// Interaction logic for Products.xaml
    /// </summary>
    public partial class Products : UserControl
    {
        public Products()
        {
            InitializeComponent();

            // Resolve ProductVM from the DI container
            DataContext = App.ServiceProvider.GetService<ProductVM>();
        }        // Event handler for price-affecting field changes to trigger price recalculation
        private async void PriceAffecting_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is ProductVM viewModel)
            {
                await viewModel.RecalculateProductPriceAsync();
            }
        }
    }
}
