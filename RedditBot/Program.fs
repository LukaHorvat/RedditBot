// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open System.Net.Http
open FSharp.Data

let client = new HttpClient()

let postToSlack score url = async {
    use cont = new StringContent("New worldnews top post with " + score.ToString() + " upvotes:\n" + url)
    return!
        client.PostAsync(API.url, cont)
        |> Async.AwaitTask
}

let check set = async {
    let time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"))
    if time.Hour < 18 then return set
    else 
        let! res = Async.AwaitTask <| client.GetStringAsync("https://www.reddit.com/r/worldnews.json?sort=top&t=day&limit=1")
        let json = JsonValue.Parse(res)
        let topData = json.GetProperty("data").GetProperty("children").AsArray().[0].GetProperty("data")
        let url = topData.GetProperty("url").AsString()
        let score = topData.GetProperty("score").AsInteger()
        if Set.contains url set then
            printfn "[LOG] Top URL \"%s\" has already been posted" url
            return set
        else if (score > 5000) then
            printfn "[LOG] New top link, score %d, url \"%s\"" score url
            let! resp = postToSlack score url
            //check
            return (Set.add url set)
        else return set
}

let rec g2aCheck lastDay : unit Async = async {
    let url = "https://www.g2a.com/lucene/item/915"
    let time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"))
    if time.Hour >= 18 && time.Day <> lastDay then
        let! res = Async.AwaitTask <| client.GetStringAsync(url)
        let json = JsonValue.Parse(res)
        let minPrice = json.GetProperty("minPrice").AsFloat()
        use cont = new StringContent("Lowest GTA V price: €" + minPrice.ToString() + "\nat https://www.g2a.com/grand-theft-auto-v-cd-key-global.html")
        do! (client.PostAsync(API.url, cont) |> Async.AwaitTask |> Async.Ignore)
        do! Async.Sleep (24 * 60 * 60 * 1000)
        return! g2aCheck time.Day
    else 
        do! Async.Sleep (5 * 60 * 1000)
        return! g2aCheck lastDay
}

[<EntryPoint>]
let main argv =
    let rec repeat set = async {
        let! newSet = check set
        do! Async.Sleep (60 * 1000)
        do! repeat newSet
    }
    printfn "RedditBot running..."
    Async.Parallel [repeat Set.empty; g2aCheck -1] |> Async.RunSynchronously |> ignore
    0 // return an integer exit code
