using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using CC.Domain.Entities;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace CC.Services
{
    public class PdfReportOptions
    {
        public string CompanyName { get; set; } = "Custom Cake CRMS";
        public string BranchName { get; set; } = "All Branches";
        public string ReportTitle { get; set; } = "OVERALL TRANSACTIONS & REVENUE REPORT";
        public string ReportType { get; set; } = "All Transactions";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ActiveSearchQuery { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = "Administrator";
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public List<TransactionRecord> Records { get; set; } = new();
        public PageOrientation Orientation { get; set; } = PageOrientation.Landscape;
    }

    /// <summary>
    /// Professional PDF Report Generator using PDFsharp-GDI.
    /// Supports multi-page tables, header metadata, branch-aware badges, page numbering (Page X of Y),
    /// and clean financial alignment without cutoffs or overlapping.
    /// </summary>
    public static class PdfReportService
    {
        public static void GenerateTransactionReport(string filePath, PdfReportOptions options)
        {
            var document = new PdfDocument();
            document.Info.Title = $"{options.CompanyName} - {options.ReportTitle}";
            document.Info.Author = options.GeneratedBy;
            document.Info.Subject = $"{options.ReportType} ({options.BranchName})";

            // Colors
            var colPrimary = XColor.FromArgb(140, 70, 90);      // Maroon #8C465A
            var colDark = XColor.FromArgb(35, 30, 30);          // Dark text #231E1E
            var colMuted = XColor.FromArgb(115, 105, 100);      // Muted text
            var colBorder = XColor.FromArgb(220, 212, 205);     // Subtle border
            var colHeaderBg = XColor.FromArgb(245, 240, 238);   // Header background
            var colAltRow = XColor.FromArgb(252, 250, 248);     // Alternating row
            var colWhite = XColor.FromArgb(255, 255, 255);

            var penBorder = new XPen(colBorder, 0.8);
            var penDivider = new XPen(colPrimary, 1.5);
            var penHeaderDivider = new XPen(colBorder, 1.0);

            // Fonts
            var fontCompany = new XFont("Segoe UI", 15, XFontStyleEx.Bold);
            var fontReportTitle = new XFont("Segoe UI", 12, XFontStyleEx.Bold);
            var fontMeta = new XFont("Segoe UI", 8.5, XFontStyleEx.Regular);
            var fontMetaBold = new XFont("Segoe UI", 8.5, XFontStyleEx.Bold);
            var fontColHeader = new XFont("Segoe UI", 8.5, XFontStyleEx.Bold);
            var fontCell = new XFont("Segoe UI", 8.5, XFontStyleEx.Regular);
            var fontCellBold = new XFont("Segoe UI", 8.5, XFontStyleEx.Bold);
            var fontFooter = new XFont("Segoe UI", 8, XFontStyleEx.Regular);

            // Margins and printable areas (A4 Landscape: 842 x 595 pt)
            double leftMargin = 36;
            double topMargin = 36;
            double bottomMargin = 40;

            // Column widths (Total width: 770 pt)
            double[] colWidths = new double[]
            {
                105, // 0: Date & Time
                95,  // 1: Ref #
                75,  // 2: Type
                145, // 3: Customer
                175, // 4: Details
                95,  // 5: Amount (PHP)
                80   // 6: Status
            };
            double tableWidth = colWidths.Sum(); // 770 pt

            string[] colHeaders = new string[]
            {
                "DATE & TIME",
                "REFERENCE #",
                "TYPE",
                "CUSTOMER",
                "DETAILS / METHOD",
                "AMOUNT (PHP)",
                "STATUS"
            };

            int currentPageIndex = 0;
            PdfPage page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = options.Orientation;
            XGraphics gfx = XGraphics.FromPdfPage(page);

            double pageWidth = page.Width.Point;
            double pageHeight = page.Height.Point;
            double currentY = topMargin;

            // ----------------------------------------------------
            // Helper to draw Page 1 Header
            // ----------------------------------------------------
            void DrawPageHeader(bool isFirstPage)
            {
                if (isFirstPage)
                {
                    // Brand / Company Title
                    gfx.DrawString(options.CompanyName.ToUpperInvariant(), fontCompany, new XSolidBrush(colPrimary), new XPoint(leftMargin, currentY + 14));
                    currentY += 20;

                    // Report Title
                    gfx.DrawString(options.ReportTitle, fontReportTitle, new XSolidBrush(colDark), new XPoint(leftMargin, currentY + 12));

                    // Metadata Box on Right Side
                    double metaBoxWidth = 280;
                    double metaBoxX = leftMargin + tableWidth - metaBoxWidth;
                    double metaBoxY = topMargin;

                    string dateRangeStr = (options.FromDate.HasValue && options.ToDate.HasValue)
                        ? $"{options.FromDate.Value:MMM d, yyyy} – {options.ToDate.Value:MMM d, yyyy}"
                        : (options.FromDate.HasValue ? $"From {options.FromDate.Value:MMM d, yyyy}" : "All Available Records");

                    gfx.DrawString("Date Range:", fontMetaBold, new XSolidBrush(colDark), new XPoint(metaBoxX, metaBoxY + 12));
                    gfx.DrawString(dateRangeStr, fontMeta, new XSolidBrush(colDark), new XPoint(metaBoxX + 70, metaBoxY + 12));

                    gfx.DrawString("Branch Scope:", fontMetaBold, new XSolidBrush(colDark), new XPoint(metaBoxX, metaBoxY + 25));
                    gfx.DrawString(options.BranchName, fontMeta, new XSolidBrush(colPrimary), new XPoint(metaBoxX + 70, metaBoxY + 25));

                    gfx.DrawString("Report Filter:", fontMetaBold, new XSolidBrush(colDark), new XPoint(metaBoxX, metaBoxY + 38));
                    gfx.DrawString(options.ReportType, fontMeta, new XSolidBrush(colDark), new XPoint(metaBoxX + 70, metaBoxY + 38));

                    currentY += 24;
                    gfx.DrawLine(penDivider, leftMargin, currentY, leftMargin + tableWidth, currentY);
                    currentY += 12;
                }
                else
                {
                    // Continuation header on subsequent pages
                    gfx.DrawString($"{options.CompanyName} \u00B7 {options.ReportTitle} (Continued)", fontCellBold, new XSolidBrush(colMuted), new XPoint(leftMargin, currentY + 10));
                    gfx.DrawString($"Branch: {options.BranchName}", fontMeta, new XSolidBrush(colPrimary), new XPoint(leftMargin + tableWidth - 160, currentY + 10));
                    currentY += 16;
                    gfx.DrawLine(penHeaderDivider, leftMargin, currentY, leftMargin + tableWidth, currentY);
                    currentY += 10;
                }

                // Table Column Header Row
                double headerHeight = 22;
                var headerRect = new XRect(leftMargin, currentY, tableWidth, headerHeight);
                gfx.DrawRectangle(new XSolidBrush(colHeaderBg), headerRect);
                gfx.DrawRectangle(penBorder, headerRect);

                double curX = leftMargin;
                for (int c = 0; c < colHeaders.Length; c++)
                {
                    var colRect = new XRect(curX + 6, currentY + 4, colWidths[c] - 12, headerHeight - 6);
                    var format = new XStringFormat
                    {
                        Alignment = (c == 5) ? XStringAlignment.Far : ((c == 6) ? XStringAlignment.Center : XStringAlignment.Near),
                        LineAlignment = XLineAlignment.Center
                    };

                    gfx.DrawString(colHeaders[c], fontColHeader, new XSolidBrush(colDark), colRect, format);
                    curX += colWidths[c];
                }

                currentY += headerHeight;
            }

            // Draw initial header
            DrawPageHeader(true);

            // ----------------------------------------------------
            // Table Rows
            // ----------------------------------------------------
            double rowHeight = 20;

            if (options.Records == null || options.Records.Count == 0)
            {
                // Empty result notice
                currentY += 20;
                var emptyRect = new XRect(leftMargin, currentY, tableWidth, 60);
                gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(250, 248, 245)), emptyRect);
                gfx.DrawRectangle(penBorder, emptyRect);

                var emptyFormat = new XStringFormat
                {
                    Alignment = XStringAlignment.Center,
                    LineAlignment = XLineAlignment.Center
                };
                gfx.DrawString("No transaction records found matching the specified date range and filters.", fontCellBold, new XSolidBrush(colDark), emptyRect, emptyFormat);
                currentY += 70;
            }
            else
            {
                int rowIndex = 0;
                foreach (var rec in options.Records)
                {
                    // Check page break
                    if (currentY + rowHeight > pageHeight - bottomMargin - 30)
                    {
                        // Add new page
                        page = document.AddPage();
                        page.Size = PageSize.A4;
                        page.Orientation = options.Orientation;
                        gfx = XGraphics.FromPdfPage(page);
                        currentPageIndex++;
                        currentY = topMargin;

                        DrawPageHeader(false);
                    }

                    // Alternating background
                    var rowRect = new XRect(leftMargin, currentY, tableWidth, rowHeight);
                    if (rowIndex % 2 == 1)
                    {
                        gfx.DrawRectangle(new XSolidBrush(colAltRow), rowRect);
                    }
                    gfx.DrawRectangle(penBorder, rowRect);

                    double curX = leftMargin;

                    // 0: Date
                    string dateStr = rec.Date.ToString("yyyy-MM-dd HH:mm");
                    gfx.DrawString(dateStr, fontCell, new XSolidBrush(colDark), new XRect(curX + 6, currentY + 3, colWidths[0] - 12, rowHeight - 6),
                        new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
                    curX += colWidths[0];

                    // 1: Ref No
                    gfx.DrawString(rec.ReferenceNo, fontCellBold, new XSolidBrush(colPrimary), new XRect(curX + 6, currentY + 3, colWidths[1] - 12, rowHeight - 6),
                        new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
                    curX += colWidths[1];

                    // 2: Type
                    gfx.DrawString(rec.Type, fontCell, new XSolidBrush(colDark), new XRect(curX + 6, currentY + 3, colWidths[2] - 12, rowHeight - 6),
                        new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
                    curX += colWidths[2];

                    // 3: Customer (Truncate if needed)
                    string cust = rec.CustomerName;
                    if (cust.Length > 24) cust = cust.Substring(0, 22) + "...";
                    gfx.DrawString(cust, fontCellBold, new XSolidBrush(colDark), new XRect(curX + 6, currentY + 3, colWidths[3] - 12, rowHeight - 6),
                        new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
                    curX += colWidths[3];

                    // 4: Details
                    string details = rec.Details;
                    if (details.Length > 32) details = details.Substring(0, 30) + "...";
                    gfx.DrawString(details, fontCell, new XSolidBrush(colDark), new XRect(curX + 6, currentY + 3, colWidths[4] - 12, rowHeight - 6),
                        new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
                    curX += colWidths[4];

                    // 5: Amount (PHP)
                    string amtStr = $"PHP {rec.Amount:N2}";
                    gfx.DrawString(amtStr, fontCellBold, new XSolidBrush(colDark), new XRect(curX + 6, currentY + 3, colWidths[5] - 12, rowHeight - 6),
                        new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Center });
                    curX += colWidths[5];

                    // 6: Status
                    gfx.DrawString(rec.Status, fontCell, new XSolidBrush(colMuted), new XRect(curX + 6, currentY + 3, colWidths[6] - 12, rowHeight - 6),
                        new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });

                    currentY += rowHeight;
                    rowIndex++;
                }

                // ----------------------------------------------------
                // Totals Bar
                // ----------------------------------------------------
                if (currentY + 28 > pageHeight - bottomMargin)
                {
                    page = document.AddPage();
                    page.Size = PageSize.A4;
                    page.Orientation = options.Orientation;
                    gfx = XGraphics.FromPdfPage(page);
                    currentPageIndex++;
                    currentY = topMargin;
                    DrawPageHeader(false);
                }

                currentY += 4;
                double totalBarHeight = 24;
                var totalRect = new XRect(leftMargin, currentY, tableWidth, totalBarHeight);
                gfx.DrawRectangle(new XSolidBrush(colHeaderBg), totalRect);
                gfx.DrawRectangle(new XPen(colPrimary, 1.2), totalRect);

                decimal totalSum = options.Records.Sum(r => r.Amount);
                string totalLabel = $"TOTAL ({options.Records.Count:N0} records):";
                string totalAmtStr = $"PHP {totalSum:N2}";

                gfx.DrawString(totalLabel, fontColHeader, new XSolidBrush(colDark),
                    new XRect(leftMargin + 12, currentY + 4, 300, totalBarHeight - 8),
                    new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });

                gfx.DrawString(totalAmtStr, fontCompany, new XSolidBrush(colPrimary),
                    new XRect(leftMargin + tableWidth - 250, currentY + 2, 238, totalBarHeight - 4),
                    new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Center });

                currentY += totalBarHeight + 10;
            }

            // ----------------------------------------------------
            // Page Footers (Page X of Y on every page)
            // ----------------------------------------------------
            int totalPages = document.PageCount;
            for (int i = 0; i < totalPages; i++)
            {
                var p = document.Pages[i];
                using var pGfx = XGraphics.FromPdfPage(p);

                double footerY = pageHeight - bottomMargin + 12;

                // Subtle top line
                pGfx.DrawLine(penHeaderDivider, leftMargin, footerY - 8, leftMargin + tableWidth, footerY - 8);

                // Left: Company, Branch, Timestamp
                string footerLeft = $"Generated on {options.GeneratedDate:yyyy-MM-dd HH:mm} \u00B7 {options.CompanyName} \u00B7 {options.BranchName} \u00B7 By {options.GeneratedBy}";
                pGfx.DrawString(footerLeft, fontFooter, new XSolidBrush(colMuted), new XPoint(leftMargin, footerY + 2));

                // Right: Page X of Y
                string footerRight = $"Page {i + 1} of {totalPages}";
                pGfx.DrawString(footerRight, fontFooter, new XSolidBrush(colDark),
                    new XRect(leftMargin + tableWidth - 100, footerY - 8, 100, 16),
                    new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Center });
            }

            // Save PDF
            document.Save(filePath);
        }
    }
}
