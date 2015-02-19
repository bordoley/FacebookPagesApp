namespace FSharp.Control.Reactive

open System
open System.Reactive.Linq;
open System.Reactive.Threading.Tasks;
open System.Threading;
open System.Threading.Tasks;


module Async =
    let createWithTask subscribe =
        Observable.Create(Func<IObserver<'Result>, CancellationToken, Task> subscribe)

    let toObservable (comp:Async<'T>) =
        let subscribe (observer:IObserver<'T>) (ct:CancellationToken) =
            let computation = async {
                let! result = comp |> Async.Catch
                match result with
                | Choice1Of2 result -> 
                    observer.OnNext(result)
                | Choice2Of2 exn -> 
                    observer.OnError exn
            } 

            Async.StartAsTask(computation, cancellationToken = ct) :> Task

        createWithTask subscribe