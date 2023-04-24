# SnakeAI

## information
FoxhoundAI simulates the codec conversations from Metal Gear Solid 1 using many characters from the game. Conversations can be had between up to 4 characters at a time, and are generated in realtime. Conversations can be about random or user generated topics, and AI generated images can be set as well. It was originally running continously on Twitch.tv until funds ran out. It uses Monogame as the basis for rendering graphics on screen. OpenAI generates the AI responses using the GPT3 Curie model, and Google Cloud converts those to audio files which are then spoken by the respective characters. There is also integration with Twitch's API to add chat input.

## Future
There is a template for a basic 2D game set up underneath the app. In the future, it is planned to use this game as a transition with chat integration. Also, better voices matching the original characters are planned using AI TTS. Access to GPT-4 has been approved as well, and the app will be moved to this when possible.

## Instructions
Long version:
As is, it will not run. There are several places where credentials are needed. In the Game1.cs file, there are places for OpenAI bearer tokens, Google Cloud JSON credentials, and a Twitch client ID and secret. I have placed comments near where these are needed, but more detailed instructions will be added shortly. Additionally, there is a token.txt and refreshtoken.txt that need tokens from Twitch's API, otherwise chat input will not work. There are also two fonts that may need to be downloaded, located in the files with the .spritefont extension in the Content folder. Finally, there are game soundtrack files needed from Metal Gear Solid 1, these are listed in ostlist.txt and should be placed in content\ost. They can be found on abandonware sites. 

Short but rough list of above:
1. Input Google Cloud JSON path, Twitch client ID and secret, and OpenAI tokens in the relevant places in Game1.cs
2. Add Twitch API token and refresh token in token.txt and refreshtoken.txt, respectively
3. Add OST files matching filenames in ostlist.txt to Content\ost
4. Font names in the files with extension .spritefont should be installed in the system

Easier, more detailed instructions to be added.

## Demo
Since it may be difficult to get this to compile as is, there is a demo available on YouTube here:
https://www.youtube.com/watch?v=DM_xKVVenZA

There are also clips availabe on the main streaming channel of the AI on Twitch:
https://www.twitch.tv/foxhoundai

The website currently directs to the above demo, but more details and information will be added shortly:
https://foxhoundai.net

## Stream Info
When available, this will stream at https://www.twitch.tv/foxhoundai 
However, due to lack of funds for both Google Cloud and OpenAI, it is not currently running regularly.

## Contact
If you'd like to collaborate, hire, or just ask me something, please feel free to contact me at mgsfoxhoundai@gmail.com. If you'd like to support me, please use https://ko-fi.com/kushastronaut

## Copyright
Metal Gear Solid is copyright © Konami. No original assets have been provided in this repository. and the content here is for entertainment and research purposes only.

Source is being written by Christopher Hardin. Please provide credit if creating your own work from this source. 