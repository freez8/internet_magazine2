using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace Interner_magazine
{
    public partial class CatalogWindow : Window
    {
        private DatabaseConnection _dbConnection = new DatabaseConnection();
        private int _userId;
        private string _firstName;
        private string _lastName;
        private decimal _totalSum = 0;

        public class ProductItem
        {
            public int ProductId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public int StockQuantity => Quantity;
            public string Category { get; set; }
            public string Manufacturer { get; set; }
            public string ImagePath { get; set; }
        }

        public class CartItem
        {
            public int ProductId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public string ImagePath { get; set; }
            public decimal TotalPrice => Price * Quantity; // Добавим свойство для общей суммы
        }

        public CatalogWindow(int userId, string firstName, string lastName)
        {
            InitializeComponent();

            _userId = userId;
            _firstName = firstName;
            _lastName = lastName;

            txtUserInfo.Text = $"{firstName} {lastName}";
            cmbDeliveryMethod.SelectedIndex = 0;

            // Устанавливаем источник данных для корзины
            lvCart.ItemsSource = cartItems;

            LoadProducts();
            lvProducts.SelectionChanged += LvProducts_SelectionChanged;

            // Инициализируем отображение суммы
            CalculateTotalSum();
        }

        private void LoadProducts()
        {
            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();

                    string query = @"
                SELECT p.productid, p.nameproduct, p.priceproduct, p.quantity, 
                       c.namecategory, m.namemanufacturer, p.imagepath
                FROM product p
                JOIN category c ON p.categoryid = c.categoryid
                JOIN manufacturer m ON p.manufacturerid = m.manufacturerid
                ORDER BY p.productid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        var products = new List<ProductItem>();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new ProductItem
                                {
                                    ProductId = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Price = reader.GetDecimal(2),
                                    Quantity = reader.GetInt32(3),
                                    Category = reader.GetString(4),
                                    Manufacturer = reader.GetString(5),
                                    ImagePath = reader.IsDBNull(6) ? null : reader.GetString(6)
                                });
                            }
                        }

                        lvProducts.ItemsSource = products;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                LoadProducts();
                return;
            }

            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();

                    string query = @"
                SELECT p.productid, p.nameproduct, p.priceproduct, p.quantity, 
                       c.namecategory, m.namemanufacturer, p.imagepath
                FROM product p
                JOIN category c ON p.categoryid = c.categoryid
                JOIN manufacturer m ON p.manufacturerid = m.manufacturerid
                WHERE LOWER(p.nameproduct) LIKE LOWER(@search)
                ORDER BY p.productid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@search", $"%{searchText}%");

                        var products = new List<ProductItem>();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string imagePath = reader.IsDBNull(6) ? "/ProductImages/no-image.png" :
                                    "C:\\Users\\3loqic\\source\\repos\\Interner_magazine\\Interner_magazine\\ProductImages\\" + reader.GetString(6);

                                products.Add(new ProductItem
                                {
                                    ProductId = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Price = reader.GetDecimal(2),
                                    Quantity = reader.GetInt32(3),
                                    Category = reader.GetString(4),
                                    Manufacturer = reader.GetString(5),
                                    ImagePath = imagePath
                                });
                            }
                        }

                        lvProducts.ItemsSource = products;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LvProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotalSum();
        }

        private void UpdateCartCount()
        {
            int totalItems = cartItems.Sum(c => c.Quantity);
            string itemText;
            if (totalItems == 1)
                itemText = "товар";
            else if (totalItems >= 2 && totalItems <= 4)
                itemText = "товара";
            else
                itemText = "товаров";
            txtCartCount.Text = $"{totalItems} {itemText}";
        }

        private void CalculateTotalSum()
        {
            // Сумма товаров
            _totalSum = cartItems.Sum(c => c.Price * c.Quantity);

            // Количество товаров
            int totalItems = cartItems.Sum(c => c.Quantity);

            // Стоимость доставки
            decimal deliveryCost = 0;
            if (cmbDeliveryMethod.SelectedItem != null)
            {
                var selectedItem = cmbDeliveryMethod.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    if (selectedItem.Content.ToString() == "Почтой")
                        deliveryCost = 100; // Стоимость почтовой доставки
                    else if (selectedItem.Content.ToString() == "Доставка курьером")
                        deliveryCost = 500; // Стоимость курьерской доставки
                                            // Самовывоз = 0 (по умолчанию)
                }
            }

            // Итоговая сумма (товары + доставка)
            decimal finalSum = _totalSum + deliveryCost;

            // Обновляем текстовые поля
            txtItemsCount.Text = totalItems.ToString();
            txtDeliveryPrice.Text = $"{deliveryCost:N2} руб.";
            txtTotalSum.Text = $"{finalSum:N2} руб.";
            txtFinalSum.Text = $"{finalSum:N2} руб.";
        }

        private void btnMakeOrder_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите товары для заказа", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Создаем доставку
                            int deliveryId;
                            string deliveryQuery = @"INSERT INTO delivery (address, deliverymethod)
                        VALUES (@address, @method)
                        RETURNING deliveryid";

                            using (var command = new NpgsqlCommand(deliveryQuery, connection, transaction))
                            {
                                string address = cmbDeliveryMethod.SelectedIndex == 1 ? "Самовывоз" : txtDeliveryAddress.Text;

                                // Проверка длины адреса
                                if (address.Length > 100)
                                {
                                    MessageBox.Show("Адрес доставки слишком длинный. Максимум 100 символов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }

                                command.Parameters.AddWithValue("@address", address);

                                // Получаем только текст выбранного способа доставки
                                var deliveryMethod = (cmbDeliveryMethod.SelectedItem as ComboBoxItem)?.Content.ToString();
                                command.Parameters.AddWithValue("@method", deliveryMethod);
                                deliveryId = (int)command.ExecuteScalar();
                            }

                            // 2. Чек (checkreceipt)
                            int checkId;
                            string checkQuery = @"INSERT INTO checkreceipt (price, createdat)
                                     VALUES (@price, @date)
                                     RETURNING checkid";

                            using (var command = new NpgsqlCommand(checkQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@price", _totalSum);
                                command.Parameters.AddWithValue("@date", DateTime.Now);
                                checkId = (int)command.ExecuteScalar();
                            }

                            // 3. Создаем заказ (ordertable)
                            int orderId;
                            string orderQuery = @"INSERT INTO ordertable (userid, deliveryid, checkid, orderdate, status)
                                     VALUES (@userid, @deliveryid, @checkid, @date, @status)
                                     RETURNING orderid";

                            using (var command = new NpgsqlCommand(orderQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@userid", _userId);
                                command.Parameters.AddWithValue("@deliveryid", deliveryId);
                                command.Parameters.AddWithValue("@checkid", checkId);
                                command.Parameters.AddWithValue("@date", DateTime.Now);
                                command.Parameters.AddWithValue("@status", "Processing");
                                orderId = (int)command.ExecuteScalar();
                            }

                            // 4. Добавляем товары в заказ и обновляем количество в БД
                            foreach (var item in cartItems)
                            {
                                // Добавляем товар в заказ
                                string orderProductQuery = @"INSERT INTO orderproduct (orderid, productid, quantity)
                                                VALUES (@orderid, @productid, @quantity)";

                                using (var command = new NpgsqlCommand(orderProductQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@orderid", orderId);
                                    command.Parameters.AddWithValue("@productid", item.ProductId);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);
                                    command.ExecuteNonQuery();
                                }

                                // Обновляем количество товара в БД
                                string updateProductQuery = @"UPDATE product 
                                                   SET quantity = quantity - @quantity 
                                                   WHERE productid = @productid";

                                using (var command = new NpgsqlCommand(updateProductQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@productid", item.ProductId);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);
                                    command.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            ResetOrderForm();
                            LoadProducts(); // Перезагружаем список товаров для обновления количества
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetOrderForm()
        {
            // Очищаем корзину
            cartItems.Clear();
            lvCart.ItemsSource = null;
            lvCart.ItemsSource = cartItems;

            // Очищаем информацию о заказе
            lvProducts.UnselectAll();
            txtDeliveryAddress.Clear();
            cmbDeliveryMethod.SelectedIndex = 0;
            txtTotalSum.Text = "0.00 руб.";
            txtItemsCount.Text = "0";
            txtDeliveryPrice.Text = "0.00 руб.";
            txtFinalSum.Text = "0.00 руб.";
            UpdateCartCount(); // Update cart count
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        private List<CartItem> cartItems = new List<CartItem>();

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.DataContext as ProductItem;

            if (product == null) return;

            // Получаем TextBox из шаблона
            var container = FindParent<ListViewItem>(button);
            if (container == null) return;

            var txtQuantity = FindChild<TextBox>(container, "txtQuantity");
            if (txtQuantity == null || !int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (product.Quantity < quantity)
            {
                MessageBox.Show($"Недостаточно товара на складе. Доступно: {product.Quantity}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, есть ли товар уже в корзине
            var existingItem = cartItems.FirstOrDefault(c => c.ProductId == product.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                // Добавляем новый товар в корзину
                cartItems.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImagePath = product.ImagePath
                });
            }

            // Уменьшаем количество в каталоге
            product.Quantity -= quantity;

            // Обновляем отображение
            lvProducts.Items.Refresh();
            lvCart.ItemsSource = null;
            lvCart.ItemsSource = cartItems;
            CalculateTotalSum();
            UpdateCartCount(); // Add this line
        }

        private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button?.DataContext as CartItem;

            if (cartItem != null)
            {
                // Находим соответствующий товар в каталоге
                var product = (lvProducts.ItemsSource as IEnumerable<ProductItem>)?
                    .FirstOrDefault(p => p.ProductId == cartItem.ProductId);

                if (product != null)
                {
                    // Возвращаем количество в каталог
                    product.Quantity += cartItem.Quantity;
                    lvProducts.Items.Refresh();
                }

                // Удаляем из корзины
                cartItems.Remove(cartItem);

                // Обновляем отображение
                lvCart.ItemsSource = null;
                lvCart.ItemsSource = cartItems;
                CalculateTotalSum();
                UpdateCartCount(); // Add this line
            }
        }

        private void cmbDeliveryMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotalSum();
        }

        // Вспомогательные методы для поиска элементов в визуальном дереве
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            T foundChild = null;
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    if (!string.IsNullOrEmpty(childName))
                    {
                        var frameworkElement = child as FrameworkElement;
                        if (frameworkElement != null && frameworkElement.Name == childName)
                        {
                            return typedChild;
                        }
                    }
                    else
                    {
                        foundChild = typedChild;
                        break;
                    }
                }
                foundChild = FindChild<T>(child, childName);
                if (foundChild != null) break;
            }
            return foundChild;
        }
    }
}