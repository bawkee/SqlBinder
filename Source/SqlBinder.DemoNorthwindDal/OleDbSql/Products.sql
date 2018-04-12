SELECT P.*,
	(SELECT CategoryName FROM Categories WHERE CategoryID = P.CategoryID) AS CategoryName,
	(SELECT CompanyName FROM Suppliers WHERE SupplierID = P.SupplierID) AS SupplierCompany
FROM Products P 
{WHERE 
{ProductID [productId]} 
{ProductName [productName]} 
{SupplierID [supplierIds]}
{CategoryID [categoryIds]} 
{UnitPrice [unitPrice]} 
{UnitPrice [priceGreaterThanAvg]} 
{Discontinued [isDiscontinued]}}