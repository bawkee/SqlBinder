# SqlBinder :paperclip:

![License](https://img.shields.io/github/license/bawkee/SqlBinder) ![Stars](https://img.shields.io/github/stars/bawkee/SqlBinder?style=social) [![NuGet](https://img.shields.io/nuget/v/SqlBinder.svg)](https://www.nuget.org/packages/SqlBinder/)

SqlBinder is a free, open-source library designed to effortlessly generate valid SQL statements by transforming SQL templates based on a set of conditions.

- **Not an ORM**: SqlBinder isn't an Object-Relational Mapping tool. It's a DBMS-independent, SQL-centric templating engine that removes the overhead of SQL generation and bind variables.
  
- **Enhanced Composability**: Unlike typical SQL builders, SqlBinder is geared towards crafting complex queries, going beyond simple placeholder replacements.
  
- **Plays Well with Others**: Compatible with other ORM tools like Dapper, PetaPoco, and EntityFramework.
  
- **Security First**: Automatically handles bind variables, mitigating SQL injection risks.

## Key Features

- 🚀 **Scalable**: Tackles both basic and complex SQL generation needs. No performance penalties, no overhead.
- 📚 **Examples**: Covering everything from beginner to expert use cases.
- ⚡ **Performance**: Outstanding speed metrics, even when used alongside tools like Dapper, PetaPoco, and EntityFramework.
- 🧪 **Well Tested**: A wide range of tests and great coverage makes it super stable.
- 🏆 **Award-Winning Article**: Check out our [CodeProject article](#link-to-article) for an in-depth walkthrough.
- 🌐 **Real-world Demo**: A WPF-based application demonstrating SqlBinder's capabilities with the Northwind database.

## A Quick Demonstration

Consider the following method signature:

```C#
IEnumerable<CategorySale> GetCategorySales(
	IDbConnection connection,
	IEnumerable<int> categoryIds = null,
	DateTime? fromShippingDate = null, DateTime? toShippingDate = null,
	DateTime? fromOrderDate = null, DateTime? toOrderDate = null,
	IEnumerable<string> shippingCountries = null);
```

Implementation of this method should return a summary of sales grouped by categories and filtered by any combination of the following criteria: categories, shipping dates, order dates and shipping countries. 

Instead of manually building SQL using Fluent APIs or string concatenation, see how SqlBinder, combined with Dapper, simplifies the process:

```C#
IEnumerable<CategorySale> GetCategorySales(
	IDbConnection connection,
	IEnumerable<int> categoryIds = null,
	DateTime? fromShippingDate = null, DateTime? toShippingDate = null,
	DateTime? fromOrderDate = null, DateTime? toOrderDate = null,
	IEnumerable<string> shippingCountries = null)
{
	var query = new Query(GetEmbeddedResource("CategorySales.sql")); // SqlBinder!

	query.SetCondition("categoryIds", categoryIds);
	query.SetConditionRange("shippingDates", fromShippingDate, toShippingDate);
	query.SetConditionRange("orderDates", fromOrderDate, toOrderDate);
	query.SetCondition("shippingCountries", shippingCountries);

	return connection.Query<CategorySale>(query.GetSql(), query.SqlParameters);
}
```

The SQL templates, like this one, offer native support for multiple `WHERE` clauses, varied `ORDER BY` statements, and numerous sub-queries.

Another script with shortcut aliases and an optional sub-query:

```SQL
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
```

What's this *optional* sub-query? Well, since our `OD.OrderID IN` condition is enclosed within `{ }` braces it means that it won't be used if it's *not needed* - in other words, if it's not needed then output SQL won't contain it along with its sub-query `SELECT OrderID FROM Orders ...`. Again, the whole part enclosed in `{ }` would be removed if its conditions aren't used, specifically if none of the `:shippingDates`, `:orderDates` or `:shippingCountries` are used. The `:categoryIds` condition is separate from this and belongs to the parent query, SqlBinder will connect it with the above condition automatically (*if* it's used) with an `AND` operand.

This way, SqlBinder ensures SQL scripts are decoupled from your code, improving maintainability.

The next template script uses different aliases and would work just the same:

```SQL
SELECT
	Categories.CategoryID, 
	Categories.CategoryName, 
	SUM(CCUR(OrderDetails.UnitPrice * OrderDetails.Quantity * 
		(1 - OrderDetails.Discount) / 100) * 100) AS TotalSales
FROM ((Categories		
	INNER JOIN Products ON Products.CategoryID = Categories.CategoryID)
	INNER JOIN OrderDetails ON OrderDetails.ProductID = Products.ProductID)
{WHERE 	
	{OrderDetails.OrderID IN (SELECT OrderID FROM Orders WHERE 
			{Orders.ShippedDate :shippingDates} 
			{Orders.OrderDate :orderDates}
			{Orders.ShipCountry :shippingCountries})} 
	{Categories.CategoryID :categoryIds}}
GROUP BY 
	Categories.CategoryID, Categories.CategoryName
```

You don't need to modify your `GetCategorySales` method for this template to work, it'll work as long as the parameter names are the same.

Next template uses a completely different join and has no sub-queries:

```SQL
SELECT
	Categories.CategoryID, 
	Categories.CategoryName, 
	SUM(CCUR(OrderDetails.UnitPrice * OrderDetails.Quantity * 
		(1 - OrderDetails.Discount) / 100) * 100) AS TotalSales
FROM (((Categories		
	INNER JOIN Products ON Products.CategoryID = Categories.CategoryID)
	INNER JOIN OrderDetails ON OrderDetails.ProductID = Products.ProductID)
	INNER JOIN Orders ON Orders.OrderID = OrderDetails.OrderID)
{WHERE
	{Orders.ShippedDate :shippingDates} 
	{Orders.OrderDate :orderDates}
	{Orders.ShipCountry :shippingCountries} 
	{Categories.CategoryID :categoryIds}}
GROUP BY 
	Categories.CategoryID, Categories.CategoryName
```

Here's another template which has two `WHERE` clauses, is using a different syntax to join and has no `GROUP BY`. This works out of the box and would produce the same data:

```SQL
SELECT 
	Categories.CategoryID, 
	Categories.CategoryName, 
	(SELECT SUM(CCUR(UnitPrice * Quantity * (1 - Discount) / 100) * 100) 
	FROM OrderDetails WHERE ProductID IN 
		(SELECT ProductID FROM Products WHERE Products.CategoryID = Categories.CategoryID)
		{AND OrderID IN (SELECT OrderID FROM Orders WHERE 
			{Orders.ShippedDate :shippingDates} 
			{Orders.OrderDate :orderDates}
			{Orders.ShipCountry :shippingCountries})}) AS TotalSales
FROM Categories {WHERE {Categories.CategoryID :categoryIds}}
```

What SqlBinder does is it binds `SqlBinder.Condition` objects to its template scripts returning a valid SQL which you can then pass to your ORM.

## Tutorials, Examples and Demo App

- **In-depth Guide**: Discover more examples and tutorials in the [SqlBinder's Code Project article](https://www.codeproject.com/Articles/1246990/SqlBinder-Library).

- **Demo App**: The SqlBinder source code includes a hands-on demo application, offering a deeper dive into its capabilities. More details can be found in the [Code Project article](https://www.codeproject.com/Articles/1246990/SqlBinder-Library).

⭐ Don't forget to rate the article if it helped!

## Performance Metrics

SqlBinder's performance, when combined with ORMs like Dapper, is exceptional:

**LocalDB (Sql Sever Express):**
```
    Dapper +SqlBinder
---------------------
     52.88      53.46
     57.31      59.55
     56.22      68.07
     55.97      56.16
     66.52      55.59
     54.82      52.96
     50.98      61.97
     59.06      57.53
     50.38      53.97
    AVG 56     AVG 58

 ^ Dapper = Just Dapper.
 ^ +SqlBinder = Dapper with SqlBinder.
```

**OleDb (Access):**
```
    Dapper +SqlBinder
---------------------
    335.42     336.38
    317.99     318.89
    342.56     324.85
    317.20     320.84
    327.91     324.56
    320.29     326.86
    334.42     338.73
    344.43     326.33
    315.32     322.48
   AVG 328    AVG 327

 ^ Dapper = Just Dapper.
 ^ +SqlBinder = Dapper with SqlBinder.
```

As you can observe, on SqlServer we've had an additional overhead of 2ms which is the time it took SqlBinder to formulate a query based on different criteria. On the OleDb Access test this difference was so insignificant it was lost entirely in deviations (most likely in interaction with the DB).

Each row in the test results was a result of 500 executions of the following queries:

```SQL
SELECT * FROM POSTS WHERE ID IN @id
```
And
```SQL
SELECT * FROM POSTS {WHERE {ID @id}}
```
Where the latter was used in Dapper + SqlBinder combination. 

It is important to note that SqlBinder has the ability to re-use compiled templates as it completely separates the parsing and templating concerns. You may create a SqlBinder query template once and then build all the subsequent SQL queries from the same pre-parsed template. One of the key functionalities of SqlBinder is that it doesn't parse or generate the whole SQL *every time*.

Performance tests are available in the source folder. Benchmark SqlBinder on your own!

## The Syntax

Consists of two basic types of elements: scopes and parameter placeholders. Scopes are defined by curly braces `{ ... }` and parameter placeholders can be defined by the typical SQL syntax (i.e. `:parameter` or `@parameter`) or by custom SqlBinder syntax (if configured so, i.e. `[parameter]`). 

Explained with regex:
```SQL
... [@\+]{ ... [:?@]paramPlaceholder  ... } ...
```

Or, consider the following set of valid examples where `...` can be any SQL:
```SQL
... { ... :paramPlaceholder  ... } ...

... { ... @paramPlaceholder  ... } ...

... { ... { ... :paramPlaceholder  ... } ... } ...

... @{ ... { ... :paramPlaceholder1  ... } ... { ... :paramPlaceholder2 ... } ... } ...

... +{ ... { ... :paramPlaceholder1  ... } ... { ... :paramPlaceholder2 ... } ... } ...

... { ... { ... [place holder 1]  ... } ... { ... [place holder 2] ... } ... } ...
```

Further explanation of above examples:
* Curly braces `{ ... }` define a scope. Scope can either contain child scopes or a single parameter placeholder. Scope that does not contain either of those will always be removed as that's considered pointless. Otherwise, the scope is removed only if all its child scopes are removed or its parameter placeholder is removed, *which* in turn is removed if no matching *condition* was found for it (conditions are explained further on). 
* `:paramPlaceholder` can be any alphanumeric name that will be matched against `Query.Conditions` collection. This is referred to as *parameter* in the SqlBinder objects. If a parameter doesn't match any condition it will be removed along with its entire parent scope. The output SQL bind variable will be formatted with the same prefix as the parameter (acceptable prefixes are `:` or `@` or `?`). These parameters are not bind-variables and you must respect the aforementioned syntax, i.e. the Oracle variable `:"MyVariable"` won't be recognized as a parameter - if you need custom formatting in your output variables which you can't accomplish with the SqlBinder syntax names there ways to do so via events and delegates (see the Query class). Note that there can only be one placeholder in a given scope. When you need multiple placeholders put each one in its own separate scope.
* `[place holder xy]` works the same way as above except any character is allowed and you must provide the parameter prefix manually (in C#) by overriding the `Query` class, `DbQuery` class or setting the appropriate property. Also, this syntax doesn't work by default, you have to enable a special hint via `Query.ParserHints` property since `[]` characters are used by some SQL flavors. With that said, you can still escape these tags into the output SQL `[[like this]]`.
* The `@` character before the scope (i.e. `@{`) tells the SqlBinder to connect scopes with an `OR` rather than default `AND` operator. 
* The `+` character before the scope (i.e. `+{`) instructs the SqlBinder to not automatically connect this specific scope with its previous sibling by an `AND` (or any  operator), to instead just leave the white space as it already is. 
* The `...` can be any SQL from any DBMS or just about any text. The string literals won't be processed by the SqlBinder which means they can safely contain SqlBinder syntax. The special flavors of literals such as PostgreSQL dollar literals or Oracle AQM literals are recognized as well and can safely contain any special character used by the SqlBinder. The same goes for SQL comments.

**The comment syntax** looks like this:
```SQL
/*{ ...sql binder comment... }*/

/*{
	...sql binder comment...
}*/
```
SqlBinder comments will be removed entirely from the output SQL while the SQL comments will remain intact.

**The SQL literals** are respected and may contain SqlBinder syntax which won't be processed:
```SQL
'Anything can be here: [] {} ...' 

"Or here {} [] ..."

'Or in ''escaped literal {} [] ...'''

q'{Or in this Oracle literal {} [] which can get very creative ...}'

$myTag$Or in this PostgreSQL literal {} [] ...$myTag$
```
None of these are processed against SqlBinder syntax. You may safely put parameter placeholder or scope syntax in here and it won't be altered in any way. SqlBinder does not do simple find-replace, it parses the script and re-builds the SQL based on it.

## Origin Story

SqlBinder was born in 2009 out of a necessity to simplify front-end development against complex Oracle databases. With performance and bandwidth being critical, we needed a solution that could leverage powerful SQLs without compromising code maintainability. SqlBinder grew from a prototype to the robust tool it is today, often used alongside Dapper for optimal results.
