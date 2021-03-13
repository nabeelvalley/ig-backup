open DotNetEnv
open FSharp.Data
open FSharp.Json
open System
open System.IO
open System.Net.Http

type MediaFile = { Url: string; Path: string }

type Post =
    { Id: string
      Caption: string
      MediaType: string
      Medias: MediaFile array }

type ApiResponse = JsonProvider<"./data/api-response.json", SampleIsList=true>

let getStringAsync (url: string) =
    async {
        use client = new HttpClient()
        use! response = client.GetAsync(url) |> Async.AwaitTask

        response.EnsureSuccessStatusCode() |> ignore

        let! content =
            response.Content.ReadAsStringAsync()
            |> Async.AwaitTask

        return content
    }

let rec getAllPosts (url: string) =
    async {
        let! instagramData = ApiResponse.AsyncLoad url

        match instagramData.Paging.Next with
        | Some s ->
            let! nextData = ApiResponse.AsyncLoad s

            return Array.append instagramData.Data nextData.Data

        | None -> return instagramData.Data
    }

let getUserPosts (accessToken: string) =
    async {
        let fields =
            [ "id"
              "caption"
              "media_type"
              "media_url"
              "id"
              "children"
              "children.media_url"
              "children.media_type" ]

        let url =
            "https://graph.instagram.com/me/media?limit=100&fields="
            + String.Join(',', fields)
            + "&access_token="
            + accessToken

        let! posts = getAllPosts url

        return posts
    }

let getFileName url =
    let uri = Uri(url)
    Path.GetFileName(uri.LocalPath)

let downloadImage (url: string) (path: string) =
    async {
        use client = new HttpClient()
        let! response = client.GetAsync url |> Async.AwaitTask

        response.EnsureSuccessStatusCode() |> ignore

        let! bytes =
            response.Content.ReadAsByteArrayAsync()
            |> Async.AwaitTask

        if File.Exists(path) then
            File.Delete path

        File.WriteAllBytesAsync(path, bytes) |> ignore
    }

let downloadAllPosts (posts: Post array) =
    posts
    |> Array.fold (fun s t -> Array.append s t.Medias) Array.empty
    |> Array.map (fun m -> downloadImage m.Url m.Path)

let convertResponseDatumToPost (folderPath: string) (p: ApiResponse.Datum) =
    let getMedia url =
        let fileName = getFileName url
        let filePath = Path.Join(folderPath, fileName)
        printfn "%s" fileName

        { Url = p.MediaUrl; Path = filePath }

    let mainMedia = getMedia p.MediaUrl

    let carouselUrls =
        match p.Children with
        | Some s -> s.Data |> Array.map (fun c -> getMedia c.MediaUrl)
        | None -> Array.empty

    { Id = p.Id
      Caption = p.Caption
      MediaType = p.MediaType
      Medias = Array.append [| mainMedia |] carouselUrls }

[<EntryPoint>]
let main argv =
    Env.Load() |> ignore // load .env file

    let accessToken =
        Environment.GetEnvironmentVariable "IG_ACCESS_TOKEN"

    async {
        let! postData = getUserPosts accessToken

        let posts =
            Array.map (convertResponseDatumToPost "./out") postData

        let json = Json.serialize posts
        File.WriteAllText("./out/posts.json", json)

        downloadAllPosts posts |> Async.Parallel |> ignore

        return 0
    }
    |> Async.RunSynchronously
