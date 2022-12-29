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

    type VoteAddTimeType = 
        | CheckinStart
        | StageCompletion

    type Voting = 
        | Kick of uint64*string
        | Ban of uint64*string
        | AddTime of VoteAddTimeType*System.TimeSpan
        | RevertMatchResult of int

    type AcceptVoteResult = 
        | NoVoting
        | TheVoteIsOver
        | Accepted
        | YouCanNotVote
        | AlreadyVoted
        | CompletedByThisVote

    type StartVotingResult = 
        | NotAllowed
        | NoPermission
        | AlreadyHasVoting
        | Completed

    type CompleteVotingResult = 
        | NoVoting
        | NoEnoughVotes
        | Completed
        | TheVoteIsOver

    type IVoteHandler =
        abstract HandleVoteKick : uint64*string -> Unit
        abstract HandleVoteBan : uint64*string -> Unit
        abstract HandleVoteAddTime : VoteAddTimeType*System.TimeSpan -> Unit
        abstract HandleVoteRevertMatchResult : int -> Unit

    let SwitchVote ev (handler: IVoteHandler) = 
        match ev with
        | Kick (id, name) -> handler.HandleVoteKick (id, name)
        | Ban (id, name) -> handler.HandleVoteBan (id, name)
        | AddTime (r, t) -> handler.HandleVoteAddTime (r, t)
        | RevertMatchResult matchId -> handler.HandleVoteRevertMatchResult matchId

    type Event = 
        | StartCurrentTournament
        | StartPreCheckingTimeVote
        | StartCheckIn
        | CompleteVoting
        | StartNextStage
        | CompleteStage

    type IEventsHandler =
        abstract DoStartCurrentTournament : Unit -> Unit
        abstract DoStartPreCheckingTimeVote : Unit -> Unit
        abstract DoStartCheckIn : Unit -> Unit
        abstract DoCompleteVoting : Unit -> Unit
        abstract DoStartNextStage : Unit -> Unit
        abstract DoCompleteStage : Unit -> Unit

    let SwitchEvent ev (handler: IEventsHandler) = 
        match ev with
        | StartCurrentTournament -> handler.DoStartCurrentTournament()
        | StartPreCheckingTimeVote -> handler.DoStartPreCheckingTimeVote()
        | StartCheckIn -> handler.DoStartCheckIn()
        | CompleteVoting -> handler.DoCompleteVoting()
        | StartNextStage -> handler.DoStartNextStage()
        | CompleteStage -> handler.DoCompleteStage()

    type BotButtonStyle = 
        | Primary = 1
        | Secondary = 2
        | Success = 3
        | Danger = 4
        | Link = 5

    type VotingProgress = {
        Voting: Voting
        VotesNeeded: int32
        VoteOptions: (string*string*BotButtonStyle) array
        Voted: (uint64*string) array
        AdminForcingEnabled: bool
        CompletedWithResult: (bool*(string option)) option
    }

    type GuildRole = 
        | Everyone = 1
        | Moderator = 2
        | Administrator = 3

    let StartVoting voting votesNeeded options forcingEnabled =
        {
            Voting = voting
            VotesNeeded = votesNeeded
            VoteOptions = options
            Voted = [||]
            AdminForcingEnabled = forcingEnabled
            CompletedWithResult = None
        }

    let AddVote progress (dicordId, selectedOptionId) =
        if progress.CompletedWithResult.IsSome then
            progress 
        else
            { progress with Voted = progress.Voted |> Array.append([|(dicordId, selectedOptionId)|]); }

    let CompleteVote progress =
        if progress.CompletedWithResult.IsSome then
            progress
        else
            if progress.VotesNeeded > progress.Voted.Length then
                { progress with CompletedWithResult = Some (false, None) }
            else
                let groups = progress.Voted |> Array.groupBy(fun (_, id) -> id)

                let (mostVotedId, votes) = groups |> Array.maxBy(fun (_, values) -> values.Length)

                let sameVotesGroupCount = groups |> Array.filter(fun (_, values) -> values.Length = votes.Length) |> Array.length

                if sameVotesGroupCount > 1 then
                    { progress with CompletedWithResult = Some (false, None) }
                else
                    { progress with CompletedWithResult = Some (false, Some mostVotedId) }

    let ForceCompleteVote progress id =
        { progress with CompletedWithResult = Some (true, Some id) }
