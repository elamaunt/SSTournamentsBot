namespace SSTournaments

open Domain
open System

module AutoDomain =

    let CreateMatch id p1 p2 (random:System.Random) =
        let r1 = GetOrGenerateRace (Some p1) (random.Next()) |> Option.get
        let r2 = GetOrGenerateRace (Some p2) (random.Next()) |> Option.get

        {  
           Id = id
           Player1 = Some (p1, r1)
           Player2 = Some (p2, r2)
           Map = GetRandomMapForPlayers (Some p1) (Some p2) random
           BestOf = One
           Result = NotCompleted (0, 0)
           Replays = []
        }