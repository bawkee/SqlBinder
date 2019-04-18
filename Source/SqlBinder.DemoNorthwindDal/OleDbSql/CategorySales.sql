SELECT
	Categories.CategoryID, 
	Categories.CategoryName, 
	SUM(CCUR(OrderDetails.UnitPrice * OrderDetails.Quantity * (1 - OrderDetails.Discount) / 100) * 100) AS TotalSales
FROM ((Categories		
	INNER JOIN Products ON Products.CategoryID = Categories.CategoryID)
	INNER JOIN OrderDetails ON OrderDetails.ProductID = Products.ProductID)
{WHERE 
	{OrderDetails.OrderID IN (SELECT OrderID FROM Orders WHERE 
			{Orders.ShippedDate :shippingDates}
			{Orders.OrderDate :orderDates}
			{Orders.ShipCountry :shipCountry})}
	{Categories.CategoryID :categoryIds}}
GROUP BY 
	Categories.CategoryID, Categories.CategoryName