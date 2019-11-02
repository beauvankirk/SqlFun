﻿namespace SqlFun.NpgSql.Tests

open System
open SqlFun
open Common

module Data = 

    type PostStatus = 
        | [<EnumValue("N")>] New = 0
        | [<EnumValue("P")>] Published = 1
        | [<EnumValue("A")>] Archived = 2

    type Post = {
        postId: int
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
        blogId: int
        name: string
        title: string
        description: string
        owner: string
        createdAt: DateTime
        modifiedAt: DateTime option
        modifiedBy: string option
        posts: Post list
    }

module Tooling = 
    
    let getNumberOfBlogs: DataContext -> int = 
        sql "select count(*) from blog"

    let deleteAllButFirstBlog: DataContext -> unit = 
        sql "delete from blog where blogid > 1"

