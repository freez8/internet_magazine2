using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Interner_magazine
{
    public partial class AdminPanelWindow : Window
    {
        private DatabaseConnection _dbConnection = new DatabaseConnection();
        private int _adminId;

        #region Models
        public class Product
        {
            public int ProductId { get; set; }
            public string NameProduct { get; set; }
            public decimal PriceProduct { get; set; }
            public int Quantity { get; set; }
            public string CategoryName { get; set; }
            public string ManufacturerName { get; set; }
            public string Characteristic { get; set; }
            public string ImagePath { get; set; } // Add this property
        }
        public class User
        {
            public int UserId { get; set; }
            public string Login { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Phone { get; set; }
            public bool IsActive { get; set; }
            public int LoginAttempts { get; set; }
        }
        public class Order { public int OrderId { get; set; } public int UserId { get; set; } public string UserName { get; set; } public DateTime OrderDate { get; set; } public string DeliveryAddress { get; set; } public string DeliveryMethod { get; set; } public string Status { get; set; } }
        public class OrderProduct { public int ProductId { get; set; } public string ProductName { get; set; } public decimal Price { get; set; } public int Quantity { get; set; } public decimal TotalPrice => Price * Quantity; }
        public class Category { public int CategoryId { get; set; } public string NameCategory { get; set; } public string Description { get; set; } }
        public class Manufacturer { public int ManufacturerId { get; set; } public string NameManufacturer { get; set; } public string CountryName { get; set; } }
        public class Country
        {
            public int CountryId { get; set; }
            public string NameCountry { get; set; }
        }
        #endregion

        #region Collections
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();
        public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();
        public ObservableCollection<OrderProduct> OrderProducts { get; set; } = new ObservableCollection<OrderProduct>();
        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();
        public ObservableCollection<Manufacturer> Manufacturers { get; set; } = new ObservableCollection<Manufacturer>();
        public ObservableCollection<Country> Countries { get; set; } = new ObservableCollection<Country>();
        #endregion

        public AdminPanelWindow(int adminId, string firstName, string lastName)
        {
            InitializeComponent();
            _adminId = adminId;
            txtAdminInfo.Text = $"{firstName} {lastName}";

            // Set data sources
            dgProducts.ItemsSource = Products;
            dgUsers.ItemsSource = Users;
            dgOrders.ItemsSource = Orders;
            dgOrderProducts.ItemsSource = OrderProducts;
            dgCategories.ItemsSource = Categories;
            dgManufacturers.ItemsSource = Manufacturers;
            dgCountries.ItemsSource = Countries;

            LoadAllData();
        }

        private void LoadAllData()
        {
            LoadProducts();
            LoadUsers();
            LoadOrders();
            LoadCategories();
            LoadManufacturers();
            LoadCountries();
        }

        #region Database Operations
        private void ExecuteQuery(string query, Action<NpgsqlDataReader> processReader, string errorMessage)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    var command = new NpgsqlCommand(query, connection);
                    var reader = command.ExecuteReader();
                    processReader(reader);
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ExecuteNonQuery(string query, Action<NpgsqlCommand> setParameters, string successMessage, string errorMessage)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var command = new NpgsqlCommand(query, connection);
                            setParameters(command);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                                MessageBox.Show(successMessage, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                return true;
                            }
                            else
                            {
                                transaction.Rollback();
                                MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"{errorMessage}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        #endregion

        #region Data Loading Methods
        private void LoadProducts()
        {
            string query = @"
        SELECT p.productid, p.nameproduct, p.priceproduct, p.quantity, 
               c.namecategory, m.namemanufacturer, p.characteristic, p.imagepath
        FROM product p
        JOIN category c ON p.categoryid = c.categoryid
        JOIN manufacturer m ON p.manufacturerid = m.manufacturerid
        ORDER BY p.productid";

            ExecuteQuery(query, reader => {
                Products.Clear();
                while (reader.Read())
                {
                    Products.Add(new Product
                    {
                        ProductId = reader.GetInt32(0),
                        NameProduct = reader.GetString(1),
                        PriceProduct = reader.GetDecimal(2),
                        Quantity = reader.GetInt32(3),
                        CategoryName = reader.GetString(4),
                        ManufacturerName = reader.GetString(5),
                        Characteristic = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        ImagePath = reader.IsDBNull(7) ? string.Empty : reader.GetString(7) // Важно!
                    });
                }
            }, "Ошибка загрузки товаров");
        }

        private void LoadUsers()
        {
            string query = @"
        SELECT userid, login, firstname, lastname, phone, isactive, login_attempts
        FROM useraccount
        ORDER BY userid";

            ExecuteQuery(query, reader => {
                Users.Clear();
                while (reader.Read())
                {
                    Users.Add(new User
                    {
                        UserId = reader.GetInt32(0),
                        Login = reader.GetString(1),
                        FirstName = reader.GetString(2),
                        LastName = reader.GetString(3),
                        Phone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        IsActive = reader.IsDBNull(5) ? false : reader.GetBoolean(5),
                        LoginAttempts = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                    });
                }
            }, "Ошибка загрузки пользователей");
        }



        private void LoadOrders()
        {
            string query = @"
                SELECT o.orderid, o.userid, u.firstname || ' ' || u.lastname as username, 
                       o.orderdate, d.address, d.deliverymethod, o.status
                FROM ordertable o
                JOIN useraccount u ON o.userid = u.userid
                LEFT JOIN delivery d ON o.deliveryid = d.deliveryid
                ORDER BY o.orderid DESC";

            ExecuteQuery(query, reader => {
                Orders.Clear();
                while (reader.Read())
                {
                    Orders.Add(new Order
                    {
                        OrderId = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        UserName = reader.GetString(2),
                        OrderDate = reader.GetDateTime(3),
                        DeliveryAddress = reader.IsDBNull(4) ? "Не указан" : reader.GetString(4),
                        DeliveryMethod = reader.IsDBNull(5) ? "Не указан" : reader.GetString(5),
                        Status = reader.IsDBNull(6) ? "Новый" : reader.GetString(6)
                    });
                }
            }, "Ошибка загрузки заказов");
        }

        private void LoadOrderProducts(int orderId)
        {
            string query = @"
        SELECT op.productid, p.nameproduct, p.priceproduct, op.quantity
        FROM orderproduct op
        JOIN product p ON op.productid = p.productid
        WHERE op.orderid = " + orderId;

            ExecuteQuery(query, reader => {
                OrderProducts.Clear();
                while (reader.Read())
                {
                    OrderProducts.Add(new OrderProduct
                    {
                        ProductId = reader.GetInt32(0),
                        ProductName = reader.GetString(1),
                        Price = reader.GetDecimal(2),
                        Quantity = reader.GetInt32(3)
                    });
                }
            }, "Ошибка загрузки товаров заказа");
        }

        private void LoadCategories()
        {
            string query = @"
                SELECT categoryid, namecategory, description
                FROM category
                ORDER BY categoryid";

            ExecuteQuery(query, reader => {
                Categories.Clear();
                while (reader.Read())
                {
                    Categories.Add(new Category
                    {
                        CategoryId = reader.GetInt32(0),
                        NameCategory = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                    });
                }
            }, "Ошибка загрузки категорий");
        }

        private void LoadManufacturers()
        {
            string query = @"
        SELECT m.manufacturerid, m.namemanufacturer, c.namecountry
        FROM manufacturer m
        JOIN country c ON m.countryid = c.countryid
        ORDER BY m.manufacturerid";

            ExecuteQuery(query, reader => {
                Manufacturers.Clear();
                while (reader.Read())
                {
                    Manufacturers.Add(new Manufacturer
                    {
                        ManufacturerId = reader.GetInt32(0),
                        NameManufacturer = reader.GetString(1),
                        CountryName = reader.GetString(2)
                    });
                }
            }, "Ошибка загрузки производителей");
        }
        #endregion

        #region Event Handlers

        private void btnExportOrderToPdf_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ для экспорта в PDF", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Order selectedOrder = dgOrders.SelectedItem as Order;

            // Make sure we have the latest order products data
            LoadOrderProducts(selectedOrder.OrderId);

            try
            {
                // Convert ObservableCollection to List for the PDF generator
                var orderProductsList = new List<OrderProduct>();
                foreach (var product in OrderProducts)
                {
                    orderProductsList.Add(product);
                }

                // Create PDF generator instance with order data
                var pdfGenerator = new OrderPdfGenerator(
                    selectedOrder.OrderId,
                    selectedOrder.UserName,
                    selectedOrder.OrderDate,
                    selectedOrder.DeliveryAddress,
                    selectedOrder.DeliveryMethod,
                    selectedOrder.Status,
                    orderProductsList
                );

                // Generate the PDF file
                pdfGenerator.GeneratePdf();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте заказа в PDF: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Products
        // Для кнопки "Добавить товар"
        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var categories = new List<KeyValuePair<int, string>>();
            var manufacturers = new List<KeyValuePair<int, string>>();

            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();

                    // Получаем список категорий
                    var categoryCommand = new NpgsqlCommand("SELECT categoryid, namecategory FROM category ORDER BY namecategory", connection);
                    var categoryReader = categoryCommand.ExecuteReader();
                    while (categoryReader.Read())
                    {
                        categories.Add(new KeyValuePair<int, string>(
                            categoryReader.GetInt32(0),
                            categoryReader.GetString(1)
                        ));
                    }
                    categoryReader.Close();

                    // Получаем список производителей
                    var manufacturerCommand = new NpgsqlCommand("SELECT manufacturerid, namemanufacturer FROM manufacturer ORDER BY namemanufacturer", connection);
                    var manufacturerReader = manufacturerCommand.ExecuteReader();
                    while (manufacturerReader.Read())
                    {
                        manufacturers.Add(new KeyValuePair<int, string>(
                            manufacturerReader.GetInt32(0),
                            manufacturerReader.GetString(1)
                        ));
                    }
                    manufacturerReader.Close();
                }

                // Открываем диалог добавления товара
                var dialog = new ProductAddDialog(categories, manufacturers);
                if (dialog.ShowDialog() == true)
                {
                    // Добавляем проверку на дубликаты (опционально)
                    if (CheckProductDuplicate(dialog.ProductName, dialog.CategoryId, dialog.ManufacturerId))
                    {
                        var result = MessageBox.Show("Товар с такими параметрами уже существует. Добавить все равно?",
                            "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    // Используем транзакцию для безопасного добавления
                    AddProductWithTransaction(
                        dialog.ProductName,
                        dialog.Price,
                        dialog.Quantity,
                        dialog.CategoryId,
                        dialog.ManufacturerId,
                        dialog.Characteristic,
                        dialog.ImagePath

                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при работе с базой данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Проверка на дубликаты (опционально)
        private bool CheckProductDuplicate(string name, int categoryId, int manufacturerId)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    var command = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM product WHERE nameproduct = @name " +
                        "AND categoryid = @categoryId AND manufacturerid = @manufacturerId",
                        connection);

                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@categoryId", categoryId);
                    command.Parameters.AddWithValue("@manufacturerId", manufacturerId);

                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        // Безопасное добавление с транзакцией
        private void AddProductWithTransaction(string name, decimal price, int quantity,
    int categoryId, int manufacturerId, string characteristic, string imagePath)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var command = new NpgsqlCommand(
                                "INSERT INTO product (nameproduct, priceproduct, quantity, " +
                                "categoryid, manufacturerid, characteristic) " +
                                "VALUES (@name, @price, @quantity, @categoryId, @manufacturerId, @characteristic)",
                                connection, transaction);

                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@price", price);
                            command.Parameters.AddWithValue("@quantity", quantity);
                            command.Parameters.AddWithValue("@categoryId", categoryId);
                            command.Parameters.AddWithValue("@manufacturerId", manufacturerId);
                            command.Parameters.AddWithValue("@characteristic",
                                string.IsNullOrEmpty(characteristic) ? (object)DBNull.Value : characteristic);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                                MessageBox.Show("Товар успешно добавлен", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadProducts(); // Обновляем список товаров
                            }
                            else
                            {
                                transaction.Rollback();
                                MessageBox.Show("Не удалось добавить товар", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (PostgresException ex) when (ex.SqlState == "23505")
                        {
                            transaction.Rollback();
                            MessageBox.Show("Ошибка: товар с таким ID уже существует", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при добавлении товара: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Для кнопки "Изменить товар"
        private void btnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для редактирования", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Product selectedProduct = dgProducts.SelectedItem as Product;

            // Получаем полную информацию о товаре, категории и производителе
            int categoryId = 0;
            int manufacturerId = 0;
            var categories = new List<KeyValuePair<int, string>>();
            var manufacturers = new List<KeyValuePair<int, string>>();

            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();

                    // Получаем ID категории и производителя выбранного товара
                    var productCommand = new NpgsqlCommand(
                        "SELECT categoryid, manufacturerid FROM product WHERE productid = @productId",
                        connection);
                    productCommand.Parameters.AddWithValue("@productId", selectedProduct.ProductId);
                    var productReader = productCommand.ExecuteReader();
                    if (productReader.Read())
                    {
                        categoryId = productReader.GetInt32(0);
                        manufacturerId = productReader.GetInt32(1);
                    }
                    productReader.Close();

                    // Получаем список категорий
                    var categoryCommand = new NpgsqlCommand("SELECT categoryid, namecategory FROM category ORDER BY namecategory", connection);
                    var categoryReader = categoryCommand.ExecuteReader();
                    while (categoryReader.Read())
                    {
                        categories.Add(new KeyValuePair<int, string>(
                            categoryReader.GetInt32(0),
                            categoryReader.GetString(1)
                        ));
                    }
                    categoryReader.Close();

                    // Получаем список производителей
                    var manufacturerCommand = new NpgsqlCommand("SELECT manufacturerid, namemanufacturer FROM manufacturer ORDER BY namemanufacturer", connection);
                    var manufacturerReader = manufacturerCommand.ExecuteReader();
                    while (manufacturerReader.Read())
                    {
                        manufacturers.Add(new KeyValuePair<int, string>(
                            manufacturerReader.GetInt32(0),
                            manufacturerReader.GetString(1)
                        ));
                    }
                    manufacturerReader.Close();
                }

                // Открываем диалог редактирования товара
                var dialog = new ProductEditDialog(
                    selectedProduct,
                    categories,
                    manufacturers,
                    categoryId,
                    manufacturerId
                );

                if (dialog.ShowDialog() == true)
                {
                    UpdateProduct(
                        selectedProduct.ProductId,
                        dialog.ProductName,
                        dialog.Price,
                        dialog.Quantity,
                        dialog.CategoryId,
                        dialog.ManufacturerId,
                        dialog.Characteristic,
                        dialog.ImagePath
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавляем метод для добавления товара в БД
        private void AddProduct(string name, decimal price, int quantity, int categoryId,
    int manufacturerId, string characteristic, string imagePath)
        {
            if (ExecuteNonQuery(
                "INSERT INTO product (nameproduct, priceproduct, quantity, categoryid, manufacturerid, characteristic, imagepath) " +
                "VALUES (@name, @price, @quantity, @categoryId, @manufacturerId, @characteristic, @imagePath)",
                cmd => {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@manufacturerId", manufacturerId);
                    cmd.Parameters.AddWithValue("@characteristic",
                        string.IsNullOrEmpty(characteristic) ? (object)DBNull.Value : characteristic);
                    cmd.Parameters.AddWithValue("@imagePath",
                        string.IsNullOrEmpty(imagePath) ? (object)DBNull.Value : imagePath);
                },
                "Товар успешно добавлен", "Не удалось добавить товар"))
            {
                LoadProducts();
            }
        }

        // Добавляем метод для обновления товара в БД
        private void UpdateProduct(int productId, string name, decimal price, int quantity,
    int categoryId, int manufacturerId, string characteristic, string imagePath)
        {
            if (ExecuteNonQuery(
                "UPDATE product SET nameproduct = @name, priceproduct = @price, quantity = @quantity, " +
                "categoryid = @categoryId, manufacturerid = @manufacturerId, characteristic = @characteristic, " +
                "imagepath = @imagePath WHERE productid = @productId",
                cmd => {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@manufacturerId", manufacturerId);
                    cmd.Parameters.AddWithValue("@characteristic",
                        string.IsNullOrEmpty(characteristic) ? (object)DBNull.Value : characteristic);
                    cmd.Parameters.AddWithValue("@imagePath",
                        string.IsNullOrEmpty(imagePath) ? (object)DBNull.Value : imagePath);
                    cmd.Parameters.AddWithValue("@productId", productId);
                },
                "Товар успешно обновлен", "Не удалось обновить товар"))
            {
                LoadProducts();
            }
        }


        private void btnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            DeleteItem<Product>(dgProducts, "товар", "NameProduct",
                (product) => ExecuteNonQuery(
                    "DELETE FROM product WHERE productid = @id",
                    cmd => cmd.Parameters.AddWithValue("@id", product.ProductId),
                    "Товар успешно удален", "Не удалось удалить товар"
                ),
                LoadProducts);  // Перезагружаем список товаров
        }

        private void btnRefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();  // Перезагружаем список товаров
        }


        // Users
        private void btnBlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is User selectedUser)
            {
                if (selectedUser.IsActive)
                {
                    UpdateUserActiveStatus(selectedUser.UserId, false);
                }
                else
                {
                    MessageBox.Show("Пользователь уже заблокирован", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для блокировки", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnUnblockUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is User selectedUser)
            {
                if (!selectedUser.IsActive)
                {
                    UpdateUserActiveStatus(selectedUser.UserId, true);
                }
                else
                {
                    MessageBox.Show("Пользователь уже активен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для разблокировки", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnRefreshUsers_Click(object sender, RoutedEventArgs e) => LoadUsers();

        // Orders
        private void btnViewOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ для просмотра", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Order selectedOrder = dgOrders.SelectedItem as Order;
            MessageBox.Show($"Просмотр деталей заказа №{selectedOrder.OrderId}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnUpdateOrderStatus_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem is Order selectedOrder)
            {
                string[] statuses = { "Новый", "Подтвержден", "Обрабатывается", "Отправлен", "Доставлен", "Отменен" };
                var dialog = new SelectDialog("Обновление статуса заказа", "Выберите новый статус:", statuses, selectedOrder.Status);

                if (dialog.ShowDialog() == true)
                {
                    UpdateOrderStatus(selectedOrder.OrderId, dialog.SelectedValue);
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для обновления статуса", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnRefreshOrders_Click(object sender, RoutedEventArgs e) => LoadOrders();

        // Categories
        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditDialog<Category>("Добавление категории", null, AddCategory);
            dialog.ShowDialog();
        }

        private void btnEditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (dgCategories.SelectedItem is Category selectedCategory)
            {
                var dialog = new EditDialog<Category>("Редактирование категории",
                    selectedCategory.NameCategory, selectedCategory.Description,
                    (name, description) => UpdateCategory(selectedCategory.CategoryId, name, description));
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show("Выберите категорию для редактирования", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            DeleteItem<Category>(dgCategories, "категорию", "NameCategory",
                (category) => {
                    bool success = ExecuteNonQuery(
                        "DELETE FROM product WHERE categoryid = @id",
                        cmd => cmd.Parameters.AddWithValue("@id", category.CategoryId),
                        "", ""
                    );

                    if (success)
                    {
                        return ExecuteNonQuery(
                            "DELETE FROM category WHERE categoryid = @id",
                            cmd => cmd.Parameters.AddWithValue("@id", category.CategoryId),
                            "Категория успешно удалена", "Не удалось удалить категорию"
                        );
                    }
                    return false;
                },
                () => { LoadCategories(); LoadProducts(); });
        }

        private void btnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            DeleteItem<Order>(dgOrders, "заказ", "OrderId",
                (order) => {
                    // Сначала удаляем связанные товары в заказе
                    bool success = ExecuteNonQuery(
                        "DELETE FROM orderproduct WHERE orderid = @id",
                        cmd => cmd.Parameters.AddWithValue("@id", order.OrderId),
                        "", ""
                    );

                    if (success)
                    {
                        // Затем удаляем сам заказ
                        return ExecuteNonQuery(
                            "DELETE FROM ordertable WHERE orderid = @id",
                            cmd => cmd.Parameters.AddWithValue("@id", order.OrderId),
                            "Заказ успешно удален", "Не удалось удалить заказ"
                        );
                    }
                    return false;
                },
                () => {
                    LoadOrders();
                    OrderProducts.Clear(); // Очищаем список товаров заказа
                });
        }

        // Для кнопки "Добавить производителя"
        private void btnAddManufacturer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var countries = GetCountryList(); // Changed from LoadCountries()
                if (countries == null || countries.Count == 0)
                {
                    MessageBox.Show("Нет доступных стран. Сначала добавьте страны.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new ManufacturerAddDialog(countries);
                if (dialog.ShowDialog() == true)
                {
                    AddManufacturer(dialog.ManufacturerName, dialog.SelectedCountry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> GetCountryList()
        {
            var countries = new List<string>();
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "SELECT namecountry FROM country ORDER BY namecountry", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                countries.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки стран: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return countries;
        }

        // Для кнопки "Изменить производителя"
        private void btnEditManufacturer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgManufacturers.SelectedItem == null)
                {
                    MessageBox.Show("Выберите производителя для редактирования", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var countries = GetCountryList(); // Changed from LoadCountries()
                if (countries == null || countries.Count == 0)
                {
                    MessageBox.Show("Нет доступных стран.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Manufacturer selectedManufacturer = dgManufacturers.SelectedItem as Manufacturer;
                var dialog = new ManufacturerEditDialog(selectedManufacturer, countries);

                if (dialog.ShowDialog() == true)
                {
                    UpdateManufacturer(selectedManufacturer.ManufacturerId,
                                     dialog.ManufacturerName,
                                     dialog.SelectedCountry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Для кнопки "Удалить производителя"
        private void btnDeleteManufacturer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgManufacturers.SelectedItem == null)
                {
                    MessageBox.Show("Выберите производителя для удаления", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Manufacturer manufacturer = dgManufacturers.SelectedItem as Manufacturer;

                // Проверяем есть ли связанные товары
                if (HasProducts(manufacturer.ManufacturerId))
                {
                    MessageBox.Show("Невозможно удалить производителя, так как существуют связанные товары",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Подтверждение удаления
                var result = MessageBox.Show($"Вы действительно хотите удалить производителя '{manufacturer.NameManufacturer}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (ExecuteNonQuery(
                        "DELETE FROM manufacturer WHERE manufacturerid = @id",
                        cmd => cmd.Parameters.AddWithValue("@id", manufacturer.ManufacturerId),
                        "Производитель успешно удален",
                        "Не удалось удалить производителя"))
                    {
                        LoadManufacturers();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении производителя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Для кнопки "Добавить страну"
        private void btnAddCountry_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CountryAddDialog();
            if (dialog.ShowDialog() == true)
            {
                AddCountry(dialog.CountryName);
            }
        }

        // Для кнопки "Изменить страну"
        private void btnEditCountry_Click(object sender, RoutedEventArgs e)
        {
            if (dgCountries.SelectedItem == null)
            {
                MessageBox.Show("Выберите страну для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Country selectedCountry = dgCountries.SelectedItem as Country;
            var dialog = new CountryEditDialog(selectedCountry);

            if (dialog.ShowDialog() == true)
            {
                UpdateCountry(selectedCountry.CountryId, dialog.CountryName);
            }
        }

        private void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            DeleteItem<User>(dgUsers, "пользователя", "Login",
                (user) => {
                    // Проверяем, есть ли заказы у пользователя
                    if (HasUserOrders(user.UserId))
                    {
                        MessageBox.Show("Невозможно удалить пользователя, так как у него есть заказы",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    return ExecuteNonQuery(
                        "DELETE FROM useraccount WHERE userid = @id",
                        cmd => cmd.Parameters.AddWithValue("@id", user.UserId),
                        "Пользователь успешно удален",
                        "Не удалось удалить пользователя"
                    );
                },
                LoadUsers);
        }

        // Проверка наличия заказов у пользователя
        private bool HasUserOrders(int userId)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM ordertable WHERE userid = @userId",
                        connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        return Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                }
            }
            catch
            {
                return true; // В случае ошибки считаем, что заказы есть (для безопасности)
            }
        }

        // Для кнопки "Удалить страну"
        private void btnDeleteCountry_Click(object sender, RoutedEventArgs e)
        {
            if (dgCountries.SelectedItem == null)
            {
                MessageBox.Show("Выберите страну для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Country selectedCountry = dgCountries.SelectedItem as Country;

            var result = MessageBox.Show($"Вы действительно хотите удалить страну '{selectedCountry.NameCountry}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteCountry(selectedCountry.CountryId);
            }
        }

        // Проверка наличия товаров производителя
        private bool HasProducts(int manufacturerId)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM product WHERE manufacturerid = @manufacturerId",
                        connection))
                    {
                        command.Parameters.AddWithValue("@manufacturerId", manufacturerId);
                        return Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                }
            }
            catch
            {
                return true; // В случае ошибки считаем что товары есть (для безопасности)
            }
        }


// Добавление производителя
private void AddManufacturer(string name, string countryName)
        {
            try
            {
                if (ExecuteNonQuery(
                    "INSERT INTO manufacturer (namemanufacturer, countryid) " +
                    "VALUES (@name, (SELECT countryid FROM country WHERE namecountry = @countryName))",
                    cmd => {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@countryName", countryName);
                    },
                    "Производитель успешно добавлен",
                    "Не удалось добавить производителя"))
                {
                    LoadManufacturers();
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                MessageBox.Show("Производитель с таким названием уже существует", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательный метод для получения ID страны
        private int? GetCountryId(string countryName)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "SELECT countryid FROM country WHERE namecountry = @name", connection))
                    {
                        command.Parameters.AddWithValue("@name", countryName);
                        var result = command.ExecuteScalar();
                        return result != null ? (int?)Convert.ToInt32(result) : null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // Вспомогательный метод для создания новой страны
        private int? CreateCountry(string countryName)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "INSERT INTO country (namecountry) VALUES (@name) RETURNING countryid",
                        connection))
                    {
                        command.Parameters.AddWithValue("@name", countryName);
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // Метод для обновления производителя
        private void UpdateManufacturer(int manufacturerId, string name, string countryName)
        {
            try
            {
                if (ExecuteNonQuery(
                    "UPDATE manufacturer SET " +
                    "namemanufacturer = @name, " +
                    "countryid = (SELECT countryid FROM country WHERE namecountry = @countryName) " +
                    "WHERE manufacturerid = @id",
                    cmd => {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@countryName", countryName);
                        cmd.Parameters.AddWithValue("@id", manufacturerId);
                    },
                    "Производитель успешно обновлен",
                    "Не удалось обновить производителя"))
                {
                    LoadManufacturers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка стран
        private void LoadCountries()
        {
            string query = "SELECT countryid, namecountry FROM country ORDER BY namecountry";

            ExecuteQuery(query, reader => {
                Countries.Clear();
                while (reader.Read())
                {
                    Countries.Add(new Country
                    {
                        CountryId = reader.GetInt32(0),
                        NameCountry = reader.GetString(1)
                    });
                }
            }, "Ошибка загрузки стран");
        }

        // Добавление страны
        private void AddCountry(string name)
        {
            // First check if country with this name already exists
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();

                    // Check if country already exists
                    using (var checkCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM country WHERE LOWER(namecountry) = LOWER(@name)", connection))
                    {
                        checkCommand.Parameters.AddWithValue("@name", name);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Страна с таким названием уже существует", "Предупреждение",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Insert new country without specifying ID
                    using (var command = new NpgsqlCommand(
                        "INSERT INTO country (namecountry) VALUES (@name)", connection))
                    {
                        command.Parameters.AddWithValue("@name", name);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Страна успешно добавлена", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadCountries();
                            // If you updated any references to LoadCountries() as suggested:
                            LoadManufacturers();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось добавить страну", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                MessageBox.Show("Страна с таким названием уже существует", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении страны: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обновление страны
        private void UpdateCountry(int countryId, string name)
        {
            if (ExecuteNonQuery(
                "UPDATE country SET namecountry = @name WHERE countryid = @id",
                cmd => {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@id", countryId);
                },
                "Страна успешно обновлена",
                "Не удалось обновить страну"))
            {
                LoadCountries();
                LoadManufacturers(); // Обновляем список производителей
            }
        }

        // Удаление страны
        private void DeleteCountry(int countryId)
        {
            // Проверяем, есть ли производители из этой страны
            if (HasManufacturers(countryId))
            {
                MessageBox.Show("Нельзя удалить страну, так как существуют производители из этой страны",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ExecuteNonQuery(
                "DELETE FROM country WHERE countryid = @id",
                cmd => cmd.Parameters.AddWithValue("@id", countryId),
                "Страна успешно удалена",
                "Не удалось удалить страну"))
            {
                LoadCountries();
            }
        }

        // Проверка наличия производителей из страны
        private bool HasManufacturers(int countryId)
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM manufacturer WHERE countryid = @countryId", connection))
                    {
                        command.Parameters.AddWithValue("@countryId", countryId);
                        return Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                }
            }
            catch
            {
                return true; // В случае ошибки считаем, что производители есть
            }
        }



        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrders.SelectedItem is Order selectedOrder)
            {
                LoadOrderProducts(selectedOrder.OrderId);
            }
            else
            {
                OrderProducts.Clear();
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите выйти из системы?", "Подтверждение выхода",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MainWindow loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
        }
        #endregion

        #region Helper Methods
        private void DeleteItem<T>(DataGrid dataGrid, string itemName, string nameProperty,
            Func<T, bool> deleteAction, Action refreshAction)
        {
            if (dataGrid.SelectedItem is T selectedItem)
            {
                string displayName = GetPropertyValue(selectedItem, nameProperty)?.ToString() ?? "выбранный элемент";

                if (MessageBox.Show($"Вы действительно хотите удалить {itemName} '{displayName}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (deleteAction(selectedItem))
                    {
                        refreshAction();
                    }
                }
            }
            else
            {
                MessageBox.Show($"Выберите {itemName} для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
        }

        private void UpdateUserActiveStatus(int userId, bool isActive)
        {
            string query = isActive
                ? "UPDATE useraccount SET isactive = @isActive, login_attempts = 0 WHERE userid = @userId"
                : "UPDATE useraccount SET isactive = @isActive WHERE userid = @userId";

            ExecuteNonQuery(
                query,
                cmd => {
                    cmd.Parameters.AddWithValue("@isActive", isActive);
                    cmd.Parameters.AddWithValue("@userId", userId);
                },
                isActive ? "Пользователь успешно разблокирован" : "Пользователь успешно заблокирован",
                "Не удалось изменить статус пользователя"
            );

            LoadUsers();
        }

        private void UpdateOrderStatus(int orderId, string status)
        {
            if (ExecuteNonQuery(
                "UPDATE ordertable SET status = @status WHERE orderid = @orderId",
                cmd => {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                },
                "Статус заказа успешно обновлен", "Не удалось обновить статус заказа"))
            {
                LoadOrders();
            }
        }

        private void AddCategory(string name, string description)
        {
            if (ExecuteNonQuery(
                "INSERT INTO category (namecategory, description) VALUES (@name, @description)",
                cmd => {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                },
                "Категория успешно добавлена", "Не удалось добавить категорию"))
            {
                LoadCategories();
            }
        }

        private void UpdateCategory(int categoryId, string name, string description)
        {
            if (ExecuteNonQuery(
                "UPDATE category SET namecategory = @name, description = @description WHERE categoryid = @categoryId",
                cmd => {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                },
                "Категория успешно обновлена", "Не удалось обновить категорию"))
            {
                LoadCategories();
            }
        }
        #endregion

        private void dgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }



    #region Dialog Windows
    public class SelectDialog : Window
    {
        private ComboBox comboOptions;
        public string SelectedValue { get; private set; }

        public SelectDialog(string title, string message, string[] options, string currentValue)
        {
            Title = title;
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            Grid grid = new Grid();
            grid.Margin = new Thickness(10);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(new TextBlock
            {
                Text = message,
                Margin = new Thickness(0, 0, 0, 10)
            });
            Grid.SetRow(grid.Children[0], 0);

            comboOptions = new ComboBox
            {
                ItemsSource = options,
                SelectedItem = currentValue,
                Margin = new Thickness(0, 0, 0, 20)
            };
            grid.Children.Add(comboOptions);
            Grid.SetRow(comboOptions, 1);

            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            buttons.Children.Add(new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            });
            ((Button)buttons.Children[0]).Click += (s, e) => {
                SelectedValue = comboOptions.SelectedItem as string;
                DialogResult = true;
            };

            buttons.Children.Add(new Button
            {
                Content = "Отмена",
                Width = 80,
                IsCancel = true
            });
            ((Button)buttons.Children[1]).Click += (s, e) => DialogResult = false;

            grid.Children.Add(buttons);
            Grid.SetRow(buttons, 2);

            Content = grid;
        }
    }

    public class EditDialog<T> : Window
    {
        public string ItemName { get; private set; }
        public string ItemDescription { get; private set; }

        private TextBox txtItemName;
        private TextBox txtItemDescription;
        private Action<string, string> saveAction;

        public EditDialog(string title, string initialName, Action<string, string> onSave)
            : this(title, initialName, "", onSave) { }

        public EditDialog(string title, string initialName, string initialDescription, Action<string, string> onSave)
        {
            Title = title;
            Width = 400;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            saveAction = onSave;

            Grid grid = new Grid();
            grid.Margin = new Thickness(10);
            for (int i = 0; i < 5; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = i < 4 ? GridLength.Auto : new GridLength(1, GridUnitType.Star)
                });
            }

            // Name label
            grid.Children.Add(new TextBlock
            {
                Text = "Название:",
                Margin = new Thickness(0, 0, 0, 5)
            });
            Grid.SetRow(grid.Children[0], 0);

            // Name input
            txtItemName = new TextBox
            {
                Text = initialName,
                Margin = new Thickness(0, 0, 0, 10)
            };
            grid.Children.Add(txtItemName);
            Grid.SetRow(txtItemName, 1);

            // Description label
            grid.Children.Add(new TextBlock
            {
                Text = "Описание:",
                Margin = new Thickness(0, 0, 0, 5)
            });
            Grid.SetRow(grid.Children[2], 2);

            // Description input
            txtItemDescription = new TextBox
            {
                Text = initialDescription,
                Margin = new Thickness(0, 0, 0, 20),
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true
            };
            grid.Children.Add(txtItemDescription);
            Grid.SetRow(txtItemDescription, 3);

            // Buttons
            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            buttons.Children.Add(new Button
            {
                Content = "Сохранить",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            });
            ((Button)buttons.Children[0]).Click += SaveButton_Click;

            buttons.Children.Add(new Button
            {
                Content = "Отмена",
                Width = 80,
                IsCancel = true
            });
            ((Button)buttons.Children[1]).Click += (s, e) => DialogResult = false;

            grid.Children.Add(buttons);
            Grid.SetRow(buttons, 4);

            Content = grid;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                MessageBox.Show("Название не может быть пустым", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ItemName = txtItemName.Text;
            ItemDescription = txtItemDescription.Text;
            saveAction?.Invoke(ItemName, ItemDescription);
            DialogResult = true;
        }
    }
    #endregion
}