namespace FacebookPagesApp
open System
open System.Collections.Generic
open System.Reactive.Disposables
open RxApp
open FSharp.Control.Reactive

module Controllers =
    let loginController (vm: ILoginControllerModel) (sessionManager:ISessionManager) =
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

        subscription

type FacebookPagesApplicationController(navStack:INavigationStack,
                                        sessionState:IObservable<Option<LoginState>>,
                                        sessionManager:ISessionManager) = 

    let mutable subscription :IDisposable = null                                            

    member this.Init () =
        subscription <-
            sessionState 
            |> Observable.subscribe (fun state ->
                match state with
                | Some LoggedIn -> ()
                | Some LoggedOut -> LoginModel() |> navStack.SetRoot
                | _ -> ())

    member this.Bind (model:obj) = 
        match model with 
        | :? ILoginControllerModel as vm -> Controllers.loginController vm sessionManager
        | _ -> failwith ("Unknown controller model type: " + model.ToString())

    interface IDisposable with
        member this.Dispose () = 
            subscription.Dispose ()