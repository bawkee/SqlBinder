SELECT 
	Categories.CategoryID, 
	Categories.CategoryName, 
	SUM(CCUR(OD.UnitPrice * OD.Quantity * (1 - OD.Discount) / 100) * 100) AS TotalSales, 
	COUNT(*) AS Cnt
FROM 
	Categories, 
	Orders, 
	$[Order Details]$ OD, 
	Products
WHERE
{{Orders.ShippedDate [shippingDates]}
{Categories.CategoryID [categoryIds]} AND}
Products.ProductID = OD.ProductID AND
OD.OrderID = Orders.OrderID AND
Categories.CategoryID = Products.CategoryID
GROUP BY Categories.CategoryID, Categories.CategoryName