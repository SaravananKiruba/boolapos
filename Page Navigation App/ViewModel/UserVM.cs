using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class UserVM : ViewModelBase
    {
        private readonly UserService _userService;
        
        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand ToggleActiveCommand { get; }

        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();
        public ObservableCollection<string> AvailableRoles { get; } = new ObservableCollection<string> 
        { 
            "Admin", 
            "Manager", 
            "Cashier", 
            "Inventory", 
            "Sales" 
        };

        private User _selectedUser = new User();
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        private string _searchUsername;
        public string SearchUsername
        {
            get => _searchUsername;
            set
            {
                _searchUsername = value;
                OnPropertyChanged();
                SearchUsers();
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        private string _selectedRole;
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
            }
        }

        public UserVM(UserService userService)
        {
            _userService = userService;
            LoadUsers();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateUser(), _ => CanAddOrUpdateUser());
            ClearCommand = new RelayCommand<object>(_ => ClearForm());
            SearchCommand = new RelayCommand<object>(_ => SearchUsers());
            ChangePasswordCommand = new RelayCommand<object>(_ => ChangePassword(), _ => SelectedUser?.UserID > 0);
            ToggleActiveCommand = new RelayCommand<object>(_ => ToggleUserActive(), _ => SelectedUser?.UserID > 0);
        }

        private async void LoadUsers()
        {
            Users.Clear();
            var users = await _userService.GetUsers(true);
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }

        private async void AddOrUpdateUser()
        {
            if (string.IsNullOrEmpty(SelectedUser.Username) || string.IsNullOrEmpty(Password))
                return;

            if (SelectedUser.UserID == 0)
            {
                var newUser = new User
                {
                    Username = SelectedUser.Username,
                    FullName = SelectedUser.FullName,
                    Email = SelectedUser.Email,
                    Phone = SelectedUser.Phone,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var createdUser = await _userService.CreateUser(newUser, Password);
                if (createdUser != null && !string.IsNullOrEmpty(SelectedRole))
                {
                    await _userService.AssignRoles(createdUser.UserID, new List<string> { SelectedRole });
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Password))
                {
                    await _userService.ResetPassword(SelectedUser.UserID, Password);
                }
                // Update roles if changed
                if (!string.IsNullOrEmpty(SelectedRole))
                {
                    await _userService.AssignRoles(SelectedUser.UserID, new List<string> { SelectedRole });
                }
            }

            LoadUsers();
            ClearForm();
        }

        private bool CanAddOrUpdateUser()
        {
            return !string.IsNullOrEmpty(SelectedUser?.Username) && 
                   !string.IsNullOrEmpty(SelectedUser?.FullName) && 
                   !string.IsNullOrEmpty(SelectedRole);
        }

        private void SearchUsers()
        {
            if (string.IsNullOrEmpty(SearchUsername))
            {
                LoadUsers();
                return;
            }

            var filteredUsers = Users.Where(u => 
                u.Username.Contains(SearchUsername, StringComparison.OrdinalIgnoreCase) ||
                u.FullName.Contains(SearchUsername, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Users.Clear();
            foreach (var user in filteredUsers)
            {
                Users.Add(user);
            }
        }

        private void ClearForm()
        {
            SelectedUser = new User();
            Password = string.Empty;
            SelectedRole = null;
        }

        private async void ChangePassword()
        {
            if (SelectedUser == null || string.IsNullOrEmpty(Password))
                return;

            await _userService.ResetPassword(SelectedUser.UserID, Password);
            Password = string.Empty;
        }

        private async void ToggleUserActive()
        {
            if (SelectedUser == null)
                return;

            SelectedUser.IsActive = !SelectedUser.IsActive;
            // TODO: Implement UpdateUser in UserService
            // await _userService.UpdateUser(SelectedUser);
            LoadUsers();
        }
    }
}