module FSharp.Reactive.Tests.IOSpecs

open System
open System.IO
open FSharp.Control.Reactive
open FSharp.Control.Reactive.IO
open NUnit.Framework
open System.Net
open System.Threading

let createDir sub = 
    let dir = Path.Combine(Environment.CurrentDirectory, sub)
    Directory.CreateDirectory dir |> ignore
    Directory.GetFiles dir |> Seq.iter File.Delete
    dir

[<Test>]
[<Category("IO")>]
let ``file is picked up and stored during subscription`` () =
    let incoming = createDir "in"
    let outgoing = createDir "out"
    let wait = WaitHandle.Signal

    File.createdAny incoming
    |> File.create
        (fun x -> Path.Combine(outgoing, x.Name)) 
        (fun x -> WaitHandle.flag wait
                  File.OpenRead x.FullPath)
    |> ignore

    let testFileName = Guid.NewGuid().ToString() + ".dat"
    File.WriteAllText (Path.Combine(incoming, testFileName), "test")
    Assert.True (wait.WaitOne (TimeSpan.FromSeconds 5.))

[<Test>]
[<Category("IO")>]
let ``file should be appended during subscription`` () =
    let incoming = createDir "in"
    let outgoing = createDir "out"

    let testFileName = Guid.NewGuid().ToString() + ".dat"
    let inTestFileName = Path.Combine(incoming, testFileName)
    let outTestFileName = Path.Combine(outgoing, testFileName)
    File.WriteAllText (inTestFileName, "")
    File.WriteAllText (outTestFileName, "hello ")
        
    let wait = WaitHandle.Signal

    File.modifiedAny incoming
    |> File.append
        (fun _ -> outTestFileName) 
        (fun x -> WaitHandle.flag wait
                  File.OpenRead x.FullPath)
    |> ignore

    Thread.Sleep (TimeSpan.FromMilliseconds 100.)
    File.AppendAllText (inTestFileName, "world!")
    Assert.True (wait.WaitOne (TimeSpan.FromSeconds 5.))
    Thread.Sleep (TimeSpan.FromMilliseconds 100.)
    Assert.True(File.ReadAllText outTestFileName |> fun s -> s.Contains "world!")

[<Test>]
[<Category("IO")>]
let ``web request is picked up`` () =
    let host = "http://localhost:1234/"
    Http.receive host
    |> Observable.subscribe (fun ctx ->
        ctx.Response.StatusCode <- 200
        ctx.Response.AddHeader ("my-header", "my-header-value")
        ctx.Response.Close ())
    |> ignore

    Http.get host
    |> Observable.toEnumerable
    |> Seq.head
    |> fun res ->
        Assert.AreEqual (HttpStatusCode.OK, res.StatusCode)
        Assert.AreEqual ("my-header-value", res.Headers.Get "my-header")