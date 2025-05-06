using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;
using System.Collections.Generic;
using System;

namespace Page_Navigation_App.ViewModel
{
    public class CategoryVM : ViewModelBase
    {
        private readonly CategoryService _categoryService;

        public ICommand AddCategoryCommand { get; }
        public ICommand UpdateCategoryCommand { get; }
        public ICommand ClearCategoryCommand { get; }
        public ICommand AddSubcategoryCommand { get; }
        public ICommand UpdateSubcategoryCommand { get; }
        public ICommand ClearSubcategoryCommand { get; }
        public ICommand DeleteSubcategoryCommand { get; }
        public ICommand ReassignSubcategoryCommand { get; }

        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();
        public ObservableCollection<Subcategory> Subcategories { get; set; } = new ObservableCollection<Subcategory>();
        public ObservableCollection<Subcategory> AllSubcategories { get; set; } = new ObservableCollection<Subcategory>();

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

        private Category _targetCategory;
        public Category TargetCategory
        {
            get => _targetCategory;
            set
            {
                _targetCategory = value;
                OnPropertyChanged();
            }
        }

        private Subcategory _selectedSubcategory = new Subcategory();
        public Subcategory SelectedSubcategory
        {
            get => _selectedSubcategory;
            set
            {
                _selectedSubcategory = value;
                // Ensure the CategoryID is always set to the currently selected category
                if (_selectedSubcategory != null && SelectedCategory != null && SelectedCategory.CategoryID > 0)
                {
                    _selectedSubcategory.CategoryID = SelectedCategory.CategoryID;
                }
                OnPropertyChanged();
            }
        }

        public CategoryVM(CategoryService categoryService)
        {
            _categoryService = categoryService;
            LoadCategories();
            LoadAllSubcategories();

            AddCategoryCommand = new RelayCommand<object>(_ => AddCategory(), _ => CanAddCategory());
            UpdateCategoryCommand = new RelayCommand<object>(_ => UpdateCategory(), _ => CanUpdateCategory());
            ClearCategoryCommand = new RelayCommand<object>(_ => ClearCategoryForm(), _ => true);
            AddSubcategoryCommand = new RelayCommand<object>(_ => AddSubcategory(), _ => CanAddSubcategory());
            UpdateSubcategoryCommand = new RelayCommand<object>(_ => UpdateSubcategory(), _ => CanUpdateSubcategory());
            ClearSubcategoryCommand = new RelayCommand<object>(_ => ClearSubcategoryForm(), _ => true);
            DeleteSubcategoryCommand = new RelayCommand<object>(_ => DeleteSubcategory(), _ => CanDeleteSubcategory());
            ReassignSubcategoryCommand = new RelayCommand<object>(_ => ReassignSubcategory(), _ => CanReassignSubcategory());
        }

        // Load all categories
        private async void LoadCategories()
        {
            Categories.Clear();
            var categories = await _categoryService.GetAllCategories(true);
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }

        // Load all subcategories across all categories for mapping view
        private async void LoadAllSubcategories()
        {
            AllSubcategories.Clear();
            var subcategories = await _categoryService.GetAllSubcategoriesWithCategories(true);
            foreach (var subcategory in subcategories)
            {
                AllSubcategories.Add(subcategory);
            }
        }

