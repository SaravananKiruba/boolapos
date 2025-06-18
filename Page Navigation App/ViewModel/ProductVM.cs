using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class ProductVM : ViewModelBase
    {
        private readonly ProductService _productService;
        private readonly SupplierService _supplierService;
        private readonly RateMasterService _rateService;  // Added RateMasterService

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }

        public ProductVM(
            ProductService productService, 
            SupplierService supplierService,
            RateMasterService rateService)  // Added RateMasterService parameter
        {
            _productService = productService;
            _supplierService = supplierService;
            _rateService = rateService;
            
            LoadProducts();
            LoadSuppliers();
            InitializeCollections();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateProduct(), _ => CanAddOrUpdateProduct());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchProducts(), _ => true);
        }

        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public ObservableCollection<Supplier> Suppliers { get; set; } = new ObservableCollection<Supplier>();
        public ObservableCollection<string> MetalTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Purities { get; set; } = new ObservableCollection<string>();

        private Product _selectedProduct = new Product();
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
               
                OnPropertyChanged();
            }
        }

      

        private string _searchName;
        public string SearchName
        {
            get => _searchName;
            set
            {
                _searchName = value;
                OnPropertyChanged();
                AutoSelectProduct();
            }
        }

        private void InitializeCollections()
        {
            MetalTypes.Clear();
            Purities.Clear();

            // Initialize metal types
            MetalTypes.Add("Gold");
            MetalTypes.Add("Silver");
            MetalTypes.Add("Platinum");

            // Initialize purities
            Purities.Add("24k");
            Purities.Add("22k");
            Purities.Add("18k");
            Purities.Add("14k");
            Purities.Add("92.5"); // For Silver
            Purities.Add("95.0"); // For Platinum
        }

        private async void LoadProducts()
        {
            Products.Clear();
            var products = await _productService.GetAllProducts();
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }        private async void LoadSuppliers()
        {
            try
            {
                Suppliers.Clear();
                var suppliers = await _supplierService.GetAllSuppliers();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
                
                // Log supplier count for debugging
                Console.WriteLine($"Loaded {Suppliers.Count} suppliers");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error");
            }
        }

        private void AutoSelectProduct()
        {
            var matchedProduct = Products.FirstOrDefault(p =>
                !string.IsNullOrEmpty(SearchName) && p.ProductName.Contains(SearchName));

            if (matchedProduct != null)
            {
                SelectedProduct = matchedProduct;
            }
            else
            {
                SelectedProduct = new Product
                {
                    ProductName = SearchName,
                    IsActive = true
                };
            }
        }        private async void AddOrUpdateProduct()
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(SelectedProduct.MetalType) ||
                    string.IsNullOrEmpty(SelectedProduct.Purity))
                {
                    System.Windows.MessageBox.Show("Please fill in all required fields", "Validation Error");
                    return;
                }
                
                // Ensure supplier is selected
                if (SelectedProduct.SupplierID <= 0 && Suppliers.Count > 0)
                {
                    System.Windows.MessageBox.Show("Please select a supplier", "Validation Error");
                    return;
                }
                
                // Log for debugging
                Console.WriteLine($"Saving product with SupplierID: {SelectedProduct.SupplierID}");

                // Calculate prices in INR
                decimal currentRate = await GetCurrentMetalRate();
                if (currentRate == 0)
                {
                    System.Windows.MessageBox.Show("No current rate found for selected metal type and purity", "Rate Error");
                    return;
                }

                // Calculate base price directly without await
                SelectedProduct.BasePrice = Math.Round(SelectedProduct.NetWeight * currentRate, 2);
                
                // Calculate making charges
                decimal makingCharges = SelectedProduct.MakingCharges;               

                // Calculate wastage
                decimal wastagePercentage = SelectedProduct.WastagePercentage;
               
                decimal wastageAmount = (SelectedProduct.BasePrice * wastagePercentage) / 100;
                decimal makingAmount = (SelectedProduct.BasePrice * makingCharges) / 100;

                // Calculate final price with all components - ensure we're not awaiting a decimal
                SelectedProduct.FinalPrice = Math.Round(
                    SelectedProduct.BasePrice + 
                    makingAmount + 
                    SelectedProduct.StoneValue +
                    wastageAmount +
                    (SelectedProduct.ValueAdditionPercentage > 0 
                        ? (SelectedProduct.BasePrice * SelectedProduct.ValueAdditionPercentage) / 100 
                        : 0), 
                    2);

                bool result;
                if (SelectedProduct.ProductID > 0)
                {
                    result = await _productService.UpdateProduct(SelectedProduct);
                }
                else
                {
                    var addedProduct = await _productService.AddProduct(SelectedProduct);
                    result = addedProduct != null;
                }

                if (result)
                {
                    System.Windows.MessageBox.Show("Product saved successfully!", "Success");
                    LoadProducts();
                    ClearForm();
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to save product. Please check your inputs.", "Error");
                }
            }
            catch (Exception ex)
            {
                // Show full exception details for debugging
                System.Windows.MessageBox.Show($"Error saving product: {ex.Message}\n{ex.StackTrace}", "Error");
                
                if (ex.InnerException != null)
                {
                    System.Windows.MessageBox.Show($"Inner exception: {ex.InnerException.Message}", "Error Details");
                }
            }
        }

        private Task<decimal> GetCurrentMetalRate()
        {
            if (string.IsNullOrEmpty(SelectedProduct.MetalType) || 
                string.IsNullOrEmpty(SelectedProduct.Purity))
                return Task.FromResult(0m);

            // Use the synchronous GetCurrentRate method and wrap the result in a Task
            decimal rate = _rateService.GetCurrentRate(
                SelectedProduct.MetalType,
                SelectedProduct.Purity);
                
            return Task.FromResult(rate);
        }

        private void ClearForm()
        {
            SelectedProduct = new Product { IsActive = true };
            SearchName = string.Empty;
        }

        private bool CanAddOrUpdateProduct()
        {
            return !string.IsNullOrEmpty(SelectedProduct.ProductName) &&
                   !string.IsNullOrEmpty(SelectedProduct.MetalType) &&
                   !string.IsNullOrEmpty(SelectedProduct.Purity) &&
                   SelectedProduct.GrossWeight > 0 &&
                   SelectedProduct.NetWeight > 0;
        }

        private async void SearchProducts()
        {
            Products.Clear();
            var products = await _productService.FilterProducts(SearchName);
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }
    }
}
