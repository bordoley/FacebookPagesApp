namespace FacebookAPI
open System

open FSharpx.Collections

type UserInfo = {
        firstName:string
    }

type Page = {
        id:string
        accessToken:string
        name:string
    }

type Post = {
        id:string
        message:string
        createdTime:DateTime
    }

type PostFeed = { 
        previous:Uri
        next:Uri
        posts:PersistentVector<Post>
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PostFeed =
    let Empty = { 
        previous = Uri("",UriKind.RelativeOrAbsolute); 
        next = Uri("", UriKind.RelativeOrAbsolute); 
        posts = PersistentVector.empty }

type CreatePostData = {
        post:Post
        shouldPublish:bool
    }