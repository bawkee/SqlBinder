SELECT
	CAT.CategoryID, 
	CAT.CategoryName, 
	SUM(CCUR(OD.UnitPrice * OD.Quantity * (1 - OD.Discount) / 100) * 100) AS TotalSales
FROM ((Categories AS CAT		
	INNER JOIN Products AS PRD ON PRD.CategoryID = CAT.CategoryID)
	INNER JOIN OrderDetails AS OD ON OD.ProductID = PRD.ProductID)
{WHERE 	
	{OD.OrderID IN (SELECT OrderID FROM Orders AS ORD WHERE 
			{ORD.ShippedDate :shippingDates} 
			{ORD.OrderDate :orderDates}
			{ORD.ShipCountry :shippingCountries})} 
	{CAT.CategoryID :categoryIds}}
GROUP BY 
	CAT.CategoryID, CAT.CategoryName