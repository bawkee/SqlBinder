# SqlBinder
Is a free, open-source library that helps you transform a given SQL-like script and a set of conditions into an `IDbCommand`.

It is *not* an ORM - instead, it is DBMS-independent, SQL-centric **templating engine**. All it does is it removes the hassle of writing/generating SQL queries and bind variables. It does *not* generate the entire SQL, only very small parts of it - you still write 99% of the SQL and the 1% that *it does* generate is very easily configurable.

