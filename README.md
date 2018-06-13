# SqlBinder

Is a free, open-source library that helps you transform a given SQL template and a set of conditions into any number of valid SQL statements along with their associated parameters.

**It isn't an ORM solution** - instead, it is DBMS-independent, SQL-centric **templating engine**. All it does is it removes the hassle of writing code that generates SQLs and bind variables . It does *not* generate the entire SQL itself, it transforms an existing SQL template instead. 

**It isn't 'SQL builder'** due to its high degree of composability, it is aimed at writing more complex queries. It isn't a swiss army knife either, it can be elegantly used alongside other popular tools such as Dapper, Dapper.Contrib, PetaPoco and others.

[![NuGet](https://img.shields.io/nuget/v/SqlBinder.svg)](https://www.nuget.org/packages/SqlBinder/)

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

Usually, you'd implement this method by building an SQL via some Fluent API (e.g. PetaPoco's `Sql.Builder`), Dapper.Contrib's nice `SqlBuilder`  or just `StringBuilder`. Instead, I'm going to show you how you could implement this method via SqlBinder and regular Dapper. It would look like this:

```C#
IEnumerable<CategorySale> GetCategorySales(
	IDbConnection connection,
	IEnumerable<int> categoryIds = null,
	DateTime? fromShippingDate = null, DateTime? toShippingDate = null,
	DateTime? fromOrderDate = null, DateTime? toOrderDate = null,
	IEnumerable<string> shippingCountries = null)
{
	var query = new Query(GetEmbeddedResource("CategorySales.sql"));

	query.SetCondition("categoryIds", categoryIds);
	query.SetConditionRange("shippingDates", fromShippingDate, toShippingDate);
	query.SetConditionRange("orderDates", fromOrderDate, toOrderDate);
	query.SetCondition("shippingCountries", shippingCountries);

	return connection.Query<CategorySale>(query.GetSql(), query.SqlParameters);
}
```

But where's the SQL, what's in this `CategorySales.sql`? Now here's the nice part, you can safely store the SQL somewhere else and it may have multiple `WHERE` clauses, multiple `ORDER BY`'s and any number of sub-queries - all of this is natively supported by SqlBinders templates, being so composable there's almost never a reason to store templates in your method unless they're one-liners and very small. 

There are multiple possible SQL scripts which will all work with the above method. 

For example this script with shortcut aliases and an optional sub-query:

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

The next script  uses different aliases and would work just the same:

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

It's the same thing except it uses different aliases - please note that you don't need to modify your `GetCategorySales` method for this template to work, it'll work as long as the parameter names are the same.

Next template uses a completely different join and has no sub-queries, it may be a little less optimal but it'll work just the same:

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

Or if you want something totally different, here's another template which has two `WHERE` clauses, is using a different syntax to join and has no `GROUP BY` - again, it works out of the box and would produce the same data:

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

Any one of aforementioned scripts may be put in the `CategorySales.sql` file and used without modifying the C# code. With SqlBinder your SQL scripts can be *truly* separate from everything else. What SqlBinder does is it binds `SqlBinder.Condition` objects to its template scripts returning a valid SQL which you can then pass to your ORM.

## Tutorials, Examples and Demo App
On [SqlBinder's Code Project article](https://www.codeproject.com/Articles/1246990/SqlBinder-Library) you may explore more in-depth examples offering some deeper insight and easy to follow tutorials.

A very useful Demo App comes with SqlBinder source code which may introduce you better to SqlBinder than examples or anything else. It is described in more detail on the [Code Project article](https://www.codeproject.com/Articles/1246990/SqlBinder-Library).

Don't forget to rate the article if you like what you see!

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

## The Performance
SqlBinder is *very* fast but I have nothing to compare it with. Instead, you can combine it with micro ORM solutions like Dapper and measure the potential overhead. I took Dapper for reference as it's the fastest micro-ORM that I currently know of.

Consider the following tables. On the left column you will see performance of Dapper alone and on the right column you will see Dapper doing the exact same thing but with added overhead of SqlBinder doing its magic.

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
Where the latter was used in Dapper+SqlBinder combination. 

It is important to note that SqlBinder has the ability to re-use compiled templates as it completely separates the parsing and templating concerns. You may create a SqlBinder query template once and then build all the subsequent SQL queries from the same pre-parsed template. One of the key functionalities of SqlBinder is that it doesn't parse or generate the whole SQL *every time*. Also, it relies on hand coded parser which is well optimized. 

Simple performance tests are available in the Source folder where you can benchmark SqlBinder on your own.

## The Purpose
I originally wrote the first version of this library back in 2009 to make my life easier. The projects I had worked on relied on large and very complex Oracle databases with all the business logic in them so I used SQL to access anything I needed which worked out great. I was in charge of developing the front-end which involved great many filters and buttons which helped the user customize the data to be visualized. Fetching thousands of records and then filtering them on client side was out of the question, we had both our own and business client DBAs keeping a close eye on performance and bandwidth. Therefore, with some help of DBAs, PLSQL devs etc. we were able to muster up some very performant, complex and crafty SQLs. 

This however, resulted in some pretty awkward SQL-generating and variable-binding code that was hard to maintain, optimize and alter. Tools like NHibernate solved a lot of problems we didn't have but didn't entirely solve the one we had. I wasn't aware of Dapper back then but while it would lessen the problems it still couldn't solve them (otherwise I wouldn't be posting any of this and would just switch to Dapper as it's a really great library). This is where my SqlBinder-like metalanguage came to rescue, all that mess was converted into a `string.Format`-like code where I could write the whole script and then pass the variables (or don't pass them). From a proof of concept and experiment it eventually grew up to be SqlBinder as I used my free time to tweak and improve it. It helped me greatly and I'm releasing it here so it may help someone else too. I tend to use it in combination with Dapper but any other (existing or your own) ORM may be used just as well.
