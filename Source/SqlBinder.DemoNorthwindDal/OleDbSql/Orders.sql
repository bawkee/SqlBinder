SELECT O.*,
	(SELECT ContactName FROM Customers WHERE CustomerID = O.CustomerID) AS CustomerName,
	(SELECT FirstName + ' ' + LastName FROM Employees WHERE EmployeeID = O.EmployeeID) AS EmployeeName,
	(SELECT CompanyName FROM Shippers WHERE ShipperID = O.ShipVia) AS ShippedVia
FROM Orders O 
{WHERE 
{OrderID [orderId]} 
{CustomerID [customerIds]} 
{EmployeeID [employeeIds]} 
{ShipVia [shipperIds]} 
{OrderDate [orderDate]} 
{RequiredDate [reqDate]} 
{ShippedDate [shipDate]} 
{Freight [freight]} 
{ShipCity [shipCity]} 
{ShipCountry [shipCountry]}
{OrderID IN (SELECT OrderID FROM $[Order Details]$ WHERE {ProductID [productIds]})}}