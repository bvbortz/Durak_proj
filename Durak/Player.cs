using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Durak
{
    public class Player
    {
        public const int OUT_OF_RANGE = 99;
        public  List<Card> cardsInHand;

        public Player()
        {
            cardsInHand = new List<Card>();
        }

        /// <summary>
        /// Attacks with the card the player chose, if allowed by the game rules
        /// </summary>
        /// <param name="index">shows what is the index of the card in the player's cards list.</param>
        /// <returns>true if OK, false if failure</returns>
        public bool MoveCardToAttack(int index, Desk desk, Player player)
        {
            if (Game1.gameState == GameState.endGame)
                return false;

            //only if a legal card was sent and there still place in the attack
            if (index != OUT_OF_RANGE && desk.attackCards.Count < Game1.maxCardsInAttack) 
            {
                Card temp = player.cardsInHand[index];
                if (desk.cardsRanksPossiblities.Count == 0)
                {
                    desk.cardsRanksPossiblities.Add(temp.rank);
                }
                if (desk.cardsRanksPossiblities.Contains(temp.rank))
                {
                    temp.isMoving = true;
                    temp.isBlack = false;
                    temp.destX = (int)(Desk.attackPos[desk.attackCards.Count].X);
                    temp.destY = (int)(Desk.attackPos[desk.attackCards.Count].Y);
                    desk.attackCards.Add(temp);
                    player.cardsInHand.Remove(temp);
                    return true;                    
                }
            }
            return false;
        }

        /// <summary>
        /// Defends with the card the player chose, if allowed by the game rules
        /// </summary>
        /// <param name="cardIndex">shows what is the index of the card in the player's cards list.</param>
        /// <returns>true if OK, false if failure</returns>
        public bool MoveCardToDefense(int cardIndex, Desk desk, Player player)
        {
            if (Game1.gameState == GameState.endGame)
                return false;
            if (cardIndex != OUT_OF_RANGE) 
            {
                Card temp = player.cardsInHand[cardIndex];
                List<Card> possibilities = Game1.DefendPossibilities(desk.attackCards[desk.defendCards.Count], player);
                if (possibilities.Contains(temp))
                {
                    if (!desk.cardsRanksPossiblities.Contains(temp.rank))
                    {
                        desk.cardsRanksPossiblities.Add(temp.rank);
                    }
                    temp.isMoving = true;
                    temp.isBlack = false;
                    temp.destX = (int)(Desk.attackPos[desk.defendCards.Count].X + 10);
                    temp.destY = (int)(Desk.attackPos[desk.defendCards.Count].Y + 10);
                    desk.defendCards.Add(temp);
                    player.cardsInHand.Remove(temp);
                    return true;
                    
                }
            }
            return false;
        }
    }
}
