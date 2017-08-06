Npgsql for CrateDB
=============

### What Is Npgsql for CrateDB?

Npgsql for CrateDB is a fork of the Npgsql project containing the necessary changes to use Npgsql as a .NET data provider for [CrateDB](https://crate.io/overview/). It allows you to connect and interact with CrateDB using .NET.

We are working to get these changes merged upstream. 

It is important to understand, that although CrateDB [contains support for the PostgreSQL wire protocol](https://crate.io/docs/crate/reference/protocols/postgres.html), it is not a PostgreSQL clone and therefore it only supports a subset of PostgreSQLs features and data types.

### Server Compatibility Mode

The adaptions that are necessary to use Npgsql to connect and interact with CrateDB are activated by setting the `Server Compatibility Mode` to *CrateDB* in the connection string.

```c#
var conn = new NpgsqlConnection("Server=localhost;Server Compatibility Mode=CrateDB");
conn.Open();
...
```

This disables some unsupported features, adapts metadata queries and adds additional type mappings.



For information about CrateDB, please visit the Crate website at https://crate.io.

For information about Npgsql, please visit the Npgsql website at [http://www.npgsql.org](http://www.npgsql.org).


