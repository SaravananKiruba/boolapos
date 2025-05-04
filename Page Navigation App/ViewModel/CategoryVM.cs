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
    public class CategoryVM : ViewModelBase
    {
        private readonly CategoryService _categoryService;

        public ICommand AddCategoryCommand { get; }
        public ICommand UpdateCategoryCommand { get; }
        public ICommand ClearCategoryCommand { get; }
        public ICommand AddSubcategoryCommand { get; }
        public ICommand ClearSubcategoryCommand { get; }

        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();
        public ObservableCollection<Subcategory> Subcategories { get; set; } = new ObservableCollection<Subcategory>();

        private Category _selectedCategory = new Category();
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                if (value != null && value.CategoryID > 0)
                {
                    LoadSubcategories(value.CategoryID);
                }
            }
        }

        private Subcategory _selectedSubcategory = new Subcategory();
        public Subcategory SelectedSubcategory
        {
            get => _selectedSubcategory;
            set
            {
                _selectedSubcategory = value;
                OnPropertyChanged();
            }
        }

        public CategoryVM(CategoryService categoryService)
        {
            _categoryService = categoryService;
            LoadCategories();

            AddCategoryCommand = new RelayCommand<object>(_ => AddCategory(), _ => CanAddCategory());
            UpdateCategoryCommand = new RelayCommand<object>(_ => UpdateCategory(), _ => CanUpdateCategory());
            ClearCategoryCommand = new RelayCommand<object>(_ => ClearCategoryForm(), _ => true);
            AddSubcategoryCommand = new RelayCommand<object>(_ => AddSubcategory(), _ => CanAddSubcategory());
            ClearSubcategoryCommand = new RelayCommand<object>(_ => ClearSubcategoryForm(), _ => true);
        }

        private async void LoadCategories()
        {
            Categories.Clear();
            var categories = await _categoryService.GetAllCategories(true);
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

        private async void AddCategory()
        {
            // Validate the category
            var validationContext = new ValidationContext(SelectedCategory, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(SelectedCategory, validationContext, validationResults, true);

            if (!isValid)
            {
                string errorMessage = string.Join("\n", validationResults.Select(v => v.ErrorMessage));
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            await _categoryService.AddCategory(SelectedCategory);
            LoadCategories();
            ClearCategoryForm();
        }

        private async void UpdateCategory()
        {
            // Validate the category
            var validationContext = new ValidationContext(SelectedCategory, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(SelectedCategory, validationContext, validationResults, true);

            if (!isValid)
            {
                string errorMessage = string.Join("\n", validationResults.Select(v => v.ErrorMessage));
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            await _categoryService.UpdateCategory(SelectedCategory);
            LoadCategories();
        }

        private async void AddSubcategory()
        {
            // Ensure the subcategory has the categoryID set
            SelectedSubcategory.CategoryID = SelectedCategory.CategoryID;

            // Validate the subcategory
            var validationContext = new ValidationContext(SelectedSubcategory, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(SelectedSubcategory, validationContext, validationResults, true);

            if (!isValid)
            {
                string errorMessage = string.Join("\n", validationResults.Select(v => v.ErrorMessage));
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            await _categoryService.AddSubcategory(SelectedSubcategory);
            LoadSubcategories(SelectedCategory.CategoryID);
            ClearSubcategoryForm();
        }

        private void ClearCategoryForm()
        {
            SelectedCategory = new Category { IsActive = true };
        }

        private void ClearSubcategoryForm()
        {
            SelectedSubcategory = new Subcategory { CategoryID = SelectedCategory.CategoryID };
        }

        private bool CanAddCategory()
        {
            return SelectedCategory != null && 
                   !string.IsNullOrEmpty(SelectedCategory.CategoryName) &&
                   SelectedCategory.DefaultMakingCharges >= 0 &&
                   SelectedCategory.DefaultWastage >= 0;
        }

        private bool CanUpdateCategory()
        {
            return SelectedCategory != null && 
                   SelectedCategory.CategoryID > 0 && 
                   !string.IsNullOrEmpty(SelectedCategory.CategoryName);
        }

        private bool CanAddSubcategory()
        {
            return SelectedCategory != null && 
                   SelectedCategory.CategoryID > 0 && 
                   !string.IsNullOrEmpty(SelectedSubcategory.SubcategoryName);
        }
    }
}