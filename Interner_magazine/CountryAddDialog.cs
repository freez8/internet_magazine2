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
    public class CountryAddDialog : Window
    {
        private TextBox txtName;

        public string CountryName => txtName.Text;

        public CountryAddDialog()
        {
            Title = "Добавление страны";
            Width = 300;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Grid grid = new Grid();
            grid.Margin = new Thickness(10);

            // Создаем 3 строки
            for (int i = 0; i < 3; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = i < 2 ? GridLength.Auto : new GridLength(1, GridUnitType.Star)
                });
            }

            int currentRow = 0;

            // Название страны
            grid.Children.Add(new TextBlock { Text = "Название страны:", Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(grid.Children[currentRow], currentRow++);

            txtName = new TextBox { Margin = new Thickness(0, 0, 0, 20) };
            grid.Children.Add(txtName);
            Grid.SetRow(txtName, currentRow++);

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
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            btnAdd.Click += AddButton_Click;
            buttons.Children.Add(btnAdd);

            Button btnCancel = new Button
            {
                Content = "Отмена",
                Width = 80,
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
                MessageBox.Show("Название страны не может быть пустым", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
