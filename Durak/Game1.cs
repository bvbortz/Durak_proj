using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace Durak
{
    #region enums
    public enum StatesInQtable
    {
        beginning = 1,
        end
    }
    //used to decide whose turn to play/defend
    public enum ActivePlayerState
    {
        none,
        waitingForHumanPlayerAttack,
        waitingForHumanPlayerDefend,
        waitingForBotAttack,
        waitingForBotDefend
    }

    public enum GameState
    {
        welcome,                    //show the welcome screen
        gameRules,                  //show the gameRules screen
        howToPlay,                  //show the howToPlay screen
        addAllYouCan,               //when human player selected "take" and the bot needs to add everything he can.
        waitingForTake,             //when the computer attacks, shows that the program is waiting for the human player to press on the take button
        takeBot,                    //after the human player clicks the button "nothing to add"
        waitingForAdd,              //when the computer player needs to take (to allow to check whether the player has something to add)
        waitingForBita,             //after the human player attacks and the computer player defends, it's showing that the program is waiting for the player to press on the bita button
        botHasNothingToAdd,         //connected to addAllYouCan, becomes true when the computer has nothing to add.
        endGame,
        intermediate                //none of the above
    }

    //who is the winner of the game
    public enum Winner
    {
        computer,
        human,
        none
    }  
    #endregion

    /// <summary>
    /// This is the main class of the game
    /// </summary>
    public class Game1 : Game
    {
        #region staticBooleans
        public static bool hasQtableUpdated = false;        //has the Q table been updated
        public static bool hasTacticChanged = false;        //has the tactic been changed when the deck ended?
        public static bool needToNullify = false;           //becomes true after the restart button is pressed, so the game needs to be reinitialized
        public static bool gameOn = false;                  //Has a game started ?    
        public static bool switchTurnToComputer = false;    //this variable is needed in order to make the computer wait for the cards to finish moving before he takes its turn
        public static bool switchTurnToOpponent = false;    //opposite to switchTurnToComputer; used only in training mode
        public static bool training = false;                //is the computer in training mode(when it's true the comuter plays against himself)?
        #endregion

        #region fields
        public const int OUT_OF_RANGE = 99;
        public static int distanceBetweenCards = 30;
        public static int maxCardsInAttack = 6;
        public static ActivePlayerState activePlayerState = ActivePlayerState.none;
        public static GameState gameState = GameState.welcome;
        public static Winner winner = Winner.none;
        public static Winner lastResult = Winner.none;

        GraphicsDeviceManager graphics; // GraphicsDeviceManager is a Monogame class
        SpriteBatch spriteBatch;        // SpriteBatch is a Monogame class
        Desk desk;
        Vector2 positionBita;
        Vector2 positionTurn;
        Vector2 positionAdd;
        Vector2 positionTake;
        Vector2 positionWon;
        Vector2 positionExit;
        Vector2 positionRetry;
        Vector2 positionLost;
        Vector2 positionStart;
        Vector2 positionClose;
        Vector2 positionGameRulesButton;
        Vector2 positionHowToPlayButton;
        Texture2D gameRulesTexture;
        Texture2D howToPlayTexture;
        Texture2D closeTexture;
        Texture2D gameRulesButtonTexture;
        Texture2D howToPlayButtonTexture;
        Texture2D bitaTexture;
        Texture2D exitTexture;
        Texture2D retryTexture;
        Texture2D addTexture;
        Texture2D takeTexture;
        Texture2D turnTexture;
        Texture2D mouseTexture;
        Texture2D wonTexture;
        Texture2D lostTexture;
        Texture2D welcomeTexture;
        Texture2D startTexture;
        Vector2 mousePos;
        BotPlayer botPlayer;
        Player humanPlayer;
        public int idxClickedCard = OUT_OF_RANGE;
        Process processLoadGif = new Process(); 
        #endregion

        /// <summary>
        /// to overcome an error when loading the textures you need to make sure the Monogame's GraphicsProfile is set to HiDef.
        /// And this function sets the GraphicsProfile to HiDef
        /// </summary>
        void Graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.GraphicsProfile = GraphicsProfile.HiDef;
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content"; //Content field is inherited from Monogame's Game class
            graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs> (Graphics_PreparingDeviceSettings);
        }

        /// <summary>
        /// This is the first function called by Monogame in the start of the program
        /// The rest of the comment is an auto Monogame comment:
        /// 
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            botPlayer = new BotPlayer();
            humanPlayer = new Player();
            graphics.PreferredBackBufferHeight = 1000;
            graphics.PreferredBackBufferWidth = 1000;
            graphics.ApplyChanges();
            desk = new Desk(humanPlayer, botPlayer); 
            desk.Init();
            Strategy.Initialize(botPlayer, humanPlayer);
            Strategy.UpdatePolicies(StatesInQtable.beginning);

            positionGameRulesButton = new Vector2(400, 540);
            positionHowToPlayButton= new Vector2(400,600);
            positionClose = new Vector2(450, 900);
            positionWon = new Vector2(400, 200);
            positionExit = new Vector2(450, 900);
            positionRetry = new Vector2(400, 600);
            positionStart = new Vector2(450, 800);
            positionLost = new Vector2(400, 200);
            positionBita = new Vector2(500, 900);
            positionTurn = new Vector2(100, 700);
            positionAdd = new Vector2(50, 900);
            positionTake = new Vector2(500, 900);
            mousePos = new Vector2(graphics.GraphicsDevice.Viewport.
                                   Width / 2,
                                   graphics.GraphicsDevice.Viewport.
                                   Height / 2);

            base.Initialize();
        }

        /// <summary>
        /// opens a loading gif while all the textures are loading
        /// </summary>
        protected override void LoadContent()
        {
            //launch a separate app to display the loading screen
            processLoadGif.StartInfo.FileName = "..\\loading screen.exe"; //opens an exe file while the textures are loading
            processLoadGif.StartInfo.UseShellExecute = false;
            processLoadGif.Start();

            // Create a new SpriteBatch, which can be used to draw textures
            spriteBatch = new SpriteBatch(GraphicsDevice);
            desk.Load(Content, spriteBatch);

            closeTexture = this.Content.Load<Texture2D>("close");
            gameRulesButtonTexture = this.Content.Load<Texture2D>("game rules button");
            gameRulesTexture = this.Content.Load<Texture2D>("game rules");
            howToPlayButtonTexture = this.Content.Load<Texture2D>("how to play button");
            howToPlayTexture = this.Content.Load<Texture2D>("how to play");
            bitaTexture = this.Content.Load<Texture2D>("bita");
            takeTexture = this.Content.Load<Texture2D>("take");
            turnTexture = this.Content.Load<Texture2D>("your turn");
            addTexture = this.Content.Load<Texture2D>("no add");
            mouseTexture= this.Content.Load<Texture2D>("mouse");
            wonTexture = this.Content.Load<Texture2D>("you won");
            exitTexture = this.Content.Load<Texture2D>("exit");
            retryTexture = this.Content.Load<Texture2D>("try again");
            lostTexture = this.Content.Load<Texture2D>("you lost");
            welcomeTexture = this.Content.Load<Texture2D>("welcome");
            startTexture = this.Content.Load<Texture2D>("start");
            positionStart.X = 500 - (int)(0.5 * startTexture.Width);
            positionGameRulesButton.X = 500 - (int)(0.5 * gameRulesButtonTexture.Width);
            positionHowToPlayButton.X = 500 - (int)(0.5 * howToPlayButtonTexture.Width);
            positionClose.X = 500 - (int)(0.5 * closeTexture.Width);
            positionExit.X = 500 - (int)(0.5 * exitTexture.Width);
            positionLost.X = 500 - (int)(0.5 * lostTexture.Width);
            positionRetry.X = 500 - (int)(0.5 * retryTexture.Width);
            positionWon.X = 500 - (int)(0.5 * wonTexture.Width);
            //remove the loading screen:
            try
            {
                using (processLoadGif)
                {
                    processLoadGif.Kill(); //closes the exe file
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// this function updates the result of the game and changes the Q table file
        /// </summary>
        private void UpdateResults(Winner winner)
        {
            Game1.winner = winner;
            lastResult = winner;
            gameState = GameState.endGame;
            gameOn = false;
            string writeStr = Strategy.CalcRewards(winner);
            System.IO.File.WriteAllText("..\\Qtable.csv", writeStr);
            hasQtableUpdated = true;
        }

        /// <summary>
        /// finds on which card of the player's cards the mouse is pointing
        /// 150 is the height of the card after scale
        /// 104 is the width of the card after scale
        /// </summary>
        private int FindIdxClicked()
        {
            int idxClickedCard = OUT_OF_RANGE;
            for (int i = 0; i < humanPlayer.cardsInHand.Count; i++)
            {
                if (mousePos.Y > humanPlayer.cardsInHand[i].location.Y && mousePos.Y < humanPlayer.cardsInHand[i].location.Y + 150)
                {
                    if (i == 0)
                    {
                        if (mousePos.X > humanPlayer.cardsInHand[i].location.X && mousePos.X < humanPlayer.cardsInHand[i].location.X + 104)
                        {
                            idxClickedCard = i;
                        }
                    }
                    else
                    {
                        if (mousePos.X > humanPlayer.cardsInHand[i].location.X && mousePos.X < humanPlayer.cardsInHand[i].location.X + distanceBetweenCards)
                        {
                            idxClickedCard = i;
                        }
                    }
                }
            }
            return idxClickedCard;
        }

        /// <summary>
        /// return a list with all the possibilities the given player has to defend against the card passed to the function as parameter
        /// </summary>
        public static List<Card> DefendPossibilities(Card card, Player player)
        {
            List<Card> possibilities = new List<Card>();
            if (card.suit == (int)Desk.cozir)
            {
                for (int i = 0; i < player.cardsInHand.Count; i++)
                {
                    if (player.cardsInHand[i].suit == card.suit && player.cardsInHand[i].rank > card.rank)
                    {
                        possibilities.Add(player.cardsInHand[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < player.cardsInHand.Count; i++)
                {
                    if (player.cardsInHand[i].suit == (int)Desk.cozir ||
                        (player.cardsInHand[i].suit == card.suit && player.cardsInHand[i].rank > card.rank))
                    {
                        possibilities.Add(player.cardsInHand[i]);
                    }
                }
            }
            return possibilities;
        }

        /// <summary>
        /// calls to botPlayer's function to make moves when training mode is on
        /// </summary>
        private void TrainingPlay()
        {
            if (desk.hasFinishedMoving && Game1.activePlayerState == ActivePlayerState.waitingForHumanPlayerDefend)
            {
                StatusDefense statusDefense = botPlayer.MakeDefenseMove(desk, humanPlayer, false); ;
                if (statusDefense == StatusDefense.add)
                {
                    Game1.gameState = GameState.addAllYouCan;
                    Game1.activePlayerState = ActivePlayerState.waitingForBotAttack;
                }
                else if (statusDefense == StatusDefense.bita)
                {
                    Game1.gameState = GameState.intermediate;
                    Game1.activePlayerState = ActivePlayerState.waitingForBotAttack;
                }
            }
            else if (desk.hasFinishedMoving && Game1.activePlayerState == ActivePlayerState.waitingForHumanPlayerAttack)
            {
                if (desk.cardsRanksPossiblities.Count == 0)
                {
                    Game1.maxCardsInAttack = botPlayer.cardsInHand.Count;
                }
                if (Game1.maxCardsInAttack > 6)
                {
                    Game1.maxCardsInAttack = 6;
                }
                StatusAttack statusAttack = botPlayer.MakeAttackMove(desk, humanPlayer, false);
                if (statusAttack == StatusAttack.nothingToAdd)
                {
                    desk.DoTake(botPlayer, Desk.yBotCards, true);
                    if (desk.deck.Count != 0)
                    {
                        desk.TakeFromDeck(humanPlayer, Desk.yHumanCards, false);
                    }
                    Game1.gameState = GameState.intermediate;
                    Game1.activePlayerState = ActivePlayerState.none;
                    Game1.switchTurnToOpponent = true;
                }
                if (statusAttack == StatusAttack.doBita)
                {
                    desk.DoBita(false);
                    Game1.activePlayerState = ActivePlayerState.none;
                    Game1.switchTurnToComputer = true;
                }
                if (statusAttack == StatusAttack.defenseTurn)
                {
                    Game1.activePlayerState = ActivePlayerState.waitingForBotDefend;
                }
            }
            else if (desk.hasFinishedMoving && Game1.activePlayerState == ActivePlayerState.waitingForBotDefend)
            {
                StatusDefense status = botPlayer.MakeDefenseMove(desk, botPlayer, true);
                if (status == StatusDefense.add)
                {
                    Game1.gameState = GameState.addAllYouCan;
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                }
                else if (status == StatusDefense.bita)
                {
                    Game1.gameState = GameState.intermediate;
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                }
            }
            else if (desk.hasFinishedMoving && Game1.activePlayerState == ActivePlayerState.waitingForBotAttack)
            {
                if (desk.cardsRanksPossiblities.Count == 0)
                {
                    Game1.maxCardsInAttack = humanPlayer.cardsInHand.Count;
                }
                if (Game1.maxCardsInAttack > 6)
                {
                    Game1.maxCardsInAttack = 6;
                }
                StatusAttack statusAttack = botPlayer.MakeAttackMove(desk, botPlayer, true);
                if (statusAttack == StatusAttack.nothingToAdd)
                {
                    desk.DoTake(humanPlayer, Desk.yHumanCards, false);
                    if (desk.deck.Count != 0)
                    {
                        desk.TakeFromDeck(botPlayer, Desk.yBotCards, true);
                    }
                    Game1.switchTurnToComputer = true;
                    Game1.gameState = GameState.intermediate;
                    Game1.activePlayerState = ActivePlayerState.none;
                }
                if (statusAttack == StatusAttack.doBita)
                {
                    desk.DoBita(true);
                    Game1.gameState = GameState.intermediate;
                    Game1.activePlayerState = ActivePlayerState.none;
                    Game1.switchTurnToOpponent = true;
                }
                if (statusAttack == StatusAttack.defenseTurn)
                {
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerDefend;
                }
            }
        }

        /// <summary>
        /// checks what  was clicked
        /// </summary>
        private void checkClick(MouseState state)
        {
            //Is the start button pressed when the game hasn't started yet?
            if (gameState == GameState.welcome)
            {
                if ((state.X > this.positionStart.X) && (state.X < this.positionStart.X + startTexture.Width) &&
                    (state.Y > this.positionStart.Y) && (state.Y < this.positionStart.Y + startTexture.Height))
                {
                    gameOn = true;
                    gameState = GameState.intermediate;
                }
                if ((state.X > this.positionGameRulesButton.X) && (state.X < this.positionGameRulesButton.X + gameRulesButtonTexture.Width) &&
                    (state.Y > this.positionGameRulesButton.Y) && (state.Y < this.positionGameRulesButton.Y + gameRulesButtonTexture.Height))
                {
                    gameState = GameState.gameRules;
                }
                if ((state.X > this.positionHowToPlayButton.X) && (state.X < this.positionHowToPlayButton.X + howToPlayButtonTexture.Width) &&
                    (state.Y > this.positionHowToPlayButton.Y) && (state.Y < this.positionHowToPlayButton.Y + howToPlayButtonTexture.Height))
                {
                    gameState = GameState.howToPlay;
                }
            }
            if (gameState == GameState.howToPlay || gameState == GameState.gameRules)
            {
                if ((state.X > this.positionClose.X) && (state.X < this.positionClose.X + closeTexture.Width) &&
                    (state.Y > this.positionClose.Y) && (state.Y < this.positionClose.Y + closeTexture.Height))
                {
                    gameState = GameState.welcome;
                }
            }
            if (desk.gameStarted)
            {
                idxClickedCard = FindIdxClicked();
            }

            //Are the exit or retry buttons pressed when the game has ended?
            if (gameState == GameState.endGame && desk.hasFinishedMoving)
            {
                if ((state.X > this.positionExit.X) && (state.X < this.positionExit.X + exitTexture.Width) &&
                    (state.Y > this.positionExit.Y) && (state.Y < this.positionExit.Y + exitTexture.Height))
                {
                    Exit();
                }
                if ((state.X > this.positionRetry.X) && (state.X < this.positionRetry.X + retryTexture.Width) &&
                    (state.Y > this.positionRetry.Y) && (state.Y < this.positionRetry.Y + retryTexture.Height))
                {
                    needToNullify = true;
                }
            }

            if (!training)
            {
                //Is the bita button pressed when the program is waiting for the user to click on in, the game is still on and the cards finished moving?
                if (gameState == GameState.waitingForBita && desk.hasFinishedMoving && gameState != GameState.endGame)
                {
                    if ((state.X > this.positionBita.X) && (state.X < this.positionBita.X + bitaTexture.Width) &&
                        (state.Y > this.positionBita.Y) && (state.Y < this.positionBita.Y + bitaTexture.Height))
                    {
                        desk.DoBita(false);
                        switchTurnToComputer = true;
                        gameState = GameState.intermediate;
                    }
                }

                //Is the "nothing to add" button pressed when the program is waiting for the user to click on in, the game is still on and the cards finished moving?
                if (gameState == GameState.waitingForAdd && desk.hasFinishedMoving && gameState != GameState.endGame)
                {
                    if ((state.X > this.positionAdd.X) && (state.X < this.positionAdd.X + addTexture.Width) &&
                        (state.Y > this.positionAdd.Y) && (state.Y < this.positionAdd.Y + addTexture.Height))
                    {
                        gameState = GameState.takeBot;
                        activePlayerState = ActivePlayerState.waitingForBotAttack;
                    }
                }

                //Is the take button pressed when the program is waiting for the user to click on in, the game is still on and the cards finished moving?
                if (gameState == GameState.waitingForTake && desk.hasFinishedMoving && gameState != GameState.endGame)
                {
                    if ((state.X > this.positionTake.X) && (state.X < this.positionTake.X + takeTexture.Width) &&
                        (state.Y > this.positionTake.Y) && (state.Y < this.positionTake.Y + takeTexture.Height))
                    {
                        gameState = GameState.addAllYouCan;
                        activePlayerState = ActivePlayerState.waitingForBotAttack;
                    }
                }
                if (idxClickedCard != OUT_OF_RANGE)
                {
                    if (activePlayerState == ActivePlayerState.waitingForHumanPlayerAttack)
                    {
                        if (humanPlayer.MoveCardToAttack(idxClickedCard, desk, humanPlayer))
                        {
                            if (desk.defendCards.Count == 0)
                            {
                                maxCardsInAttack = botPlayer.cardsInHand.Count;
                            }
                            if (maxCardsInAttack > 6)
                                maxCardsInAttack = 6;

                            idxClickedCard = OUT_OF_RANGE;
                            if (gameState != GameState.waitingForAdd)
                            {
                                activePlayerState = ActivePlayerState.waitingForBotDefend;
                            }
                        }
                    }
                    else if (activePlayerState == ActivePlayerState.waitingForHumanPlayerDefend)
                    {
                        if (humanPlayer.MoveCardToDefense(idxClickedCard, desk, humanPlayer))
                        {
                            idxClickedCard = OUT_OF_RANGE;
                            gameState = GameState.intermediate;
                            activePlayerState = ActivePlayerState.waitingForBotAttack;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// calls to botPlayer's function to make moves when training mode is off
        /// </summary>
        private void regularPlayBot()
        {
            if (desk.hasFinishedMoving && Game1.activePlayerState == ActivePlayerState.waitingForBotDefend)
            {
                StatusDefense statusDefense = botPlayer.MakeDefenseMove(desk, botPlayer, true);
                if (statusDefense == StatusDefense.add)
                {
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                    Game1.gameState = GameState.waitingForAdd;
                }
                else if (statusDefense == StatusDefense.bita)
                {
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                    Game1.gameState = GameState.waitingForBita;
                }
            }
            if (desk.hasFinishedMoving && Game1.activePlayerState == ActivePlayerState.waitingForBotAttack)
            {
                if (desk.cardsRanksPossiblities.Count == 0)
                {
                    Game1.maxCardsInAttack = humanPlayer.cardsInHand.Count;
                }
                if (Game1.maxCardsInAttack > 6)
                {
                    Game1.maxCardsInAttack = 6;
                }
                StatusAttack statusAttack = botPlayer.MakeAttackMove(desk, botPlayer, true);
                if (statusAttack == StatusAttack.defenseTurn)
                {
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerDefend;
                    Game1.gameState = GameState.waitingForTake;
                }
                if (statusAttack == StatusAttack.doBita)
                {
                    desk.DoBita(true);
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                }
                if (statusAttack == StatusAttack.nothingToAdd)
                {
                    Game1.activePlayerState = ActivePlayerState.none;
                    Game1.gameState = GameState.botHasNothingToAdd;
                }
            }
        }

        /// <summary>
        /// This function is called by Monogame 60 times per second.
        /// The rest of the comment is an auto Monogame comment:
        /// 
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            MouseState state = Mouse.GetState();
            mousePos.X = state.X;
            mousePos.Y = state.Y;
            ButtonState bs = state.LeftButton;

            if (desk.gameStarted)
            {
                //check if game ended; if yes, update the Q-table
                if(desk.deck.Count == 0 && desk.hasFinishedMoving && (humanPlayer.cardsInHand.Count == 0 || botPlayer.cardsInHand.Count == 0))
                {
                    if (!hasQtableUpdated)
                    {
                        if(botPlayer.cardsInHand.Count == 0)
                        {
                            UpdateResults(Winner.computer);
                        }
                        else
                        {
                            UpdateResults(Winner.human);
                        }
                    }
                }            
            }
           
            if (bs == ButtonState.Pressed)
            {
                checkClick(state);               
            }            
           
            if (desk.hasFinishedMoving && gameState == GameState.botHasNothingToAdd)
            {
                desk.DoTake(humanPlayer, Desk.yHumanCards, false);
                if (desk.deck.Count != 0)
                {
                    desk.TakeFromDeck(botPlayer, Desk.yBotCards, true);
                }
                switchTurnToComputer = true;
                gameState = GameState.intermediate;
                activePlayerState = ActivePlayerState.none;
            }
            if(desk.hasFinishedMoving && gameState == GameState.takeBot)
            {
                desk.DoTake(botPlayer, Desk.yBotCards, true);
                if (desk.deck.Count != 0)
                {
                    desk.TakeFromDeck(humanPlayer, Desk.yHumanCards, false);
                }
                activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                gameState = GameState.intermediate;
            }
           
            if (needToNullify)
            {
                desk.gameStarted = false;
                Nullify();
                Strategy.Initialize(botPlayer, humanPlayer);
                Strategy.UpdatePolicies(StatesInQtable.beginning);
                desk.ReInit();
                needToNullify = false;
                gameOn = true;
            }
            desk.Update();
            if (Game1.training)
            {
                TrainingPlay();
            }
            else
            {
                regularPlayBot();
            }
            if (Game1.switchTurnToComputer && desk.hasFinishedMoving)
            {
                Game1.activePlayerState = ActivePlayerState.waitingForBotAttack;
                Game1.switchTurnToComputer = false;
            }
            if (Game1.switchTurnToOpponent && desk.hasFinishedMoving)
            {
                Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                Game1.switchTurnToOpponent = false;
            }
            if (desk.hasFinishedMoving && !desk.gameStarted)
            {
                desk.gameStarted = true;
                if (Game1.lastResult == Winner.computer)
                {
                    Game1.activePlayerState = ActivePlayerState.waitingForBotAttack;
                }
                else if (Game1.lastResult == Winner.human)
                {
                    Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                }
                else
                {
                    if (desk.DoesHumanPlayerBegin())
                    {
                        Game1.activePlayerState = ActivePlayerState.waitingForHumanPlayerAttack;
                    }
                    else
                    {
                        Game1.activePlayerState = ActivePlayerState.waitingForBotAttack;
                    }
                }
            }
            desk.hasFinishedMoving = desk.CheckFinished();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        /// <summary>
        /// Nullifies the enums and the boolean variables of Game1 and strategy
        /// </summary>
        private void Nullify()
        {
            hasQtableUpdated = false;
            hasTacticChanged = false;
            gameOn = false;
            switchTurnToComputer = false;
            switchTurnToOpponent = true;
            gameState = GameState.intermediate;
            activePlayerState = ActivePlayerState.none;
            winner = Winner.none;
            Strategy.noCozirAttackActive = false;
            Strategy.noCozirDefendActive = false;
            Strategy.noMercyAttackActive = false;
            Strategy.noHighAttackActive = false;
            Strategy.takeHighDefendActive = false;
            Strategy.takeNoLowDefendActive = false;
            Strategy.tillTheEndDefendActive = false;
            Strategy.attackTacticBeginning = AttackTactics.none;
            Strategy.attackTacticEnd = AttackTactics.none;
            Strategy.defenseTacticBeginning = DefenseTactics.none;
            Strategy.defenseTacticEnd = DefenseTactics.none;
        }

        /// <summary>
        /// This is called by Monogame when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
           
            GraphicsDevice.Clear(Color.CornflowerBlue);
            desk.Draw();
            spriteBatch.Begin();
            //draw the texture only if needed
            if (gameState == GameState.welcome)
            {
                spriteBatch.Draw(welcomeTexture, new Vector2(0, 0), Color.White);
                spriteBatch.Draw(startTexture, positionStart, Color.White);
                spriteBatch.Draw(howToPlayButtonTexture, positionHowToPlayButton, Color.White);
                spriteBatch.Draw(gameRulesButtonTexture, positionGameRulesButton, Color.White);
            }
            if(gameState == GameState.gameRules)
            {
                spriteBatch.Draw(gameRulesTexture, new Vector2(0, 0), Color.White);
                spriteBatch.Draw(closeTexture, positionClose, Color.White);
            }
            if (gameState == GameState.howToPlay)
            {
                spriteBatch.Draw(howToPlayTexture, new Vector2(0, 0), Color.White);
                spriteBatch.Draw(closeTexture, positionClose, Color.White);
            }
            if (gameState == GameState.endGame && desk.hasFinishedMoving)
            {
                spriteBatch.Draw(retryTexture, positionRetry, Color.White);
                spriteBatch.Draw(exitTexture, positionExit, Color.White);
            }
            if (!training)
            {
                if (gameState == GameState.waitingForBita && desk.hasFinishedMoving && gameState != GameState.endGame)
                {
                    spriteBatch.Draw(bitaTexture, positionBita, Color.White);
                }
                if (activePlayerState == ActivePlayerState.waitingForHumanPlayerAttack && desk.hasFinishedMoving && gameState != GameState.endGame)
                {
                    spriteBatch.Draw(turnTexture, positionTurn, Color.White);
                }
                if (gameState == GameState.waitingForAdd && desk.hasFinishedMoving && gameState != GameState.endGame)
                {
                    spriteBatch.Draw(addTexture, positionAdd, Color.White);
                }
                if (gameState == GameState.waitingForTake && desk.hasFinishedMoving && gameState != GameState.endGame)
                {
                    spriteBatch.Draw(takeTexture, positionTake, Color.White);
                }
            }
           
            if (winner == Winner.computer && desk.hasFinishedMoving)
            {
                spriteBatch.Draw(lostTexture, positionLost, Color.White);
            }
            if (winner == Winner.human && desk.hasFinishedMoving)
            {
                spriteBatch.Draw(wonTexture, positionWon, Color.White);
            }
            spriteBatch.Draw(mouseTexture,mousePos, null,Color.White,0,new Vector2(0,0),0.1f,SpriteEffects.None,0);
            spriteBatch.End();
            
            base.Draw(gameTime);
        }
    }
}
