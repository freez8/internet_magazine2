using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Interner_magazine
{
    public class ProductAddDialog : Window
    {
        private TextBox txtName;
        private TextBox txtPrice;
        private TextBox txtQuantity;
        private ComboBox comboCategory;
        private ComboBox comboManufacturer;
        private TextBox txtCharacteristic;
        private Image imgPreview;
        private Button btnBrowse;
        private TextBlock txtSelectedImage;
        private Button btnClearImage;

        public string ProductName => txtName.Text;
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }
        public int CategoryId => (int)comboCategory.SelectedValue;
        public int ManufacturerId => (int)comboManufacturer.SelectedValue;
        public string Characteristic => txtCharacteristic.Text;
        public string ImagePath { get; private set; }

        public ProductAddDialog(
            List<KeyValuePair<int, string>> categories,
            List<KeyValuePair<int, string>> manufacturers)
        {
            Title = "Добавление товара";
            Width = 700;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            InitializeComponents(categories, manufacturers);
        }

        private void InitializeComponents(List<KeyValuePair<int, string>> categories, List<KeyValuePair<int, string>> manufacturers)
        {
            Grid grid = new Grid();
            grid.Margin = new Thickness(10);

            // 15 строк: 7 пар label + input + кнопки
            for (int i = 0; i < 15; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            int currentRow = 0;

            // Название товара
            AddLabel(grid, "Название товара:", currentRow++);
            txtName = AddTextBox(grid, currentRow++);

            // Цена
            AddLabel(grid, "Цена:", currentRow++);
            txtPrice = AddTextBox(grid, currentRow++);
            txtPrice.Text = "0.00";

            // Количество
            AddLabel(grid, "Количество:", currentRow++);
            txtQuantity = AddTextBox(grid, currentRow++);
            txtQuantity.Text = "0";

            // Категория
            AddLabel(grid, "Категория:", currentRow++);
            comboCategory = AddComboBox(grid, categories, currentRow++);

            // Производитель
            AddLabel(grid, "Производитель:", currentRow++);
            comboManufacturer = AddComboBox(grid, manufacturers, currentRow++);

            // Характеристики
            AddLabel(grid, "Характеристики:", currentRow++);
            txtCharacteristic = AddTextBox(grid, currentRow++, true);
            txtCharacteristic.Height = 60;

            // Изображение
            AddLabel(grid, "Изображение:", currentRow++);
            var imgPanel = CreateImagePanel();
            grid.Children.Add(imgPanel);
            Grid.SetRow(imgPanel, currentRow++);

            // Кнопки
            var buttons = CreateButtonsPanel();
            grid.Children.Add(buttons);
            Grid.SetRow(buttons, currentRow);

            Content = grid;
        }

        private void AddLabel(Grid grid, string text, int row)
        {
            var label = new TextBlock { Text = text, Margin = new Thickness(0, 0, 0, 5) };
            grid.Children.Add(label);
            Grid.SetRow(label, row);
        }

        private TextBox AddTextBox(Grid grid, int row, bool isMultiline = false)
        {
            var textBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = isMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                AcceptsReturn = isMultiline
            };
            grid.Children.Add(textBox);
            Grid.SetRow(textBox, row);
            return textBox;
        }

        private ComboBox AddComboBox(Grid grid, List<KeyValuePair<int, string>> items, int row)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = items,
                DisplayMemberPath = "Value",
                SelectedValuePath = "Key",
                Margin = new Thickness(0, 0, 0, 10)
            };
            grid.Children.Add(comboBox);
            Grid.SetRow(comboBox, row);
            return comboBox;
        }

        private StackPanel CreateImagePanel()
        {
            StackPanel imgPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Preview image
            imgPreview = new Image
            {
                Width = 100,
                Height = 100,
                Stretch = System.Windows.Media.Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 10, 0)
            };
            imgPanel.Children.Add(imgPreview);

            // Control panel
            StackPanel controlPanel = new StackPanel { Orientation = Orientation.Vertical };

            // Browse button
            btnBrowse = new Button
            {
                Content = "Выбрать изображение",
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            btnBrowse.Click += BrowseImage_Click;
            controlPanel.Children.Add(btnBrowse);

            // Clear button
            btnClearImage = new Button
            {
                Content = "Удалить изображение",
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5),
                IsEnabled = false
            };
            btnClearImage.Click += ClearImage_Click;
            controlPanel.Children.Add(btnClearImage);

            // Selected image info
            txtSelectedImage = new TextBlock
            {
                Text = "Изображение не выбрано",
                TextWrapping = TextWrapping.Wrap,
                Width = 250
            };
            controlPanel.Children.Add(txtSelectedImage);

            imgPanel.Children.Add(controlPanel);
            return imgPanel;
        }

        private StackPanel CreateButtonsPanel()
        {
            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button btnAdd = new Button
            {
                Content = "Добавить",
                Width = 100,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            btnAdd.Click += AddButton_Click;
            buttons.Children.Add(btnAdd);

            Button btnCancel = new Button
            {
                Content = "Отмена",
                Width = 100,
                IsCancel = true
            };
            btnCancel.Click += (s, e) => DialogResult = false;
            buttons.Children.Add(btnCancel);

            return buttons;
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выбор изображения товара",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    ImagePath = openFileDialog.FileName;
                    txtSelectedImage.Text = Path.GetFileName(ImagePath);
                    btnClearImage.IsEnabled = true;

                    // Load image preview
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(ImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ClearImage();
                }
            }
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
        }

        private void ClearImage()
        {
            ImagePath = null;
            imgPreview.Source = null;
            txtSelectedImage.Text = "Изображение не выбрано";
            btnClearImage.IsEnabled = false;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            if (!string.IsNullOrEmpty(ImagePath))
            {
                try
                {
                    string productImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages");
                    if (!Directory.Exists(productImagesFolder))
                        Directory.CreateDirectory(productImagesFolder);

                    string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(ImagePath)}";
                    string destPath = Path.Combine(productImagesFolder, uniqueFileName);

                    File.Copy(ImagePath, destPath, true);
                    ImagePath = Path.Combine("ProductImages", uniqueFileName); // Сохраняем относительный путь
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ImagePath = null;
                }
            }

            DialogResult = true;
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Название товара не может быть пустым", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) || price < 0)
            {
                MessageBox.Show("Укажите корректную цену товара", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Price = price;

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity < 0)
            {
                MessageBox.Show("Укажите корректное количество товара", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Quantity = quantity;

            if (comboCategory.SelectedValue == null)
            {
                MessageBox.Show("Выберите категорию товара", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (comboManufacturer.SelectedValue == null)
            {
                MessageBox.Show("Выберите производителя товара", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}