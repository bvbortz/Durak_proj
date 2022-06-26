using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using System.Text;
using System.Threading.Tasks;

namespace Durak
{
    #region enums
    public enum AttackTactics
    {
        noMercyAttack,
        noHighAttack,
        noCozirAttack,
        none
    }
    public enum DefenseTactics
    {
        noCozirDefend,
        takeHighDefend,
        tillTheEndDefend,
        takeNoLowDefend,
        none
    } 
    #endregion

    static class Strategy
    {
        #region tactics
        public static bool noMercyAttackActive = false; //the computer can attack with all the cards he has
        public static bool noCozirAttackActive = false; // the computer  attacks with trumps (cozirs) only when he has no other choice
        public static bool noHighAttackActive = false; //the computer attacks with cards higher than jack or with cozirs only when he has no other choice
        public static bool noCozirDefendActive = false; //the computer will not defend with cozirs
        public static bool takeHighDefendActive = false; //the computer will take if there's a card in the attack that's higher than jack
        public static bool tillTheEndDefendActive = false;//the computer will keep defending till there's no choice- he has to take
        public static bool takeNoLowDefendActive = false;//when there's a card in the attack that's lower than 9, use tillTheEndDefendActive; in other cases use takeHighDefendActive.
        #endregion

        #region fields
        private static int lengthTableLine = 11; //the length of a line in the table, to avoid errors
        static float seriesImportance; //when evaluating a card, how important is the length of series of cards with the same rank
        static float weightImportance; //when evaluating a card, how important is the weight of the card
        static bool weightMoreImportant;// based on seriesImportance and weightImportance, is the weight more important?
        public static AttackTactics attackTacticBeginning = AttackTactics.none; //saves the attack tactic that was chosen in the beginning
        public static DefenseTactics defenseTacticBeginning = DefenseTactics.none;//saves the defense tactic that was chosen in the beginning
        public static AttackTactics attackTacticEnd = AttackTactics.none;//saves the attack tactic that was chosen in the end
        public static DefenseTactics defenseTacticEnd = DefenseTactics.none;//saves the defense tactic that was chosen in the end
        static int numOfTimesWritten; //the number of times the Q-values file has been updated. The value of this variable is written in the file itself

        static List<List<float>> attackTacticsQvalues = new List<List<float>>(); // saves the probability of success of all the attack tactics based on the Q-data from the file
        static List<List<float>> defenseTacticsQvalues = new List<List<float>>();// saves the probability of success of all the defense tactics based on the Q-data from the file
        static Random random = new Random();
        static IEnumerable<string> fileText;
        static int initialTrainingCycle = 100;
        static BotPlayer botPlayer;
        static Player humanPlayer;
        #endregion

        #region methods

        /// <summary>       
        /// finds the lowest card with the lowest rank in the list "possibilities" 
        /// </summary>  
        public static Card MinCard(List<Card> possibilities)
        {
            Card card = possibilities[0];
            for (int i = 1; i < possibilities.Count; i++)
            {
                if (card.suit == (int)Desk.cozir)
                {
                    if (possibilities[i].suit != card.suit)
                    {
                        card = possibilities[i];
                    }
                    else if (possibilities[i].rank < card.rank)
                    {
                        card = possibilities[i];
                    }
                }
                else if (possibilities[i].rank < card.rank && possibilities[i].suit != (int)Desk.cozir)
                {
                    card = possibilities[i];
                }
            }
            return card;
        }

