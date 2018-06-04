
// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
    #r "netstandard"
#endif

open Fake.Tools
open Fake.Core
open Fake.DotNet
open Fake.BuildServer
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open System.Threading
// --------------------------------------------------------------------------------------
// Provide project-specific details below
// --------------------------------------------------------------------------------------

let projects = [ "src/FSharp.Control.Reactive/FSharp.Control.Reactive.fsproj" 
                 "src/FSharp.Control.Reactive.Testing/FSharp.Control.Reactive.Testing.fsproj" ]
// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "git@github.com:fsprojects"
// The name of the project on GitHub
let gitName = "FSharp.Control.Reactive"

let buildDir = Path.GetFullPath "bin"

let docsDir = Path.GetFullPath "docs"

// --------------------------------------------------------------------------------------
// The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document

let release = ReleaseNotes.load  "RELEASE_NOTES.md"
let isAppVeyorBuild = Environment.environVar "APPVEYOR" |> isNull |> not
let isTaggedBuild = Environment.environVarAsBoolOrDefault "APPVEYOR_REPO_TAG" false
let versionSuffix =
    let buildVersion = BuildServer.buildVersion
    if isTaggedBuild then ""
    elif buildVersion = "LocalBuild" then "-LocalBuild"
    else "-beta" + buildVersion
let nugetVersion = release.NugetVersion + versionSuffix

BuildServer.install [
    AppVeyor.Installer
]
// Generate assembly info files with the right version & up-to-date information
Target.create "BuildVersion" (fun _ ->
    Shell.Exec("appveyor", sprintf "UpdateBuild -Version \"%s\"" nugetVersion) |> ignore
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "bin"
    ++ "temp"
    |> Shell.cleanDirs 
)

Target.create "CleanDocs" (fun _ ->
    Shell.cleanDir "docs/output"
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    Environment.setEnvironVar "GenerateDocumentationFile" "true"
    projects
    |> List.iter (DotNet.build id)

)

Target.create "CopyLicense" (fun _ ->
    [ "LICENSE.txt" ] |> Shell.copyTo "bin"
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target.create "RunTests" (fun _ ->
        DotNet.test (fun p -> { p with Configuration = DotNet.BuildConfiguration.Release}) "tests"
        Trace.publish (ImportData.Nunit NunitDataVersion.Nunit3) buildDir
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "Pack" (fun _ ->
    Environment.setEnvironVar "PackageVersion" nugetVersion
    Environment.setEnvironVar "Version" nugetVersion
    Environment.setEnvironVar "PackageReleaseNotes" (release.Notes |> String.toLines)
    projects
    |> List.iter (DotNet.pack (fun p -> 
            { p with
                OutputPath = Some buildDir
                NoBuild = true
            }) )
)

Target.create "Push" (fun _ ->
    Paket.push (fun p -> 
        { p with
            WorkingDir = buildDir }) 
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target.create "GenerateDocs" (fun _ ->
    
    let githubLink = "https://github.com/fsprojects/FSharp.Control.Reactive"
    // Paths with template/source/output locations

    let content    = docsDir @@ "content"
    let output     = docsDir @@ "output"
    let outputContent = output @@ "content"
    let outputReference = output @@ "reference"
    let files      = docsDir @@ "files"
    let templates  = docsDir @@ "tools/templates"
    let formatting = Path.GetFullPath "packages/build/FSharp.Formatting.CommandTool"
    let docTemplate = formatting @@ "templates/docpage.cshtml"

    // Where to look for *.csproj templates (in this order)
    let layoutRoots = [ templates; formatting @@ "templates"
                        formatting @@ "templates/reference" ]
    let info =
      [ "project-name", "FSharp.Control.Reactive"
        "project-author", "Ryan Riley, Steffen Forkmann, and Jared Heseter"
        "project-summary", "A F#-friendly wrapper for the Reactive Extensions."
        "project-github", "http://github.com/fsprojects/FSharp.Reactive"
        "project-nuget", "https://www.nuget.org/packages/FSharp.Control.Reactive" ]

    Shell.copyDir output files FileFilter.allFiles
    Directory.ensure outputContent
    Shell.copyDir outputContent (formatting @@ "styles") FileFilter.allFiles         
    FSFormatting.createDocs (fun s ->
        { s with
            Source = content
            OutputDirectory = output
            Template = docTemplate
            ProjectParameters = info
            LayoutRoots = layoutRoots })

    Directory.ensure outputReference
    let dlls =
        !! "src/FSharp.Control.*/bin/Release/**/FSharp.Control.Reactive*.dll"
        |> Seq.distinctBy Path.GetFileName
        |> List.ofSeq
    let libDirs =
        dlls
        |> Seq.map Path.GetDirectoryName
        |> Seq.distinct
        |> List.ofSeq

    dlls
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = outputReference
            LayoutRoots =  layoutRoots
            LibDirs = libDirs
            ProjectParameters = info
            SourceRepository = githubLink @@ "tree/master" })
)

Target.create "HostDocs" (fun _ ->
    let configureApp (app:IApplicationBuilder) = 
       app.UseFileServer(FileServerOptions(EnableDefaultFiles = true))
       |> ignore
    let port = 8081
    let directory = docsDir @@ "output"
    let host = 
        WebHostBuilder()
            .UseWebRoot(directory)
            .UseContentRoot(directory)
            .PreferHostingUrls(false)
            .UseKestrel(fun o -> o.ListenLocalhost(port))
            .Configure(System.Action<IApplicationBuilder> configureApp)
            .Build()
    use cts =  new CancellationTokenSource()        
    host.RunAsync(cts.Token) |> ignore
    let psi = System.Diagnostics.ProcessStartInfo(sprintf "http://localhost:%d/index.html" port)
    psi.UseShellExecute <- true
    System.Diagnostics.Process.Start (psi) |> ignore
    Trace.traceImportant "Press any key to stop."
    System.Console.ReadKey() |> ignore
    cts.Cancel()   
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target.create "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    Shell.cleanDir tempDocsDir
    Git.Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    Shell.copyRecursive "docs/output" tempDocsDir true |> Trace.tracefn "%A"
    Git.Staging.stageAll tempDocsDir
    Git.Commit.exec tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Git.Branches.push tempDocsDir
)

Target.create "Release" (fun _ ->
    Git.Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bump version to %s" release.NugetVersion)
    Git.Branches.push ""

    Git.Branches.tag "" release.NugetVersion
    Git.Branches.pushTag "" "origin" release.NugetVersion
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore
open Fake.Core.TargetOperators

"Clean"
  =?> ("BuildVersion", isAppVeyorBuild)
  ==> "CopyLicense"
  ==> "Build"
  ==> "RunTests"
  ==> "All"
  =?> ("GenerateDocs", BuildServer.isLocalBuild && not Environment.isMono)
  =?> ("ReleaseDocs", BuildServer.isLocalBuild && not Environment.isMono)

"All" 
  ==> "Pack"
  ==> "Push"

"CleanDocs"
  ==> "GenerateDocs"
(*    
"ReleaseDocs"
  ==> "Release"
*)
"Push"
  ==> "Release"

Target.runOrDefault "All"
