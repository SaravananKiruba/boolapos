using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class ProductVM : ViewModelBase
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly SupplierService _supplierService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }

        public ProductVM(ProductService productService, CategoryService categoryService, SupplierService supplierService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _supplierService = supplierService;
            
            LoadProducts();
            LoadCategories();
            LoadSuppliers();
            InitializeCollections();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateProduct(), _ => CanAddOrUpdateProduct());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchProducts(), _ => true);
        }

        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();
        public ObservableCollection<Subcategory> Subcategories { get; set; } = new ObservableCollection<Subcategory>();
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
                if (_selectedProduct?.CategoryID > 0)
                {
                    LoadSubcategories(_selectedProduct.CategoryID);
                }
                OnPropertyChanged();
            }
        }

        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                if (_selectedCategory != null)
                {
                    LoadSubcategories(_selectedCategory.CategoryID);
                }
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
            foreach (var type in new[] { "Gold", "Silver", "Platinum" })
            {
                MetalTypes.Add(type);
            }

            Purities.Clear();
            foreach (var purity in new[] { "14k", "18k", "22k", "24k", "92.5", "95", "99.9" })
            {
                Purities.Add(purity);
            }
        }

        private async void LoadProducts()
        {
            Products.Clear();
            var products = await _productService.GetAllProducts();
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }

        private async void LoadCategories()
        {
            Categories.Clear();
            var categories = await _categoryService.GetAllCategories();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }

        private async void LoadSubcategories(int categoryId)
        {
            Subcategories.Clear();
            var subcategories = await _categoryService.GetSubcategoriesByCategory(categoryId);
            foreach (var subcategory in subcategories)
            {
                Subcategories.Add(subcategory);
            }
        }

        private async void LoadSuppliers()
        {
            Suppliers.Clear();
            var suppliers = await _supplierService.GetAllSuppliers();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
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
        }

        private async void AddOrUpdateProduct()
        {
            // Calculate final price based on components
            SelectedProduct.BasePrice = SelectedProduct.NetWeight * GetCurrentMetalRate();
            SelectedProduct.FinalPrice = SelectedProduct.BasePrice + 
                                       SelectedProduct.MakingCharges + 
                                       SelectedProduct.StoneValue +
                                       (SelectedProduct.BasePrice * SelectedProduct.WastagePercentage / 100);

            // Validate the product before saving
            var validationContext = new ValidationContext(SelectedProduct, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(SelectedProduct, validationContext, validationResults, true);

            if (!isValid)
            {
                string errorMessage = string.Join("\n", validationResults.Select(v => v.ErrorMessage));
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedProduct.ProductID > 0)
            {
                await _productService.UpdateProduct(SelectedProduct);
            }
            else
            {
                await _productService.AddProduct(SelectedProduct);
            }

            LoadProducts();
            ClearForm();
        }

        private decimal GetCurrentMetalRate()
        {
            // TODO: Implement actual rate lookup from RateMaster
            return 1000.00m; // Default rate for testing
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
                   SelectedProduct.NetWeight > 0 &&
                   SelectedProduct.CategoryID > 0;
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
