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
    public enum StatusAttack
    {
        nothingToAdd,
        doBita,
        defenseTurn,
        none
    }

    public enum StatusDefense
    {
        add,
        bita,
        none
    }

    public class BotPlayer:Player
    {
        /// <summary>       
        /// based on the game state it evaluates the possible moves using Strategy class
        /// and decides what move the computer will make in the attack.
        /// <param name="activateTactics">does it is needed to get into account the tactics (in auto mode, when both players are bots, it doesn't)</param>
        /// </summary>  
        public StatusAttack MakeAttackMove(Desk desk, Player player, bool activateTactics)        
        {
            if (Game1.gameState == GameState.endGame)
                return StatusAttack.none;  
            
            if (desk.deck.Count == 0 && !Game1.hasTacticChanged)
            {
                Strategy.UpdatePolicies(StatesInQtable.end);
                Game1.hasTacticChanged = true;
            }
            List<Card> possibilities = new List<Card>();
            for (int i = 0; i < player.cardsInHand.Count; i++)
            {
                if (desk.cardsRanksPossiblities.Count == 0 || desk.cardsRanksPossiblities.Contains(player.cardsInHand[i].rank))
                {
                    possibilities.Add(player.cardsInHand[i]);
                }
            }
            if (activateTactics)
            {
                possibilities = Strategy.NarrowAttackPossibilities(desk, possibilities);
            }
            
            if (Game1.gameState == GameState.addAllYouCan)
            {
                for (int i = 0; i < possibilities.Count; i++)
                {
                    if(!possibilities[i].trumpFlag ||(possibilities[i].trumpFlag && desk.deck.Count == 0))
                    {
                        MoveCardToAttack(player.cardsInHand.IndexOf(possibilities[i]), desk, player);                       
                    }                   
                }               
                return StatusAttack.nothingToAdd;
            }
            if (possibilities.Count == 0 && desk.attackCards.Count == desk.defendCards.Count)
            {
                return StatusAttack.doBita;
            }
            if (possibilities.Count != 0)
            {
                Strategy.SortBySeries(false, desk);

                Card temp;
                if (desk.cardsRanksPossiblities.Count > 0)
                {
                    temp=possibilities[0];
                }
                else
                {
                    List<double> probabilitiesOfSuccess = Strategy.EvaluateCards(possibilities, desk);
                    temp = possibilities[probabilitiesOfSuccess.IndexOf(probabilitiesOfSuccess.Max())];
                }
                if (MoveCardToAttack(player.cardsInHand.IndexOf(temp), desk, player))
                {
                    return StatusAttack.defenseTurn;
                }
                else
                {
                    return StatusAttack.doBita;
                }
            }
            return StatusAttack.none;
        }

        /// <summary>       
        /// based on the game state it evaluates the possible moves using Strategy class
        /// and decides what move the computer will make in the defense.
        /// <param name="activateTactics">does it is needed to get into account the tactics (when both players are bots)</param>
        /// </summary>  
        public StatusDefense MakeDefenseMove(Desk desk, Player player, bool activateTactics)
        {
            if (Game1.gameState == GameState.endGame)
                return StatusDefense.none;

            if(desk.deck.Count == 0 && !Game1.hasTacticChanged)
            {
                Strategy.UpdatePolicies(StatesInQtable.end);
                Game1.hasTacticChanged = true;
            }
            List<Card> possibilities = Game1.DefendPossibilities(desk.attackCards[desk.defendCards.Count], player);
            if (activateTactics)
            {
                possibilities = Strategy.NarrowDefendPossibilities(desk, possibilities);
            }            
            if (possibilities.Count == 0)
            {                
                return StatusDefense.add;
            }

            Strategy.SortBySeries(true, desk);
            List<double> probabilitiesOfSuccess = Strategy.EvaluateCards(possibilities, desk);
            Card temp = possibilities[probabilitiesOfSuccess.IndexOf(probabilitiesOfSuccess.Max())];
            if(MoveCardToDefense(player.cardsInHand.IndexOf(temp), desk, player))
            {
                return StatusDefense.bita;
            }
            return StatusDefense.none;
        }
    }
}