        /// <summary>
        /// heuristic function that calculates the rewards and returns the string that needs to be written to the history file to update the changes
        /// </summary>
        public static string CalcRewards(Winner winner)
        {
            float deltaProportionForBeginningState = 0.1f;
            float deltaProportionForEndState = 0.05f;

            if (numOfTimesWritten > initialTrainingCycle)//if the computer is experienced,  the change must be less drastic
            {
                deltaProportionForBeginningState /= 2;
                deltaProportionForEndState /= 2;
            }
            if (winner == Winner.human)//if the computer lost, the delta should be negative, to decrease the probability of success of the tactics usedd
            {
                deltaProportionForBeginningState *= -1;
                deltaProportionForEndState *= -1;
            }

            String[] textArr = fileText.ToArray();
            String[] parametersStateBeginning = textArr[1].Split(',');//the parameters are the actions' probability of success

            numOfTimesWritten++;
            parametersStateBeginning[lengthTableLine - 1] = numOfTimesWritten + "";

            //changing the probability of success of the used tactics - proportionally to their value, so the value will never be zero
            float temp;
            temp = float.Parse(parametersStateBeginning[(int)attackTacticBeginning + 1]);// selects position (from 1 to 3) corresponding to the attack tactic used at beginning
            temp += (deltaProportionForBeginningState * temp);
            parametersStateBeginning[(int)attackTacticBeginning + 1] = temp + "";

            temp = float.Parse(parametersStateBeginning[(int)defenseTacticBeginning + 4]);// selects position (from 4 to 7) corresponding to the defense tactic used at beginning
            temp += (deltaProportionForBeginningState * temp);
            parametersStateBeginning[(int)defenseTacticBeginning + 4] = temp + "";

            //in the parameters of series and weight importance the program always increase one and decrease the other
            float temp2;
            temp = float.Parse(parametersStateBeginning[8]);
            temp2 = float.Parse(parametersStateBeginning[9]);
         
            if (weightMoreImportant)
            {
                temp2 += (deltaProportionForBeginningState * temp2);
                temp -= (deltaProportionForBeginningState * temp);
            }
            else
            {
                temp += (deltaProportionForBeginningState * temp);
                temp2 -= (deltaProportionForBeginningState * temp2);
            }
            parametersStateBeginning[8] = temp + "";
            parametersStateBeginning[9] = temp2 + "";
            
            string[] parametersStateEnd = textArr[2].Split(',');

            temp = float.Parse(parametersStateEnd[(int)attackTacticEnd + 1]);
            temp += (deltaProportionForEndState * temp);
            parametersStateEnd[(int)attackTacticEnd + 1] = temp + "";

            temp = float.Parse(parametersStateEnd[(int)defenseTacticEnd + 4]);
            temp += (deltaProportionForEndState * temp);
            parametersStateEnd[(int)defenseTacticEnd + 4] = temp + "";

            //series length and weight importance don't change between beginning and end
            parametersStateEnd[8] = parametersStateBeginning[8];
            parametersStateEnd[9] = parametersStateBeginning[9];

            //creates a string that the program will write to the file that will include the changes that have been made
            string strToWrite = textArr[0] + "\n";
            for (int i = 0; i < parametersStateBeginning.Length; i++)
            {
                strToWrite += parametersStateBeginning[i] + ",";
            }
            strToWrite += "\n";
            for (int i = 0; i < parametersStateEnd.Length; i++)
            {
                strToWrite += parametersStateEnd[i] + ",";
            }
            return strToWrite;
        }

