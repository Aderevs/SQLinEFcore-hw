using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Data.SqlClient;
using System.IO.Packaging;
using System.IO;
using Microsoft.Data.SqlClient;

namespace SQLinEFcore_hw
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        List<Word> _keyParams = new();
        List<string> _productsNames = new();
        private async void ActionWithProduct(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProductAddRadio.IsChecked == true)
                {
                    await AddProductToDb();
                }
                else if (ProductUpdateRadio.IsChecked == true)
                {
                    await UpdateProductInDb();
                }
                else if (ProductDeleteRadio.IsChecked == true)
                {
                    await DeleteProductFromDb();
                }
                else
                {
                    MessageBox.Show("You have to choose an option", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ProductName.Text = "";
            ProductPrice.Text = "";
            ProductDescription.Text = "";
            ProductCategory.Text = "";
            ProductKeyWord.Text = "";
            IdentityProductName.Foreground = Brushes.Gray;
            IdentityProductName.FontSize = 11;
            IdentityProductName.Text = "Product to update or delete";
        }

        private async Task AddProductToDb()
        {
            string productName;
            if (ProductName.Text != "")
            {
                productName = ProductName.Text;
            }
            else
            {
                MessageBox.Show("Product must have Name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double price;
            if (!double.TryParse(ProductPrice.Text, out price))
            {
                MessageBox.Show("Product must have price and it must be numeric value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string productDescription = ProductDescription.Text;
            using MyDbContext dbContext = new MyDbContext();
            var categoriesNames = await dbContext.Category
                .Select(c => c.Name)
                .ToListAsync();
            if (!categoriesNames.Contains(ProductCategory.Text))
            {
                MessageBox.Show("Product must have category that already exists", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string productCategory = ProductCategory.Text;

            string productKeyWord = ProductKeyWord.Text;

            var productId = Guid.NewGuid();
            Product product = new Product
            {
                Id = productId,
                Name = productName,
                Price = price,
                Description = productDescription,
                Category = await dbContext.Category.Where(c => c.Name == productCategory).FirstAsync()
            };
            dbContext.Add(product);

            foreach (Word word in _keyParams)
            {
                dbContext.Add(word);
            }
            foreach (Word word in _keyParams)
            {
                dbContext.Add(
                    new KeyParams
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productId,
                        KeyWordsId = word.Id
                    });
            }

            await dbContext.SaveChangesAsync();
            _keyParams.Clear();
            MessageBox.Show("Product has been saved to Database", "Success");
        }
        private async Task UpdateProductInDb()
        {
            if (IdentityProductName.Text != "")
            {
                using MyDbContext dbContext = new MyDbContext();
                bool IsAnyFieldToUpdate = false;

                if (ProductPrice.Text != "")
                {
                    if (double.TryParse(ProductPrice.Text, out double price))
                    {
                        await dbContext.Database.ExecuteSqlRawAsync($"UPDATE Product SET Price = {price} WHERE Name = '{IdentityProductName.Text}'");
                        IsAnyFieldToUpdate = true;
                    }
                    else
                    {
                        MessageBox.Show("Price you enter is not valid it has to be numeric type", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                if (ProductDescription.Text != "")
                {
                    await dbContext.Database.ExecuteSqlRawAsync($"UPDATE Product SET Description = {ProductDescription.Text} WHERE Name = '{IdentityProductName.Text}'");
                    IsAnyFieldToUpdate = true;
                }
                if (ProductCategory.Text != "")
                {
                    Guid productCategoryId;
                    try
                    {
                        productCategoryId = await dbContext.Category
                        .Where(c => c.Name == ProductCategory.Text)
                        .Select(c => c.Id)
                        .FirstAsync();
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show("You have to enter name of category that already exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    await dbContext.Database.ExecuteSqlRawAsync($"UPDATE Product SET CategoryId = {productCategoryId} WHERE Name = '{IdentityProductName.Text}'");
                    IsAnyFieldToUpdate = true;
                }
                if (_keyParams.Any())
                {
                    var productId = await dbContext.Product
                        .Where(p => p.Name == IdentityProductName.Text)
                        .Select(p => p.Id)
                        .FirstAsync();

                    var wordIdsToDelete = await dbContext.KeyLink
                        .Where(kl => kl.ProductId == productId)
                        .Select(kl => kl.KeyWordsId)
                        .ToArrayAsync();
                    if (wordIdsToDelete.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var wordId in wordIdsToDelete)
                        {
                            sb.Append($"'{wordId}', ");
                        }
                        var wordIdsToDeleteString = sb.ToString();
                        wordIdsToDeleteString = wordIdsToDeleteString.Substring(0, wordIdsToDeleteString.Length - 2);
                        await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM Word WHERE Id IN ({wordIdsToDeleteString})");
                    }
                    foreach (Word word in _keyParams)
                    {
                        dbContext.Add(word);
                    }
                    foreach (Word word in _keyParams)
                    {
                        dbContext.Add(
                            new KeyParams
                            {
                                Id = Guid.NewGuid(),
                                ProductId = productId,
                                KeyWordsId = word.Id
                            });
                    }

                }
                if (ProductName.Text != "")
                {
                    await dbContext.Database.ExecuteSqlRawAsync($"UPDATE Product SET Name = {ProductName.Text} WHERE Name = '{IdentityProductName.Text}'");
                    IsAnyFieldToUpdate = true;
                }
                if (!IsAnyFieldToUpdate)
                {
                    MessageBox.Show("You didn't enter any field to update", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                await dbContext.SaveChangesAsync();
            }
            else
            {
                MessageBox.Show("You have to enter name of product you want to update to field \"Identity name of product\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task DeleteProductFromDb()
        {
            if (IdentityProductName.Text != "")
            {
                using MyDbContext dbContext = new MyDbContext();
                var name = IdentityProductName.Text;
                if (dbContext.Product.Where(p => p.Name == name).FirstOrDefault() != null)
                {
                    await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM Product WHERE Name = '{name}'");
                    MessageBox.Show("Product was deleted", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                else
                {
                    MessageBox.Show("There are no products in database with such name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("You have to enter name of product you want to delete to field \"Identity name of product\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddKeyWord(object sender, RoutedEventArgs e)
        {
            using MyDbContext dbContext = new MyDbContext();
            var categoriesNames = await dbContext.Category
                .Select(c => c.Name)
                .ToListAsync();

            if (ProductCategory.Text != "")
            {
                if (!categoriesNames.Contains(ProductCategory.Text))
                {
                    MessageBox.Show("Product must have category that already exists", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (ProductKeyWord.Text != "")
                {
                    _keyParams.Add(
                        new Word
                        {
                            Id = Guid.NewGuid(),
                            Header = ProductCategory.Text.ToLower(),
                            KeyWord = ProductKeyWord.Text
                        });
                    ProductKeyWord.Text = "";
                }
            }
            else
            {
                MessageBox.Show("You have to fill category before adding key word", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ActionWithUser(object sender, RoutedEventArgs e)
        {
            try
            {
                if (UserAddRadio.IsChecked == true)
                {
                    await AddUserToDb();
                }
                else if (UserUpdateRadio.IsChecked == true)
                {
                    await UpdateUserInDb();
                }
                else if (UserDeleteRadio.IsChecked == true)
                {
                    await DeleteUserFromDb();
                }
                else
                {
                    MessageBox.Show("You have to choose an option", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UserName.Text = "";
            UserLogin.Text = "";
            UserPassword.Text = "";
            UserEmail.Text = "";
            UserProducts.Text = "";
            IdentityUserLogin.Foreground = Brushes.Gray;
            IdentityUserLogin.FontSize = 11;
            IdentityUserLogin.Text = "User to update or delete";
        }
        private async void AddUsersProduct(object sender, RoutedEventArgs e)
        {
            using MyDbContext dbContext = new MyDbContext();
            var productnames = await dbContext.Product
                .Select(p => p.Name)
                .ToListAsync();

            if (!productnames.Contains(UserProducts.Text))
            {
                MessageBox.Show("User must have product that already exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (UserProducts.Text != "")
            {
                _productsNames.Add(UserProducts.Text);
                UserProducts.Text = "";
            }
        }

        private async Task AddUserToDb()
        {
            string userName;
            if (UserName.Text != "")
            {
                userName = UserName.Text;
            }
            else
            {
                MessageBox.Show("User must have Name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string userLogin;
            if (UserLogin.Text != "")
            {
                userLogin = UserLogin.Text;
            }
            else
            {
                MessageBox.Show("User must have login", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Regex passwordCheck = new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{8,}");
            string userPassword;
            if (UserPassword.Text != "")
            {
                if (passwordCheck.IsMatch(UserPassword.Text))
                {
                    userPassword = UserPassword.Text;
                }
                else
                {
                    MessageBox.Show("Password is to easy, it must more than 8 characters has at least one number, one uppercase letter and one lowercase letter", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("User must have password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string userEmail;
            Regex emailCheck = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (UserEmail.Text != "")
            {
                if (emailCheck.IsMatch(UserEmail.Text))
                {
                    userEmail = UserEmail.Text;
                }
                else
                {
                    MessageBox.Show("Email is not valid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("User must have email", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using MyDbContext dbContext = new MyDbContext();
            var userId = Guid.NewGuid();
            User user = new User
            {
                Id = userId,
                Name = userName,
                Login = userLogin,
                Password = userPassword,
                Email = userEmail
            };
            dbContext.Add(user);

            foreach (var productName in _productsNames)
            {
                var productId = await dbContext.Product
                    .Where(p => p.Name == productName)
                    .Select(p => p.Id)
                    .FirstAsync();
                dbContext.Add(
                    new Cart
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ProductId = productId
                    });
            }

            await dbContext.SaveChangesAsync();
            _productsNames.Clear();
            MessageBox.Show("User has been saved to Database", "Success");
        }
        private async Task DeleteUserFromDb()
        {
            if (IdentityUserLogin.Text != "")
            {
                using MyDbContext dbContext = new MyDbContext();
                var login = IdentityUserLogin.Text;
                if (dbContext.User.Where(u => u.Login == login).FirstOrDefault() != null)
                {
                    await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM [User] WHERE Login = '{login}'");
                    MessageBox.Show("User was deleted", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                else
                {
                    MessageBox.Show("There are no users in database with such login", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("You have to enter login of user you want to delete to field \"Identity user's login\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task UpdateUserInDb()
        {
            if (IdentityUserLogin.Text != "")
            {
                using MyDbContext dbContext = new MyDbContext();
                bool IsAnyFieldToUpdate = false;

                if (UserName.Text != "")
                {
                    await dbContext.Database.ExecuteSqlRawAsync($"UPDATE [User] SET Name = {UserName.Text} WHERE Login = '{IdentityUserLogin.Text}'");
                    IsAnyFieldToUpdate = true;
                }

                if (UserPassword.Text != "")
                {
                    Regex passwordCheck = new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{8,}");
                    string userPassword;
                    if (UserPassword.Text != "")
                    {
                        if (passwordCheck.IsMatch(UserPassword.Text))
                        {
                            userPassword = UserPassword.Text;
                            await dbContext.Database.ExecuteSqlRawAsync($"UPDATE [User] SET Password = {userPassword} WHERE Login = '{IdentityUserLogin.Text}'");
                        }
                        else
                        {
                            MessageBox.Show("Password is to easy, it must more than 8 characters has at least one number, one uppercase letter and one lowercase letter", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }

                if (UserEmail.Text != "")
                {
                    Regex emailCheck = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                    if (UserEmail.Text != "")
                    {
                        if (emailCheck.IsMatch(UserEmail.Text))
                        {
                            string userEmail = UserEmail.Text;
                            await dbContext.Database.ExecuteSqlRawAsync($"UPDATE [User] SET Email = {userEmail} WHERE Login = '{IdentityUserLogin.Text}'");
                        }
                        else
                        {
                            MessageBox.Show("Email is not valid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }

                if (_productsNames.Any())
                {
                    var userId = await dbContext.User
                        .Where(u => u.Login == IdentityUserLogin.Text)
                        .Select(u => u.Id)
                        .FirstAsync();


                    await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM Cart WHERE UserId = '{userId}'");

                    foreach (var productName in _productsNames)
                    {
                        var productId = await dbContext.Product
                            .Where(p => p.Name == productName)
                            .Select(p => p.Id)
                            .FirstAsync();
                        dbContext.Add(
                            new Cart
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                ProductId = productId
                            });
                    }

                }

                if (UserLogin.Text != "")
                {
                    await dbContext.Database.ExecuteSqlRawAsync($"UPDATE [User] SET Login = {UserLogin.Text} WHERE Login = '{IdentityUserLogin.Text}'");
                    IsAnyFieldToUpdate = true;
                }
                if (!IsAnyFieldToUpdate)
                {
                    MessageBox.Show("You didn't enter any field to update", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                await dbContext.SaveChangesAsync();
            }
            else
            {
                MessageBox.Show("You have to enter login of user you want to update to field \"Identity user's login\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ActionWithCategory(object sender, RoutedEventArgs e)
        {
            if (CategoryName.Text == "")
            {
                MessageBox.Show("Category must have name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            using MyDbContext dbContext = new MyDbContext();
            var categoriesNames = await dbContext.Category
                .Select(c => c.Name)
                .ToListAsync();
            if (!categoriesNames.Contains(CategoryName.Text))
            {
                dbContext.Add(
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = CategoryName.Text,
                    });
                await dbContext.SaveChangesAsync();
                MessageBox.Show("Category has been saved to Database", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            else
            {
                MessageBox.Show("This category already exist in Database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            CategoryName.Text = "";
        }


        private async void CrereBackupForDb(object sender, RoutedEventArgs e)
        {
            string backupName = BackupName.Text;
            if (backupName == "")
            {
                MessageBox.Show("Database backup must have name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string databaseName = "NewBaseTest2";
            string backupPath = @"E:\CyberByonicSystematics\Entity Framework Core\Homeworks\";

            try
            {
                BackupDatabase(databaseName, backupName, backupPath);
                MessageBox.Show("Database backup created", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                BackupName.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        static void BackupDatabase(string databaseName, string backupName, string backupPath)
        {
            using (var context = new MyDbContext())
            {
                string backupFileName = backupPath + backupName + ".bak";
                string backupQuery = $"BACKUP DATABASE [{databaseName}] TO DISK='{backupFileName}'";

                context.Database.ExecuteSqlRaw(backupQuery);
            }
        }

        private async void RestoreDb(object sender, RoutedEventArgs e)
        {
            string backupName = BackupRestoreName.Text;
            if (backupName == "")
            {
                MessageBox.Show("Database backup must have name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string databaseName = "NewBaseTest2";
            string backupPath = @"E:\CyberByonicSystematics\Entity Framework Core\Homeworks\";
            try
            {
                RestoreDatabase(databaseName, backupName, backupPath);
                MessageBox.Show("Database restored", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        static void RestoreDatabase(string databaseName, string backupName, string backupPath)
        {
            using (var dbContext = new MyDbContext())
            {
                string closeConnectionsSql = $@"
            USE [master];
            ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";

                dbContext.Database.ExecuteSqlRaw(closeConnectionsSql);

                string restoreSql = $@"
            USE [master];
            RESTORE DATABASE [{databaseName}] FROM DISK = '{backupPath}{backupName}.bak' WITH REPLACE;";

                dbContext.Database.ExecuteSqlRaw(restoreSql);
            }
        }

        private void ShowAvaliableBackups(object sender, RoutedEventArgs e)
        {
            string directoryPath = @"E:\CyberByonicSystematics\Entity Framework Core\Homeworks\";
            string fileExtension = ".bak";

            try
            {
                string[] files = Directory.GetFiles(directoryPath, "*" + fileExtension);

                if (files.Length > 0)
                {
                    // Extract file names without full path
                    string[] fileNames = files.Select(Path.GetFileName).ToArray();

                    string filesList = string.Join(Environment.NewLine, fileNames);
                    MessageBox.Show("Files with extension " + fileExtension + " in directory " + directoryPath + ":\n\n" + filesList, "Files Found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No files with extension " + fileExtension + " found in directory " + directoryPath, "Files Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IdentityProductName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (IdentityProductName.FontSize == 11)
            {
                IdentityProductName.Text = "";
                IdentityProductName.Foreground = Brushes.Black;
                IdentityProductName.FontSize = 12;
            }
        }

        private void IdentityProductName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IdentityProductName.Text == "")
            {
                IdentityProductName.Text = "Product to update or delete";
                IdentityProductName.Foreground = Brushes.Gray;
                IdentityProductName.FontSize = 11;
            }
        }

        private void IdentityUserlogin_GotFocus(object sender, RoutedEventArgs e)
        {
            if (IdentityUserLogin.FontSize == 11)
            {
                IdentityUserLogin.Text = "";
                IdentityUserLogin.Foreground = Brushes.Black;
                IdentityUserLogin.FontSize = 12;
            }
        }

        private void IdentityUserlogin_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IdentityUserLogin.Text == "")
            {
                IdentityUserLogin.Text = "User to update or delete";
                IdentityUserLogin.Foreground = Brushes.Gray;
                IdentityUserLogin.FontSize = 11;
            }
        }
    }

}