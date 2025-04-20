using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static Interner_magazine.AdminPanelWindow;

namespace Interner_magazine
{
    public class ManufacturerEditDialog : Window
    {
        private TextBox txtName;
        private ComboBox cmbCountry;

        public string ManufacturerName => txtName.Text;
        public string SelectedCountry => cmbCountry.SelectedItem?.ToString();

        public ManufacturerEditDialog(Manufacturer manufacturer, List<string> countries)
        {
            Title = "Редактирование производителя";
            Width = 400;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            InitializeComponents(manufacturer, countries);
        }

        private void InitializeComponents(Manufacturer manufacturer, List<string> countries)
        {
            Grid grid = new Grid();
            grid.Margin = new Thickness(10);

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

            txtName = new TextBox { Text = manufacturer.NameManufacturer, Margin = new Thickness(0, 0, 0, 10) };
            grid.Children.Add(txtName);
            Grid.SetRow(txtName, currentRow++);

            // Страна (теперь ComboBox)
            grid.Children.Add(new TextBlock { Text = "Страна:", Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(grid.Children[currentRow], currentRow++);

            cmbCountry = new ComboBox
            {
                ItemsSource = countries,
                Margin = new Thickness(0, 0, 0, 20),
                SelectedItem = manufacturer.CountryName // Устанавливаем текущую страну
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

            Button btnSave = new Button
            {
                Content = "Сохранить",
                Width = 100,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            btnSave.Click += SaveButton_Click;
            buttons.Children.Add(btnSave);

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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
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