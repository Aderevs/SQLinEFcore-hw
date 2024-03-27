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
        private async void AddProductToDb(object sender, RoutedEventArgs e)
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
            ProductName.Text = "";
            ProductPrice.Text = "";
            ProductDescription.Text = "";
            ProductCategory.Text = "";
            ProductKeyWord.Text = "";
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

        private async void AddUserToDb(object sender, RoutedEventArgs e)
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
            UserName.Text = "";
            UserLogin.Text = "";
            UserPassword.Text = "";
            UserEmail.Text = "";
            UserProducts.Text = "";
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

        private async void AddCategoryToDb(object sender, RoutedEventArgs e)
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
    }
    
}