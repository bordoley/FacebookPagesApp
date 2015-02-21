namespace FacebookAPI
open System
open System.IO
open System.Threading

open FunctionalHttp.Core
open FunctionalHttp.Client

// FIXME: I really hate JSON.Net on a visceral level
open Newtonsoft.Json
open Newtonsoft.Json.Linq

open FSharpx.Collections

open Splat

type HttpClient<'TReq, 'TResp> = HttpRequest<'TReq> -> Async<HttpResponse<'TResp>>

module internal SplatConverters =
    let streamToIBitmap (contentInfo:ContentInfo, stream:Stream) = async {
        let! bitmap = Splat.BitmapLoader.Current.Load(stream, Nullable(), Nullable()) |> Async.AwaitTask
        return (contentInfo, bitmap)
    }

// FIXME: JSON.Net...blehhh...not a real asynchronous parser which is a bummer.
module internal FacebookConverters =
    let streamToUser (contentInfo:ContentInfo, stream:Stream) = async {
        do! Async.SwitchToThreadPool ()

        use stream = stream
        use sr = new StreamReader(stream)
        let o = JToken.ReadFrom(new JsonTextReader(sr)) :?> JObject
        let firstName = string o.["first_name"]

        return (contentInfo, { firstName = firstName })
    }

    let streamToPosts (contentInfo:ContentInfo, stream:Stream) = async {
        do! Async.SwitchToThreadPool ()

        use stream = stream
        use sr = new StreamReader(stream)
        let o = JToken.ReadFrom(new JsonTextReader(sr))

        let posts =
            o.["data"].Children() 
            |> Seq.map(fun o ->
                let id = string o.["id"]
                let message = string o.["message"]
                //let created = match o.["created_time"] :> obj with | :? DateTime as d -> d | _ -> failwith "not a date" 
                {message = message; id = id; createdTime = DateTime.Now; })
            |> PersistentVector.ofSeq

        let prev = string o.["paging"].["previous"] |> (fun x -> Uri(x))
        let next = string o.["paging"].["next"] |> (fun x -> Uri(x))

        let result = { previous = prev; next = next; posts = posts }

        return (contentInfo, result)
    }

    let streamToPages (contentInfo:ContentInfo, stream:Stream) = async {
        do! Async.SwitchToThreadPool ()

        use stream = stream
        use sr = new StreamReader(stream)
        let o = JToken.ReadFrom(new JsonTextReader(sr))
       
        let result =
            o.["data"].Children() 
            |> Seq.map(fun o ->
                let id = string o.["id"]
                let accessToken = string o.["access_token"]
                let name = string o.["name"]

                { id = id; accessToken = accessToken; name = name })
            |> List.ofSeq

        return (contentInfo, result)
    }

[<Sealed>]
type FacebookClient (httpClient:HttpClient<Stream,Stream>, tokenProvider:unit->string) =
    let pagesClient = httpClient |> HttpClient.usingConverters (Converters.fromUnitToStream, FacebookConverters.streamToPages)
    let profileClient = httpClient |> HttpClient.usingConverters (Converters.fromUnitToStream, SplatConverters.streamToIBitmap) 
    let userInfoClient = httpClient |> HttpClient.usingConverters (Converters.fromUnitToStream, FacebookConverters.streamToUser)
    let postsClient = httpClient |> HttpClient.usingConverters (Converters.fromUnitToStream, FacebookConverters.streamToPosts)

    let withAuthorization req =
        let authorizationCredentials = Challenge.OAuthToken <| tokenProvider()
        req |> HttpRequest.withAuthorization authorizationCredentials

    member this.ListPages = async {
        let request = 
            let uri = new System.Uri("https://graph.facebook.com/v2.2/me/accounts")
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization

        let! response = request |> pagesClient
        return response.Entity
    }

    member this.CreatePost (post:CreatePostData) = async {
        ()
    }

    member this.ListPosts (pageid:string, showUnpublished:bool) = async {
        let request =
            let showUnpublished = if showUnpublished then "true" else "false"
            let uri = Uri(sprintf "https://graph.facebook.com/v2.2/%s/feed?is_published=%s&limit=10" pageid showUnpublished)
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        
        let! response = request |> postsClient
        return response.Entity
    }

    member this.RefreshPostsAfter (currentPosts:PostFeed) = async {
        let request =
            let uri = currentPosts.previous
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        
        let! response = request |> postsClient
        return 
            match response.Entity with
            | Choice1Of2 x -> 
                let posts = PersistentVector.append x.posts currentPosts.posts 
                let next = currentPosts.next
                let prev = x.previous

                Choice1Of2 { previous = prev; next = next; posts = posts }
            | Choice2Of2 x -> Choice2Of2 x 
    }

    member this.LoadMorePostsBefore(currentPosts:PostFeed) = async {
        let request =
            let uri = currentPosts.next
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        
        let! response = request |> postsClient
        return 
            match response.Entity with
            | Choice1Of2 x -> 
                let posts = PersistentVector.append currentPosts.posts x.posts
                let next = x.next
                let prev = currentPosts.previous

                Choice1Of2 { previous = prev; next = next; posts = posts }
            | Choice2Of2 x -> 
                Choice2Of2 x 
    }

    member this.ProfilePhoto = async {
        let request =
            let uri = Uri("https://graph.facebook.com/v2.2/me/picture?type=large")
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        let! response = request |> profileClient
        return response.Entity
    }

    member this.UserInfo = async {
        let request =
            let uri = new System.Uri("https://graph.facebook.com/v2.2/me")
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        let! response = request |> userInfoClient
        return response.Entity
    }     