namespace SSTournaments

open Domain
open System

module SecondaryDomain = 

    [<Flags>]
    type GuildThread = 
        | TournamentChat = 1
        | EventsTape = 2
        | Leaderboard= 4
        | History = 8
        | Logging = 16
        | VotingsTape = 32

    type Stats = {
        SteamId: uint64
        Games1v1: int32
        Games: int32
        SoloMmr: int32
    }

    type StartResult = 
        | NoTournament
        | NotEnoughPlayers
        | AlreadyStarted
        | Done

    type CheckInResult = 
        | NoTournament
        | NotEnoughPlayers
        | AlreadyStarted
        | Done

    type UserCheckInResult = 
        | NoTournament
        | NotRegisteredIn
        | Done
        | AlreadyCheckIned
        | NotCheckInStageNow

    type StartNextStageResult = 
        | NoTournament
        | TheStageIsTerminal
        | Done

    type CompleteStageResult = 
        | NoTournament
        | NotAllMatchesFinished
        | Completed
        | CompletedAndFinishedTheTournament

    type SubmitGameResult = 
        | NoTournament
        | DifferentRace
        | DifferentMod
        | DifferentMap
        | DifferentGameType
        | TooShortDuration
        | MatchNotFound
        | Completed
        | CompletedAndFinishedTheStage

    type MentionSetting =
        | Default
        | OnlyCheckin

    type BotButtonStyle = 
        | Primary
        | Secondary
        | Success
        | Danger
        | Link

    type VotingOption = {
        Message: string
        Style: BotButtonStyle
    }

    type Voting = {
        Message: string
        Options: VotingOption array
        MinimumVotes: int 
        AdminForcingEnabled: bool
        Handler: int option -> unit
    }

    type AcceptVoteResult = 
        | NoVoting
        | TheVoteIsOver
        | Accepted
        | YouCanNotVote
        | AlreadyVoted
        | CompletedByThisVote

    type StartVotingResult = 
        | AlreadyHasVoting
        | Completed

    type CompleteVotingResult = 
        | NoVoting
        | CompletedWithNoEnoughVotes
        | Completed
        | TheVoteIsOver

    type Event = 
        | StartCurrentTournament
        | StartPreCheckingTimeVote
        | StartCheckIn
        | CompleteVoting
        | StartNextStage
        | CompleteStage

    type EventInfo = {
        Event: Event
        StartDate: DateTime
        Period: TimeSpan option
    }

    let GetTimeBeforeEvent info = info.StartDate - GetMoscowTime()

    type ITournamentEventsHandler =
        abstract DoStartCurrentTournament : Unit -> Unit
        abstract DoStartPreCheckingTimeVote : Unit -> Unit
        abstract DoStartCheckIn : Unit -> Unit
        abstract DoCompleteVoting : Unit -> Unit
        abstract DoStartNextStage : Unit -> Unit
        abstract DoCompleteStage : Unit -> Unit

    let SwitchEvent ev (handler: ITournamentEventsHandler) = 
        match ev with
        | StartCurrentTournament -> handler.DoStartCurrentTournament()
        | StartPreCheckingTimeVote -> handler.DoStartPreCheckingTimeVote()
        | StartCheckIn -> handler.DoStartCheckIn()
        | CompleteVoting -> handler.DoCompleteVoting()
        | StartNextStage -> handler.DoStartNextStage()
        | CompleteStage -> handler.DoCompleteStage()

    type VoteCompletionResult = 
        | NotCompleted
        | CompletedWithNotEnoughVotes
        | ForceCompleted of int
        | Completed of int option

    type VotingProgress = {
        Voting: Voting
        Voted: (uint64*int) array
        State: VoteCompletionResult
    }

    let InitVotingProgress voting = {
        Voting = voting
        Voted = Array.empty
        State = NotCompleted
    }

    type GuildRole = 
        | Everyone = 1
        | Moderator = 2
        | Administrator = 3

    let AddVote progress (dicordId, selectedOptionId) =
        if progress.State <> NotCompleted then
            progress 
        else
            { progress with Voted = progress.Voted |> Array.append([|(dicordId, selectedOptionId)|]); }

    
    let AddVoteOption voting message style = 
        { voting with Options = [| { Message = message; Style = style } |] |> Array.append(voting.Options) }

    let CreateVoting message minimumVotes adminForcingEnabled handler = {
        Message = message
        Options = Array.empty
        MinimumVotes = minimumVotes
        AdminForcingEnabled = adminForcingEnabled
        Handler = handler
    }

    let CompleteVote progress =
        if progress.State <> NotCompleted then
            progress
        else
            if  progress.Voted.Length = 0 || progress.Voting.MinimumVotes > progress.Voted.Length then
                { progress with State = CompletedWithNotEnoughVotes }
            else
                let groups = progress.Voted |> Array.groupBy(fun (_, id) -> id)

                let (mostVotedId, votes) = groups |> Array.maxBy(fun (_, values) -> values.Length)

                let sameVotesGroupCount = groups |> Array.filter(fun (_, values) -> values.Length = votes.Length) |> Array.length

                if sameVotesGroupCount > 1 then
                    { progress with State = Completed None }
                else
                    { progress with State = Completed(Some(mostVotedId)) }

    let ForceCompleteVote progress id =
        if progress.State <> NotCompleted then
            progress
        else
            { progress with State = ForceCompleted id }

    let SharedUnit = ()

    let SwitchVotingResult result notEnoughVotesHandler noResultHandler optionIndexHandler = 
        match result with 
        | NotCompleted -> ()
        | CompletedWithNotEnoughVotes -> notEnoughVotesHandler()
        | ForceCompleted opt -> optionIndexHandler opt
        | Completed opt -> if opt |> Option.isSome then optionIndexHandler(Option.get(opt)) else noResultHandler()
