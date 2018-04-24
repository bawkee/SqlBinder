# SqlBinder

Is a free, open-source library that helps you transform a given SQL-like script and a set of conditions into an `IDbCommand`.

It is *not* an ORM solution - instead, it is DBMS-independent, SQL-centric **templating engine**. All it does is it removes the hassle of writing SQL and bind variable generating code. It does *not* generate the SQL itself, it lets you re-format an existing SQL instead.

So with one template you can create multiple different (but similar) queries.

## Example 1: Query employees 

At the heart of SqlBinder is an abstract class called `QueryBase`, we will use it to define our OleDbQuery class:

```C#
public class OleDbQuery : QueryBase<OleDbConnection, OleDbCommand>
{
	protected override string DefaultParameterFormat => "@{0}";

	public OleDbQuery(OleDbConnection connection, string script)
		: base(connection, script) { }
}
```

Done. Now let's connect to Northwind demo database: 

```C#
var connection = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb")
```


And then write a simple OleDB SQL query which will retreive the list of employees.

```C#
var query = new OleDbQuery(connection, @"SELECT * FROM Employees {WHERE EmployeeID [employeeId]}");
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

Now let's single out an **employee by his ID**:

```C#
query.SetCondition("employeeId", 1);

cmd = query.CreateCommand();

Console.WriteLine(cmd.CommandText); // Output the passed SQL
```

This is the output:

```SQL
SELECT * FROM Employees WHERE EmployeeID = @pemployeeId_1
```

Notice that we're using the same query to create two entirely different commands.

Let's go further and retrieve **employees by IDs 1 and 2**. Again, we use the same query but different parameters are supplied to the crucial `SetCondition` method.

```C#
query.SetCondition("employeeId", new[] { 1, 2 });

cmd = query.CreateCommand();

Console.WriteLine(cmd.CommandText); // Output the passed SQL
```

Output:

```SQL
SELECT * FROM Employees WHERE EmployeeID IN (@pemployeeId_1, @pemployeeId_2)
```

**So what happened?** Let's first go back to our SQL *template*:

```SQL
SELECT * FROM Employees {WHERE EmployeeID [employeeId]}
```

This was passed to the `OleDbQuery`, a class we previously created that inherits from `SqlBinder.QueryBase`.

**In the first example**, `OleDbQuery` was not provided any conditions, so, it removed all the magical syntax that begins with `{` and ends with `}` as it served no purpose. 

**In the second example**, we called `SetCondition("employeeId", 1);` so now the magical syntax comes into play.

So, this template:

```SQL
... {WHERE EmployeeID [employeeId]} ...
```
Plus this method:
```C#
SetCondition("employeeId", 1);
```
Produced this SQL:
```SQL
... WHERE EmployeeID = @pemployeeId_1 ...
```

The `[employeeId]` placeholder was simply replaced by `= @pemployeeId_1`. SqlBinder also automatically takes care of the command parameters (bind variables) that will be passed to `IDbCommand`.

**In the third example**, we called `SetCondition("employeeId", new[] { 1, 2 });` which means we would like two employees this time. 

This caused the query:
```SQL
... {WHERE EmployeeID [employeeId]} ...
```
To be translated into this:
```SQL
... WHERE EmployeeID IN (@pemployeeId_1, @pemployeeId_2) ...
```

There are great many things into which `[employeeId]` can be translated into.

## Example 2: Query employees even more
Let's do a different query this time:
```SQL
SELECT * FROM Employees {WHERE {City [city]} {HireDate [hireDate]} {YEAR(HireDate) [hireDateYear]}}
```
This time we have nested *scopes* `{...{...}...}`. First of, note that this syntax can be put anywhere in the SQL and that the `WHERE` clause means nothing to SqlBinder, it's just plain text that will be removed if its parent *scope* is removed. The scope is removed only if all its child scopes *and* all its child `[...]` placeholders (if any) are removed. A placeholder will be removed if no matching *condition* was found for it.

Again, if we don't pass any *conditions*, all the magical stuff is removed and you end up with:

```SQL
SELECT * FROM Employees
```

But let’s pass some conditions, **let’s try and get employees hired in 1993**:

```C#
query.SetCondition("hireDateYear", 1993);
```

This will produce the following SQL:

```SQL
SELECT * FROM Employees WHERE YEAR(HireDate) = @phireDateYear_1
```

Again, don't worry about values, they are already passed to command parameters.

As you can see, the scopes `{City [city]}` and `{HireDate [hireDate]}` were eliminated as SqlBinder did not find any matching conditions for those.

**Now let's get employees hired after July 1993!** 

```C#
query.Conditions.Clear();
query.SetCondition("hireDate", from: new DateTime(1993, 6, 1));
```

This is just regular C# syntax, the `from:` part helps compiler find the right overload as there are many. Also, this time we're clearing the conditions collection as we don't want `hireDateYear` as well, we just want `hireDate` right now - if you look at the SQL again you'll see that they are different placeholders.

The resulting SQL:

```SQL
SELECT * FROM Employees WHERE HireDate >= @phireDate_1
```

**How about employees from London that were hired between 1993 and 1994?**

```C#
query.Conditions.Clear();
query.SetCondition("hireDateYear", 1993, 1994);
query.SetCondition("city", "London");
```

Now we have two conditions that will be automatically connected with an `AND` operator in the output SQL. All *consecutive* scopes will automatically be connected with an operator (e.g. AND, OR). 

The resulting SQL:
```SQL
SELECT * FROM Employees WHERE City = @pcity_1 AND YEAR(HireDate) BETWEEN @phireDateYear_1 AND @phireDateYear_2
```

Neat!

For complete source code of these examples refer to the `Source/SqlBinder.ConsoleTutorial` folder where you can experiment on your own.

## The Demo App

This library comes with a very nice, interactive Demo App developed in WPF which serves as a more complex example of the SqlBinder usage. It's still actually quite basic but offers a quite solid insight into the core features. 

![screenshot1](https://raw.githubusercontent.com/bawkee/SqlBinder/master/Source/SqlBinder.DemoApp/screenshot1.png "Demo Screenshot")

You can browse the Northwind database using some relatively complex queries which come as *.sql files you can alter any way you like and watch SqlBinder work its magic in the Debug Log.

## The Syntax
The syntax looks like this:
```SQL
... <tag>{ ... [placeholderName] ... } ...
```

Where:
* `<tag>` can currently only be `@` character which tells the SqlBinder to connect scopes with an `OR` text rather than default `AND`. 
* `placeholderName` can be any name that will be matched against `Query.Conditions` collection. This name is referred to as *parameter* in the SqlBinder objects (e.g. `Condition.ParameterName`).
* The `...` can be any SQL from any DBMS or just about any text.

**Following things should be kept in mind:**
* A scope is defined by curly braces `{}` while a placeholder is defined with square brackets `[]`.
* Scopes can be nested (i.e. contain child scopes), placeholders cannot.
* Scopes that do not contain at least one child scope or a placeholder with a matching condition will be removed along with its entire contents and other child scopes.
* Placeholders that haven't been matched by any `Query.Condition` will cause its parent scope to be removed.
* There can only be one placeholder in given scope. When you need multiple placeholders put each one in its own scope.

**The comment syntax** looks like this:
```SQL
{* ...comment... *}

