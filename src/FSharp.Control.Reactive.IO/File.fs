namespace FSharp.Control.Reactive.IO

module File =
    open System
    open System.IO
    open System.Reactive.Linq

    let private any = "*.*"

    let private fileWatch path pattern () =
        let w = new FileSystemWatcher (path, pattern)
        w.EnableRaisingEvents <- true
        w.IncludeSubdirectories <- true
        w.NotifyFilter <- NotifyFilters.LastAccess 
                          ||| NotifyFilters.LastWrite 
                          ||| NotifyFilters.FileName 
                          ||| NotifyFilters.DirectoryName 
                          ||| NotifyFilters.CreationTime
        w

    let private fileEvent f path ext =
        Observable.Using ((fileWatch path ext), (fun w -> f w :> IObservable<'a>))

    /// `IObservable<_>` that emits values when a file with a specified search pattern is created.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    ///  * `pattern` - Search pattern that the files must have.
    let created = fileEvent (fun w -> w.Created)
    
    /// `IObservable<_>` that emits values when a any file is created.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    let createdAny path = created path any

    /// `IObservable<_>` that emits values when a file is modified.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    ///  * `pattern` - Search pattern that the files must have.
    let modified = fileEvent (fun w -> w.Changed)

    /// `IObservable<_>` that emits values when a file is modified.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    ///  * `pattern` - Search pattern that the files must have.
    let modifiedAny path = modified path any 

    let private (<|>) = Observable.merge

    /// `IObservable<_>` that emits values when a file is created or modified.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    ///  * `pattern` - Search pattern that the files must have.
    let createdOrModified path ext = created path ext <|> modified path ext

    /// `IObservable<_>` that emits values when a file is created or modified.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    ///  * `pattern` - Search pattern that the files must have
    let createdOrModifiedAny path = createdOrModified path any

    /// `IObservable<_>` that emits values when a file is deleted.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    ///  * `pattern` - Search pattern that the files must have.
    let deleted = fileEvent (fun w -> w.Deleted)

    /// `IObservable<_>` that emits values when any file is deleted.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    let deletedAny path = deleted path any

    /// `IObservable<_>` that emits values when a file is renamed.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    ///  * `pattern` - Search pattern that the files must have.
    let renamed = fileEvent (fun w -> w.Renamed)

    /// `IObservable<_>` that emits values when any file is renamed.
    /// ## Parameters
    ///  * `path` - Path to listen for files.
    let renamedAny path = renamed path any

    /// Subsribe the 'OnNext' calls to a new file if it doesn't exists or overrides an existing.
    /// ## Parameters
    /// - `valueOf` - Function to find out the contents (`Stream`) to be written to the file.
    /// - `createDes` - Function to create a custom `FileStream` for a incoming emit.
    /// - `source` - Source observable to use to write files for each emit.
    let write valueOf createDes source =
        let writeNext x =
            try use src = valueOf x : #Stream
                use des = createDes x : FileStream
                src.CopyTo des
                des.Flush ()    
            with _ -> ()
        
        Observable.subscribe writeNext source

    /// Subsribe the 'OnNext' calls to a new file if it doesn't exists or overrides an existing.
    /// ## Parameters
    /// - `nameOf` - Function to find out the full path name of the file.
    /// - `valueOf` - Function to find out the contents (`Stream`) to be written to the file.
    /// - `source` - Source observable to use to write files for each emit.
    let create nameOf valueOf source =
        write valueOf (nameOf >> File.Create) source

    /// Subsribe the 'OnNext' calls to a new file if it doesn't exists or overrides an existing.
    /// ## Parameters
    /// - `nameOf` - Function to find out the full path name of the file.
    /// - `valueOf` - Function to find out the contents (`Stream`) to be written to the file.
    /// - `source` - Source observable to use to write files for each emit.
    let append nameOf valueOf source =
        write valueOf (nameOf >> fun n -> File.Open (n, FileMode.Append)) source


    let appendText nameOf txt source =
        append nameOf (fun _ -> new MemoryStream(System.Text.Encoding.UTF8.GetBytes(txt : string))) source
