# VonageBot
A connector to permit Bot framework with Directline and Vonage Voice to communicate
## What you will need

### Vonage account

You will need to have an account on Vonage to use it. If you don't have one, you can create one [here](https://dashboard.nexmo.com/sign-up).

### Ngrok

ngrok secure introspectable tunnels to localhost webhook development tool and debugging tool.
You will need it to create a tunnel between Vonage and your computer.

### Bot Framework with Speech directline connected

You will need a bot and Speech directline connected. How to do it? [Click here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk)

### Visual Studio or VS Code

You will need VSCode or Visual Studio to run the Terminal project.

## How to use it?
### Run the program

To run it, first, you will need to change some values inside the program.cs file: 
```csharp
const string speechSubscriptionKey = ""; // Your subscription key
const string region = ""; // Your subscription service region.
```
You will need to put your subscription key and the region of your Speech service.
After, you will need to put your language:
```csharp
botConfig.Language = "fr-FR";
```
When everything is done, you can run the program.

### Create a Ngrok tunnel

When the program is launched, you will need to create a tunnel Ngrok. To make that, you will need to go in a terminal and type
```
ngrok http 8181
```
It will create a tunnel and you will have an url like that: deb96263f404.ngrok.io

### Connect on Vonage

When you are connected, you can go on the [dashboard](https://dashboard.nexmo.com/). Next, you go on Voice Playground and you will need this json:
```json
[ {
      "action": "talk",
      "text": "Please wait while we connect you to the echo server"
    },
  {
    "action": "connect",
    "eventType": "synchronous",
    "eventUrl": [
      "https://example.com/events"
    ],
    "endpoint": [
      {
        "type": "websocket",
        "uri": "ws://deb96263f404.ngrok.io",
        "content-type": "audio/l16;rate=16000"
      }
    ]
  }
]
```
You will need to change the uri inside the endpoint and put your ngrok url. Don't forget to change the HTTP to ws:// because we are using websocket.
What this json will make is to connect when it will call you to the connector and will say the text inside the json before connecting.

After that, inside the playground, you can click on call me and you will discuss by voice with your both in "real time".
