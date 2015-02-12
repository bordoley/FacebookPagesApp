namespace FacebookSession

open System

open Android.App
open Android.Content
open Android.Util
open Android.Support.V4.Content
open FSharp.Control.Reactive
open Xamarin.Facebook

type LoginResult = Successful | Failed

type ISessionManager = 
    abstract Login: Async<LoginResult> with get
    abstract Logout: Async<unit> with get
    abstract AccessToken:Option<string> with get

type LoginState = LoggedIn | LoggedOut

module FacebookSession =
    type private SessionStatusChangedCallback (cb) =    
        inherit Java.Lang.Object ()
        interface Session.IStatusCallback with 
            member this.Call(session:Session, state:SessionState, ex:Java.Lang.Exception) = cb session

     type private SessionBroadcastReceiver (cb) =
        inherit BroadcastReceiver()

        override this.OnReceive(context:Context, intent:Intent) = cb ()

    type private RequestCallback (cb) =    
        inherit Java.Lang.Object ()
        interface Session.IStatusCallback with 
            member this.Call(session:Session, state:SessionState, ex:Java.Lang.Exception) = 
                match state with 
                | s when s.Equals(SessionState.Opened) -> cb(Successful)
                | s when s.Equals(SessionState.ClosedLoginFailed) -> cb(Failed)
                | _ -> Log.Info("FacebookSessionManager.RequestCallback", session.ToString()) |> ignore
                       Log.Info("FacebookSessionManager.RequestCallback", state.ToString()) |> ignore   

    let private activeSession () =
        match Session.ActiveSession with 
        | null -> None
        | s -> Some s
     

    type internal FacebookSessionManager(activityProvider: unit->Activity) =
        interface ISessionManager with
            member this.Login 
                with get () = Async.FromContinuations(
                                fun (cont, econt, ccont) ->
                                    let session = Session.ActiveSession
                                    let request = new Session.OpenRequest(activityProvider())
                                    request.SetDefaultAudience(SessionDefaultAudience.OnlyMe)
                                           .SetLoginBehavior(SessionLoginBehavior.SsoWithFallback)
                                           //.SetPermissions(null)
                                           .SetCallback(new RequestCallback(cont)) |> ignore
                                    session.OpenForRead(request))
                                
            member this.Logout 
                with get () = async {
                    Session.ActiveSession.CloseAndClearTokenInformation()
                }

            member this.AccessToken 
                with get() = 
                    match activeSession () with
                    | None -> None
                    | Some x -> 
                        match x.AccessToken with
                        | null -> None
                        | at -> Some at
                       
    let private createSession context = (new Session.Builder(context)).SetApplicationId(Settings.ApplicationId).Build()

    let private loginState () =
        match Session.ActiveSession with
        | null -> None
        | s -> Some (if s.IsOpened then LoggedIn else LoggedOut)

    let private currentSession (context:Context) =
        let onSubscribe(observer:IObserver<Option<Session>>) =
            let activeSessionClosedFilter = 
                let intentFilter = new IntentFilter()
                intentFilter.AddAction(Session.ActionActiveSessionClosed)
                intentFilter

            let activeSessionSetFilter = 
                let intentFilter = new IntentFilter()
                intentFilter.AddAction(Session.ActionActiveSessionSet)
                intentFilter

            let activeSessionClosedReceiver = 
                new SessionBroadcastReceiver(fun () ->
                    Session.ActiveSession <- createSession context)

            let activeSessionSetReceiver = new SessionBroadcastReceiver(fun () -> 
               activeSession () |> observer.OnNext)  
 
            let localBroadcastManager = 
                let instance = LocalBroadcastManager.GetInstance context
                instance.RegisterReceiver(activeSessionSetReceiver, activeSessionSetFilter)
                instance.RegisterReceiver(activeSessionClosedReceiver, activeSessionClosedFilter)
                instance

            let dispose() = 
                localBroadcastManager.UnregisterReceiver activeSessionSetReceiver
                localBroadcastManager.UnregisterReceiver activeSessionClosedReceiver

            dispose

        Observable.create onSubscribe

    let observe (context:Context) : IObservable<Option<LoginState>> =
        let onSubscribe(observer:IObserver<Option<LoginState>>) =
            let publishState (session:Session) =
                let state =
                    match session with
                    | null -> None
                    | s -> Some (if s.IsOpened then LoggedIn else LoggedOut)
                observer.OnNext state

            let sessionStatusChangedCallback = new SessionStatusChangedCallback(publishState) :> Session.IStatusCallback

            let currentSessionSubscription = 
                (currentSession context) 
                |> Observable.scanInit (None, None) (fun (_,prev) current -> (prev, current))
                |> Observable.subscribe (fun (prev, current) -> 
                    match prev with
                    | Some session -> session.RemoveCallback sessionStatusChangedCallback
                    | None -> ()

                    match current with
                    | Some session -> session.AddCallback(sessionStatusChangedCallback)
                    | None -> ())

            let dispose () =
                currentSessionSubscription.Dispose()
                match activeSession () with
                | Some session -> session.RemoveCallback sessionStatusChangedCallback
                | _ -> ()

            dispose

        Observable.create onSubscribe

    let getManager (activityProvider:unit->Activity) =
        new FacebookSessionManager(activityProvider) :> ISessionManager

    let onActivityResultDelegate (activity:Activity) requestCode (resultCode:Result) data =
        let session = Session.ActiveSession
        session.OnActivityResult(activity, requestCode, (int32 resultCode), data) |> ignore

        // Work around a bug in FB's Session class where ActionActiveSessionClosed isn't
        // broadcasted when the user cancels the login attempt
        match session.State with
        | s when s = SessionState.Closed ||
                 s = SessionState.ClosedLoginFailed -> 
                    Session.ActiveSession <- createSession activity.ApplicationContext
        | _ -> ()