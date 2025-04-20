using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Interner_magazine
{
    public class ManufacturerAddDialog : Window
    {
        private TextBox txtName;
        private ComboBox cmbCountry;

        public string ManufacturerName => txtName.Text;
        public string SelectedCountry => cmbCountry.SelectedItem?.ToString();

        public ManufacturerAddDialog(List<string> countries)
        {
            Title = "Добавление производителя";
            Width = 400;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            InitializeComponents(countries);
        }

        private void InitializeComponents(List<string> countries)
        {
            Grid grid = new Grid();
            grid.Margin = new Thickness(10);

            // Создаем 5 строк
            for (int i = 0; i < 5; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = i < 4 ? GridLength.Auto : new GridLength(1, GridUnitType.Star)
                });
            }

            int currentRow = 0;

            // Название производителя
            grid.Children.Add(new TextBlock { Text = "Название производителя:", Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(grid.Children[currentRow], currentRow++);

            txtName = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            grid.Children.Add(txtName);
            Grid.SetRow(txtName, currentRow++);

            // Страна (теперь ComboBox вместо TextBox)
            grid.Children.Add(new TextBlock { Text = "Страна:", Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(grid.Children[currentRow], currentRow++);

            cmbCountry = new ComboBox
            {
                ItemsSource = countries,
                Margin = new Thickness(0, 0, 0, 20),
                SelectedIndex = 0 // Выбираем первую страну по умолчанию
            };
            grid.Children.Add(cmbCountry);
            Grid.SetRow(cmbCountry, currentRow++);

            // Кнопки
            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
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
            buttons.Children.Add(btnCancel);

            grid.Children.Add(buttons);
            Grid.SetRow(buttons, currentRow);

            Content = grid;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs())
                DialogResult = true;
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Название производителя не может быть пустым", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (cmbCountry.SelectedItem == null)
            {
                MessageBox.Show("Выберите страну производителя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
