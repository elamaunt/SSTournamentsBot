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
        | Kick of Player
        | Ban of Player
        | AddTime of VoteAddTimeType*System.TimeSpan
        | RevertMatchResult of Match

    type AcceptVoteResult = 
        | NoVoting
        | TheVoteIsOver
        | Accepted
        | YouCanNotVote
        | AlreadyVoted

    type CompleteVotingResult = 
        | NoVoting
        | NoEnoughVotes
        | CompletedPositive
        | CompletedNegative
        | TheVoteIsOver

    type IVoteHandler =
        abstract HandleVoteKick : Player -> Unit
        abstract HandleVoteBan : Player -> Unit
        abstract HandleVoteAddTime : VoteAddTimeType*System.TimeSpan -> Unit
        abstract HandleVoteRevertMatchResult : Match -> Unit

    let SwitchVote ev (handler: IVoteHandler) = 
        match ev with
        | Kick p -> handler.HandleVoteKick p
        | Ban p -> handler.HandleVoteBan p
        | AddTime (r, t) -> handler.HandleVoteAddTime (r, t)
        | RevertMatchResult m -> handler.HandleVoteRevertMatchResult m

    type Event = 
        | StartCurrentTournament
        | StartCheckIn
        | CompleteVoting
        | StartNextStage
        | CompleteStage

    type IEventsHandler =
        abstract DoStartCurrentTournament : Unit -> Unit
        abstract DoStartCheckIn : Unit -> Unit
        abstract DoCompleteVoting : Unit -> Unit
        abstract DoStartNextStage : Unit -> Unit
        abstract DoCompleteStage : Unit -> Unit

    let SwitchEvent ev (handler: IEventsHandler) = 
        match ev with
        | StartCurrentTournament -> handler.DoStartCurrentTournament()
        | StartCheckIn -> handler.DoStartCheckIn()
        | CompleteVoting -> handler.DoCompleteVoting()
        | StartNextStage -> handler.DoStartNextStage()
        | CompleteStage -> handler.DoCompleteStage()

    type VotingProgress = {
        Voting: Voting
        VotesNeeded: int32
        VoteOptions: (string*string) array
        Voted: (uint64*string) array
        AdminForcingEnabled: bool
        IsCompleted: bool
    }

    let AddVote progress (dicordId, selectedOptionId) =
        if progress.IsCompleted then
            progress 
        else
            { progress with Voted = progress.Voted |> Array.append([|(dicordId, selectedOptionId)|]); }

    let CompleteVOte progress = { progress with IsCompleted = true }