        /// <summary>
        /// changing the tactic of the current state (beginning or end), based on the Q-data from the file 
        /// <param name="state">which state of the Q table is ralevant and needs to be changed</param>
        /// </summary>
        public static void UpdatePolicies(StatesInQtable state)
        {         
            String[] textArr = fileText.ToArray();
            String[] parameters = textArr[(int)state].Split(',');
            int idx = 1;

            if (state == StatesInQtable.beginning)
            {              
                SetImportances(parameters);
            }

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    attackTacticsQvalues[(int)state - 1].Add(float.Parse(parameters[idx]));
                }
                catch (Exception)
                {
                    throw new System.ArgumentException("Qtable.csv: bad format!");
                }               
                idx++;
            }
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    defenseTacticsQvalues[(int)state - 1].Add(float.Parse(parameters[idx]));
                }
                catch (Exception)
                {
                    throw new System.ArgumentException("Qtable.csv: bad format!");
                }               
                idx++;
            }

            //if the computer isn't experienced enough- use randomize values for exploration
            if (numOfTimesWritten < initialTrainingCycle)
            {
                for (int i = 0; i < attackTacticsQvalues[(int)state - 1].Count; i++)
                {
                    attackTacticsQvalues[(int)state - 1][i] *= (float)random.NextDouble();
                }
                for (int i = 0; i < defenseTacticsQvalues[(int)state - 1].Count; i++)
                {
                    defenseTacticsQvalues[(int)state - 1][i] *= (float)random.NextDouble();
                }
            }

            //choose the attack tactic with the best probability of success
            switch (attackTacticsQvalues[(int)state - 1].IndexOf(attackTacticsQvalues[(int)state - 1].Max()))
            {
                case 0:
                    noMercyAttackActive = true;
                    noHighAttackActive = false;
                    noCozirAttackActive = false;
                    if (state == StatesInQtable.end)
                    {
                        attackTacticEnd = AttackTactics.noMercyAttack;
                    }
                    else
                    {
                        attackTacticBeginning = AttackTactics.noMercyAttack;
                    }                    
                    break;
                case 1:
                    noHighAttackActive = true;
                    noCozirAttackActive = false;
                    noMercyAttackActive = false;
                    if (state == StatesInQtable.end)
                    {
                        attackTacticEnd = AttackTactics.noHighAttack;
                    }
                    else
                    {
                        attackTacticBeginning = AttackTactics.noHighAttack;
                    }
                    break;
                case 2:
                    noCozirAttackActive = true;
                    noHighAttackActive = false;
                    noMercyAttackActive = false;
                    if (state == StatesInQtable.end)
                    {
                        attackTacticEnd = AttackTactics.noCozirAttack;
                    }
                    else
                    {
                        attackTacticBeginning = AttackTactics.noCozirAttack;
                    }
                    break;
            }

            //choose the defense tactic with the best probability of success
            switch (defenseTacticsQvalues[(int)state - 1].IndexOf(defenseTacticsQvalues[(int)state - 1].Max()))
            {
                case 0:
                    noCozirDefendActive = true;
                    takeHighDefendActive = false;
                    tillTheEndDefendActive = false;
                    takeNoLowDefendActive = false;
                    if (state == StatesInQtable.end)
                    {
                        defenseTacticEnd = DefenseTactics.noCozirDefend;
                    }
                    else
                    {
                        defenseTacticBeginning = DefenseTactics.noCozirDefend;
                    }
                    break;
                case 1:
                    takeHighDefendActive = true;
                    tillTheEndDefendActive = false;
                    takeNoLowDefendActive = false;
                    noCozirDefendActive = false;
                    if (state == StatesInQtable.end)
                    {
                        defenseTacticEnd = DefenseTactics.takeHighDefend;
                    }
                    else
                    {
                        defenseTacticBeginning = DefenseTactics.takeHighDefend;
                    }
                    break;
                case 2:
                    tillTheEndDefendActive = true;
                    takeHighDefendActive = false;
                    takeNoLowDefendActive = false;
                    noCozirDefendActive = false;
                    if (state == StatesInQtable.end)
                    {
                        defenseTacticEnd = DefenseTactics.tillTheEndDefend;
                    }
                    else
                    {
                        defenseTacticBeginning = DefenseTactics.tillTheEndDefend;
                    }
                    break;
                case 3:
                    takeNoLowDefendActive = true;
                    tillTheEndDefendActive = false;
                    takeHighDefendActive = false;
                    noCozirDefendActive = false;
                    if (state == StatesInQtable.end)
                    {
                        defenseTacticEnd = DefenseTactics.takeNoLowDefend;
                    }
                    else
                    {
                        defenseTacticBeginning = DefenseTactics.takeNoLowDefend;
                    }
                    break;
            }          
        }

        /// <summary>
        /// initialzes the 2D lists, gets botPlayer and humanPlayer so the Strategy class will be able to use them later on and reads the data from the file
        /// </summary>
        public static void Initialize(BotPlayer botPlayer, Player humanPlayer)
        {
            if (!File.Exists("..\\Qtable.csv"))
            {
                throw new System.ArgumentException("The file Qtable.csv doesn't exist");
            }
            Strategy.botPlayer = botPlayer;
            Strategy.humanPlayer = humanPlayer;
            //As there are currently 2 states (beginniing and end) create 2 lines in the 2D lists
            attackTacticsQvalues = new List<List<float>>();
            defenseTacticsQvalues = new List<List<float>>();
            attackTacticsQvalues.Add(new List<float>());    //for state beginning
            attackTacticsQvalues.Add(new List<float>());    //for state end
            defenseTacticsQvalues.Add(new List<float>());   //for state beginning
            defenseTacticsQvalues.Add(new List<float>());   //for state end
            fileText = System.IO.File.ReadLines("..\\Qtable.csv");
            String[] textArr = fileText.ToArray();
            String[] parameters = textArr[1].Split(',');
            try
            {
                numOfTimesWritten = int.Parse(parameters[lengthTableLine - 1]);
            }
            catch (Exception)
            {
                throw new System.ArgumentException("Qtable.csv: bad format!");
            }
        }

        /// <summary>
        /// sets seriesImportance and weightImportance, based on the Q-data from the file
        /// </summary>
        static void SetImportances(String[] parameters)
        {
            //gets seriesImportance and weightImportance from the file
            try
            {
                seriesImportance = float.Parse(parameters[8]);
                weightImportance = float.Parse(parameters[9]);
            }
            catch (Exception)
            {
                throw new System.ArgumentException("Qtable.csv: bad format!");
            }

            if (numOfTimesWritten < initialTrainingCycle)//if the computer isn't experienced enough - use random for exploration
            {
                seriesImportance *= (float)random.NextDouble();
                weightImportance *= (float)random.NextDouble();
            }
            if (weightImportance > seriesImportance)
            {
                weightMoreImportant = true;
            }
            else
            {
                weightMoreImportant = false;
            }
        }

        /// <summary>
        /// sets lengthOfSeries in all of the computer's cards
        /// creates a 2D card list and in every line puts cards with the same rank
        /// sets lengthOfSeries of every card to the length of the corrsponding line in the 2D list
        /// </summary>
        public static void SortBySeries(bool isDefense, Desk desk)
        {
            List<List<Card>> myCards2D = new List<List<Card>>();
            List<Card> allMyCards = new List<Card>();

            //move all computer's cards to the 1D list
            for (int i = 0; i < botPlayer.cardsInHand.Count; i++)
            {
                allMyCards.Add(botPlayer.cardsInHand[i]);
            }
            if (isDefense)
            {
                for (int i = 0; i < desk.defendCards.Count; i++)
                {
                    allMyCards.Add(desk.defendCards[i]);
                }
            }
            else
            {
                for (int i = 0; i < desk.attackCards.Count; i++)
                {
                    allMyCards.Add(desk.attackCards[i]);
                }
            }

            Card temp;
            int cnt = 0;

            //go through the 1D list and move every card to the 2D list, when cards with the same rank will be in the same line in the 2D list
            while (allMyCards.Count != 0)
            {
                myCards2D.Add(new List<Card>());
                temp = allMyCards[0];
                allMyCards.Remove(temp);
                myCards2D[cnt].Add(temp);
                for (int i = 1; i < allMyCards.Count; i++)
                {
                    if (allMyCards[i].rank == temp.rank)
                    {
                        allMyCards.Remove(temp);
                        myCards2D[cnt].Add(temp);
                    }
                }
                cnt++;
            }
            //set lengthOfSeries of each card based on the length of the line the card is located in the 2D list
            for (int i = 0; i < myCards2D.Count; i++)
            {
                for (int j = 0; j < myCards2D[i].Count; j++)
                {
                    myCards2D[i][j].lengthOfSeries = myCards2D[i].Count;
                }
            }
        }

        /// <summary>
        /// return whether the first card can defend the second card
        /// </summary>
        public static bool IsFirstStronger(Card first, Card second)
        {
            if (first.trumpFlag && !second.trumpFlag)
            {
                return true;
            }
            if (first.suit == second.suit && first.rank > second.rank)
            {
                return true;
            }
            if (first.trumpFlag && first.rank > second.rank)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// gets a cards and counts the number of cards that are less strong than this card by using the function IsFirstStronger
        /// </summary>
        public static int CalcWeight(Card card, Desk desk)
        {
            //the function goes through the cards that are still in the game
            int weight = 0;
            for (int i = 0; i < desk.deck.Count; i++)
            {
                if (IsFirstStronger(card, desk.deck[i]))
                {
                    weight++;
                }
            }
            for (int i = 0; i < humanPlayer.cardsInHand.Count; i++)
            {
                if (IsFirstStronger(card, humanPlayer.cardsInHand[i]))
                {
                    weight++;
                }
            }
            return weight;
        }

        /// <summary>
        /// evaluate for each card of a given list the probability of success of its use for the next move
        /// As it's better to get rid of the weak cards, there a inverse proportion between the probability of success of the move and the weight of the card,
        /// so the function creates a list of cards with the same index as in the possibilities list
        /// the probability of success of a move is set to weightImportance divided by the weight of the card plus seriesImportance multiplied by the card's lengthOfSeries
        /// </summary>
        public static List<double> EvaluateCards(List<Card> possibilities, Desk desk)
        {
            List<double> preferences = new List<double>();
            for (int i = 0; i < possibilities.Count; i++)
            {
                preferences.Add(weightImportance / CalcWeight(possibilities[i], desk)
                    + seriesImportance * possibilities[i].lengthOfSeries);
            }
            return preferences;
        }

        /// <summary>
        /// narrows the defend options of the computer based on the tactic that is being used
        /// </summary>
        public static List<Card> NarrowDefendPossibilities(Desk desk, List<Card> possibilities)
        {
            if (possibilities.Count == 0)
            {
                return possibilities;
            }
            if (noCozirDefendActive)//remove the cozirs
            {
                for (int i = 0; i < possibilities.Count; i++)
                {
                    if (possibilities[i].trumpFlag)
                    {
                        possibilities.RemoveAt(i);
                        i--;
                    }
                }
            }

            //high cards are higher than jack and low cards are lower than 9
            bool hasLow = false;
            bool hasHigh = false;
            for (int i = 0; i < desk.attackCards.Count; i++)
            {
                if (desk.attackCards[i].rank > 11)
                    hasHigh = true;
                if (desk.attackCards[i].rank < 9)
                    hasLow = true;
            }
            //if takeNoLowDefendActive and there are low cards in the attack, activate tillTheEndDefendActive
            //else, activate takeHighDefendActive
            //when takeNoLowDefendActive, takeHighDefendActive and tillTheEndDefendActive are being updated in evry defense move
            if (takeNoLowDefendActive)
            {
                if (hasLow)
                {
                    takeHighDefendActive = false;
                    tillTheEndDefendActive = true;
                }
                else
                {
                    takeHighDefendActive = true;
                    tillTheEndDefendActive = false;
                }
            }

            //if takeHighDefendActive and there are high cards return empty list or in other words - take
            if (takeHighDefendActive)
            {
                if (hasHigh)
                {
                    return (new List<Card>());
                }
            }
            return possibilities;
        }

        /// <summary>
        /// narrows the attack options of the computer based on the tactic that is being used
        /// </summary>
        public static List<Card> NarrowAttackPossibilities(Desk desk, List<Card> possibilities)
        {

            //if noCozirAttackActive or noHighAttackActive remove the cozirs
            if (noCozirAttackActive || noHighAttackActive)
            {
                for (int i = 0; i < possibilities.Count; i++)
                {
                    if (possibilities[i].trumpFlag)
                    {
                        possibilities.RemoveAt(i);
                        i--;
                    }
                }
            }

            //if noHighAttackActive remove the cards that are higher than jack
            if (noHighAttackActive)
            {
                for (int i = 0; i < possibilities.Count; i++)
                {
                    if (possibilities[i].rank > 11)
                    {
                        possibilities.RemoveAt(i);
                        i--;
                    }
                }
            }
            if (possibilities.Count == 0 && desk.cardsRanksPossiblities.Count == 0)
            {
                possibilities.Add(MinCard(botPlayer.cardsInHand));
            }
            return possibilities;
        } 
        #endregion
    }
}