{*
	...comment...
*}
```
SqlBinder comments will be entirely removed from the output SQL. The SQL comments will remain intact.

**The SQL literals** can contain anything:
```SQL
'Anything can be here: [] {}...' 

"My Table Name?" -- preferred over [My Table Name]
```
They are not processed against SqlBinder syntax.

**The escape strings** can be used, in case you do need any of the SqlBinder's special characters:
```SQL
$[Some Table]$ -> [Some Table]

${Something}$ -> {Something}
```

## The Regex
The script is processed with the following Microsoft's Regex recursive script:
```Regex
(?<tag>[\@])*(?<symbol>[\[{])(?<content>(?>[^{}\[\]]+|[{\[](?<depth>)|[}\]](?<-depth>))*(?(depth)(?!)))[}\]]
```

If you are unfamiliar with recursive regex you can find many tutorials on the web. The `<symbol>` group contains the type of braces used (square or curly).

The regex is doing recursive search against both [] and {} which isn't *very* optimal but if I were to further optimize this I'd rather not use Regex at all. 

Also, there's a *hidden* syntax here (the pipe character) which you can also see in the source code - there is support for 'compound' parameter placeholders, for some rare and extremely complicated queries. It's undocumented and doesn't have unit tests so I'm not releasing it yet, especially if nobody else needs it. They are useful in scenarios where you want to query a, say, Project table to find a project on which both John AND Mike employees worked on through an EmployeeProject connecting table (intersection of John's and Mike's projects). It's something you'll very rarely find on production because DBAs hate it and no one really ever needs it. Current plan is to remove this feature altogether.


## The Purpose
I originally wrote the first version of this library back in 2009 to make my life easier. The projects I had worked on relied on large and very complex Oracle databases with all the business logic in them so I used SQL to access anything I needed which really worked great. I was in charge of developing the front-end which involved great many filters and buttons which helped the user customize the data he can see. Fetching thousands of records and then filtering them on client machines was a no-go, we had both our own and business client DBAs keeping a close eye on performance and bandwidth. Therefore, with some help of DBAs, PLSQL devs etc. we were able to muster up some very performant, complex and crafty SQLs which for reasons I won't go into here would not be optimal as DB views. 

This however, resulted in some pretty awkward SQL-generating and variable-binding code that was hard to maintain, optimize and alter. Tools like Hibernate and Dapper solved a lot of problems we didn't have but didn't entirely solve the one we had (but of the two, Dapper, along with its flexible license was a best fit). This is where my SqlBinder-like set of classes came to rescue, all that mess was converted into a `string.Format`-like code where I could write the whole script and then pass the variables (or don't pass them). From a proof of concept and experiment it eventually grew up to be SqlBinder as I used my free time to tweak and improve it. It helped me greatly and I'm releasing it here so it may help someone else too.

## Issues... ?
SqlBinder entirely relies on Regex - it might be faster if it was processed by tools like ANTLR but my idea was to keep it small and simple and so far had no issues with performance. There is some minor performance penalty for first-time use of each script as it uses a *compiled* Regex which is statically declared and will remain active as long as your app or app pool lives. You can of course change this to work any way you like.
