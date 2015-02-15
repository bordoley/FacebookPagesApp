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

module internal FacebookConverters =
    let streamToUser (contentInfo:ContentInfo, stream:Stream) = async {
        let callingContext = SynchronizationContext.Current
        do! Async.SwitchToThreadPool ()

        use stream = stream
        use sr = new StreamReader(stream)
        let o = JToken.ReadFrom(new JsonTextReader(sr)) :?> JObject
        let firstName = string o.["first_name"]

        do! Async.SwitchToContext callingContext

        return (contentInfo, { firstName = firstName })
    }

[<Sealed>]
type FacebookClient (httpClient:HttpClient<Stream,Stream>, tokenProvider:unit->string) =
    let profileClient = httpClient |> HttpClient.usingConverters (Converters.fromUnitToStream, SplatConverters.streamToIBitmap) 
    let userInfoClient = httpClient |> HttpClient.usingConverters (Converters.fromUnitToStream, FacebookConverters.streamToUser)

    let withAuthorization req =
        let authorizationCredentials = Challenge.OAuthToken <| tokenProvider()
        req |> HttpRequest.withAuthorization authorizationCredentials

    member this.ListPages () =
        let request = 
            let uri = new System.Uri("https://graph.facebook.com/v2.0/me/accounts")
            let authorizationCredentials = Challenge.OAuthToken <| tokenProvider()
            HttpRequest<Stream>.Create(Method.Get, uri, Stream.Null, authorization = authorizationCredentials)

        async {
            let! response = request |> httpClient
            return response.Entity
        }

    member this.CreatePost post = ()

    member this.ListPosts () = ()

    member this.ProfilePhoto = async {
        let request =
            let uri = Uri("https://graph.facebook.com/v2.0/me/picture?type=large")
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        let! response = request |> profileClient
        return response.Entity
    }

    member this.UserInfo = async {
        let request =
            let uri = new System.Uri("https://graph.facebook.com/v2.0/me")
            HttpRequest<unit>.Create(Method.Get, uri, ()) |> withAuthorization
        let! response = request |> userInfoClient
        return response.Entity
    }



       