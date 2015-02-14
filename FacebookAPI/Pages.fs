namespace FacebookAPI
open System
open System.IO

open FunctionalHttp.Core
open FunctionalHttp.Client

type HttpClient<'TReq, 'TResp> = HttpRequest<'TReq> -> Async<HttpResponse<'TResp>>

type FacebookHttpClient (httpClient:HttpClient<Stream,Stream>, tokenProvider:unit->string) =
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


       