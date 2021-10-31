# TwitchPredictionsBG
A HDT Plugin that can publish Twitch Predictions while you are playing Hearthstone Battlegrounds

## How to install?

1. [Click here](https://github.com/getjump/TwitchPredictionsBG/releases) to download the latest TwitchPredictionsBG.zip from the [releases page](https://github.com/getjump/TwitchPredictionsBG/releases).
2.  Unblock the zip file before unzipping, by  [right-clicking it and choosing properties](http://blogs.msdn.com/b/delay/p/unblockingdownloadedfile.aspx):  
[![Unblock](https://i.imgur.com/jic3r5R.png?raw=true)](https://i.imgur.com/jic3r5R.png?raw=true)
3.  Make sure you remove any old versions of TwitchPredictionsBG directory in the plugins directory of Hearthstone Deck Tracker completely, before upgrading versions.
4.  Unzip the archive to  `%AppData%/HearthstoneDeckTracker/Plugins`  To find this directory, you can click the following button in the Hearthstone Deck Tracker options menu:  `Options -> Tracker -> Plugins -> Plugins Folder`
5.  If you've done it correctly, TwitchPredictionsBG directory should be inside the Plugins directory. Inside the directory, should be a bunch of files, including a file called TwitchPredictionsBG.dll.
6.  Launch Hearthstone Deck Tracker. Enable the plugin in  `Options -> Tracker -> Plugins`.
8.  If it is not working you can enable a debug mode in the options window and join my Discord to tell me whats wrong. https://discord.gg/TqT5axZ


## How to use?
0. You ***MUST*** be Twitch partner/affiliate in order to use Twitch Predictions
1. Click on Authorize Twitch in Hearthstone Deck Tracker plugins list
2. You can configure plugin via `TwitchPredictionsBG.config` file in folder `%AppData%\TwitchPredictionsBG\data` 


File Format Description
```
{
  "authToken": {} <- this is your stored twitch token DON'T SHOW THIS TO ANYONE AND DON'T TOUCH THIS,
  "user": {} <- this is your cached twitch user DON'T TOUCH THIS,
  "delay": 0 <- here you can specify your stream delay in seconds,
  "debug": false <- if you set this to true for now there will be no check if your stream is live,
  "hsUserName": "GetJump#2842" <- this is your Battle.net tag stored, probably there is no need to touch this,
  "hsRegion": "EU" <- this is last region where yo uhave played, probably there is no need to touch this,
  "blueOutcomeTitle": [
    "1-3"
  ], <- You can specify outcome titles like this
  "pinkOutcomeTitle": [
    "4-7"
  ], <- You can specify outcome titles like this
  "predictionTitle": [
    "Делаем ставки работяги"
  ], <- You can specify outcome titles like this
  "predictionStrategy": "ConfigurablePredictionOutcomeStrategy", <- You can choose prediction strategy here
  ConfigurablePredictionOutcomeStrategy - it needs parameters blueOutcomePlacements, pinkOutcomePlacements, returnOutcomePlacements which specify outcomes
  PredictionOutcomeStrategy13_BLUE_47_PINK_8_RETURN - 1-3 Blue, 4-7 Pink, 8 Return it can finish prediction if there are only 3 heroes alive 
  PredictionOutcomeStrategy13_BLUE_57_PINK_48_RETURN - 1-3 Blue, 5-7 Pink, 4/8 Return it can finish prediction if there are only 3 heroes alive 
  "blueOutcomePlacements": [
    1,
    2,
    3
  ], <- You can specify blue outcome placements here for configurable prediction strategy
  "pinkOutcomePlacements": [
    4,
    5,
    6,
    7
  ], <- You can specify pink outcome placements here for configurable prediction strategy
  "returnOutcomePlacements": [
    8
  ], <- You can specify return outcome placements here for configurable prediction strategy
  "roflanRatio": 25, <- This is probability for outcomes to swap (just for the memes),
  "outcomesSwapped": true <- DON'T TOUCH THIS,
  "predictionWindowSeconds": 120 <- This is prediction window in seconds,
  "prediction": { <- DON'T TOUCH THIS this is current prediction
    "id": "7e6313a7-63a0-4688-aee0-41843bbfae46",
    "broadcaster_id": "39684923",
    "broadcaster_name": "getjump",
    "broadcaster_login": "getjump",
    "title": "Делаем ставки работяги",
    "winning_outcome_id": null,
    "outcomes": [
      {
        "id": "68f55161-b4f7-43e3-ae43-b5953fc9ce81",
        "title": "4-7",
        "users": 0,
        "channel_points": 0,
        "color": 0,
        "top_predictors": null
      },
      {
        "id": "110fcecc-45ea-4f73-b012-f3caaad5f45c",
        "title": "1-3",
        "users": 0,
        "channel_points": 0,
        "color": 1,
        "top_predictors": null
      }
    ],
    "prediction_window": 120,
    "status": 1,
    "created_at": "2021-10-20T09:46:15.7122855Z",
    "ended_at": "0001-01-01T00:00:00",
    "locked_at": "0001-01-01T00:00:00"
  }
} <- DON'T TOUCH THIS this is current prediction
```

## Contact
If you have any comments just come into my discord https://discord.gg/TqT5axZ

## Credits
Much of inspiration was taken from https://github.com/boonwin/BoonwinsBattlegroundsTracker
