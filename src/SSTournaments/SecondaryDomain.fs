namespace SSTournaments

open Domain

module SecondaryDomain = 

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
        | Done
        | AlreadyCheckIned
        | NotCheckInStageNow

    type CompleteVotingResult = 
        | NoVoting
        | NotEnoughVotes
        | Completed of int32
        | Cancelled

    type StartNextStageResult = 
        | NoTournament
        | TheStageIsTerminal
        | Done of Match array

    type CompleteStageResult = 
        | NoTournament
        | NotAllMatchesFinished
        | Completed

    type Voting = 
        | Kick of Player
        | Ban of Player
        | AddTime of System.TimeSpan
        | RevertMatchResult of Match

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
