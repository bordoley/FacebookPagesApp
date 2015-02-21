namespace FacebookPagesApp
open System
open System.IO
open System.Collections.Generic
open System.Reactive.Disposables
open System.Reactive.Linq;
open System.Reactive.Subjects;
open RxApp
open FSharp.Control.Reactive
open System.Threading
open FunctionalHttp.Core
open FunctionalHttp.Client
open FacebookAPI
open FSharpx.Collections

type HttpClient<'TReq, 'TResp> = HttpRequest<'TReq> -> Async<HttpResponse<'TResp>>

module ApplicationController =
    let private loginController (vm: ILoginControllerModel) (sessionManager:ISessionManager) =
        vm.Login 
        |> Observable.iter (fun _ -> vm.CanLogin.Value <- false)
        |> Observable.bind (fun _ -> sessionManager.Login |> Async.toObservable)
        |> Observable.iter (fun result -> 
            if result = Failed 
            then 
                vm.LoginFailed.Execute()
                vm.CanLogin.Value <- true)
        |> Observable.subscribe (fun _ -> ())

    let private pagesController (vm:IPagesControllerModel) (navStack:INavigationStack) (sessionManager:ISessionManager) (httpClient:HttpClient<Stream,Stream>) =
        // FIXME: This is what should be injected
        let facebookClient = FacebookClient(httpClient, fun () -> sessionManager.AccessToken)

        async {
            // FIXME: Ideally we cache the image in SQLite and check if its available before making the http request. Also need 
            // some retry logic here.
            let! profilePhoto = facebookClient.ProfilePhoto
            match profilePhoto with
            | Choice1Of2 bitmap -> 
                vm.ProfilePhoto.Value <- bitmap
            | _ -> ()
        } |> Async.StartImmediate

        async {
            // FIXME: Ideally we cache the image in SQLite and check if its available before making the http request. Also need 
            // some retry logic here.
            let! userInfo = facebookClient.UserInfo
            match userInfo with
            | Choice1Of2 userInfo -> 
                vm.UserName.Value <- userInfo.firstName 
            | _ -> ()
        } |> Async.StartImmediate

        async {
            // FIXME: Ideally we cache the image in SQLite and check if its available before making the http request. Also need 
            // some retry logic here.
            let! pages = facebookClient.ListPages
            match pages with
            | Choice1Of2 pages -> 
                vm.Pages.Value <- System.Collections.Generic.List(pages)
            | _ ->()
        } |> Async.StartImmediate
      
        //let requestLock
        Disposable.Combine(
            vm.LogOut |> Observable.subscribe (fun _ -> 
                sessionManager.Logout |> Async.StartImmediate),

            vm.CreatePost
                |> Observable.map (fun _ -> (vm.CurrentPage.FirstAsync().Wait(), vm.Pages.FirstAsync().Wait()))
                |> Observable.filter (fun (currentPage, _) -> Option.isSome currentPage)
                |> Observable.map (fun (currentPage, pages) -> (currentPage.Value, pages))
                |> Observable.map (fun (currentPage, pages) ->  
                    new NewPostModel(pages,  currentPage) :> INavigableControllerModel)
                |> Observable.subscribe (fun x -> navStack.Push x),

            // First load of the data
            Observable.CombineLatest(vm.CurrentPage, vm.ShowUnpublishedPosts)
                // Clear the posts, and prevent the user from trying to refresh or load more
                |> Observable.iter (fun _ -> 
                    vm.CanLoadMorePosts.Value <- false)

                // FIXME: Try to grab the requestLock otherwise abandon

                // Nothing to do if the current page is None
                |> Observable.filter (fun (currentPage, _) -> Option.isSome currentPage)
                |> Observable.map (fun (currentPage, showUnpublishedPosts) -> (currentPage.Value, showUnpublishedPosts))

                // Throttle a second
                |> Observable.throttle (TimeSpan(0,0,1))

                |> Observable.bind (fun (currentPage, showUnpublishedPosts) -> 
                    facebookClient.ListPosts(currentPage.id, showUnpublishedPosts) |> Async.toObservable)
                |> Observable.iter (fun x ->
                    match x with
                    | Choice1Of2 result -> 
                        vm.Posts.Value <- result
                    | _ -> 
                        // Else use command to send error message
                        ())

                // Unblock trying to load more or refresh
                |> Observable.iter (fun _ -> 
                    vm.CanLoadMorePosts.Value <- true)

                // FIXME: Release the AsyncLock

                |> Observable.subscribe (fun _ -> ()),


            vm.LoadMorePosts

                // Get the current page, whether to show unpublished posts and the current posts being displayed
                |> Observable.map (fun _ -> (vm.CurrentPage.FirstAsync().Wait(), vm.ShowUnpublishedPosts.FirstAsync().Wait(), vm.Posts.FirstAsync().Wait()))
                |> Observable.filter(fun (currentPage, _, _) -> Option.isSome currentPage)
                |> Observable.map(fun (currentPage, showUnpublishedPosts, posts) -> (currentPage.Value, showUnpublishedPosts, posts))

                // FIXME: Try to grab the requestLock otherwise abandon

                // Prevent further requests for data
                |> Observable.iter (fun _ -> 
                    vm.CanLoadMorePosts.Value <- false)
                |> Observable.delay (TimeSpan(0, 0, 5))

                // Go and load the posts
                // FIXME: There is a race condition if the user changes the page or the show unpublished posts at this point
                // To address it we should pass in a cancellation token to cancel the request at this point
                // and abandon the transaction
                |> Observable.bind (fun (currentPage, showUnpublishedPosts, posts) -> 
                    facebookClient.LoadMorePostsBefore(posts) |> Async.toObservable)
                |> Observable.iter (fun x ->
                    match x with
                    | Choice1Of2 result -> 
                        vm.Posts.Value <- result
                    | _ -> 
                        // Else use command to send error message
                        ())

                // Unblock trying to load more or refresh
                |> Observable.iter (fun _ -> 
                    vm.CanLoadMorePosts.Value <- true)

                // FIXME: Release the AsyncLock

                |> Observable.subscribe (fun _ -> ()),

            vm.RefreshPosts

                // Get the current page, whether to show unpublished posts and the current posts being displayed
                |> Observable.map (fun _ -> 
                    (vm.CurrentPage.FirstAsync().Wait(), vm.ShowUnpublishedPosts.FirstAsync().Wait(), vm.Posts.FirstAsync().Wait()))
                |> Observable.filter(fun (currentPage, _, _) -> 
                    Option.isSome currentPage)
                |> Observable.map(fun (currentPage, showUnpublishedPosts, posts) -> 
                    (currentPage.Value, showUnpublishedPosts, posts))


                // FIXME: Try to grab the requestLock otherwise abandon

                // Prevent further requests for data
                |> Observable.iter (fun _ -> 
                    vm.CanLoadMorePosts.Value <- false
                    vm.ShowRefresher.Value <- true)
               
                // Go and load the posts
                // FIXME: There is a race condition if the user changes the page or the show unpublished posts at this point
                // To address it we should pass in a cancellation token to cancel the request at this point
                // and abandon the transaction
                |> Observable.bind (fun (currentPage, showUnpublishedPosts, posts) -> 
                    facebookClient.RefreshPostsAfter(posts) |> Async.toObservable)
                
                |> Observable.iter (fun x ->
                    match x with
                    | Choice1Of2 result -> 
                        vm.Posts.Value <- result
                    | _ -> 
                        // Else use command to send error message
                        ())

                // Unblock trying to load more or refresh
                |> Observable.iter (fun _ -> 
                    vm.CanLoadMorePosts.Value <- true
                    vm.ShowRefresher.Value <- false)

                // FIXME: Release the AsyncLock

                |> Observable.subscribe (fun _ -> ())
        )

    let private newPostController (vm:INewPostControllerModel) =
        Observable.CombineLatest(vm.PublishPost, vm.Page, vm.PublishDate, vm.PublishTime, vm.PostContent, vm.ShouldPublishPost)
        |> Observable.iter (fun _ -> vm.CanPublishPost.Value <- false)
        |> Observable.map (fun (_, page, publishDate, publishTime, content, shouldPublish) ->
            let publishDateTime = 
                DateTime(
                    publishDate.Year, 
                    publishDate.Month, 
                    publishDate.Day,
                    publishTime.Hours, 
                    publishTime.Minutes, 
                    publishTime.Seconds)
            let post = { id = ""; message = content; createdTime = publishDateTime }
            { post = post; page = page; shouldPublish = shouldPublish }) 
        |> Observable.iter (fun _ -> ()) // Send Facebook the post
        |> Observable.observeOnContext SynchronizationContext.Current
        // Check the result, if exception pop up error and set canpublish true otherwise pop the viewmodel
        |> Observable.subscribe (fun _ -> vm.Back.Execute())

    let create (navStack:INavigationStack) (sessionState:IObservable<LoginState>) (sessionManager:ISessionManager) (httpClient:HttpClient<Stream, Stream>) = 
        let subscription : IDisposable ref= ref null                                         

        { new IApplication with
            member this.Init () =
                UnknownStateModel() |> navStack.SetRoot

                subscription :=
                    sessionState 
                    |> Observable.observeOnContext SynchronizationContext.Current
                    |> Observable.map (function
                        // FIXME: Add factories that make this less painful
                        | LoggedIn -> PagesModel() :> INavigableControllerModel
                        | LoggedOut -> LoginModel() :> INavigableControllerModel) 
                    // FIXME: Update NavStack to make binding easier
                    |> Observable.subscribe(fun x -> navStack.SetRoot x)
                 
            member this.Bind (model:obj) = 
                match model with 
                | :? ILoginControllerModel as vm -> loginController vm sessionManager
                | :? IUnknownStateControllerModel as vm -> Disposable.Empty
                | :? IPagesControllerModel as vm -> pagesController vm navStack sessionManager httpClient
                | :? INewPostControllerModel as vm -> newPostController vm
                | _ -> failwith ("Unknown controller model type: " + model.ToString())

            member this.Dispose () = 
                (!subscription).Dispose ()
        }