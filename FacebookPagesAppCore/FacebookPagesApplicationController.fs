namespace FacebookPagesApp
open System
open System.IO
open System.Collections.Generic
open System.Reactive.Disposables
open System.Reactive.Linq;
open System.Reactive.Subjects;
open RxApp
open FSharp.Control.Reactive
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

    let private pagesController (vm:IPagesControllerModel) (sessionManager:ISessionManager) (httpClient:HttpClient<Stream,Stream>) =
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
                |> Observable.bind (fun _ -> 
                    Observable.CombineLatest(vm.CurrentPage, vm.Pages) 
                    |> Observable.first)
                |> Observable.filter(fun(_, pages) -> pages.Count <> 0)
                |> Observable.filter (fun (currentPage, _) -> Option.isSome currentPage)
                |> Observable.map (fun (currentPage, pages) -> (currentPage.Value, pages))
                |> Observable.map (fun (currentPage, pages) ->  
                    new NewPostModel(pages,  currentPage) :> INavigationModel)
                |> Observable.subscribe (fun x -> vm.Open.Execute(x)),

            // First load of the data
            Observable.CombineLatest(vm.CurrentPage, vm.ShowUnpublishedPosts)
                // Clear the posts, and prevent the user from trying to refresh or load more
                |> Observable.iter (fun _ -> 
                    vm.CanCreatePost.Value <- true
                    vm.CanLoadMorePosts.Value <- false)

                // FIXME: Try to grab the requestLock otherwise abandon

                // Nothing to do if the current page is None
                |> Observable.filter (fun (currentPage, _) -> Option.isSome currentPage)
                |> Observable.map (fun (currentPage, showUnpublishedPosts) -> (currentPage.Value, showUnpublishedPosts))

                // Throttle a second
                |> Observable.throttle (TimeSpan(0,0,1))

                |> Observable.bind (fun (currentPage, showUnpublishedPosts) -> 
                    facebookClient.ListPosts(currentPage, showUnpublishedPosts) |> Async.toObservable)
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

    let private newPostController (vm:INewPostControllerModel)  (sessionManager:ISessionManager) (httpClient:HttpClient<Stream,Stream>) =
        // FIXME: This is what should be injected
        let facebookClient = FacebookClient(httpClient, fun () -> sessionManager.AccessToken)

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
            {page = page; post = post; shouldPublish = shouldPublish })
        |> Observable.bind (fun createPostData  -> 
            // Send Facebook the post
            facebookClient.CreatePost createPostData |> Async.toObservable)
 
        // Check the result, if exception pop up error and set canpublish true otherwise pop the viewmodel
        |> Observable.subscribe (fun _ -> vm.Back.Execute())

    let create (sessionState:IObservable<LoginState>) (sessionManager:ISessionManager) (httpClient:HttpClient<Stream, Stream>) = 
        let subscription : IDisposable ref= ref null                                         

        { new IApplication with
            member this.ResetApplicationState 
                with get () =
                    sessionState 
                    // FIXME: Observe on the context so that we for a mainloop hop on every change.
                    |> Observable.observeOnContext System.Threading.SynchronizationContext.Current
                    |> Observable.map (function
                        // FIXME: Add factories that make this less painful
                        | LoggedIn -> PagesModel() :> INavigationModel
                        | LoggedOut -> LoginModel() :> INavigationModel) 
                    |> Observable.startWith ([UnknownStateModel() :> INavigationModel])
                 
            member this.Bind (model:obj) = 
                match model with 
                | :? ILoginControllerModel as vm -> loginController vm sessionManager
                | :? IUnknownStateControllerModel as vm -> Disposable.Empty
                | :? IPagesControllerModel as vm -> pagesController vm sessionManager httpClient
                | :? INewPostControllerModel as vm -> newPostController vm sessionManager httpClient
                | _ -> failwith ("Unknown controller model type: " + model.ToString())
        }