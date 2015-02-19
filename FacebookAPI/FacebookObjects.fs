namespace FacebookAPI
open System

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

type CreatePostData = {
        post:Post
        page:Page
        shouldPublish:bool
    }