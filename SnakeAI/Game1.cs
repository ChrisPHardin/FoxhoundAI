using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Text.RegularExpressions;
using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;
using System.Net;
using Autofac;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using SnakeAI.Systems;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.Api;
using System.Linq;


//using Google.Type;
//using Plugin.TextToSpeech;

namespace SnakeAI
{
    public class Game1 : GameBase
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _bgSprite;
        private SpriteBatch _codexLeft;
        private SpriteBatch _codexRight;
        private SpriteBatch _bgSpecial;
        private SpriteBatch _loadingBatch;
        private SpriteBatch _stationBatch;
        private SpriteBatch _name1batch;
        private SpriteBatch _name2batch;
        private SpriteBatch _helpbatch;


        private TiledMap _map;
        private TiledMapRenderer _renderer;
        private EntityFactory _entityFactory;
        private OrthographicCamera _camera;
        private World _world;

        Texture2D codexLeftMaster;
        Texture2D codexRightMaster;
        Texture2D callBG1;
        Texture2D callBG2;
        SoundEffect codecOpen;
        SoundEffect codecClose;
        Texture2D bgTexture1;
        Texture2D bgTexture2;
        Texture2D bgTexture3;
        Texture2D bgTexture4;
        Texture2D bgAnim;
        Texture2D bgAnimSpecial;
        Song mainMus;
        SpriteBatch _logBatch;
        SpriteFont scoreFont;
        SpriteFont alarmFont;
        String currentDraw;


        //Twitch stuff
        TwitchClient tclient;
        bool justUsedGenTopic = false;
        string itopic;
        string topic;
        string lastTopic = "";
        string nextTopic = "";

        private Vector2 _motwPosition;

        bool char1talking = false;
        bool char2talking = false;
        bool dialogOver = false;
        bool goodForNext = false;
        bool playingCallSound = false;
        bool callEnding = false;
        bool doneSavingImage = false;
        bool specialEvent = false;
        bool waitingForDialog = false;
        bool usingTopic = true;
        bool usingItopic = true;


        TextToSpeechClient client;

        //for animations and timing
        float timeSinceBG = 0;
        float timeSingConvo = 0;
        float timeSinceLastUpdate = 0;
        float timeAPIcheck = 0;
        float callTime = 0;
        float voiceGameTime = 0;
        float animLeftTime = 0;
        float animRightTime = 0;
        float messageTime = 0;
        int sessioncount = 0;
        int tempIndex = 0;

        bool isTalking = false;
        bool isCalling;

        double speakingRate = 1;

        string voiceName;
        string locale = "en-US";
        SsmlVoiceGender voiceGender = SsmlVoiceGender.Male;

        private Resources<SoundEffect> _sfxLoad = new();
        private Resources<Texture2D> _texLoad = new();
        private Resources<Song> _songLoad = new();
        private Resources<SpriteFont> _fontLoad = new();

        AudioFileReader audioFile;

        string holdingDialog = "";
        string speakDialog = "";
        string tempFunnyDialog = "";
        string char1dialog1 = "";
        string char1dialog2 = "";
        string char1dialog3 = "";
        string char1dialog4 = "";
        string char1dialog5 = "";
        string char2dialog1 = "";
        string char2dialog2 = "";
        string char2dialog3 = "";
        string char2dialog4 = "";
        string char2dialog5 = "";
        string savedFileName = "default.mp3";

        string prompt1;

        List<string> topicList = new List<string>();
        List<string> itopicList = new List<string>();
        int lastTopicType = 0;

        private static TwitchAPI api;

        string station;

        string imageprompt = "";
        string promptspecial;

        string imageURL = "";


        double pitch = -20;
        string startChar1;
        string startChar2;
        string startChar3;
        string startChar4;
        string startChar = "Solid Snake:";

        WaveOutEvent outputDevice;

        Random bgRnd = new Random();
        int rndSessionID = 0;

        public Game1()
        {
            _graphics = GraphicsDeviceManager;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            MediaPlayer.IsRepeating = false;
            MediaPlayer.IsShuffled = false;
            MediaPlayer.Volume = 0.1f;
            IsMouseVisible = true;

            //            Console.ReadLine();
        }

        protected override void RegisterDependencies(ContainerBuilder builder)
        {
            _camera = new OrthographicCamera(GraphicsDevice);

            builder.RegisterInstance(new SpriteBatch(GraphicsDevice));
            builder.RegisterInstance(_camera);
        }


        protected override void Initialize()
        {
            //Path below should be to JSON credential file of google cloud 
            string credential_path = @"";
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credential_path);
            client = TextToSpeechClient.Create();
            rndSessionID = bgRnd.Next(200);
            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader("codeccall.mp3");
            outputDevice.Init(audioFile);
            isCalling = true;
            itopic = "";
            topic = "";
            //game.Run();
            List<string> tokList = LoadToken();
            string token = (string)tokList[0];
            ConnectionCredentials credentials = new ConnectionCredentials("foxhoundai", token);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            api = new TwitchAPI();
            //Below should be Twitch client ID
            api.Settings.ClientId = "";
            api.Settings.AccessToken = token; // App Secret is not an Accesstoken


            WebSocketClient customClient = new WebSocketClient(clientOptions);
            tclient = new TwitchClient(customClient);
            tclient.Initialize(credentials, "foxhoundai");

            tclient.OnLog += Client_OnLog;
            tclient.OnJoinedChannel += Client_OnJoinedChannel;
            tclient.OnMessageReceived += Client_OnMessageReceived;
            tclient.OnConnected += Client_OnConnected;
            tclient.OnConnectionError += Client_OnConnectionError;
            tclient.Connect();

