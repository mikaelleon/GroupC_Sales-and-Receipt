Imports System.Collections.Generic

''' <summary>
''' Structured Logo data for grid display and persistence hints.
''' </summary>
Public Class LogoSnapshot

    ''' <summary>
    ''' Gets or sets the store display name.
    ''' </summary>
    Public Property StoreName As String

    ''' <summary>
    ''' Gets or sets the footer text shown on the Logo.
    ''' </summary>
    Public Property FooterText As String

    ''' <summary>
    ''' Gets or sets the currency symbol or code for labels.
    ''' </summary>
    Public Property CurrencySymbol As String

    ''' <summary>
    ''' Gets or sets the cashier or operator name printed on the Logo.
    ''' </summary>
    Public Property CashierName As String

    ''' <summary>
    ''' Gets or sets the database sale id (set after save).
    ''' </summary>
    Public Property SaleId As Integer

    ''' <summary>
    ''' Gets or sets the transaction timestamp printed on the Logo.
    ''' </summary>
    Public Property SaleDateTime As DateTime

    ''' <summary>
    ''' Gets or sets the printed Logo number (for example RCP-000042).
    ''' </summary>
    Public Property LogoNumber As String

    ''' <summary>
    ''' Gets or sets the transaction reference printed on the Logo.
    ''' </summary>
    Public Property TransactionReference As String

    ''' <summary>
    ''' Gets or sets the payment method label (for example Cash).
    ''' </summary>
    Public Property PaymentMethod As String

    ''' <summary>
    ''' Gets or sets the line items.
    ''' </summary>
    Public Property Lines As List(Of LogoLineRow)

    ''' <summary>
    ''' Gets or sets the sum of line subtotals before discount.
    ''' </summary>
    Public Property SubtotalBeforeDiscount As Decimal

    ''' <summary>
    ''' Gets or sets the discount percent applied to the subtotal.
    ''' </summary>
    Public Property DiscountPercent As Decimal

    ''' <summary>
    ''' Gets or sets whether <see cref="DiscountPercent"/> is a rate (true) or ignored for fixed discount (false).
    ''' </summary>
    Public Property DiscountIsPercent As Boolean

    ''' <summary>
    ''' Gets or sets a Logo label for the applied discount (for example PWD 20%).
    ''' </summary>
    Public Property DiscountLabel As String

    ''' <summary>
    ''' Gets or sets the discount amount.
    ''' </summary>
    Public Property DiscountAmount As Decimal

    ''' <summary>
    ''' Gets or sets the net amount after discount and before tax.
    ''' </summary>
    Public Property AmountBeforeTax As Decimal

    ''' <summary>
    ''' Gets or sets whether tax was applied.
    ''' </summary>
    Public Property TaxApplied As Boolean

    ''' <summary>
    ''' Gets or sets the tax percent.
    ''' </summary>
    Public Property TaxPercent As Decimal

    ''' <summary>
    ''' Gets or sets the tax amount.
    ''' </summary>
    Public Property TaxAmount As Decimal

    ''' <summary>
    ''' Gets or sets the grand total (amount due).
    ''' </summary>
    Public Property GrandTotal As Decimal

    ''' <summary>
    ''' Gets or sets the cash tendered.
    ''' </summary>
    Public Property AmountTendered As Decimal

    ''' <summary>
    ''' Gets or sets the change returned.
    ''' </summary>
    Public Property ChangeGiven As Decimal

    ''' <summary>
    ''' Gets or sets the generated monospace Logo text.
    ''' </summary>
    Public Property LogoText As String

End Class

''' <summary>
''' One row for Logo structured grid.
''' </summary>
Public Class LogoLineRow

    ''' <summary>
    ''' Gets or sets the product name.
    ''' </summary>
    Public Property ProductName As String

    ''' <summary>
    ''' Gets or sets the quantity.
    ''' </summary>
    Public Property Quantity As Integer

    ''' <summary>
    ''' Gets or sets the unit price.
    ''' </summary>
    Public Property UnitPrice As Decimal

    ''' <summary>
    ''' Gets or sets the line subtotal.
    ''' </summary>
    Public Property LineTotal As Decimal

End Class
