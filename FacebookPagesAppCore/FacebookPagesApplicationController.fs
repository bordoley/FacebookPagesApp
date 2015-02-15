namespace FacebookPagesApp
open System
open System.IO
open System.Collections.Generic
open System.Reactive.Disposables
open RxApp
open FSharp.Control.Reactive
open System.Threading
open FunctionalHttp.Core
open FunctionalHttp.Client
open FacebookAPI

type HttpClient<'TReq, 'TResp> = HttpRequest<'TReq> -> Async<HttpResponse<'TResp>>

module ApplicationController =
    let private loginController (vm: ILoginControllerModel) (sessionManager:ISessionManager) =
        let canLogin : bool ref = ref true

        let setCanLogin value =
            canLogin := value
            vm.CanLogin <- value

        let doLogin () = async { 
            setCanLogin false
            let! loginResult = sessionManager.Login

            // If login succeeds there is no need to set canLogin to true
            // as the app will switch states to the loggedIn state.
            // If we set it back to true, users could actually trigger
            // a crash by reclicking the button causing an attempt to reopen 
            // and already open session.
            if loginResult = Failed 
            then 
                setCanLogin true
                vm.LoginFailed.Execute()
        }

        let subscription = new CompositeDisposable()

        subscription.Add (
            vm.Login 
            |> Observable.filter (fun _ -> !canLogin)

            // Protect against multiple calls to execute due to
            // users clicking the login button several times in a row.
            // Results in crashes due to an attempt to reopen 
            // and already open session.
            |> Observable.subscribe (fun _ -> doLogin () |> Async.StartImmediate)
        )

        subscription :> IDisposable

    let private pagesController (vm:IPagesControllerModel) (navStack:INavigationStack) (sessionManager:ISessionManager) (httpClient:HttpClient<Stream,Stream>) =
        let facebookClient = FacebookClient(httpClient, fun () -> sessionManager.AccessToken)

        async {
            // FIXME: Ideally we cache the image in SQLite and check if its available before making the http request. Also need 
            // some retry logic here.
            let! profilePhoto = facebookClient.ProfilePhoto
            match profilePhoto with
            | Choice1Of2 bitmap -> 
                vm.ProfilePhoto <- bitmap
            | _ ->()
        } |> Async.StartImmediate

        async {
            // FIXME: Ideally we cache the image in SQLite and check if its available before making the http request. Also need 
            // some retry logic here.
            let! userInfo = facebookClient.UserInfo
            match userInfo with
            | Choice1Of2 userInfo -> 
                vm.UserName <- userInfo.firstName 
            | _ ->()
        } |> Async.StartImmediate

        let retval = new CompositeDisposable()
        retval.Add (vm.CreatePost |> Observable.subscribe (fun _ -> navStack.Push (NewPostModel())))
        retval.Add (vm.LogOut |> Observable.subscribe(fun _ -> sessionManager.Logout |> Async.StartImmediate))

        retval :> IDisposable

    let private newPostController (vm:INewPostControllerModel) (navStack:INavigationStack) =
        vm.PublishPost 
        |> Observable.map (fun _ -> 
            DateTime(
                vm.PublishDate.Year, 
                vm.PublishDate.Month, 
                vm.PublishDate.Day, 
                vm.PublishTime.Hours, 
                vm.PublishTime.Minutes, 
                vm.PublishTime.Seconds)) 
        |> Observable.subscribe(fun _ -> ())

    // FIXME: Ideally I want this to be a PCL and to have a FunctionalHTtpClient passed in, but I'm getting all kinds of assembly issues in FunctionalPagesAppCore with 
    // F# types so fuck it.
    let create (navStack:INavigationStack) (sessionState:IObservable<LoginState>) (sessionManager:ISessionManager) (httpClient:System.Net.Http.HttpClient) = 
        let subscription : IDisposable ref= ref null    
        let httpClient = HttpClient.fromNetHttpClient httpClient                                      

        { new IApplication with
            member this.Init () =
                UnknownStateModel() |> navStack.SetRoot

                subscription :=
                    sessionState 
                    |> Observable.observeOnContext SynchronizationContext.Current
                    |> Observable.subscribe (fun state ->
                        match state with
                        | LoggedIn -> 
                            PagesModel() |> navStack.SetRoot
                        | LoggedOut -> 
                            LoginModel() |> navStack.SetRoot)
                 
            member this.Bind (model:obj) = 
                match model with 
                | :? ILoginControllerModel as vm -> loginController vm sessionManager
                | :? IUnknownStateControllerModel as vm -> Disposable.Empty
                | :? IPagesControllerModel as vm -> pagesController vm navStack sessionManager httpClient
                | :? INewPostControllerModel as vm -> newPostController vm navStack
                | _ -> failwith ("Unknown controller model type: " + model.ToString())

            member this.Dispose () = 
                (!subscription).Dispose ()
        }