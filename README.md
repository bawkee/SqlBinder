# SqlBinder

Is a free, open-source library that helps you transform a given SQL template and a set of conditions into any number of valid SQL statements along with their associated parameters.

It is *not* an ORM solution - instead, it is DBMS-independent, SQL-centric **templating engine**. All it does is it removes the hassle of writing code that generates SQLs and bind variables . It does *not* generate the SQL itself, it transforms an existing SQL template instead.

Essentially, with one template you can create multiple different queries.

[![NuGet](https://img.shields.io/nuget/v/SqlBinder.svg)](https://www.nuget.org/packages/SqlBinder/)

## Example 1: Query employees 

Let's connect to Northwind demo database: 

```C#
var connection = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb");
```

And then write a simple OleDB SQL query which will retreive the list of employees.

```C#
var query = new DbQuery(connection, @"SELECT * FROM Employees {WHERE EmployeeID :employeeId}");
```

As you can see this is not typical SQL, there is some *formatting* syntax in it which is later processed by the SqlBinder. It's an *SQL template* which will be used to create the actual SQL.

We can in fact create a command out of this template right now:

```C#
IDbCommand cmd = query.CreateCommand();

Console.WriteLine(cmd.CommandText); // Output the passed SQL
```

Output:
```SQL
SELECT * FROM Employees
```

Notice how the initial SQL enclosed in the `{...}` tags is not present in the output SQL. Now let's single out an **employee by his ID**:

```C#
query.SetCondition("employeeId", 1);

cmd = query.CreateCommand();

Console.WriteLine(cmd.CommandText); // Output the passed SQL
```

This is the output:

```SQL
SELECT * FROM Employees WHERE EmployeeID = :pemployeeId_1
```

We're using the same query to create two entirely different commands. This time, the `{WHERE EmployeeID :employeeId}` part wasn't eliminated.

Let's go further and retrieve **employees by IDs 1 and 2**. Again, we use the same query but different parameters are supplied to the crucial `SetCondition` method.

```C#
query.SetCondition("employeeId", new[] { 1, 2 });

cmd = query.CreateCommand();

Console.WriteLine(cmd.CommandText); // Output the passed SQL
```

Output:

```SQL
SELECT * FROM Employees WHERE EmployeeID IN (:pemployeeId_1, :pemployeeId_2)
```

**So what happened?** Let's first go back to our SQL *template*:

```SQL
SELECT * FROM Employees {WHERE EmployeeID :employeeId}
```

**In the first test**, the `query` object was not provided any conditions, so, it removed all the magical syntax that begins with `{` and ends with `}` as it served no purpose. 

**In the second test**, we called `SetCondition("employeeId", 1);` so now the magical syntax comes into play.

So, this template:

```SQL
... {WHERE EmployeeID :employeeId} ...
```
Plus this method:
```C#
SetCondition("employeeId", 1);
```
Produced this SQL:
```SQL
... WHERE EmployeeID = :pemployeeId_1 ...
```

The `:employeeId` placeholder was simply replaced by `= :pemployeeId_1`. SqlBinder also automatically takes care of the command parameters (bind variables) that will be passed to `IDbCommand`.

**In the third test**, we called `SetCondition("employeeId", new[] { 1, 2 });` which means we would like two employees this time. 

This caused the query:
```SQL
... {WHERE EmployeeID :employeeId} ...
```
To be transformed into this:
```SQL
... WHERE EmployeeID IN (:pemployeeId_1, :pemployeeId_2) ...
```

There are great many things into which `:employeeId` can be transformed but for now we'll just cover the basic concepts. 

[Try this example on DotNetFiddle!](https://dotnetfiddle.net/pa0h1H "Try it on DotNetFiddle")

## Example 2: Query yet some more employees
Let's do a different query this time:
```SQL
SELECT * FROM Employees {WHERE {City @city} {HireDate @hireDate} {YEAR(HireDate) @hireDateYear}}
```
This time we have nested *scopes* `{...{...}...}`. First and foremost, note that this syntax can be put anywhere in the SQL and that the `WHERE` clause means nothing to SqlBinder, it's just plain text that will be removed if its parent *scope* is removed. As a side note, we're using the `@` parameter prefix this time - because we can.

**Remember:** the scope is removed only if all its child scopes are removed or its child placeholder (i.e. `:param`, `@param` or `?param`) is removed which in turn is removed if no matching *condition* was found for it.

For example, if we don't pass any *conditions* at all, all the magical stuff is removed and you end up with:

```SQL
SELECT * FROM Employees
```

But if we do pass some condition, for example, **let’s try and get employees hired in 1993**:

```C#
query.SetCondition("hireDateYear", 1993);
```

This will produce the following SQL:

```SQL
SELECT * FROM Employees WHERE YEAR(HireDate) = @phireDateYear_1
```

By the way, don't worry about command parameter values, they are already passed to the command.

As you can see, the scopes `{City @city}` and `{HireDate @hireDate}` were eliminated as SqlBinder did not find any matching conditions for them.

**Now let's try and get employees hired after July 1993** 

```C#
query.Conditions.Clear(); // Remove any previous conditions
query.SetCondition("hireDate", from: new DateTime(1993, 6, 1));
```

This time we're clearing the conditions collection as we don't want `hireDateYear`, we just want `hireDate` right now - if you take a look at the SQL template again you'll see that they are different placeholders.

The resulting SQL will be:

```SQL
SELECT * FROM Employees WHERE HireDate >= @phireDate_1
```

**How about employees from London that were hired between 1993 and 1994?**

```C#
query.Conditions.Clear();
query.SetCondition("hireDateYear", 1993, 1994);
query.SetCondition("city", "London");
```

Now we have two conditions that will be automatically connected with an `AND` operator in the output SQL. All *consecutive* (i.e. separated by white-space) scopes will automatically be connected with an operator (e.g. AND, OR). 

The resulting SQL:
```SQL
SELECT * FROM Employees WHERE City = @pcity_1 AND YEAR(HireDate) BETWEEN @phireDateYear_1 AND @phireDateYear_2
```

Neat!

For complete source code of these examples refer to the `Source/SqlBinder.ConsoleTutorial` folder where you can experiment on your own.

## The Demo App

This library comes with a very nice, interactive Demo App developed in WPF which serves as a more complex example of the SqlBinder capabilities. It's still actually quite basic (it's just a MDB after all) but offers a deeper insight into the core features. 

![screenshot1](https://raw.githubusercontent.com/bawkee/SqlBinder/master/Source/SqlBinder.DemoApp/screenshot1.png "Demo Screenshot")

You can browse the Northwind database using example queries which come as *.sql files which you can alter any way you like and watch SqlBinder work its magic in the Debug Log.

## The Syntax
Consists of two basic types of elements: scopes and parameter placeholders. Scopes are defined by curly braces `{ ... }` and parameter placeholders can be defined by the typical SQL syntax (i.e. `:parameter`) or by custom SqlBinder syntax (if configured so, i.e. `[parameter]`). Observe the following set of valid examples where `...` can be any SQL:
```SQL
... { ... :paramPlaceholder  ... } ...

... { ... { ... @paramPlaceholder  ... } ... } ...

... { ... { ... :paramPlaceholder1  ... } ... { ... :paramPlaceholder2 ... } ... } ...

... @{ ... { ... :paramPlaceholder1  ... } ... { ... :paramPlaceholder2 ... } ... } ...

... @{ ... { ... [place holder 1]  ... } ... { ... [place holder 2] ... } ... } ...
```

Further explanation of above examples:
* Curly braces `{ ... }` define a scope. Scope can either contain child scopes or a single parameter placeholder. Scope that does not contain either of those will always be removed as that's considered pointless. Otherwise, the scope is removed only if all its child scopes are removed or its parameter placeholder is removed, *which* in turn is removed if no matching *condition* was found for it (conditions are explained further on). 
* `:paramPlaceholder` can be any alphanumeric name that will be matched against `Query.Conditions` collection. This is referred to as *parameter* in the SqlBinder objects. If a parameter doesn't match any condition it will be removed along with its entire parent scope. The output SQL bind variable will be formatted with the same prefix as the parameter (acceptable prefixes are `:` or `@` or `?`). These parameters are not bind-variables and you must respect the aforementioned syntax, i.e. the Oracle variable `:"MyVariable"` won't be recognized as a parameter - if you need custom formatting in your output variables which you can't accomplish with the SqlBinder syntax names there ways to do so via events and delegates (see the Query class). Note that there can only be one placeholder in a given scope. When you need multiple placeholders put each one in its own separate scope.
* `[place holder xy]` works the same way as above except any character is allowed and you must provide the parameter prefix manually (in C#) by overriding the `Query` class, `DbQuery` class or setting the appropriate property. Also, this syntax doesn't work by default, you have to enable a special hint via `Query.ParserHints` property since `[]` characters are used by some SQL flavors. With that said, you can still escape these tags into the output SQL `[[like this]]`.
* The `@` character before the scope (i.e. `@{`) tells the SqlBinder to connect scopes with an `OR` rather than default `AND` operator. 
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
SqlBinder is *very* fast but it's pointless to compare it with other tools as they do different things. However, you can combine it with micro ORM solutions like Dapper - it wouldn't make sense to compare the performance differences of SqlBinder and Dapper separately but one can measure the overhead added by utilizing both at the same time. I took Dapper for reference as it's the fastest micro-ORM that I currently know of.

Consider the following tables. On the left column you will see performance of Dapper alone and on the right column you will see Dapper doing the exact same thing but with added overhead of SqlBinder providing the SQL and command parameter values based on a given template.

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
I originally wrote the first version of this library back in 2009 to make my life easier. The projects I had worked on relied on large and very complex Oracle databases with all the business logic in them so I used SQL to access anything I needed which worked out great. I was in charge of developing the front-end which involved great many filters and buttons which helped the user customize the data to be visualized. Fetching thousands of records and then filtering them on client side was out of the question, we had both our own and business client DBAs keeping a close eye on performance and bandwidth. Therefore, with some help of DBAs, PLSQL devs etc. we were able to muster up some very performant, complex and crafty SQLs which for reasons out of scope here would not be optimal as DB views. 

This however, resulted in some pretty awkward SQL-generating and variable-binding code that was hard to maintain, optimize and alter. Tools like NHibernate solved a lot of problems we didn't have but didn't entirely solve the one we had. I wasn't aware of Dapper back then but it still wouldn't solve most of the problems. This is where my SqlBinder-like metalanguage came to rescue, all that mess was converted into a `string.Format`-like code where I could write the whole script and then pass the variables (or don't pass them). From a proof of concept and experiment it eventually grew up to be SqlBinder as I used my free time to tweak and improve it. It helped me greatly and I'm releasing it here so it may help someone else too.
