using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Globalization;
using Microsoft.Win32;
using static Interner_magazine.AdminPanelWindow;

namespace Interner_magazine
{
    public class OrderPdfGenerator
    {
        // Store order information
        private int _orderId;
        private string _userName;
        private DateTime _orderDate;
        private string _deliveryAddress;
        private string _deliveryMethod;
        private string _status;
        private List<OrderProduct> _orderProducts;

        // PDF document settings
        private readonly XFont _titleFont = new XFont("Arial", 16);
        private readonly XFont _subtitleFont = new XFont("Arial", 12);
        private readonly XFont _normalFont = new XFont("Arial", 10);
        private readonly XFont _normalBoldFont = new XFont("Arial", 10);
        private readonly XFont _footerFont = new XFont("Arial", 8);

        // Constructor that initializes with order data
        public OrderPdfGenerator(
            int orderId,
            string userName,
            DateTime orderDate,
            string deliveryAddress,
            string deliveryMethod,
            string status,
            List<OrderProduct> orderProducts)
        {
            _orderId = orderId;
            _userName = userName;
            _orderDate = orderDate;
            _deliveryAddress = deliveryAddress;
            _deliveryMethod = deliveryMethod;
            _status = status;
            _orderProducts = orderProducts;
        }

        // Main method to generate the PDF
        public void GeneratePdf()
        {
            try
            {
                // Set default encoding for PDF generation
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                // Create a new PDF document
                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
                document.Info.Title = $"Заказ №{_orderId}";
                document.Info.Author = "Интернет-магазин";
                document.Info.CreationDate = DateTime.Now;

                // Create a new page
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                // Page margins
                double leftMargin = 50;
                double topMargin = 50;
                double rightMargin = page.Width - 50;
                double contentWidth = rightMargin - leftMargin;

                // Draw header
                DrawHeader(gfx, leftMargin, ref topMargin, contentWidth);

                // Draw order info
                DrawOrderInfo(gfx, leftMargin, ref topMargin, contentWidth);

                // Draw products table
                DrawProductsTable(gfx, leftMargin, ref topMargin, contentWidth);

                // Draw footer
                DrawFooter(gfx, leftMargin, page.Height - 30, contentWidth);

                // Show save dialog
                var saveDialog = new SaveFileDialog
                {
                    Title = "Сохранить заказ как PDF",
                    FileName = $"Заказ_{_orderId}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".pdf",
                    Filter = "PDF документы (*.pdf)|*.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    document.Save(saveDialog.FileName);
                    MessageBox.Show($"Заказ успешно сохранен как PDF: {saveDialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Optional: Open the file after saving
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Draw the document header
        private void DrawHeader(XGraphics gfx, double x, ref double y, double width)
        {
            // Company logo or name
            string companyName = "ИНТЕРНЕТ-МАГАЗИН";
            var size = gfx.MeasureString(companyName, _titleFont);
            double xCenter = x + (width - size.Width) / 2; // Центрируем текст
            gfx.DrawString(companyName, _titleFont, XBrushes.DarkBlue, new XPoint(xCenter, y + size.Height));
            y += size.Height + 10;

            // Document title
            string title = $"ЗАКАЗ №{_orderId}";
            size = gfx.MeasureString(title, _titleFont);
            xCenter = x + (width - size.Width) / 2; // Центрируем текст
            gfx.DrawString(title, _titleFont, XBrushes.Black, new XPoint(xCenter, y + size.Height));
            y += size.Height + 20;

            // Horizontal line
            gfx.DrawLine(new XPen(XColors.DarkGray, 1), x, y, x + width, y);
            y += 15;
        }

        // Draw order information
        private void DrawOrderInfo(XGraphics gfx, double x, ref double y, double width)
        {
            // Customer info
            gfx.DrawString("Информация о заказе:", _subtitleFont, XBrushes.Black, new XPoint(x, y + _subtitleFont.Height));
            y += 25;

            // Customer name
            gfx.DrawString($"Клиент: {_userName}", _normalFont, XBrushes.Black, new XPoint(x, y + _normalFont.Height));
            y += 20;

            // Order date
            gfx.DrawString($"Дата заказа: {_orderDate.ToString("dd.MM.yyyy HH:mm")}", _normalFont, XBrushes.Black, new XPoint(x, y + _normalFont.Height));
            y += 20;

            // Delivery address
            gfx.DrawString($"Адрес доставки: {_deliveryAddress}", _normalFont, XBrushes.Black, new XPoint(x, y + _normalFont.Height));
            y += 20;

            // Delivery method
            gfx.DrawString($"Способ доставки: {_deliveryMethod}", _normalFont, XBrushes.Black, new XPoint(x, y + _normalFont.Height));
            y += 20;

            // Order status
            gfx.DrawString($"Статус заказа: {_status}", _normalFont, XBrushes.Black, new XPoint(x, y + _normalFont.Height));
            y += 30;

            // Horizontal line
            gfx.DrawLine(new XPen(XColors.DarkGray, 1), x, y, x + width, y);
            y += 15;
        }

        // Draw products table - FIXED VERSION
        private void DrawProductsTable(XGraphics gfx, double x, ref double y, double width)
        {
            // Table title
            gfx.DrawString("Товары в заказе:", _subtitleFont, XBrushes.Black, new XPoint(x, y + _subtitleFont.Height));
            y += 25;

            // Define column widths (percentages of total width)
            double[] columnWidthPercentages = { 0.05, 0.45, 0.15, 0.15, 0.20 };
            double[] columnWidths = new double[columnWidthPercentages.Length];
            for (int i = 0; i < columnWidthPercentages.Length; i++)
            {
                columnWidths[i] = width * columnWidthPercentages[i];
            }

            // Table header
            double currentX = x;
            string[] headers = { "№", "Наименование товара", "Цена", "Кол-во", "Итого" };
            double headerHeight = 20;

            // Draw header background
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(220, 220, 220)), x, y, width, headerHeight);

            // Draw header text
            for (int i = 0; i < headers.Length; i++)
            {
                var headerRect = new XRect(currentX, y, columnWidths[i], headerHeight);
                gfx.DrawRectangle(new XPen(XColors.Black, 0.5), headerRect);

                // Use proper format for header text alignment
                XStringFormat format = new XStringFormat();

                // Center the index and quantity column
                if (i == 0 || i == 3)
                    format.Alignment = XStringAlignment.Center;
                // Left align the product name
                else if (i == 1)
                    format.Alignment = XStringAlignment.Near;
                // Right align price columns
                else
                    format.Alignment = XStringAlignment.Far;

                format.LineAlignment = XLineAlignment.Center;

                // Apply proper text padding based on alignment
                XRect textRect;
                if (i == 1) // Product name column
                    textRect = new XRect(currentX + 5, y, columnWidths[i] - 10, headerHeight);
                else if (i == 0 || i == 3) // Index and quantity columns
                    textRect = new XRect(currentX, y, columnWidths[i], headerHeight);
                else // Price columns
                    textRect = new XRect(currentX, y, columnWidths[i] - 5, headerHeight);

                gfx.DrawString(headers[i], _normalBoldFont, XBrushes.Black, textRect, format);
                currentX += columnWidths[i];
            }
            y += headerHeight;

            // Table rows
            double rowHeight = 20;
            decimal totalSum = 0;

            for (int i = 0; i < _orderProducts.Count; i++)
            {
                var product = _orderProducts[i];
                currentX = x;

                // Row background (alternate)
                if (i % 2 == 1)
                {
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(245, 245, 245)), x, y, width, rowHeight);
                }

                // Column 1: Index
                var cell = new XRect(currentX, y, columnWidths[0], rowHeight);
                gfx.DrawRectangle(new XPen(XColors.Black, 0.5), cell);
                XStringFormat centerFormat = new XStringFormat
                {
                    Alignment = XStringAlignment.Center,
                    LineAlignment = XLineAlignment.Center
                };
                gfx.DrawString((i + 1).ToString(), _normalFont, XBrushes.Black, cell, centerFormat);
                currentX += columnWidths[0];

                // Column 2: Product name
                cell = new XRect(currentX, y, columnWidths[1], rowHeight);
                gfx.DrawRectangle(new XPen(XColors.Black, 0.5), cell);
                XStringFormat leftFormat = new XStringFormat
                {
                    Alignment = XStringAlignment.Near,
                    LineAlignment = XLineAlignment.Center
                };
                gfx.DrawString(product.ProductName, _normalFont, XBrushes.Black,
                    new XRect(currentX + 5, y, columnWidths[1] - 10, rowHeight), leftFormat);
                currentX += columnWidths[1];

                // Column 3: Price
                cell = new XRect(currentX, y, columnWidths[2], rowHeight);
                gfx.DrawRectangle(new XPen(XColors.Black, 0.5), cell);
                XStringFormat rightFormat = new XStringFormat
                {
                    Alignment = XStringAlignment.Far,
                    LineAlignment = XLineAlignment.Center
                };
                gfx.DrawString(product.Price.ToString("C", CultureInfo.CurrentCulture), _normalFont, XBrushes.Black,
                    new XRect(currentX, y, columnWidths[2] - 5, rowHeight), rightFormat);
                currentX += columnWidths[2];

                // Column 4: Quantity
                cell = new XRect(currentX, y, columnWidths[3], rowHeight);
                gfx.DrawRectangle(new XPen(XColors.Black, 0.5), cell);
                gfx.DrawString(product.Quantity.ToString(), _normalFont, XBrushes.Black, cell, centerFormat);
                currentX += columnWidths[3];

                // Column 5: Total price
                decimal totalPrice = product.Price * product.Quantity;
                totalSum += totalPrice;
                cell = new XRect(currentX, y, columnWidths[4], rowHeight);
                gfx.DrawRectangle(new XPen(XColors.Black, 0.5), cell);
                gfx.DrawString(totalPrice.ToString("C", CultureInfo.CurrentCulture), _normalFont, XBrushes.Black,
                    new XRect(currentX, y, columnWidths[4] - 5, rowHeight), rightFormat);

                y += rowHeight;
            }

            // Total row
            currentX = x;
            double totalRowHeight = 25;

            // Skip first three columns for total
            double totalLabelWidth = columnWidths[0] + columnWidths[1] + columnWidths[2] + columnWidths[3];
            var totalLabelRect = new XRect(currentX, y, totalLabelWidth, totalRowHeight);
            gfx.DrawRectangle(new XPen(XColors.Black, 0.5), totalLabelRect);

            XStringFormat totalLabelFormat = new XStringFormat
            {
                Alignment = XStringAlignment.Far,
                LineAlignment = XLineAlignment.Center
            };
            gfx.DrawString("ИТОГО:", _normalBoldFont, XBrushes.Black,
                new XRect(currentX, y, totalLabelWidth - 10, totalRowHeight), totalLabelFormat);
            currentX += totalLabelWidth;

            // Total amount
            var totalCell = new XRect(currentX, y, columnWidths[4], totalRowHeight);
            gfx.DrawRectangle(new XPen(XColors.Black, 0.5), totalCell);
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(230, 230, 230)), totalCell);

            XStringFormat totalAmountFormat = new XStringFormat
            {
                Alignment = XStringAlignment.Far,
                LineAlignment = XLineAlignment.Center
            };
            gfx.DrawString(totalSum.ToString("C", CultureInfo.CurrentCulture), _normalBoldFont, XBrushes.Black,
                new XRect(currentX, y, columnWidths[4] - 5, totalRowHeight), totalAmountFormat);

            y += totalRowHeight + 20;
        }

        // Draw footer
        private void DrawFooter(XGraphics gfx, double x, double y, double width)
        {
            // Horizontal line
            gfx.DrawLine(new XPen(XColors.DarkGray, 1), x, y - 15, x + width, y - 15);

            // Footer text
            string footerText = $"Заказ №{_orderId} | Дата печати: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")} | Стр. 1";
            gfx.DrawString(footerText, _footerFont, XBrushes.Gray, new XPoint(x, y + _footerFont.Height));
        }
    }
}