            base.Initialize();


        }


        protected override void LoadContent()
        {
            _bgSprite = new SpriteBatch(GraphicsDevice);
            _bgSpecial = new SpriteBatch(GraphicsDevice);
            _logBatch = new SpriteBatch(GraphicsDevice);
            _codexLeft = new SpriteBatch(GraphicsDevice);
            _codexRight = new SpriteBatch(GraphicsDevice);
            _loadingBatch = new SpriteBatch(GraphicsDevice);
            _stationBatch = new SpriteBatch(GraphicsDevice);
            _name1batch = new SpriteBatch(GraphicsDevice);
            _name2batch = new SpriteBatch(GraphicsDevice);
            _helpbatch = new SpriteBatch(GraphicsDevice);


            bgAnim = _texLoad.LoadContent("codex\\codexframe1", this);
            bgAnimSpecial = _texLoad.LoadContent("codex\\bg_transparent", this);
            bgTexture1 = _texLoad.LoadContent("codex\\codexframe1", this);
            bgTexture2 = _texLoad.LoadContent("codex\\codexframe2", this);
            bgTexture3 = _texLoad.LoadContent("codex\\codexframe3", this);
            bgTexture4 = _texLoad.LoadContent("codex\\codexframe4", this);
            callBG1 = _texLoad.LoadContent("codex\\bg_call", this);
            callBG2 = _texLoad.LoadContent("codex\\bg_black", this);

            codecClose = _sfxLoad.LoadContent("codecover", this);
            codecOpen = _sfxLoad.LoadContent("codecopen", this);

            codexLeftMaster = _texLoad.LoadContent("codex\\snake\\mouth_closed1", this);
            codexRightMaster = _texLoad.LoadContent("codex\\snake\\mouth_closed1", this);

            savedFileName = (rndSessionID.ToString() + "_" + sessioncount.ToString() + "_" + bgRnd.Next(200) + ".mp3");

            LoadMusic();


            int rnd1 = bgRnd.Next(100, 200);
            int rnd2 = bgRnd.Next(10, 99);
            station = (rnd1 + "." + rnd2);


            GeneratePrompt();

            scoreFont = _fontLoad.LoadContent("defaultfont", this);
            alarmFont = _fontLoad.LoadContent("alarm", this);
            HandleAPIRequest();


            _world = new WorldBuilder()
    .AddSystem(new WorldSystem())
    .AddSystem(new PlayerSystem())
    .AddSystem(new EnemySystem())
    .AddSystem(new RenderSystem(new SpriteBatch(GraphicsDevice), _camera))
    .Build();


        }
        protected void LoadMusic()
        {
            var musFile = File.ReadAllLines("ostlist.txt");
            var musList = new List<string>(musFile);
            int musRnd = bgRnd.Next(musList.Count);
            string genMus = (string)musList[musRnd];
            this.mainMus = _songLoad.LoadContent("ost\\" + genMus, this);
            MediaPlayer.Play(mainMus);
        }

        protected List<string> LoadToken()
        {
            var tokFile = File.ReadAllLines("token.txt");
            var tokList = new List<string>(tokFile);
            return tokList;
        }

        protected string DetermineChar(int char1rnd, bool firstChar = false, bool secondChar = false)
        {
            string rndChar1 = "";
            if (char1rnd == 0)
            {
                rndChar1 = "Solid Snake";
                if (firstChar) { startChar1 = "snake"; }
                if (secondChar) { startChar2 = "snake"; }
            }
            else if (char1rnd == 1)
            {
                rndChar1 = "Liquid";
                if (firstChar) { startChar1 = "liquid"; }
                if (secondChar) { startChar2 = "liquid"; }

            }
            else if (char1rnd == 2)
            {
                rndChar1 = "Revolver Ocelot";
                if (firstChar) { startChar1 = "ocelot"; }
                if (secondChar) { startChar2 = "ocelot"; }
            }
            else if (char1rnd == 3)
            {
                rndChar1 = "Otacon";
                if (firstChar) { startChar1 = "otacon"; }
                if (secondChar) { startChar2 = "otacon"; }
            }
            else if (char1rnd == 4)
            {
                rndChar1 = "Master Miller";
                if (firstChar) { startChar1 = "miller"; }
                if (secondChar) { startChar2 = "miller"; }
            }
            else if (char1rnd == 5)
            {
                rndChar1 = "Meryl";
                if (firstChar) { startChar1 = "meryl"; }
                if (secondChar) { startChar2 = "meryl"; }
            }
            else if (char1rnd == 6)
            {
                rndChar1 = "Naomi";
                if (firstChar) { startChar1 = "naomi"; }
                if (secondChar) { startChar2 = "naomi"; }
            }
            else if (char1rnd == 7)
            {
                rndChar1 = "Mei Ling";
                if (firstChar) { startChar1 = "mei"; }
                if (secondChar) { startChar2 = "mei"; }
            }
            else if (char1rnd == 8)
            {
                rndChar1 = "Sniper Wolf";
                if (firstChar) { startChar1 = "wolf"; }
                if (secondChar) { startChar2 = "wolf"; }
            }
            else if (char1rnd == 9)
            {
                rndChar1 = "Colonel Campbell";
                if (firstChar) { startChar1 = "campbell"; }
                if (secondChar) { startChar2 = "campbell"; }
            }
            else if (char1rnd == 10)
            {
                rndChar1 = "Nastasha";
                if (firstChar) { startChar1 = "nastasha"; }
                if (secondChar) { startChar2 = "nastasha"; }
            }
            else if (char1rnd == 11)
            {
                rndChar1 = "Raiden";
                if (firstChar) { startChar1 = "raiden"; }
                if (secondChar) { startChar2 = "raiden"; }
            }
            else if (char1rnd == 12)
            {
                Random newRnd = new Random();
                int rnd = newRnd.Next(10);
                if (rnd >= 8)
                {
                    rndChar1 = "Decoy Octopus";
                    if (firstChar) { startChar1 = "decoy"; }
                    if (secondChar) { startChar2 = "decoy"; }
                }
                else if (rnd >= 4)
                {
                    rndChar1 = "Colonel Campbell";
                    if (firstChar) { startChar1 = "campbell"; }
                    if (secondChar) { startChar2 = "campbell"; }
                }
                else if (rnd >= 0)
                {
                    rndChar1 = "Raiden";
                    if (firstChar) { startChar1 = "raiden"; }
                    if (secondChar) { startChar2 = "raiden"; }
                }
            }

            return rndChar1;
        }

        protected string DeterminePersonality(string character)
        {
            if (character == "")
            { return ""; }
            if (character == "Solid Snake")
            {
                return "Solid Snake is a sarcastic soldier and spy. ";
            }
            else if (character == "Master Miller")
            {
                return "Master Miller is a master tactician. ";
            }
            else if (character == "Raiden")
            {
                return "Raiden is very cool. ";
            }
            else if (character == "Sniper Wolf")
            {
                return "Sniper Wolf is an experienced sniper, and is trying to kill Solid Snake because he is trying to stop Foxhound on Shadow Moses Island. ";
            }
            else if (character == "Naomi")
            {
                return "Naomi is a scientist that specializes in genetic research and nanotechnology. ";
            }
            else if (character == "Meryl")
            {
                return "Meryl is a soldier in training and is working with Solid Snake. ";
            }
            else if (character == "Otacon")
            {
                return "Otacon is a scientist and nerd who likes anime and video games. ";
            }
            else if (character == "Nastasha")
            {
                return "Nastasha is a Ukrainian weapons analyst and an expert on nuclear weapons. ";
            }
            else if (character == "Revolver Ocelot")
            {
                return "Revolver Ocelot is a double agent currently working with the Foxhound terrorists on Shadow Moses Island. ";
            }
            else if (character == "Colonel Campbell")
            {
                return "Colonel Campbell is an aging officer who is in charge of the operation. ";
            }
            else if (character == "Liquid")
            {
                return "Liquid is ambitious, cunning, and very aggressive. ";
            }
            else if (character == "Mei Ling")
            {
                return "Mei Ling is a medical advisor who loves reciting ancient Chinese proverbs. ";
            }
            else if (character == "Decoy Octopus")
            {
                return "Decoy Octopus is a genetically modified soldier and copy of Solid Snake. ";
            }

            return "";
        }

        protected void GeneratePrompt()
        {
            var adjFile = File.ReadAllLines("content\\english-adjectives.txt");
            var adjList = new List<string>(adjFile);
            var nounFile = File.ReadAllLines("content\\english-nouns.txt");
            var nounList = new List<string>(nounFile);
            //var funFile = File.ReadAllLines("content\\english-fun.txt");
            //var funList = new List<string>(funFile);
            var specFile = File.ReadAllLines("content\\english-specifics.txt");
            var specList = new List<string>(specFile);

            Random bgRnd = new Random();
            int adjRnd = bgRnd.Next(adjList.Count);
            string genAdj = (string)adjList[adjRnd];
            Random bgRnd2 = new Random();
            int nounRnd = bgRnd2.Next(nounList.Count);
            string genNoun = (string)nounList[nounRnd];
            Random bgRnd5 = new Random();
            int specialRnd = bgRnd5.Next(10);
            Random bgRnd6 = new Random();
            int specRnd = bgRnd6.Next(specList.Count);
            string genSpec = (string)specList[specRnd];
            string comboAdjVerb;

            if (specialEvent == true)
            {

                    imageprompt = genAdj + " " + genNoun;
                    adjRnd = bgRnd.Next(adjList.Count);
                    genAdj = (string)adjList[adjRnd];
                    nounRnd = bgRnd.Next(nounList.Count);
                    genNoun = (string)nounList[nounRnd];
                
            }




            if (specialRnd < 3)
            {
                comboAdjVerb = genAdj + " " + genNoun;
            }
            else
            {
                comboAdjVerb = genSpec;
            }

            string rndChar1 = "Solid Snake";
            string rndChar2 = "Liquid";
            string rndChar3 = "";
            string rndChar4 = "";

            int snakeRnd = bgRnd.Next(10);
            if (snakeRnd > 7)
            {
                rndChar1 = DetermineChar(bgRnd.Next(13), true);
            }
            else
            {
                rndChar1 = "Solid Snake";
                startChar1 = "snake";
            }

            rndChar2 = DetermineChar(bgRnd.Next(13), false, true);
            while (rndChar2 == rndChar1)
            {
                rndChar2 = DetermineChar(bgRnd.Next(13), false, true);
            }

            int char3rnd = bgRnd.Next(10);
            if (char3rnd > 5)
            {
                rndChar3 = DetermineChar(bgRnd.Next(13));
                while (rndChar3 == rndChar1 || rndChar3 == rndChar2)
                {
                    rndChar3 = DetermineChar(bgRnd.Next(13));
                }
                int char4rnd = bgRnd.Next(10);
                if (char4rnd > 7)
                {
                    rndChar4 = DetermineChar(bgRnd.Next(13));
                    while (rndChar4 == rndChar3 || rndChar4 == rndChar2 || rndChar4 == rndChar1)
                    {
                        rndChar4 = DetermineChar(bgRnd.Next(13));
                    }
                }
            }

            bool cust2 = false;
            bool cust3 = false;
            bool cust4 = false;
            if (topic != "" || itopic != "")
            {
                if (topic.ToLower().Contains("snake") || itopic.ToLower().Contains("snake"))
                {
                    if (rndChar1 != "Solid Snake" && rndChar2 != "Solid Snake" && rndChar3 != "Solid Snake" && rndChar4 != "Solid Snake")
                    {
                        rndChar1 = "Solid Snake";
                        startChar1 = "snake";
                    }
                    else if (rndChar2 == "Solid Snake")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Solid Snake")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Solid Snake")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("liquid") || itopic.ToLower().Contains("liquid"))
                {
                    if (rndChar1 != "Liquid" && rndChar2 != "Liquid" && rndChar3 != "Liquid" && rndChar4 != "Liquid")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Liquid";
                            startChar2 = "liquid";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Liquid";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Liquid";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Liquid")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Liquid")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Liquid")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("meryl") || itopic.ToLower().Contains("meryl"))
                {
                    if (rndChar1 != "Meryl" && rndChar2 != "Meryl" && rndChar3 != "Meryl" && rndChar4 != "Meryl")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Meryl";
                            startChar2 = "meryl";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Meryl";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Meryl";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Meryl")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Meryl")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Meryl")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("mei") || itopic.ToLower().Contains("mei"))
                {
                    if (rndChar1 != "Mei Ling" && rndChar2 != "Mei Ling" && rndChar3 != "Mei Ling" && rndChar4 != "Mei Ling")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Mei Ling";
                            startChar2 = "mei";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Mei Ling";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Mei Ling";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Mei Ling")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Mei Ling")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Mei Ling")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("nastasha") || itopic.ToLower().Contains("nastasha"))
                {
                    if (rndChar1 != "Nastasha" && rndChar2 != "Nastasha" && rndChar3 != "Nastasha" && rndChar4 != "Nastasha")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Nastasha";
                            startChar2 = "nastasha";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Nastasha";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Nastasha";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Nastasha")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Nastasha")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Nastasha")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("sniper wolf") || itopic.ToLower().Contains("sniper wolf"))
                {
                    if (rndChar1 != "Sniper Wolf" && rndChar2 != "Sniper Wolf" && rndChar3 != "Sniper Wolf" && rndChar4 != "Sniper Wolf")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Sniper Wolf";
                            startChar2 = "wolf";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Sniper Wolf";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Sniper Wolf";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Sniper Wolf")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Sniper Wolf")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Sniper Wolf")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("otacon") || itopic.ToLower().Contains("otacon"))
                {
                    if (rndChar1 != "Otacon" && rndChar2 != "Otacon" && rndChar3 != "Otacon" && rndChar4 != "Otacon")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Otacon";
                            startChar2 = "otacon";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Otacon";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Otacon";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Otacon")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Otacon")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Otacon")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("ocelot") || itopic.ToLower().Contains("ocelot"))
                {
                    if (rndChar1 != "Revolver Ocelot" && rndChar2 != "Revolver Ocelot" && rndChar3 != "Revolver Ocelot" && rndChar4 != "Revolver Ocelot")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Revolver Ocelot";
                            startChar2 = "ocelot";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Revolver Ocelot";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Revolver Ocelot";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Revolver Ocelot")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Revolver Ocelot")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Revolver Ocelot")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("decoy octopus") || itopic.ToLower().Contains("decoy octopus"))
                {
                    if (rndChar1 != "Decoy Octopus" && rndChar2 != "Decoy Octopus" && rndChar3 != "Decoy Octopus" && rndChar4 != "Decoy Octopus")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Decoy Octopus";
                            startChar2 = "decoy";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Decoy Octopus";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Decoy Octopus";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Decoy Octopus")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Decoy Octopus")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Decoy Octopus")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("campbell") || itopic.ToLower().Contains("campbell"))
                {
                    if (rndChar1 != "Colonel Campbell" && rndChar2 != "Colonel Campbell" && rndChar3 != "Colonel Campbell" && rndChar4 != "Colonel Campbell")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Colonel Campbell";
                            startChar2 = "campbell";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Colonel Campbell";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Colonel Campbell";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Colonel Campbell")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Colonel Campbell")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Colonel Campbell")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("miller") || itopic.ToLower().Contains("miller"))
                {
                    if (rndChar1 != "Master Miller" && rndChar2 != "Master Miller" && rndChar3 != "Master Miller" && rndChar4 != "Master Miller")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Master Miller";
                            startChar2 = "miller";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Master Miller";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Master Miller";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Master Miller")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Master Miller")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Master Miller")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("raiden") || itopic.ToLower().Contains("raiden"))
                {
                    if (rndChar1 != "Raiden" && rndChar2 != "Raiden" && rndChar3 != "Raiden" && rndChar4 != "Raiden")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Raiden";
                            startChar2 = "raiden";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Raiden";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Raiden";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Raiden")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Raiden")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Raiden")
                    {
                        cust4 = true;
                    }
                }
                if (topic.ToLower().Contains("naomi") || itopic.ToLower().Contains("naomi"))
                {
                    if (rndChar1 != "Naomi" && rndChar2 != "Naomi" && rndChar3 != "Naomi" && rndChar4 != "Naomi")
                    {
                        if (!cust2)
                        {
                            rndChar2 = "Naomi";
                            startChar2 = "naomi";
                            cust2 = true;
                        }
                        else if (!cust3)
                        {
                            rndChar3 = "Naomi";
                            cust3 = true;
                        }
                        else if (!cust4)
                        {
                            rndChar4 = "Naomi";
                            cust4 = true;
                        }
                    }
                    else if (rndChar2 == "Naomi")
                    {
                        cust2 = true;
                    }
                    else if (rndChar3 == "Naomi")
                    {
                        cust3 = true;
                    }
                    else if (rndChar4 == "Naomi")
                    {
                        cust4 = true;
                    }
                }

            }

            startChar = rndChar1 + ":";

            string pers1 = DeterminePersonality(rndChar1);
            string pers2 = DeterminePersonality(rndChar2);
            string pers3 = DeterminePersonality(rndChar3);
            string pers4 = DeterminePersonality(rndChar4);

            if (rndChar3 != "")
            {
                startChar3 = rndChar3;
            }
            else
            {
                startChar3 = "ocelot";
            }

            if (rndChar4 != "")
            {
                startChar4 = rndChar4;
            }
            else
            {
                startChar4 = "campbell";
            }

            if (topic != "")
            {
                if (rndChar4 != "")
                {
                    prompt1 = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + ", " + rndChar2 + ", " + rndChar3 + ", and " + rndChar4 + " are having a discussion about " + Regex.Replace(topic, @"\[.*\]", "") + ". They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters and should be lengthy. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
                else if (rndChar3 != "")
                {
                    prompt1 = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + ", " + rndChar2 + ", and " + rndChar3 + " are having a discussion about " + Regex.Replace(topic, @"\[.*\]", "") + ". They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters and should be lengthy. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";
                }
                else
                {
                    prompt1 = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + " and " + rndChar2 + " are having a discussion about " + Regex.Replace(topic, @"\[.*\]", "") + ". They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters and should be lengthy. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
            }
            else
            {
                if (rndChar4 != "")
                {
                    prompt1 = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + ", " + rndChar2 + ", " + rndChar3 + ", and " + rndChar4 + " are all talking about " + comboAdjVerb + ". " + pers1 + pers2 + pers3 + pers4 + "They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
                else if (rndChar3 != "")
                {
                    prompt1 = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + ", " + rndChar2 + ", and " + rndChar3 + " are all talking about " + comboAdjVerb + ". " + pers1 + pers2 + pers3 + "They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";
                }
                else
                {
                    prompt1 = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + " and " + rndChar2 + " are all talking about " + comboAdjVerb + ". " + pers1 + pers2 + "They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
            }

            if (itopic != "")
            {
                if (rndChar4 != "")
                {
                    promptspecial = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + ", " + rndChar2 + ", " + rndChar3 + ", and " + rndChar4 + " are having a lengthy discussion about an image of " + Regex.Replace(itopic, @"\[.*\]", "") + ". They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters and should be long. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
                else if (rndChar3 != "")
                {
                    promptspecial = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + ", " + rndChar2 + ", " + rndChar3 + ", and " + rndChar4 + " are having a lengthy discussion about an image of " + Regex.Replace(itopic, @"\[.*\]", "") + ". They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters and should be long. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";
                }
                else
                {
                    promptspecial = "{\"model\":\"text-curie-001\",\"prompt\":\"" + rndChar1 + ", " + rndChar2 + ", " + rndChar3 + ", and " + rndChar4 + " are having a lengthy discussion about an image of  " + Regex.Replace(itopic, @"\[.*\]", "") + ". They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters and should be long. Their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
            }
            else
            {
                if (rndChar4 != "")
                {
                    promptspecial = "{\"model\":\"text-curie-001\",\"prompt\":\"" + pers1 + pers2 + pers3 + pers4 + "They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters. They are having a discussion about an image of a " + imageprompt + ", and their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
                else if (rndChar3 != "")
                {
                    promptspecial = "{\"model\":\"text-curie-001\",\"prompt\":\"" + pers1 + pers2 + pers3 + "They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters. They are having a discussion about an image of a " + imageprompt + ", and their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";
                }
                else
                {
                    promptspecial = "{\"model\":\"text-curie-001\",\"prompt\":\"" + pers1 + pers2 + "They are from the video game Metal Gear Solid and you have to talk like them. Your response should contain only the conversation between the characters. They are having a discussion about an image of a " + imageprompt + ", and their conversation starts with " + rndChar1 + " and should be in the following format:\\n" + rndChar1 + ":\",\"temperature\":0.8,\"max_tokens\":1500,\"top_p\":1.0,\"frequency_penalty\":0.5,\"presence_penalty\":0.0}";

                }
            }

        }

        private string ValidateToken()
        {
            var tokFile = File.ReadAllLines("token.txt");
            var tokList = new List<string>(tokFile);
            string token = (string)tokList[0];
            string validation = "null";
            //validation should be twitch client ID
            try
            {
                if (api.Auth.ValidateAccessTokenAsync(token).Result != null)
                {
                    validation = "";
                }

                WriteToLog("CLIENT ID: " + validation, rndSessionID);
                return validation;
            }
            catch (Exception ex)
            {
                validation = "null";
                WriteToLog("CLIENT ID: VALIDATION FAILED " + ex.Message, rndSessionID);
                return validation;
            }

        }

        private void RefreshToken()
        {
            var tokFile = File.ReadAllLines("refreshtoken.txt");
            var tokList = new List<string>(tokFile);
            string token = (string)tokList[0];
            
            //Twitch client secret should be below
            var response = api.Auth.RefreshAuthTokenAsync(token, "");


            if (response == null)
            {
                WriteToLog("REFRESH TOKEN was null!", rndSessionID);
                return;
            }

            string refreshtoken = response.GetAwaiter().GetResult().RefreshToken;
            string newtoken = response.GetAwaiter().GetResult().AccessToken;
            WriteToLog("REFRESH TOKEN: " + refreshtoken, rndSessionID);
            WriteToLog("ACCESS TOKEN: " + newtoken, rndSessionID);
            File.Delete("refreshtoken.txt");
            File.Delete("token.txt");
            try
            {
                if (!File.Exists("refreshtoken.txt"))
                {
                    using (StreamWriter sw = File.CreateText("refreshtoken.txt"))
                    {
                        sw.Write(refreshtoken);
                    }
                }
                else
                {
                    using StreamWriter file = new("refreshtoken.txt", append: false);
                    file.Write(refreshtoken);
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex.Message, rndSessionID);
            }
            try
            {
                if (!File.Exists("token.txt"))
                {
                    using (StreamWriter sw = File.CreateText("token.txt"))
                    {
                        sw.Write(newtoken);
                    }
                }
                else
                {
                    using StreamWriter file = new("token.txt", append: false);
                    file.Write(newtoken);
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex.Message, rndSessionID);
            }


            ConnectionCredentials credentials = new ConnectionCredentials("foxhoundai", newtoken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };


            //Twitch client ID should be below
            api.Settings.ClientId = "";
            api.Settings.AccessToken = newtoken; // App Secret is not an Accesstoken

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            if (tclient.IsConnected) { tclient.Disconnect(); }
            tclient.SetConnectionCredentials(credentials);
            if (!tclient.IsConnected || !tclient.IsInitialized)
            {
                tclient.Initialize(credentials, "foxhoundai");
                tclient.OnLog += Client_OnLog;
                tclient.OnJoinedChannel += Client_OnJoinedChannel;
                tclient.OnMessageReceived += Client_OnMessageReceived;
                tclient.OnConnected += Client_OnConnected;
                tclient.OnConnectionError += Client_OnConnectionError;
                tclient.Connect();
            }


        }

        protected override void Update(GameTime gameTime)
        {

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (MediaPlayer.State == MediaState.Stopped)
            {
                LoadMusic();
            }

            if ((float)gameTime.TotalGameTime.TotalSeconds - messageTime > 2700) 
            {
                tclient.SendMessage("foxhoundai", "To add a topic, type !topic and then a description. To generate an !image, type !image and a description. Character names can be specified in [] brackets to be included in the conversation. Available commands are !help !charhelp !info !queue !image !topic");

                messageTime = (float)gameTime.TotalGameTime.TotalSeconds;
            }
            //anti calling crash bug
            if ((float)gameTime.TotalGameTime.TotalSeconds - callTime > 30 && isCalling) { this.Exit(); }

            if (timeAPIcheck == 0)
            {
                timeAPIcheck = (float)gameTime.TotalGameTime.TotalSeconds;
            }
            else if ((float)gameTime.TotalGameTime.TotalSeconds - timeAPIcheck > 5 && timeAPIcheck != -1)
            {
                timeAPIcheck = -1;
            }

            DetermineBG(gameTime);

            if ((float)gameTime.TotalGameTime.TotalSeconds - timeSinceLastUpdate > 1.1)
            {
                timeSinceLastUpdate = (float)gameTime.TotalGameTime.TotalSeconds;
                if (holdingDialog != "" && isTalking == false && dialogOver == false && isCalling == false)
                {
                    isTalking = true;
                    speakDialog = ProcessDialog(holdingDialog);


                    outputDevice = new WaveOutEvent();
                    SynthesisInput input = new SynthesisInput
                    {
                        Text = speakDialog
                    };

                    VoiceSelectionParams voiceSelection = new VoiceSelectionParams
                    {
                        LanguageCode = locale,
                        SsmlGender = voiceGender,
                        Name = voiceName
                        //SsmlPitch - SsmlPitc
                    };

                    AudioConfig audioConfig = new AudioConfig
                    {
                        AudioEncoding = AudioEncoding.Mp3,
                        SpeakingRate = speakingRate,
                        Pitch = pitch

                    };
                    SynthesizeSpeechResponse response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);
                    try
                    {
                        using (Stream output = File.Create(savedFileName))
                        {

                            WriteToLog("Wrote audio file " + savedFileName, rndSessionID);
                            response.AudioContent.WriteTo(output);
                        }
                    }
                    catch (Exception e)
                    {
                        WriteToLog("Error writing audio file " + savedFileName, rndSessionID);
                        WriteToLog(e.Message, rndSessionID);
                    }

                    audioFile = new AudioFileReader(savedFileName);
                    try
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                    }
                    catch (Exception ex)
                    {
                        isTalking = false;
                        currentDraw = "Some Google Voice API error happened.";
                        WriteToLog(ex.ToString(), rndSessionID);
                    }

                }
            }
            else
            {
                if (isTalking == true)
                {
                    if (outputDevice.PlaybackState == PlaybackState.Stopped || (float)gameTime.TotalGameTime.TotalSeconds - timeSingConvo > 160)
                    {
                        if ((float)gameTime.TotalGameTime.TotalSeconds - timeSingConvo > 160) { outputDevice.Stop(); dialogOver = true; }
                        File.Delete(savedFileName);
                        isTalking = false;
                        if (dialogOver == true)
                        {
                            JoinedChannel joinedChannel;
                            try
                            {
                                joinedChannel = tclient.GetJoinedChannel("foxhoundai");
                            }
                            catch(Exception ex)
                            {
                                joinedChannel = null;
                                WriteToLog("Joined channel check failed!\n" + ex.ToString(), rndSessionID);
                            }

                            if (ValidateToken() != "" || !tclient.IsConnected || !tclient.IsInitialized || joinedChannel == null)                            {
                                RefreshToken();
                            }
                            holdingDialog = "";
                            timeSingConvo = (float)gameTime.TotalGameTime.TotalSeconds;
                            char1talking = false;
                            char2talking = false;
                            goodForNext = true;
                            codecClose.Play();
                            callEnding = true;
                        }
                    }
                }
                if (dialogOver == true && (float)gameTime.TotalGameTime.TotalSeconds - timeSingConvo > 3 && goodForNext == true)
                {
                    goodForNext = false;

                    if (specialEvent == true)
                    {
                        specialEvent = false;
                        doneSavingImage = false;
                    }


                    if (topicList.Count > 0 && lastTopicType == 1 || (lastTopicType == 0 && topicList.Count > 0 && itopicList.Count == 0))
                    { justUsedGenTopic = true; topic = topicList[0]; topicList.RemoveAt(0); lastTopicType = 0; lastTopic = topic; }
                    else if (itopicList.Count > 0 && lastTopicType == 0 || (lastTopicType == 1 && itopicList.Count > 0 && topicList.Count == 0))
                    { justUsedGenTopic = true; itopic = itopicList[0]; itopicList.RemoveAt(0); lastTopicType = 1; topic = ""; lastTopic = itopic; }
                    else
                    {
                        topic = ""; usingTopic = false; itopic = ""; usingItopic = false; justUsedGenTopic = false;
                    }
                    int eventRnd = bgRnd.Next(100);
                    if (eventRnd >= 85 || eventRnd <= 5 || itopic != "")
                    {
                        if (topic == "")
                        {
                            specialEvent = true;

                        }
                    }
                    dialogOver = false;

                    GeneratePrompt();
                    if (specialEvent == true)
                    {
                        HandleImageAPIRequest();
                    }
                    outputDevice = new WaveOutEvent();
                    audioFile = new AudioFileReader("codeccall.mp3");
                    callEnding = false;
                    outputDevice.Init(audioFile);
                    playingCallSound = false;



                    isCalling = true;
                    callTime = (float)gameTime.TotalGameTime.TotalSeconds;
                    int rnd1 = bgRnd.Next(100, 200);
                    int rnd2 = bgRnd.Next(10, 99);

                    if (startChar2 == "snake")
                    {
                        station = (rnd1 + "." + rnd2);
                    }
                    else if (startChar2 == "liquid")
                    {
                        station = "141.80";
                    }
                    else if (startChar2 == "miller")
                    {
                        station = "141.80";
                    }
                    else if (startChar2 == "ocelot")
                    {
                        station = (rnd1 + "." + rnd2);
                    }
                    else if (startChar2 == "raiden")
                    {
                        station = (rnd1 + "." + rnd2);
                    }
                    else if (startChar2 == "otacon")
                    {
                        station = "141.12";
                    }
                    else if (startChar2 == "nastasha")
                    {
                        station = "141.52";
                    }
                    else if (startChar2 == "campbell")
                    {
                        station = "140.85";
                    }
                    else if (startChar2 == "naomi")
                    {
                        station = "140.85";
                    }
                    else if (startChar2 == "mei")
                    {
                        station = "140.96";
                    }
                    else if (startChar2 == "meryl")
                    {
                        station = "140.15";
                    }
                    else if (startChar2 == "decoy")
                    {
                        station = (rnd1 + "." + rnd2);
                    }
                    else if (startChar2 == "wolf")
                    {
                        station = (rnd1 + "." + rnd2);
                    }

                    HandleAPIRequest();
                }

            }


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            if (specialEvent == true)
            {
                _bgSpecial.Begin();
                _bgSpecial.Draw(
                    bgAnimSpecial,
                    new Vector2(_graphics.PreferredBackBufferWidth / 2,
    _graphics.PreferredBackBufferHeight / 2 - 80),
                    null,
                    Microsoft.Xna.Framework.Color.White,
                    0f,
                    new Vector2(bgAnimSpecial.Width / 2, bgAnimSpecial.Height / 2),
                    Vector2.One,
                    SpriteEffects.None,
                    0f
                    );
                _bgSpecial.End();
                if (doneSavingImage == false)
                {
                    DrawString("Loading...", _graphics.PreferredBackBufferWidth / 2 - 115,
    _graphics.PreferredBackBufferHeight / 2 - 245, _loadingBatch, scoreFont, Microsoft.Xna.Framework.Color.Aquamarine);
                }
                else
                {
                    DrawString("Image:", _graphics.PreferredBackBufferWidth / 2 - 115,
    _graphics.PreferredBackBufferHeight / 2 - 245, _loadingBatch, scoreFont, Microsoft.Xna.Framework.Color.Aquamarine);
                }
            }
            if (isCalling == false && callEnding == false)
            {
                DrawString("!topic - Set a topic   !image - Generate an image", 260, 418, _helpbatch, scoreFont, Microsoft.Xna.Framework.Color.MediumAquamarine);
                if (justUsedGenTopic == true && lastTopic != "")
                {
                    DrawString("\nCurrent topic is " + lastTopic, 260, 410, _helpbatch, scoreFont, Microsoft.Xna.Framework.Color.MediumAquamarine);
                }
                else if (nextTopic != "")
                {
                    DrawString("\nNext topic is set to " + nextTopic, 260, 410, _helpbatch, scoreFont, Microsoft.Xna.Framework.Color.MediumAquamarine);
                }

                DrawString(string.Concat(startChar2[0].ToString().ToUpper(), startChar2.AsSpan(1)), 189, 30, _name1batch, scoreFont, Microsoft.Xna.Framework.Color.MediumAquamarine);
                DrawString(string.Concat(startChar1[0].ToString().ToUpper(), startChar1.AsSpan(1)), 879, 30, _name2batch, scoreFont, Microsoft.Xna.Framework.Color.MediumAquamarine);
                DrawCodex(0, startChar1, gameTime, 1);
                DrawCodex(0, startChar2, gameTime, 0);
                if (specialEvent == true)
                {
                    DrawString(station, _graphics.PreferredBackBufferWidth / 2,
    _graphics.PreferredBackBufferHeight / 2 - 320, _stationBatch, alarmFont, Microsoft.Xna.Framework.Color.Aquamarine);

                }
                else
                {
                    DrawString(station, 642, 215, _stationBatch, alarmFont, Microsoft.Xna.Framework.Color.Aquamarine);
                }
            }
            _bgSprite.Begin();
            _bgSprite.Draw(
                bgAnim,
                new Vector2(_graphics.PreferredBackBufferWidth / 2,
_graphics.PreferredBackBufferHeight / 2),
                null,
                Microsoft.Xna.Framework.Color.White,
                0f,
                new Vector2(bgAnim.Width / 2, bgAnim.Height / 2),
                Vector2.One,
                SpriteEffects.None,
                0f
                );
            _bgSprite.End();

            if (currentDraw != null)
            {
                DrawString(currentDraw, 100, 490, _logBatch, scoreFont, Microsoft.Xna.Framework.Color.White);
            }
            base.Draw(gameTime);
        }


        public static bool ContainsAny(string s, List<string> substrings)
        {
            if (string.IsNullOrEmpty(s) || substrings == null)
                return false;

            return substrings.Any(substring => s.Contains(substring, StringComparison.CurrentCultureIgnoreCase));
        }

        private static string SanitizeTopics(string usertopic)
        {


            var slurs = File.ReadAllLines("content\\slurs.txt");
            var slurList = new List<string>(slurs);
            bool b = ContainsAny(usertopic, slurList);
            if ( b || usertopic.ToLower().Contains("\"") || usertopic.ToLower().Contains("\\") || usertopic.ToLower().Contains("/") || usertopic.ToLower().Contains("{") || usertopic.ToLower().Contains("}"))
            {
                usertopic = "";
            }
            return usertopic;
        }

        private async void HandleAPIRequest()
        {
            sessioncount += 1;
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            //Open AI Bearer Token should be in the empty quotes below
            client.DefaultRequestHeaders.Add("Authorization", "");
            string json = prompt1;
            if (specialEvent == true)
            {
                json = promptspecial;
            }
            WriteToLog(json, rndSessionID);

            var SerializedJSON = JsonConvert.SerializeObject(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await ProcessRepositoriesAsync(client, content);
        }

        private async void HandleImageAPIRequest()
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            //Open AI Bearer Token should be in the empty quotes below
            client.DefaultRequestHeaders.Add("Authorization", "");
            string json = "";
            if (itopic != "")
            {
                json = "{\"prompt\":\"" + itopic + "\",\"n\":1,\"size\":\"256x256\"}";

            }
            else
            {
                json = "{\"prompt\":\"" + imageprompt + "\",\"n\":1,\"size\":\"256x256\"}";

            }
            var SerializedJSON = JsonConvert.SerializeObject(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await ProcessImageRepositoriesAsync(client, content);
            doneSavingImage = true;


        }

        public async Task ProcessRepositoriesAsync(HttpClient client, StringContent content)
        {
            try
            {
                var result = await client.PostAsync("https://api.openai.com/v1/completions", content);
                string resultContent = await result.Content.ReadAsStringAsync();
                WriteToLog(resultContent, rndSessionID);
                holdingDialog = Deserialize.ParseResponse(resultContent, rndSessionID, startChar1, startChar2, startChar3, startChar4);
            }
            catch (Exception ex)
            {
                WriteToLog("API Error!" + ex.ToString(), rndSessionID);
                holdingDialog = "";
                currentDraw = "";
                timeSingConvo = 10;
                dialogOver = true;
                goodForNext = true;
            }
        }

        public async Task ProcessImageRepositoriesAsync(HttpClient client, StringContent content)
        {
            try
            {
                var result = await client.PostAsync("https://api.openai.com/v1/images/generations", content);
                string resultContent = await result.Content.ReadAsStringAsync();
                WriteToLog(resultContent, rndSessionID);
                imageURL = Deserialize.ParseImageResponse(resultContent, rndSessionID);
                WriteToLog(imageURL, rndSessionID);
                string urlToDownload = imageURL;

                string pathToSave = "saved.png";
                if (imageURL.IndexOf("invalid_request_error") == -1)
                {
                    WebClient imageclient = new WebClient();
                    imageclient.DownloadFile(urlToDownload, pathToSave);
                }
                else
                {
                    File.Copy("placeholder.png", "saved.png", true);
                }

            }
            catch(Exception ex)
            {
                File.Copy("placeholder.png", "saved.png", true);
                WriteToLog("Exception during image request: " + ex.Message, rndSessionID);
                return;
            }
        }

        private Texture2D PremultiplyTexture(String FilePath, GraphicsDevice device)
        {
            Texture2D texture;

            FileStream titleStream = File.OpenRead(FilePath);
            texture = Texture2D.FromStream(device, titleStream);
            titleStream.Close();
            Microsoft.Xna.Framework.Color[] buffer = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
            texture.GetData(buffer);
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Microsoft.Xna.Framework.Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
            texture.SetData(buffer);

            return texture;
        }
        private void DetermineBG(GameTime gameTime)
        {
            if (((float)gameTime.TotalGameTime.TotalSeconds - timeSinceBG) < 0.1)
            {
                return;
            }
            else if (isCalling == false && callEnding == false)
            {
                if (specialEvent == true)
                {
                    timeSinceBG = (float)gameTime.TotalGameTime.TotalSeconds;
                    if (doneSavingImage == true)
                    {
                        bgAnimSpecial = PremultiplyTexture("saved.png", _graphics.GraphicsDevice);
                        bgAnim = _texLoad.LoadContent("codex\\bg_transparent", this);

                    }
                    else
                    {

                        bgAnimSpecial = PremultiplyTexture("placeholder.png", _graphics.GraphicsDevice);
                        bgAnim = _texLoad.LoadContent("codex\\bg_transparent", this);
                    }
                }
                else
                {
                    timeSinceBG = (float)gameTime.TotalGameTime.TotalSeconds;
                    int rnd = bgRnd.Next(4);
                    if (rnd == 0)
                    {
                        bgAnim = _texLoad.LoadContent("codex\\codexframe1", this);

                    }
                    else if (rnd == 1)
                    {
                        bgAnim = _texLoad.LoadContent("codex\\codexframe2", this);
                    }
                    else if (rnd == 2)
                    {
                        bgAnim = _texLoad.LoadContent("codex\\codexframe3", this);
                    }
                    else if (rnd == 3)
                    {
                        bgAnim = _texLoad.LoadContent("codex\\codexframe4", this);
                    }
                }
            }
            else if (isCalling == true && callEnding == false)
            {
                if (((float)gameTime.TotalGameTime.TotalSeconds - timeSinceBG) < 0.2)
                {
                    return;
                }
                currentDraw = "";
                timeSinceBG = (float)gameTime.TotalGameTime.TotalSeconds;
                //int rnd = bgRnd.Next(2);
                if (bgAnim == callBG2)
                {
                    bgAnim = _texLoad.LoadContent("codex\\bg_call", this);
                }
                else
                {
                    bgAnim = _texLoad.LoadContent("codex\\bg_black", this);
                }
                if (outputDevice.PlaybackState == PlaybackState.Stopped && playingCallSound == false)
                {
                    playingCallSound = true;
                    outputDevice.Play();
                }
                else if (outputDevice.PlaybackState == PlaybackState.Stopped && playingCallSound == true)
                {
                    isCalling = false;
                    codecOpen.Play();
                }
            }
            else
            {
                currentDraw = "";
                timeSinceBG = (float)gameTime.TotalGameTime.TotalSeconds;
                bgAnim = _texLoad.LoadContent("codex\\bg_black", this);
            }
        }

        public int FindSecondChar(string firstChar, int snakeIndex, int ocelotIndex, int liquidIndex, int meiIndex, int nasIndex, int naomiIndex, int wolfIndex, int decoyIndex, int campbellIndex, int millerIndex, int otaconIndex, int merylIndex, int raidenIndex)
        {
            //1
            if (firstChar == "snake")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //2
            if (firstChar == "ocelot")
            {
                if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //3
            if (firstChar == "liquid")
            {
                if (ocelotIndex != -1 && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (meiIndex != -1 && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //4
            if (firstChar == "mei")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //5
            if (firstChar == "nas")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //6
            if (firstChar == "naomi")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //7
            if (firstChar == "wolf")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //8
            if (firstChar == "decoy")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //9
            if (firstChar == "campbell")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //10
            if (firstChar == "miller")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //11
            if (firstChar == "otacon")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
                {
                    return merylIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //12
            if (firstChar == "meryl")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
                {
                    return otaconIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
                {
                    return snakeIndex;
                }
                else if (raidenIndex != -1 && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1) && (raidenIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return raidenIndex;
                }
                else
                {
                    return -1;
                }
            }

            //13
            if (firstChar == "raiden")
            {
                if (ocelotIndex != -1 && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < snakeIndex || snakeIndex == -1))
                {
                    return ocelotIndex;
                }
                else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1))
                {
                    return liquidIndex;
                }
                else if (meiIndex != -1 && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1))
                {
                    return meiIndex;
                }
                else if (nasIndex != -1 && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1))
                {
                    return nasIndex;
                }
                else if (naomiIndex != -1 && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1))
                {
                    return naomiIndex;
                }
                else if (wolfIndex != -1 && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1))
                {
                    return wolfIndex;
                }
                else if (decoyIndex != -1 && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1))
                {
                    return decoyIndex;
                }
                else if (campbellIndex != -1 && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1))
                {
                    return campbellIndex;
                }
                else if (millerIndex != -1 && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1))
                {
                    return millerIndex;
                }
                else if (otaconIndex != -1 && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1))
                {
                    return otaconIndex;
                }
                else if (merylIndex != -1 && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1))
                {
                    return merylIndex;
                }
                else if (snakeIndex != -1 && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < ocelotIndex || ocelotIndex == -1))
                {
                    return snakeIndex;
                }
                else
                {
                    return -1;
                }
            }


            return -1;
        }

        public void ChangeVoice(string char1)
        {
            if (char1 == "campbell")
            {
                pitch = -20;
                locale = "en-US";
                voiceName = "";
                voiceGender = SsmlVoiceGender.Male;
                speakingRate = 1.0;
            }
            else if (char1 == "snake")
            {
                pitch = -20;
                locale = "en-US";
                voiceName = "en-US-Neural2-D";
                voiceGender = SsmlVoiceGender.Male;
                speakingRate = 1.0;
            }
            else if (char1 == "decoy")
            {
                pitch = -5;
                locale = "en-AU";
                voiceName = "en-AU-Standard-B";
                voiceGender = SsmlVoiceGender.Male;
                speakingRate = 1.0;
            }
            else if (char1 == "otacon")
            {
                pitch = 10;
                locale = "en-US";
                voiceGender = SsmlVoiceGender.Male;
                voiceName = "";
                speakingRate = 1.2;
            }
            else if (char1 == "raiden")
            {
                pitch = 5;
                locale = "en-US";
                voiceGender = SsmlVoiceGender.Male;
                voiceName = "en-US-Standard-D";
                speakingRate = 1.0;
            }
            else if (char1 == "nastasha")
            {
                pitch = 0;
                locale = "ru-RU";
                voiceName = "ru-RU-Standard-A";
                voiceGender = SsmlVoiceGender.Female;
                speakingRate = 1.2;
            }
            else if (char1 == "miller")
            {
                pitch = -7;
                locale = "en-US";
                voiceName = "en-US-Standard-A";
                voiceGender = SsmlVoiceGender.Male;
                speakingRate = 0.8;
            }
            else if (char1 == "mei")
            {
                pitch = 2;
                locale = "cmn-CN";
                voiceName = "cmn-CN-Standard-D";
                voiceGender = SsmlVoiceGender.Female;
                speakingRate = 1.3;
            }
            else if (char1 == "liquid")
            {
                pitch = 5;
                locale = "en-GB";
                voiceName = "en-GB-Neural2-D";
                voiceGender = SsmlVoiceGender.Male;
                speakingRate = 0.8;
            }
            else if (char1 == "meryl")
            {
                pitch = 0;
                locale = "en-US";
                voiceName = "en-US-Standard-H";
                voiceGender = SsmlVoiceGender.Female;
                speakingRate = 1.2;
            }
            else if (char1 == "wolf")
            {
                pitch = -5;
                locale = "pl-PL";
                voiceName = "pl-PL-Standard-E";
                voiceGender = SsmlVoiceGender.Female;
                speakingRate = 1.0;
            }
            else if (char1 == "naomi")
            {
                pitch = -3;
                locale = "en-GB";
                voiceName = "en-GB-Standard-A";
                voiceGender = SsmlVoiceGender.Female;
                speakingRate = 1.1;
            }
            else if (char1 == "ocelot")
            {
                pitch = -3;
                locale = "en-US";
                voiceName = "en-US-Standard-B";
                voiceGender = SsmlVoiceGender.Male;
                speakingRate = 1.1;
            }
        }

        public string ProcessDialog(string dialog)
        {


            int snakeIndex = dialog.IndexOf("Snake:");
            int ocelotIndex = dialog.IndexOf("Ocelot:");
            int liquidIndex = dialog.IndexOf("Liquid:");
            int meiIndex = dialog.IndexOf("Mei Ling:");
            int nasIndex = dialog.IndexOf("Nastasha:");
            int naomiIndex = dialog.IndexOf("Naomi:");
            int wolfIndex = dialog.IndexOf("Sniper Wolf:");
            int decoyIndex = dialog.IndexOf("Decoy Octopus:");
            int campbellIndex = dialog.IndexOf("Campbell:");
            int millerIndex = dialog.IndexOf("Miller:");
            int otaconIndex = dialog.IndexOf("Otacon:");
            int merylIndex = dialog.IndexOf("Meryl:");
            int raidenIndex = dialog.IndexOf("Raiden:");

            string char1;
            string regexTest = dialog;
            int first = 0;
            int last = 0;
            int char1length = 0;
            string tempNameSelect = "";
            int testIndex1 = 0;
            int testIndex2 = 0;


            if (snakeIndex != -1 && (snakeIndex < ocelotIndex || ocelotIndex == -1) && (snakeIndex < campbellIndex || campbellIndex == -1) && (snakeIndex < liquidIndex || liquidIndex == -1) && (snakeIndex < meiIndex || meiIndex == -1) && (snakeIndex < nasIndex || nasIndex == -1) && (snakeIndex < naomiIndex || naomiIndex == -1) && (snakeIndex < wolfIndex || wolfIndex == -1) && (snakeIndex < decoyIndex || decoyIndex == -1) && (snakeIndex < millerIndex || millerIndex == -1) && (snakeIndex < otaconIndex || otaconIndex == -1) && (snakeIndex < merylIndex || merylIndex == -1) && (snakeIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = snakeIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 6;
                tempNameSelect = "Snake:";
                char1 = "snake";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }       
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 6;
                testIndex2 = FindSecondChar("snake", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (ocelotIndex != -1 && (ocelotIndex < snakeIndex || snakeIndex == -1) && (ocelotIndex < campbellIndex || campbellIndex == -1) && (ocelotIndex < liquidIndex || liquidIndex == -1) && (ocelotIndex < meiIndex || meiIndex == -1) && (ocelotIndex < nasIndex || nasIndex == -1) && (ocelotIndex < naomiIndex || naomiIndex == -1) && (ocelotIndex < wolfIndex || wolfIndex == -1) && (ocelotIndex < decoyIndex || decoyIndex == -1) && (ocelotIndex < millerIndex || millerIndex == -1) && (ocelotIndex < otaconIndex || otaconIndex == -1) && (ocelotIndex < merylIndex || merylIndex == -1) && (ocelotIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = ocelotIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 7;
                tempNameSelect = "Ocelot:";
                char1 = "ocelot";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 7;
                testIndex2 = FindSecondChar("ocelot", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (liquidIndex != -1 && (liquidIndex < ocelotIndex || ocelotIndex == -1) && (liquidIndex < campbellIndex || campbellIndex == -1) && (liquidIndex < snakeIndex || snakeIndex == -1) && (liquidIndex < meiIndex || meiIndex == -1) && (liquidIndex < nasIndex || nasIndex == -1) && (liquidIndex < naomiIndex || naomiIndex == -1) && (liquidIndex < wolfIndex || wolfIndex == -1) && (liquidIndex < decoyIndex || decoyIndex == -1) && (liquidIndex < millerIndex || millerIndex == -1) && (liquidIndex < otaconIndex || otaconIndex == -1) && (liquidIndex < merylIndex || merylIndex == -1) && (liquidIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = liquidIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 7;
                tempNameSelect = "Liquid:";
                char1 = "liquid";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 7;
                testIndex2 = FindSecondChar("liquid", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (meiIndex != -1 && (meiIndex < ocelotIndex || ocelotIndex == -1) && (meiIndex < campbellIndex || campbellIndex == -1) && (meiIndex < liquidIndex || liquidIndex == -1) && (meiIndex < snakeIndex || snakeIndex == -1) && (meiIndex < nasIndex || nasIndex == -1) && (meiIndex < naomiIndex || naomiIndex == -1) && (meiIndex < wolfIndex || wolfIndex == -1) && (meiIndex < decoyIndex || decoyIndex == -1) && (meiIndex < millerIndex || millerIndex == -1) && (meiIndex < otaconIndex || otaconIndex == -1) && (meiIndex < merylIndex || merylIndex == -1) && (meiIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = meiIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 9;
                tempNameSelect = "Mei Ling:";
                char1 = "mei";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }

                char1length = 9;
                ChangeVoice(char1);
                testIndex2 = FindSecondChar("mei", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (nasIndex != -1 && (nasIndex < ocelotIndex || ocelotIndex == -1) && (nasIndex < campbellIndex || campbellIndex == -1) && (nasIndex < liquidIndex || liquidIndex == -1) && (nasIndex < meiIndex || meiIndex == -1) && (nasIndex < snakeIndex || snakeIndex == -1) && (nasIndex < naomiIndex || naomiIndex == -1) && (nasIndex < wolfIndex || wolfIndex == -1) && (nasIndex < decoyIndex || decoyIndex == -1) && (nasIndex < millerIndex || millerIndex == -1) && (nasIndex < otaconIndex || otaconIndex == -1) && (nasIndex < merylIndex || merylIndex == -1) && (nasIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = nasIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 9;
                tempNameSelect = "Nastasha:";
                char1 = "nastasha";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 9;
                testIndex2 = FindSecondChar("nas", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (naomiIndex != -1 && (naomiIndex < ocelotIndex || ocelotIndex == -1) && (naomiIndex < campbellIndex || campbellIndex == -1) && (naomiIndex < liquidIndex || liquidIndex == -1) && (naomiIndex < meiIndex || meiIndex == -1) && (naomiIndex < nasIndex || nasIndex == -1) && (naomiIndex < snakeIndex || snakeIndex == -1) && (naomiIndex < wolfIndex || wolfIndex == -1) && (naomiIndex < decoyIndex || decoyIndex == -1) && (naomiIndex < millerIndex || millerIndex == -1) && (naomiIndex < otaconIndex || otaconIndex == -1) && (naomiIndex < merylIndex || merylIndex == -1) && (naomiIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = naomiIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 6;
                tempNameSelect = "Naomi:";
                char1 = "naomi";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 6;
                testIndex2 = FindSecondChar("naomi", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (wolfIndex != -1 && (wolfIndex < ocelotIndex || ocelotIndex == -1) && (wolfIndex < campbellIndex || campbellIndex == -1) && (wolfIndex < liquidIndex || liquidIndex == -1) && (wolfIndex < meiIndex || meiIndex == -1) && (wolfIndex < nasIndex || nasIndex == -1) && (wolfIndex < naomiIndex || naomiIndex == -1) && (wolfIndex < snakeIndex || snakeIndex == -1) && (wolfIndex < decoyIndex || decoyIndex == -1) && (wolfIndex < millerIndex || millerIndex == -1) && (wolfIndex < otaconIndex || otaconIndex == -1) && (wolfIndex < merylIndex || merylIndex == -1) && (wolfIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = wolfIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 12;
                tempNameSelect = "Sniper Wolf:";
                char1 = "wolf";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }

                ChangeVoice(char1);
                char1length = 12;
                testIndex2 = FindSecondChar("wolf", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (decoyIndex != -1 && (decoyIndex < ocelotIndex || ocelotIndex == -1) && (decoyIndex < campbellIndex || campbellIndex == -1) && (decoyIndex < liquidIndex || liquidIndex == -1) && (decoyIndex < meiIndex || meiIndex == -1) && (decoyIndex < nasIndex || nasIndex == -1) && (decoyIndex < naomiIndex || naomiIndex == -1) && (decoyIndex < wolfIndex || wolfIndex == -1) && (decoyIndex < snakeIndex || snakeIndex == -1) && (decoyIndex < millerIndex || millerIndex == -1) && (decoyIndex < otaconIndex || otaconIndex == -1) && (decoyIndex < merylIndex || merylIndex == -1) && (decoyIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = decoyIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 14;
                tempNameSelect = "Decoy Octopus:";
                char1 = "decoy";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }

                ChangeVoice(char1);
                char1length = 14;
                testIndex2 = FindSecondChar("decoy", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (millerIndex != -1 && (millerIndex < ocelotIndex || ocelotIndex == -1) && (millerIndex < campbellIndex || campbellIndex == -1) && (millerIndex < liquidIndex || liquidIndex == -1) && (millerIndex < meiIndex || meiIndex == -1) && (millerIndex < nasIndex || nasIndex == -1) && (millerIndex < naomiIndex || naomiIndex == -1) && (millerIndex < wolfIndex || wolfIndex == -1) && (millerIndex < decoyIndex || decoyIndex == -1) && (millerIndex < snakeIndex || snakeIndex == -1) && (millerIndex < otaconIndex || otaconIndex == -1) && (millerIndex < merylIndex || merylIndex == -1) && (millerIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = millerIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 7;
                tempNameSelect = "Miller:";
                char1 = "miller";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 7;
                testIndex2 = FindSecondChar("miller", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (otaconIndex != -1 && (otaconIndex < ocelotIndex || ocelotIndex == -1) && (otaconIndex < campbellIndex || campbellIndex == -1) && (otaconIndex < liquidIndex || liquidIndex == -1) && (otaconIndex < meiIndex || meiIndex == -1) && (otaconIndex < nasIndex || nasIndex == -1) && (otaconIndex < naomiIndex || naomiIndex == -1) && (otaconIndex < wolfIndex || wolfIndex == -1) && (otaconIndex < decoyIndex || decoyIndex == -1) && (otaconIndex < millerIndex || millerIndex == -1) && (otaconIndex < snakeIndex || snakeIndex == -1) && (otaconIndex < merylIndex || merylIndex == -1) && (otaconIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = otaconIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 7;
                tempNameSelect = "Otacon:";
                char1 = "otacon";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 7;
                testIndex2 = FindSecondChar("otacon", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (merylIndex != -1 && (merylIndex < ocelotIndex || ocelotIndex == -1) && (merylIndex < campbellIndex || campbellIndex == -1) && (merylIndex < liquidIndex || liquidIndex == -1) && (merylIndex < meiIndex || meiIndex == -1) && (merylIndex < nasIndex || nasIndex == -1) && (merylIndex < naomiIndex || naomiIndex == -1) && (merylIndex < wolfIndex || wolfIndex == -1) && (merylIndex < decoyIndex || decoyIndex == -1) && (merylIndex < millerIndex || millerIndex == -1) && (merylIndex < otaconIndex || otaconIndex == -1) && (merylIndex < snakeIndex || snakeIndex == -1) && (merylIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = merylIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 6;
                tempNameSelect = "Meryl:";
                char1 = "meryl";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 6;
                testIndex2 = FindSecondChar("meryl", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (raidenIndex != -1 && (raidenIndex < ocelotIndex || ocelotIndex == -1) && (raidenIndex < campbellIndex || campbellIndex == -1) && (raidenIndex < liquidIndex || liquidIndex == -1) && (raidenIndex < meiIndex || meiIndex == -1) && (raidenIndex < nasIndex || nasIndex == -1) && (raidenIndex < naomiIndex || naomiIndex == -1) && (raidenIndex < wolfIndex || wolfIndex == -1) && (raidenIndex < decoyIndex || decoyIndex == -1) && (raidenIndex < millerIndex || millerIndex == -1) && (raidenIndex < otaconIndex || otaconIndex == -1) && (raidenIndex < merylIndex || merylIndex == -1) && (raidenIndex < snakeIndex || snakeIndex == -1))
            {
                testIndex1 = raidenIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 7;
                tempNameSelect = "Raiden:";
                char1 = "raiden";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 7;
                testIndex2 = FindSecondChar("raiden", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else if (campbellIndex != -1 && (campbellIndex < ocelotIndex || ocelotIndex == -1) && (campbellIndex < liquidIndex || liquidIndex == -1) && (campbellIndex < meiIndex || meiIndex == -1) && (campbellIndex < nasIndex || nasIndex == -1) && (campbellIndex < naomiIndex || naomiIndex == -1) && (campbellIndex < wolfIndex || wolfIndex == -1) && (campbellIndex < decoyIndex || decoyIndex == -1) && (campbellIndex < millerIndex || millerIndex == -1) && (campbellIndex < otaconIndex || otaconIndex == -1) && (campbellIndex < merylIndex || merylIndex == -1) && (campbellIndex < snakeIndex || snakeIndex == -1) && (campbellIndex < raidenIndex || raidenIndex == -1))
            {
                testIndex1 = campbellIndex;
                WriteToLog("char1 testIndex1 " + testIndex1, rndSessionID);
                first = testIndex1 + 9;
                tempNameSelect = "Campbell:";
                char1 = "campbell";
                if (char1 != startChar1)
                {
                    startChar2 = char1;
                    char1talking = false;
                    char2talking = true;
                }
                else
                {
                    char1talking = true;
                    char2talking = false;
                }
                ChangeVoice(char1);
                char1length = 9;
                testIndex2 = FindSecondChar("campbell", snakeIndex, ocelotIndex, liquidIndex, meiIndex, nasIndex, naomiIndex, wolfIndex, decoyIndex, campbellIndex, millerIndex, otaconIndex, merylIndex, raidenIndex);
                if (testIndex2 == -1)
                {
                    currentDraw = regexTest;
                    EvaluateCurrentDraw();
                    dialogOver = true;
                    return regexTest.Substring(first);

                }
                else
                {
                    last = testIndex2 - first;
                }
            }
            else 
            {
                dialogOver = true; return regexTest.Substring(last);
            }



            WriteToLog(char1, rndSessionID);



  
            WriteToLog("\n\n\nModified string:" + dialog, rndSessionID);
            WriteToLog("First Index:" + first, rndSessionID);
            WriteToLog("Last Index:" + last, rndSessionID);
            WriteToLog("Char 1:" + char1, rndSessionID);
            WriteToLog(char1length.ToString(), rndSessionID);

            if (last <= 0)

            { dialogOver = true; currentDraw = ""; return ""; }
            else
            { regexTest = regexTest.Substring(first, last); }
            if (regexTest.IndexOf(tempNameSelect) != -1)
            {
                WriteToLog("Regextest before remove:  " + regexTest, rndSessionID);
                regexTest = regexTest.Remove(regexTest.IndexOf(tempNameSelect));
                WriteToLog("Regextest after remove:  " + regexTest, rndSessionID);

            }
            holdingDialog = holdingDialog.Replace(tempNameSelect + regexTest, "");
            WriteToLog("Holding dialog, replaced  " + tempNameSelect + " with empty space: " + holdingDialog, rndSessionID);
            currentDraw = tempNameSelect + regexTest;
            EvaluateCurrentDraw();
            return regexTest;
        }

        private void EvaluateCurrentDraw()
        {
            if (currentDraw.Length > 70)
            {
                tempIndex = currentDraw.IndexOf(" ", 50);
            }
            else
            {
                tempIndex = -1;
            }

            if (tempIndex != -1)
            {
                currentDraw = currentDraw.Insert(tempIndex, Environment.NewLine);
                tempIndex = tempIndex + 50;
                if (currentDraw.Length > tempIndex + 5)
                {
                    tempIndex = currentDraw.IndexOf(" ", tempIndex);
                    if (tempIndex != -1)
                    {
                        currentDraw = currentDraw.Insert(tempIndex, Environment.NewLine);
                        tempIndex = tempIndex + 50;
                        if (currentDraw.Length > tempIndex + 5)
                        {
                            tempIndex = currentDraw.IndexOf(" ", tempIndex);
                            if (tempIndex != -1)
                            {
                                currentDraw = currentDraw.Insert(tempIndex, Environment.NewLine);
                                tempIndex = tempIndex + 50;
                                if (currentDraw.Length > tempIndex + 5)
                                {
                                    tempIndex = currentDraw.IndexOf(" ", tempIndex);
                                    if (tempIndex != -1)
                                    {
                                        currentDraw = currentDraw.Insert(tempIndex, Environment.NewLine);
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }

        public void DrawCodex(int anim, string character, GameTime gameTime, int leftorright)
        {
            string path1;
            string path2;
            if (leftorright == 0 && ((float)gameTime.TotalGameTime.TotalSeconds - animLeftTime) > 0.12)
            {
                animLeftTime = (float)gameTime.TotalGameTime.TotalSeconds;
                if (char2talking == false || outputDevice.PlaybackState == PlaybackState.Stopped)
                {
                    path1 = Animate.DoAnim(0, character);
                }
                else
                {
                    path1 = Animate.DoAnim(1, character);
                }
                codexLeftMaster = _texLoad.LoadContent(path1, this);
            }
            else if (leftorright == 1 && ((float)gameTime.TotalGameTime.TotalSeconds - animRightTime) > 0.12)
            {
                animRightTime = (float)gameTime.TotalGameTime.TotalSeconds;
                if (char1talking == false || outputDevice.PlaybackState == PlaybackState.Stopped)
                {
                    path2 = Animate.DoAnim(0, character);
                }
                else
                {
                    path2 = Animate.DoAnim(1, character);
                }
                codexRightMaster = _texLoad.LoadContent(path2, this);
            }

            _codexLeft.Begin();
            _codexLeft.Draw(
                codexLeftMaster,
                new Vector2(189, 66),
                null,
                Microsoft.Xna.Framework.Color.White,
                0f,
                new Vector2(0, 0),
                Vector2.One,
                SpriteEffects.None,
                0f
                );
            _codexLeft.End();
            _codexRight.Begin();
            _codexRight.Draw(
                codexRightMaster,
                new Vector2(879, 66),
                null,
                Microsoft.Xna.Framework.Color.White,
                0f,
                new Vector2(0, 0),
                Vector2.One,
                SpriteEffects.None,
                0f
                );
            _codexRight.End();
        }


        public void DrawString(string text, int x, int y, SpriteBatch spriteBatch, SpriteFont spriteFont, Microsoft.Xna.Framework.Color color)
        {
            spriteBatch.Begin();
            try
            {
                spriteBatch.DrawString(spriteFont, text, new Vector2(x, y), color);
            }
            catch (Exception e)
            {
                WriteToLog(e.ToString(), rndSessionID);
                spriteBatch.DrawString(spriteFont, "", new Vector2(x, y), color);
            }
            spriteBatch.End();
        }



        private static void WriteToLog(string logData, int sessionID)
        {
            try
            {
                if (!File.Exists("log_" + sessionID + ".txt"))
                {
                    using (StreamWriter sw = File.CreateText("log_" + sessionID + ".txt"))
                    {
                        sw.WriteLine(DateAndTime.Now + " - Log file created at " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\log_" + sessionID + ".txt");
                        sw.WriteLine(DateAndTime.Now + " - " + logData);
                    }
                }
                else
                {
                    using StreamWriter file = new("log_" + sessionID + ".txt", append: true);
                    file.WriteLine(DateAndTime.Now + " - " + logData);
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex.Message, sessionID);
            }

        }




        //TWITCH STUFF
        private void Client_OnLog(object sender, OnLogArgs e)
        {
            WriteToLog($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}", rndSessionID);
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            WriteToLog($"Connected to {e.AutoJoinChannel}", rndSessionID);
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            WriteToLog($"Error connecting. Attempting to refresh token.", rndSessionID);
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedArgs e)
        {
            WriteToLog($"Bot disconnected. Attempting to refresh token.", rndSessionID);
        }

        public void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            WriteToLog("Bot connected.", rndSessionID);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {


            if (e.ChatMessage.Message == "!topic" || e.ChatMessage.Message == "!topic " || e.ChatMessage.Message == "!topic󠀀" || e.ChatMessage.Message == "!topic 󠀀")
            {
                tclient.SendMessage("foxhoundai", "Please include a description after !topic");
            }
            else if (e.ChatMessage.Message.StartsWith("!topic"))
            {

                string message = e.ChatMessage.Message;
                int index = 7;
                string usertopic = "";

                if (Regex.IsMatch(message, "^[a-zA-Z0-9 @&%^*\\-$!󠀀,'.:[\\]]*$", RegexOptions.IgnorePatternWhitespace, Regex.InfiniteMatchTimeout))
                {
                    usertopic = SanitizeTopics(message.Substring(index));

                }

                if (usertopic != "" && (topicList.Count + itopicList.Count < 10) && usertopic.Length <= 260)
                {
                    topicList.Add(usertopic);
                    tclient.SendMessage("foxhoundai", "Topic added to topic queue");

                }
                else if (topicList.Count + itopicList.Count >= 10)
                {
                    tclient.SendMessage("foxhoundai", "Max topic limit reached, please wait for the queue to clear.");
                }
                else if (usertopic == "" || usertopic.Length > 260)
                {
                    tclient.SendMessage("foxhoundai", "Topic was rejected, please try again.");
                }
            }
            if (e.ChatMessage.Message == "!image" || e.ChatMessage.Message == "!image " || e.ChatMessage.Message == "!image󠀀" || e.ChatMessage.Message == "!image 󠀀")
            {
                tclient.SendMessage("foxhoundai", "Please include a description after !image");
            }
            else if (e.ChatMessage.Message.StartsWith("!image"))
            {

                string message = e.ChatMessage.Message;
                int index = 7;
                string usertopic = "";
                if (Regex.IsMatch(message, "^[a-zA-Z0-9 @&%^*\\-$!󠀀,[\\]]*$", RegexOptions.IgnorePatternWhitespace, Regex.InfiniteMatchTimeout))
                {
                    usertopic = SanitizeTopics(message.Substring(index));

                }
                if (usertopic != "" && (topicList.Count + itopicList.Count < 10) && usertopic.Length <= 260)
                {
                    itopicList.Add(usertopic);
                    tclient.SendMessage("foxhoundai", "Image added to image queue.");

                }
                else if (topicList.Count + itopicList.Count >= 10)
                {
                    tclient.SendMessage("foxhoundai", "Max topic limit reached, please wait for the queue to clear.");
                }
                else if (usertopic == "" || usertopic.Length > 260)
                {
                    tclient.SendMessage("foxhoundai", "Topic was rejected, please try again.");
                }
            }

            if (e.ChatMessage.Message.StartsWith("!help") || e.ChatMessage.Message.StartsWith("!command"))
            {
                tclient.SendMessage("foxhoundai", "Type !topic and then a description to set the next topic. Type !image and a description to generate an image. There is a max queue of 10, and the queue will alternate between images and topics. When an image is set, it is also the topic. Images cannot be generated from URLs, and there can be no quotation marks. Up to 4 characters can be specified (use !charhelp for details).");
            }
            if (e.ChatMessage.Message.StartsWith("!charhelp"))
            {
                tclient.SendMessage("foxhoundai", "If a character is mentioned in a topic or image description, it will try to include that character in the conversation, up to a max of 4. Possible characters are snake, liquid, campbell, meryl, miller, nastasha, naomi, mei ling, sniper wolf, decoy octopus, revolver ocelot, otacon, and raiden. For best results, decribe the topic, then put the characters in brackets. Ex: !image Sonic the Hedgehog. [snake ocelot]");
            }
            if (e.ChatMessage.Message.StartsWith("!info"))
            {
                tclient.SendMessage("foxhoundai", "The SnakeAI was made in C# using Monogame, and generates dialog using OpenAI's API with the Curie model. It uses Google Cloud TTS for voice. For questions, suggestions, and any other feedback, please reach out to mgsfoxhoundai@gmail.com.");
            }
            if (e.ChatMessage.Message.StartsWith("!queue"))
            {
                tclient.SendMessage("foxhoundai", "There are " + (itopicList.Count + topicList.Count).ToString() + " topics in the queue and a max of 10");
            }
            if (e.ChatMessage.Message.StartsWith("!contact"))
            {
                tclient.SendMessage("foxhoundai", "For questions, suggestions, and any other feedback, please reach out to mgsfoxhoundai@gmail.com.");
            }
            if (e.ChatMessage.Message.StartsWith("!clear"))
            {
                if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator)
                {
                    tclient.SendMessage("foxhoundai", "Queue cleared!");
                    itopicList.Clear();
                    topicList.Clear();

                }

            }
            if (e.ChatMessage.Message.StartsWith("!skip"))
            {
                if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator)
                {
                    outputDevice.Stop(); 
                    dialogOver = true;
                    tclient.SendMessage("foxhoundai", "Call skipped!");


                }
            }
            if (e.ChatMessage.Message.StartsWith("!restart"))
            {
                if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator)
                {
                    tclient.SendMessage("foxhoundai", "Restarting AI!");
                    this.Exit();


                }

            }
        }
    }
}