        // Load subcategories for a specific category
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
            // Check if a category is selected first
            if (SelectedCategory == null || SelectedCategory.CategoryID <= 0)
            {
                System.Windows.MessageBox.Show("Please select a category first before adding a subcategory.", 
                    "Category Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

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

        private async void UpdateSubcategory()
        {
            // Check if a category is selected first
            if (SelectedCategory == null || SelectedCategory.CategoryID <= 0)
            {
                System.Windows.MessageBox.Show("Please select a category first before updating a subcategory.", 
                    "Category Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Ensure the subcategory still has the correct categoryID set
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

            await _categoryService.UpdateSubcategory(SelectedSubcategory);
            LoadSubcategories(SelectedCategory.CategoryID);
        }

        // New method to delete a subcategory (soft delete)
        private async void DeleteSubcategory()
        {
            if (SelectedSubcategory == null || SelectedSubcategory.SubcategoryID <= 0)
            {
                System.Windows.MessageBox.Show("Please select a subcategory to delete.", 
                    "Subcategory Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the subcategory '{SelectedSubcategory.SubcategoryName}'? This will mark it as inactive.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _categoryService.DeleteSubcategory(SelectedSubcategory.SubcategoryID);
                LoadSubcategories(SelectedCategory.CategoryID);
                LoadAllSubcategories();
                ClearSubcategoryForm();
            }
        }

        // New method to reassign a subcategory to a different category
        private async void ReassignSubcategory()
        {
            if (SelectedSubcategory == null || SelectedSubcategory.SubcategoryID <= 0)
            {
                System.Windows.MessageBox.Show("Please select a subcategory to reassign.", 
                    "Subcategory Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (TargetCategory == null || TargetCategory.CategoryID <= 0)
            {
                System.Windows.MessageBox.Show("Please select a target category to reassign the subcategory to.",
                    "Target Category Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Verify that the target category is different from the current category
            if (SelectedCategory != null && TargetCategory != null && 
                SelectedCategory.CategoryID == TargetCategory.CategoryID)
            {
                System.Windows.MessageBox.Show(
                    "The subcategory is already in this category. Please select a different target category.",
                    "Same Category", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            // Additional check to ensure all required properties are available
            if (SelectedCategory == null || string.IsNullOrEmpty(SelectedCategory.CategoryName) ||
                string.IsNullOrEmpty(TargetCategory.CategoryName) ||
                string.IsNullOrEmpty(SelectedSubcategory.SubcategoryName))
            {
                System.Windows.MessageBox.Show("Unable to reassign subcategory. Some required information is missing.",
                    "Reassignment Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Store the original category ID for later use
            int originalCategoryId = SelectedCategory.CategoryID;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to move '{SelectedSubcategory.SubcategoryName}' from '{SelectedCategory.CategoryName}' to '{TargetCategory.CategoryName}'?",
                "Confirm Reassignment", 
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // Update CategoryID of the subcategory
                    SelectedSubcategory.CategoryID = TargetCategory.CategoryID;
                    
                    // Save the changes
                    await _categoryService.UpdateSubcategory(SelectedSubcategory);
                    
                    // Reload subcategories for the current category
                    if (originalCategoryId > 0)
                    {
                        LoadSubcategories(originalCategoryId);
                    }
                    
                    // Reload all subcategories for the mapping view
                    LoadAllSubcategories();
                    
                    System.Windows.MessageBox.Show(
                        $"Subcategory '{SelectedSubcategory.SubcategoryName}' has been reassigned to '{TargetCategory.CategoryName}'.",
                        "Reassignment Successful",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error reassigning subcategory: {ex.Message}",
                        "Reassignment Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
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
                   SelectedSubcategory != null &&
                   !string.IsNullOrEmpty(SelectedSubcategory.SubcategoryName);
        }

        private bool CanUpdateSubcategory()
        {
            return SelectedSubcategory != null && 
                   SelectedSubcategory.SubcategoryID > 0 && 
                   !string.IsNullOrEmpty(SelectedSubcategory.SubcategoryName);
        }

        // Check if a subcategory can be deleted
        private bool CanDeleteSubcategory()
        {
            return SelectedSubcategory != null && SelectedSubcategory.SubcategoryID > 0;
        }

        // Check if a subcategory can be reassigned
        private bool CanReassignSubcategory()
        {
            return SelectedSubcategory != null && 
                   SelectedSubcategory.SubcategoryID > 0 && 
                   TargetCategory != null && 
                   TargetCategory.CategoryID > 0 &&
                   TargetCategory.CategoryID != SelectedCategory.CategoryID;
        }
    }
}