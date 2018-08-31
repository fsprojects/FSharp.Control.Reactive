namespace FSharp.Control.Reactive.IO

open System


module Concurrent =
    
    let maxDepth =
        int (Math.Log (float Environment.ProcessorCount, 2.0))

    let totalWorkers =
        int (2.0 ** float maxDepth)

    let defaultMaxConcurrency =
        let worker = ref 0
        let io = ref 0
        System.Threading.ThreadPool.GetMaxThreads (worker, io)
        (!io / 8) * Environment.ProcessorCount

open System.Net
open System.Reactive.Linq
open System.Reactive.Disposables
open FSharp.Control.Reactive    

module Http =

    let private receiveMaxWith getRequest max (l : HttpListener)  =
        Observable.Create (fun (o : IObserver<_>) ->
            try if not l.IsListening 
                then Some (l.Start ()) 
                else None
            with ex -> o.OnError ex; None
            |> Option.map (fun () ->
                getRequest
                |> Observable.serve max
                |> Observable.finallyDo (fun () ->
                    try l.Stop () with _ -> ())
                |> Observable.subscribeSafeObserver o)
            |> Option.defaultValue Disposable.Empty)

    /// Receive concurrent HTTP requests in a Observable.
    /// ## Parameters
    /// - `max` - The maximum number of requests that can be pushed through the observable simultaneously.
    /// - `l` - HTTP listener that listens for requests.
    let receiveMax max (l : HttpListener) =
        receiveMaxWith (fun () -> Observable.StartAsync l.GetContextAsync) max l 
    
    /// Receive concurrent HTTP requests in a Observable.
    /// ## Parameters
    /// - `uri` - HTTP Uri to which the `Observable` should listen to.
    let receive uri =
        Observable.using
            (fun () -> let l = new HttpListener () in l.Prefixes.Add uri; l)
            (fun l -> receiveMax Concurrent.defaultMaxConcurrency l)
    
    /// Sends out a HTTP request and get the response as a observable sequence.
    let sendCustom (req : HttpWebRequest) =
        req.AsyncGetResponse ()
        |> Observable.ofAsync
        |> Observable.map (fun x -> x :?> HttpWebResponse)
    
    /// Sends out a HTTP GET request for every `OnNext` emit of the specified `source` observable sequence.
    /// ## Parameters
    /// - `uri` - Endpoint to which the request must be send to.
    /// - `source` - Observable sequence to trigger the get request.
    let get uri =
        let req = WebRequest.CreateHttp (uri : string)
        req.Method <- "GET"
        sendCustom req
   
    /// Sends out a HTTP POST request for every `OnNext` emit of the specified `source` observable sequence.
    /// ## Parameters
    /// - `uri` - Uri to which the `OnNext` emit content should be send to.
    /// - `getContents` - Function to get the content that should be send with the request.
    /// - `source` - Observable sequence used to send out HTTP requests.
    let postContents uri (contents : IO.Stream) =
        let req = WebRequest.CreateHttp (uri : Uri)
        req.Method <- "POST"
        use des = req.GetRequestStream ()
        
        async {
            do! contents.CopyToAsync des |> Async.AwaitTask
            return! req.AsyncGetResponse () }
        |> Observable.ofAsync