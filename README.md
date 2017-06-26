# SqlFun
Idiomatic data access for F#

SqlFun allows to write data access code in F# in functional way. 
It's fast, type safe and gives you all powers of SQL, since you are not limited by any query DSL.

## Features
* All ADO.NET providers available
* All SQL features available
* Reasonable type safety
* Reasonable performance
* Compound, hierarchical query parameters
* Compound, hierarchical query results
* Support for parameter conversions
* Support for result transformations
* Support for enum types
* Asychronous queries
* Composable, template-based queries
* Auto-generated CRUD operations
* Computation expressions for connection and transaction handling

## Supported databases
In its core SqlFun does not use any features specific to some db provider, so it works with all ADO.NET providers. 

There are two extensions, enabling provider-specific features:
* the extension for MS SQL, that allows to use table valued parameters
* the extension for PostgreSQL, making use of array parameters possible

## How it works
Most of us think about data access code as a separate layer. We don't like to spread SQL queries across all the application.
Better way is to build an API exposing your database, consisting of structures representing database data, and functions responsible for processing this data. 

### Prerequisites
First step is to define function creating database connection,

    let createConnection () = new SqlConnection(<your database connection string>)

and wire it up with functions responsible for generating queries (using partial application):
 
    let sql commandText = sql createConnection defaultParamBuilder commandText

    let storedproc name = storedproc createConnection defaultParamBuilder name

and for executing them:

    let run f = DataContext.run createConnection f

    let runAsync f = DataContext.runAsync createConnection f
    
### Data structures
Then, data structures should be defined for results of your queries.

    type Post = {
        id: int
        blogId: int
        name: string
        title: string
        content: string
        author: string
        createdAt: DateTime
        modifiedAt: DateTime option
        modifiedBy: string option
        status: PostStatus
    }
    
    type Blog = {
        id: int
        name: string
        title: string
        description: string
        owner: string
        createdAt: DateTime
        modifiedAt: DateTime option
        modifiedBy: string option
        posts: Post list
    }
    
The most preferrable way is to use F# record types. Record fields should reflect query result columns, because they are mapped by name.
    
### Queries
The best way of defining queries is to create variables for them and place in some module:

    module Blogging =    
 
        let getBlog: int -> DataContext -> Blog = 
            sql "select id, name, title, description, owner, createdAt, modifiedAt, modifiedBy 
                 from Blog 
                 where id = @id"
            
        let getPosts: int -> DataContext -> Post list = 
            sql "select id, blogId, name, title, content, author, createdAt, modifiedAt, modifiedBy, status 
                 from post 
                 where blogId = @blogId"
        
The functions executing queries are generated during a first access to the module contents. 

At that stage, all the type checking is performed, so it's easy to make type checking part of automatic testing - one line of code for each module is needed.

The generating process uses reflection heavily, but no reflection is used while processing a query - everything is already generated.

### Executing queries
Since your queries have a DataContext as a last parameter, they can be passed to the `run` function after applying preceding parameters.

    let blog = Blogging.getBlog 1 |> run

### Async support
The query can be defined as asynchronous as well:

        let getBlog: int -> DataContext -> Blog Async = 
            sql "select id, name, title, description, owner, createdAt, modifiedAt, modifiedBy 
                 from Blog 
                 where id = @id"

and then, executed as async:

    async {
        let! blog = Blogging.getBlog 1 |> runAsync
        ...
    }

### Result transformations
Since the ADO.NET allows to execute many sql commands at once, it's possible to utilize it with SqlFun. The result is a tuple:

        let getBlogWithPosts: int -> DataContext -> Blog * Post list = 
            sql "select id, name, title, description, owner, createdAt, modifiedAt, modifiedBy 
                 from Blog 
                 where id = @id;
                 select id, blogId, name, title, content, author, createdAt, modifiedAt, modifiedBy, status 
                 from post 
                 where blogId = @id"
 
 The call of `sql` returns some function, thus it can be composed with another function, possibly performing result transformations.
 Let extend the blog type with a `posts: Post list` property. In this case, two results can be combined using a blog id as a key:
 
        let getBlogWithPosts: int -> DataContext -> Blog = 
            sql "select id, name, title, description, owner, createdAt, modifiedAt, modifiedBy 
                 from Blog 
                 where id = @id;
                 select id, blogId, name, title, content, author, createdAt, modifiedAt, modifiedBy, status 
                 from post 
                 where blogId = @id"
            >> join (fun b -> b.id) (fun p -> p.blogId) (fun b pl -> { b with posts = pl })
            |> curry  

The `curry` function is required because the function composition operator `>>` accepts only one-arg functions.

### Compound parameters
Records can be parameters as well:

    let insertPost: Post -> DataContext -> int = 
        sql "insert into post 
                    (blogId, name, title, content, author, createdAt, status)
             values (@blogId, @name, @title, @content, @author, @createdAt, @status);
             select scope_identity()"

The record fields are mapped to query parameters by name.

### Stored procedures
The result of a function calling stored procedure should be a three-element tuple (return code, output params, result):
	
    let findPosts: (PostSearchCriteria * SignatureSearchCriteria) -> DataContext -> (int * unit * Post list) =
        storedproc "FindPosts"
	
but there are transformers, that allow to ignore parts of it:

    let findPosts: (PostSearchCriteria * SignatureSearchCriteria) -> DataContext -> Post list =
        storedproc "FindPosts"
        >> resultOnly id 
        |> curry
	 
### Utilizing `dbaction` and `asyncdb` computation expressions
It's easy to execute one query with `run` function. To execute more queries in a context of one open connection, computation expression can be used:

    dbaction {
        let! postId = Blogging.insertPost post
        do! Blogging.insertComments postId comments
        do! Blogging.insertTags postId tags
    } |> run
    
The async equivalent of this expression is `asyncdb`.

### Transactions
To execute some queries in transaction, the DataContext.inTransaction should be used:

    dbaction {
        let! postId = Blogging.insertPost post
        do! Blogging.insertComments postId comments
        do! Blogging.insertTags postId tags
    } 
    |> DataContext.inTransaction
    |> run

Its async equivalent is DataContext.inTransactionAsync.
