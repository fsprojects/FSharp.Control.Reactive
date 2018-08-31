namespace FSharp.Control.IO

module Network =
    open System
    open System.Net.NetworkInformation
    open System.Reactive.Linq
    open FSharp.Control.Reactive
    open FSharp.Control.Reactive.Observable

    /// Gets an observable that will notify the subscriber of current network availability and then any changes to network availability.
    let availability =
        Observable.FromEventPattern<NetworkAvailabilityChangedEventHandler, EventArgs>(
            NetworkChange.NetworkAvailabilityChanged.AddHandler, 
            NetworkChange.NetworkAvailabilityChanged.RemoveHandler)
        |> map (fun _ -> NetworkInterface.GetIsNetworkAvailable ())
        |> publishInitial (NetworkInterface.GetIsNetworkAvailable ())
        |> refCount
        |> distinctUntilChanged