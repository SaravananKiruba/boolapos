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

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }

        public ProductVM(ProductService productService)
        {
            _productService = productService;
            LoadProducts();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateProduct(), _ => CanAddOrUpdateProduct());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchProducts(), _ => true);
        }

        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();

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

        private void LoadProducts()
        {
            Products.Clear();
            foreach (var product in _productService.GetAllProducts())
            {
                Products.Add(product);
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
                    ProductName = SearchName
                };
            }
        }

        private void AddOrUpdateProduct()
        {
            // Validate the product before saving
            var validationContext = new ValidationContext(SelectedProduct, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(SelectedProduct, validationContext, validationResults, true);

            if (!isValid)
            {
                // Show validation errors (you can use a MessageBox or a validation summary)
                string errorMessage = string.Join("\n", validationResults.Select(v => v.ErrorMessage));
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedProduct.ProductID > 0)
            {
                _productService.UpdateProduct(SelectedProduct);
            }
            else
            {
                _productService.AddProduct(SelectedProduct);
            }

            LoadProducts();
            ClearForm();
        }

        private void ClearForm()
        {
            SelectedProduct = new Product();
            SearchName = string.Empty;
        }

        private bool CanAddOrUpdateProduct()
        {
            // Ensure required fields are filled before allowing save
            return !string.IsNullOrEmpty(SelectedProduct.ProductName) &&
                   SelectedProduct.Price > 0;
        }

        private void SearchProducts()
        {
            Products.Clear();
            var filteredProducts = _productService.FilterProducts(SearchName);
            foreach (var product in filteredProducts)
            {
                Products.Add(product);
            }
        }
    }
}
