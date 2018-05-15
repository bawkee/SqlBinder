SELECT 
	Categories.CategoryID, 
	Categories.CategoryName, 
	(SELECT SUM(CCUR(UnitPrice * Quantity * (1 - Discount) / 100) * 100) FROM OrderDetails 
		WHERE ProductID IN (SELECT ProductID FROM Products WHERE Products.CategoryID = Categories.CategoryID)
		  {AND OrderID IN (SELECT OrderID FROM Orders WHERE {Orders.ShippedDate :shippingDates})}) AS TotalSales
FROM Categories
{WHERE {Categories.CategoryID :categoryIds}}