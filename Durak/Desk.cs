using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Durak
{
    
    public class Desk
    {              
        public List<Card> deck;
        public List<int> cardsRanksPossiblities; //the ranks of the cards participating now in attack and defense. Only a card with rank from this list can be added to the attack
        public static List<Vector2> attackPos = new List<Vector2>(); // to store the attack and defense cards positions on the desk
        public List<Card> bita;
        public List<Card> attackCards;
        public List<Card> defendCards;
        public static CardSuit cozir;
        public static int yBotCards = 50;
        private int yDeck = 450;
        public static int yHumanCards = 750;
        public static int xFirstCard = 800;
        BotPlayer botPlayer;
        Player humanPlayer;
        public bool gameStarted = false; 
        public bool hasFinishedMoving = false; //Have the cards finised moving already

        public Desk(Player humanPlayer, BotPlayer botPlayer)
        {
            this.botPlayer = botPlayer;
            deck = new List<Card>();
            bita = new List<Card>();
            cardsRanksPossiblities = new List<int>();
            attackCards = new List<Card>();
            defendCards = new List<Card>();
            this.humanPlayer = humanPlayer;           
        }

        public void Init()
        {
            InitDeck();
            InitAttackPos();           
        }

        private List<E> ShuffleList<E>(List<E> inputList)
        {
            List<E> randomList = new List<E>();

            Random r = new Random();
            int randomIndex = 0;
            while (inputList.Count > 0)
            {
                randomIndex = r.Next(0, inputList.Count); //Choose a random object in the list
                randomList.Add(inputList[randomIndex]); //add it to the new, random list
                inputList.RemoveAt(randomIndex); //remove to avoid duplicates
            }

            return randomList; //return the new random list
        }

        /// <summary>
        ///sets the position of the attacking cards to constant values
        /// </summary>
        public void InitAttackPos()
        {
            attackPos.Add(new Vector2(450, 300));
            attackPos.Add(new Vector2(600, 300));
            attackPos.Add(new Vector2(750, 300));
            attackPos.Add(new Vector2(450, 500));
            attackPos.Add(new Vector2(600, 500));
            attackPos.Add(new Vector2(750, 500));
        }

        /// <summary>
        /// adding all the cards to the deck in order to start the game all over again.
        /// it also shuffles the deck
        /// </summary>
        public void ReInit()
        {
            cardsRanksPossiblities = new List<int>();
            while (bita.Count != 0)
            {
                bita[0].isBlack = true;
                bita[0].location.Y = yDeck;
                bita[0].destY = yDeck;
                deck.Add(bita[0]);
                bita.RemoveAt(0);
            }
            while (attackCards.Count != 0)
            {
                attackCards[0].isBlack = true;
                attackCards[0].location.Y = yDeck;
                attackCards[0].destY = yDeck;
                deck.Add(attackCards[0]);
                attackCards.RemoveAt(0);
            }
            while (defendCards.Count != 0)
            {
                defendCards[0].location.Y = yDeck;
                defendCards[0].destY = yDeck;
                defendCards[0].isBlack = true;
                deck.Add(defendCards[0]);
                defendCards.RemoveAt(0);
            }
            while (botPlayer.cardsInHand.Count != 0)
            {
                botPlayer.cardsInHand[0].location.Y = yDeck;
                botPlayer.cardsInHand[0].destY = yDeck;
                botPlayer.cardsInHand[0].isBlack = true;
                deck.Add(botPlayer.cardsInHand[0]);
                botPlayer.cardsInHand.RemoveAt(0);
            }
            while (humanPlayer.cardsInHand.Count != 0)
            {
                humanPlayer.cardsInHand[0].location.Y = yDeck;
                humanPlayer.cardsInHand[0].destY = yDeck;
                humanPlayer.cardsInHand[0].isBlack = true;
                deck.Add(humanPlayer.cardsInHand[0]);
                humanPlayer.cardsInHand.RemoveAt(0);
            }
            deck = ShuffleList<Card>(deck);
            for (int i = 0; i < deck.Count; i++)
            {
                deck[i].location.X = (float)(200 - 1 * i);//200 is the x of the location of the deck
                deck[i].destX = (int)deck[i].location.X;
            }            
            
            DealCards();
        }

        /// <summary>
        /// creates all the card and adds them to the deck
        /// </summary>
        public void InitDeck()
        {            
            Card temp=new Card(0,0,Vector2.Zero);
            for (int i = (int)(CardSuit.spades); i < Enum.GetNames(typeof(CardSuit)).Length; i++)
            {
                for (int j = (int)(CardRank.six); j < (int)(CardRank.six)+Enum.GetNames(typeof(CardRank)).Length; j++)
                {
                    temp = new Card((CardSuit)i, (CardRank)j, new Vector2(0, yDeck));
                    deck.Add(temp);                    
                }
            }
            deck=ShuffleList<Card>(deck);
            for (int i = 0; i < deck.Count; i++)
            {
                deck[i].location.X = (float)(200 - 1 * i);
            }
            
        }

        /// <summary>
        /// selects the cozir and deals the cards to both players
        /// </summary>
        public void DealCards()
        {
            deck.Last<Card>().location.X += 20;
            deck.Last<Card>().location.Y += 10;
            deck.Last<Card>().isBlack = false;
            deck.Last<Card>().rotate = (float)Math.PI / 2;
            cozir = (CardSuit)deck.Last<Card>().suit;

            Card temp;
            for (int i = 0; i < 6; i++)
            {
                temp = deck.First<Card>();
                deck.Remove(temp);
                temp.isMoving = true;
                temp.destY = Desk.yBotCards;
                temp.destX = Desk.xFirstCard - Game1.distanceBetweenCards * i;
                botPlayer.cardsInHand.Add(temp);               
            }
            for (int i = 0; i < 6; i++)
            {
                temp = deck.First<Card>();
                deck.Remove(temp);                
                temp.isMoving = true;
                temp.isBlack = false;
                temp.destY = Desk.yHumanCards;
                temp.destX = Desk.xFirstCard - Game1.distanceBetweenCards * i;
                humanPlayer.cardsInHand.Add(temp);
            }
        }

        /// <summary>
        /// checks if the human player begins
        /// </summary>
        public bool DoesHumanPlayerBegin()
        {
            //those variables are being set in the beginning to a maximum value
            int minReal = (int)CardRank.ace + 1; 
            int minVirtual = (int)CardRank.ace + 1;
            for (int i = 0; i < humanPlayer.cardsInHand.Count; i++)
            {
                if (humanPlayer.cardsInHand[i].rank < minReal && humanPlayer.cardsInHand[i].suit == (int)cozir)
                {
                    minReal = (int)humanPlayer.cardsInHand[i].rank;
                }
            }
            for (int i = 0; i < botPlayer.cardsInHand.Count; i++)
            {
                if (botPlayer.cardsInHand[i].rank < minVirtual&& botPlayer.cardsInHand[i].suit == (int)cozir)
                {
                    minVirtual = botPlayer.cardsInHand[i].rank;
                }
            }
            if (minReal < minVirtual)
                return true;
            if (minVirtual < minReal)
                return false;
            for (int i = (int)CardRank.six; i <= (int)CardRank.ace; i++)
            {
                if (IsCardFound(i, botPlayer) && (!IsCardFound(i, humanPlayer)))
                    return false;
                if (IsCardFound(i, humanPlayer) && (!IsCardFound(i, botPlayer)))
                    return true;
            }
            return false;           
        }

        /// <summary>
        /// checks whether a card with the same rank was found in cards of the given player 
        /// </summary>
        /// <param name="player">decides whose cards the function will look in.</param>
        public bool IsCardFound(int rank, Player player)
        {
            for (int i = 0; i < player.cardsInHand.Count; i++)
            {
                if (player.cardsInHand[i].rank == rank)
                    return true;
            }           
            return false;
        }

        /// <summary>
        /// loads the texture of each card (using Monogame classes ContentManager and SpriteBatch ) and afterwards calls DealCards
        /// </summary>
        public void Load(ContentManager cm, SpriteBatch sb)
        {
            for (int i = 0; i < deck.Count; i++)
            {
                deck[i].Load(cm, sb);
            }
            DealCards();
        }

        /// <summary>
        /// if the game has started it draws all the cards
        /// </summary>
        public void Draw()
        {
            if (!Game1.gameOn)
                return;

            for (int i = 0; i < deck.Count; i++)
            {
                deck[i].DrawCard();
            }
            for (int i = humanPlayer.cardsInHand.Count-1; i >= 0; i--)
            {
                humanPlayer.cardsInHand[i].DrawCard();
            }
            for (int i = 0; i < botPlayer.cardsInHand.Count; i++)
            {
                botPlayer.cardsInHand[i].DrawCard();
            }
            for (int i = 0; i < attackCards.Count; i++)
            {
                attackCards[i].DrawCard();
            }
            for (int i = 0; i < defendCards.Count; i++)
            {
                defendCards[i].DrawCard();
            }
            for (int i = 0; i < bita.Count; i++)
            {
                bita[i].DrawCard();
            }
        }

        /// <summary>
        /// updates the game based, among other things, on the static variables of LifeCycle and moving all the cards
        /// </summary>
        public void Update()
        {                 
            CheckForEmptySpaces();
            hasFinishedMoving = CheckFinished();
                       
            for (int i = 0; i < deck.Count; i++)
            {
                deck[i].Move();                
            }
            for (int i = 0; i < humanPlayer.cardsInHand.Count; i++)
            {
                humanPlayer.cardsInHand[i].Move();
            }
            for (int i = 0; i < botPlayer.cardsInHand.Count; i++)
            {
                botPlayer.cardsInHand[i].Move();
            }
            for (int i = 0; i < attackCards.Count; i++)
            {
                attackCards[i].Move();
            }
            for (int i = 0; i < defendCards.Count; i++)
            {
                defendCards[i].Move();
            }
            for (int i = 0; i < bita.Count; i++)
            {
                bita[i].Move();
            }           
        }

        /// <summary>
        /// moves the cards which participated in the attack and defense to the bita and then both players take from the deck
        /// </summary>
        /// <param name="botFirst">does the computer need to take first</param>
        public void DoBita(bool botFirst)
        {
            for (int i = 0; i < attackCards.Count; )
            {
                bita.Add(attackCards[i]);
                attackCards.RemoveAt(i);
            }
            for (int i = 0; i < defendCards.Count;)
            {
                bita.Add(defendCards[i]);
                defendCards.RemoveAt(i);
            }
            for (int i = 0; i < bita.Count; i++)
            {
                bita[i].destX = 50 +i*3;
                bita[i].destY = 50;
                if(bita[i].destX!=bita[i].location.X|| bita[i].destY != bita[i].location.Y)
                {
                    bita[i].isMoving = true;
                }
                bita[i].isBlack = true;
            }
            if (deck.Count != 0)
            {
                if (botFirst)
                {
                    TakeFromDeck(botPlayer, Desk.yBotCards, true);
                    TakeFromDeck(humanPlayer, Desk.yHumanCards, false);
                }
                else
                {
                    TakeFromDeck(humanPlayer, Desk.yHumanCards, false);
                    TakeFromDeck(botPlayer, Desk.yBotCards, true);
                }              
            }
            for (int i = 0; i < cardsRanksPossiblities.Count;)
            {
                cardsRanksPossiblities.RemoveAt(i);
            }
        }

        /// <summary>
        /// adds cards from the deck to the given player till he will have 6 cards
        /// </summary>
        public void TakeFromDeck(Player player, int y, bool isBlack)
        {
            Card temp;
            while (player.cardsInHand.Count < 6)
            {
                if (deck.Count == 0) return;
                temp = deck.First<Card>();
                deck.Remove(temp);
                temp.isMoving = true;
                temp.rotate = 0;
                temp.isBlack = isBlack;
                temp.destY = y;
                if (player.cardsInHand.Count == 0)
                {
                    temp.destX = Desk.xFirstCard;
                }
                else
                {
                    temp.destX = player.cardsInHand[player.cardsInHand.Count - 1].destX - Game1.distanceBetweenCards;
                }
                player.cardsInHand.Add(temp);
            }
        }
                   
        /// <summary>
        /// check if all the cards have finished moving.
        /// </summary>
        public bool CheckFinished()
        {
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].isMoving)
                    return false;
            }
            for (int i = 0; i < humanPlayer.cardsInHand.Count; i++)
            {
                if (humanPlayer.cardsInHand[i].isMoving)
                    return false;
            }
            for (int i = 0; i < botPlayer.cardsInHand.Count; i++)
            {
                if (botPlayer.cardsInHand[i].isMoving && Game1.gameState != GameState.endGame)
                    return false;
            }
            for (int i = 0; i < attackCards.Count; i++)
            {
                if (attackCards[i].isMoving && Game1.gameState != GameState.endGame)
                    return false;
            }
            for (int i = 0; i < defendCards.Count; i++)
            {
                if (defendCards[i].isMoving && Game1.gameState != GameState.endGame)
                    return false;
            }
            for (int i = 0; i < bita.Count; i++)
            {
                if (bita[i].isMoving)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// adds the cards which participated in the attack or defense to the cards of the given player
        /// </summary>
        public void DoTake(Player player, int y, bool isBlack)
        {
            for (int i = 0; i < defendCards.Count;)
            {
                defendCards[i].isMoving = true;
                defendCards[i].destX = player.cardsInHand[player.cardsInHand.Count - 1].destX - Game1.distanceBetweenCards;
                defendCards[i].destY = y;
                defendCards[i].isBlack = isBlack;
                player.cardsInHand.Add(defendCards[i]);
                defendCards.Remove(defendCards[i]);
            }
            for (int i = 0; i < attackCards.Count;)
            {
                attackCards[i].isMoving = true;
                attackCards[i].destX = player.cardsInHand[player.cardsInHand.Count - 1].destX - Game1.distanceBetweenCards;
                attackCards[i].destY = y;
                attackCards[i].isBlack = isBlack;
                player.cardsInHand.Add(attackCards[i]);
                attackCards.Remove(attackCards[i]);
            }
            for (int i = 0; i < cardsRanksPossiblities.Count;)
            {
                cardsRanksPossiblities.RemoveAt(i);
            }
        }
       
        /// <summary>
        /// check if there are empty spaces between the card of the players and remove those spaces
        /// </summary>
        public void CheckForEmptySpaces()
        {           
            for (int i = 0; i < botPlayer.cardsInHand.Count - 1; i++)
            {
                if (botPlayer.cardsInHand[i].destX - botPlayer.cardsInHand[i + 1].destX != Game1.distanceBetweenCards)
                {
                    botPlayer.cardsInHand[i + 1].isMoving = true;
                    botPlayer.cardsInHand[i + 1].destX = botPlayer.cardsInHand[i].destX - Game1.distanceBetweenCards;
                }
            }
            if (botPlayer.cardsInHand.Count != 0)
            {
                if (botPlayer.cardsInHand[0].destX != Desk.xFirstCard)
                {
                    for (int i = 0; i < botPlayer.cardsInHand.Count; i++)
                    {
                        botPlayer.cardsInHand[i].isMoving = true;
                        botPlayer.cardsInHand[i].destX -= Desk.xFirstCard - botPlayer.cardsInHand[0].destX;
                    }
                }
            }
          
            for (int i = 0; i < humanPlayer.cardsInHand.Count - 1; i++)
            {
                if (humanPlayer.cardsInHand[i].destX - humanPlayer.cardsInHand[i + 1].destX != Game1.distanceBetweenCards)
                {
                    humanPlayer.cardsInHand[i + 1].isMoving = true;
                    humanPlayer.cardsInHand[i + 1].destX = humanPlayer.cardsInHand[i].destX - Game1.distanceBetweenCards;
                }
            }
            if (humanPlayer.cardsInHand.Count != 0)
            {
                if (humanPlayer.cardsInHand[0].destX != Desk.xFirstCard)
                {
                    for (int i = 0; i < humanPlayer.cardsInHand.Count; i++)
                    {
                        humanPlayer.cardsInHand[i].isMoving = true;
                        humanPlayer.cardsInHand[i].destX -= Desk.xFirstCard - humanPlayer.cardsInHand[0].destX;
                    }
                }
            }           
        }
    }
}
