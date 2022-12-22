namespace SSTournaments

module Domain =
    
    [<Literal>] 
    let tournamentStartHour = 18

    // TODO: use BracketsType
    type BracketsType = 
        | OnlyWinners
        | WinnersAndLosers

    type TournamentType = 
        | Daily
        | Weekly

    type Race = 
        | SpaceMarines
        | Chaos
        | Orks
        | Eldar
        | ImperialGuard
        | Tau
        | Necrons
        | DarkEldar
        | SisterOfBattle
        
    type RaceOrRandom = 
        | Race of Race
        | Random

    type Map = 
        | BloodRiver
        | FataMorgana
        | FallenCity
        | MeetingOfMinds
        | DeadlyFunArcheology
        | SugarOasis
        | OuterReaches
        | BattleMarshes
        | ShrineOfExcellion
        | TranquilitysEnd
        | TitansFall
        | QuestsTriumph

    type BestOf = 
        | One
        | Three
        | Five 
        | Seven

    type Player = { 
        Name: string
        SteamId: uint64
        DiscordId: uint64
        Race: RaceOrRandom
        IsBot: bool }
        
    type Stage = 
        | Brackets of Player option array
        | Groups of Player array array
    
    type Mod = 
       | Soulstorm
       | TPMod

    type Tournament = {
        Mod: Mod; 
        Type: TournamentType
        Date: System.DateTime
        RegisteredPlayers: Player array
        Seed: int
    }

    type Count = int * int

    type TechnicalWinReason = 
        | OpponentsBan
        | Voting
        | Custom of string

    type MatchResult = 
        | Winner of Player * Count
        | TechnicalWinner of Player * TechnicalWinReason
        | Draw of Count
        | NotCompleted of Count

    type Replay = { UsedMod: Mod; Url: string }

    type Match = { 
        Id: int
        Player1: (Player * Race) option
        Player2: (Player * Race) option
        Map: Map
        BestOf: BestOf
        Result: MatchResult
        Replays: Replay list
    }

    type MatchOrFreeBlock =
        | Match of Match
        | Free of Player option

    let NextDay (day: System.DayOfWeek) = 
        match day with
        | System.DayOfWeek.Monday -> System.DayOfWeek.Tuesday
        | System.DayOfWeek.Tuesday -> System.DayOfWeek.Wednesday
        | System.DayOfWeek.Wednesday -> System.DayOfWeek.Thursday
        | System.DayOfWeek.Thursday -> System.DayOfWeek.Friday
        | System.DayOfWeek.Friday -> System.DayOfWeek.Sunday
        | System.DayOfWeek.Sunday -> System.DayOfWeek.Saturday
        | System.DayOfWeek.Saturday -> System.DayOfWeek.Monday
        | _ -> System.DayOfWeek.Monday

    let TournamentTypeByDayOfTheWeek day = 
        match day with
        | System.DayOfWeek.Saturday -> Weekly
        | _ -> Daily

    let GetMoscowTime() = 
        let date = System.DateTime.UtcNow
        let timeZone = System.TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        System.TimeZoneInfo.ConvertTime(date, timeZone)

    let GetNextTournamentDate() = 
        let moscowTime = GetMoscowTime()
        if moscowTime.Hour < tournamentStartHour then moscowTime else moscowTime.AddDays(1.0)

    let GetNextTournamentTypeByDate() = 
        let moscowTime = GetMoscowTime()
        let day = moscowTime.DayOfWeek
        if moscowTime.Hour < tournamentStartHour then day else (day |> NextDay)

    let CreateTournamentByDate gameMod = { 
        Mod = gameMod
        Type = GetNextTournamentTypeByDate() |> TournamentTypeByDayOfTheWeek
        RegisteredPlayers = [||]
        Seed = System.Random().Next()
        Date = GetNextTournamentDate()
    }

    let Exists x =
        match x with
        | Some(x) -> true
        | None -> false

    let IsEnoughPlayersToPlay tournament = 
        match tournament.Type with 
        | Weekly -> tournament.RegisteredPlayers.Length >= 12
        | _ -> tournament.RegisteredPlayers.Length > 3


    let ChangeTournamentType tournament tournamentType = 
        { tournament with Type = tournamentType }

    let ChangeTournamentMod tournament gameMod = 
        { tournament with Mod = gameMod }

    let IsPlayerRegisteredInTournament tournament playerSteamId playerDiscordId =
        Array.tryFind(fun e -> e.SteamId = playerSteamId || e.DiscordId = playerDiscordId) tournament.RegisteredPlayers |> Exists
            
    let RegisterPlayerInTournament tournament player =
        if IsPlayerRegisteredInTournament tournament player.SteamId player.DiscordId then tournament
        else { tournament with RegisteredPlayers = (Array.append tournament.RegisteredPlayers [|player|]) }
    
    let RemovePlayerFromTournament tournament playerSteamId = 
        match Array.tryFind(fun e -> e.SteamId = playerSteamId) tournament.RegisteredPlayers with
        | Some player -> { tournament with RegisteredPlayers = tournament.RegisteredPlayers |> Array.filter(fun x -> x <> player) }
        | None -> tournament

    let UpdatePlayerInTournament tournament player = 
        if not (IsPlayerRegisteredInTournament tournament player.SteamId player.DiscordId) then tournament
        else 
            (RemovePlayerFromTournament tournament player.SteamId |> RegisterPlayerInTournament) player

    let Shuffle seed (elements: 'a array)  = 
        let random = System.Random(seed)

        let copy = elements |> Array.copy

        for i in [1..copy.Length-1] do 
            let index = random.Next(copy.Length)
            let e = copy.[i] 
            copy.[i] <- copy.[index]
            copy.[index] <- e

        copy

    let SplitToArrays count (elements: 'a array)  = 
        [|
            let groupMaxSize = elements.Length / count

            for i in [1..count] do
                yield elements.[i * groupMaxSize.. (min (i * groupMaxSize) (elements.Length - 1))]
        |]
        

    let Start tournament = 
        match tournament.Type with
        | Daily -> tournament.RegisteredPlayers  |> (Shuffle tournament.Seed) |> Array.choose(fun x -> Some(Some x)) |> Brackets
        | Weekly -> tournament.RegisteredPlayers |> (Shuffle tournament.Seed) |> SplitToArrays 4 |> Groups

    let rec GetPowerOfTwoNearestTo n i = 
        if i > n then (i >>> 1) else GetPowerOfTwoNearestTo n (i <<< 1)

    let rec GetPowerOfTwoValueFor n i = 
        if n <= 1 then i else GetPowerOfTwoValueFor (n >>> 1) (i+1) 

    let GetRaceByIndex index =
        match index with 
        | 1 -> Chaos
        | 2 -> Orks
        | 3 -> Eldar
        | 4 -> ImperialGuard
        | 5 -> Tau
        | 6 -> Necrons
        | 7 -> DarkEldar
        | 8 -> SisterOfBattle
        | _ -> SpaceMarines

    let GetMapByIndex index =
        match index with 
        | 1 -> FataMorgana
        | 2 -> FallenCity
        | 3 -> MeetingOfMinds
        | 4 -> DeadlyFunArcheology
        | 5 -> SugarOasis
        | 6 -> OuterReaches
        | 7 -> BattleMarshes
        | 8 -> ShrineOfExcellion
        | 9 -> TranquilitysEnd
        | 10 -> TitansFall
        | 11 -> QuestsTriumph
        | _ -> BloodRiver

    let GetOrGenerateRace (player: Player option) seed = 
        let random = System.Random(seed)

        let race = GetRaceByIndex (random.Next(9))
        
        match player with 
        | Some v -> match v.Race with 
            | Random -> Some(race)
            | Race r -> Some r
        | None -> None

    let GenerateMatchesfrom stage seed isFirstStage =
        let random = System.Random(seed)

        match stage with 
        | Brackets players -> [| 
                let power = GetPowerOfTwoNearestTo players.Length 1
                let diff = players.Length - power

                let isReducingStage = (not isFirstStage) && diff > 0

                let mutable upTo: int = (power >>> 1) - 1
                
                if isReducingStage then 
                    upTo <- diff - 1

                for index in [0 .. upTo] do

                    let player1 = players.[(index <<< 1)]
                    let player2 = players.[if isReducingStage then (power + index) else ((index <<< 1) + 1)]

                    let race1 = GetOrGenerateRace player1 (random.Next())
                    let race2 = GetOrGenerateRace player2 (random.Next())

                    yield Match { 
                        Id = index
                        Player1 = if player1.IsSome then Some (player1.Value, race1.Value) else None
                        Player2 = if player2.IsSome then Some (player2.Value, race2.Value) else None
                        Map = GetMapByIndex (random.Next(12))
                        BestOf = One
                        Result = NotCompleted (0, 0)
                        Replays = [] }

                if not isReducingStage then
                    for i in [((upTo + 1) <<< 1) .. players.Length - 1] do
                        yield Free players.[i]
                else
                    for i in [((upTo + 1) <<< 1) .. power - 1] do
                        yield Free players.[i]
                
            |] 
        | Groups groups -> [|
            let mutable id = 0

            for group in groups do
                for index in [0..group.Length-1] do 
                     for otherIndex in [index+1..group.Length-1] do 
                        
                        yield Match { 
                            Id = id
                            Player1 = Some (group.[index], GetRaceByIndex (random.Next(9)))
                            Player2 = Some (group.[otherIndex], GetRaceByIndex (random.Next(9)))
                            Map = GetMapByIndex (random.Next(12))
                            BestOf = One
                            Result = NotCompleted (0, 0)
                            Replays = [] }
                        id <- id + 1

            |]

    let IsTerminalStage stage =
        match stage with
        | Brackets players -> players.Length = 1
        | Groups _ -> false

    let IsReducingStage stage isFirstStage =
        match stage with
        | Brackets players ->
            let power = GetPowerOfTwoNearestTo players.Length 1
            let diff = players.Length - power

            (not isFirstStage) && diff > 0
        | Groups _ -> false

    let IsPlayerFree stage isFirstStage playerIndex =
            match stage with
            | Brackets players ->
                let power = GetPowerOfTwoNearestTo players.Length 1
                let diff = players.Length - power

                let isReducingStage = (not isFirstStage) && diff > 0
               
                let mutable upTo: int = (power >>> 1) - 1
                
                if isReducingStage then 
                    upTo <- diff - 1

                players.Length - (upTo <<< 1)
            | Groups _ -> 0

    let GenerateNextStageFrom stage (matches: Match array) isFirstStage = 
        match stage with
        | Brackets players ->
            Brackets [|
            
                let power = GetPowerOfTwoNearestTo players.Length 1
                let diff = players.Length - power

                let isReducingStage = (not isFirstStage) && diff > 0

                let mutable upTo: int = (power >>> 1) - 1
                
                if isReducingStage then 
                    upTo <- diff - 1

                for index in [0 .. upTo] do

                    if isReducingStage && ((index <<< 1) + 1 < players.Length) then
                        players.[(index <<< 1) + 1]
                    
                    match matches.[index].Result with 
                    | NotCompleted _ -> None
                    | Winner (w, _) -> Some w
                    | TechnicalWinner (w, _) -> Some w
                    | Draw _ -> None

                if not isReducingStage then
                    for i in [((upTo + 1) <<< 1) .. players.Length - 1] do
                        players.[i]
                else
                    for i in [((upTo + 1) <<< 1) .. power - 1] do
                        players.[i]

            |]
        | Groups groups -> 
            Brackets(groups.[0] |> Array.choose(fun x -> Some(Some x))) // TODO

    let GetMatches blocks = 
        [|
            for block in blocks do 
                match block with 
                | Match m -> m
                | Free _ -> ()
        |]