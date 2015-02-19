namespace FacebookAPI
open System
open System.IO
open System.Threading

open FunctionalHttp.Core
open FunctionalHttp.Client

// FIXME: I really hate JSON.Net on a visceral level
open Newtonsoft.Json
open Newtonsoft.Json.Linq

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

        let result : FacebookAPI.Post list = [] 

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

    member this.CreatePost post = ()

    member this.ListPosts (pageid:string) = async {
        let request =
            let uri = Uri(sprintf "https://graph.facebook.com/v2.2/%s/feed" pageid)
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        
        let! response = request |> postsClient
        return response.Entity
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