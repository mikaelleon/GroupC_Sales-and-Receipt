''' <summary>
''' Represents one cart row with numeric values for calculations.
''' </summary>
Public Class CartLineItem

    ''' <summary>
    ''' Initializes a new instance of the <see cref="CartLineItem"/> class.
    ''' </summary>
    ''' <param name="productName">Product display name.</param>
    ''' <param name="unitPrice">Unit price.</param>
    ''' <param name="quantity">Line quantity.</param>
    Public Sub New(productName As String, unitPrice As Decimal, quantity As Integer)
        Me.ProductName = productName
        Me.UnitPrice = unitPrice
        Me.Quantity = quantity
    End Sub

    ''' <summary>
    ''' Gets or sets the product name.
    ''' </summary>
    Public Property ProductName As String

    ''' <summary>
    ''' Gets or sets the unit price.
    ''' </summary>
    Public Property UnitPrice As Decimal

    ''' <summary>
    ''' Gets or sets the quantity.
    ''' </summary>
    Public Property Quantity As Integer

    ''' <summary>
    ''' Gets the line subtotal (unit price times quantity).
    ''' </summary>
    Public ReadOnly Property LineSubtotal As Decimal
        Get
            Return Me.UnitPrice * Me.Quantity
        End Get
    End Property

End Class
