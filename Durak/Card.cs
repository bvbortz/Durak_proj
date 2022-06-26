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
    #region enums
    public enum CardSuit
    {
        spades,
        hearts,
        diamonds,
        clubs
    }

    public enum CardRank
    {
        six = 6,
        seven,
        eight,
        nine,
        ten,
        jack,
        queen,
        king,
        ace
    } 
    #endregion

    public class Card
    {
        #region fields
        public ContentManager cm;  //  ContentManager is Monogame class used for loading the "textures" (i.e. graphic images)
        public SpriteBatch sb; //  SpriteBatch is Monogame class used for drawing
        private Texture2D textureBlackSide;
        private Texture2D textureRegularSide;
        private float speed = 3.0f;
        public bool trumpFlag; //indicates whether the card is a cozir (trump)
        public int suit;
        public int rank;
        public int lengthOfSeries = 0; //how many cards the computer has with this rank - helps to decide which move is better.
        public Vector2 location;
        public float rotate;
        public bool isBlack; //decides whether the card will be face down
        public bool isMoving; //is the card moving
        public int destX; //the destination of the card - where it's heading during the move
        public int destY; 
        #endregion

        public Card(CardSuit suit, CardRank rank, Vector2 location, float rotate = 0, bool isBlack = true)
        {
            this.suit = (int)suit;
            this.rank = (int)rank;
            this.location = location;
            this.rotate = rotate;
            this.isBlack = isBlack;
        }

        /// <summary>
        /// loads the texture of the cards to the memory
        /// Monogame's ContentManager is used for loading and the SpriteBatch (also from Monogame) will be used for drawing
        /// </summary>
        public void Load(ContentManager cm, SpriteBatch sb)
        {
            this.cm = cm;
            this.sb = sb;
          
            this.textureBlackSide = cm.Load<Texture2D>(Enum.GetName(typeof(CardRank), rank) + " " + Enum.GetName(typeof(CardSuit), suit) + " rotate to the regular side");
            this.textureRegularSide= cm.Load<Texture2D>(Enum.GetName(typeof(CardRank), rank) + " " + Enum.GetName(typeof(CardSuit), suit) + " rotate to the black side");
        }

        public override string ToString()
        {
            return ("the suit is "+Enum.GetName(typeof(CardSuit),suit)+" and the rank is "+ Enum.GetName(typeof(CardRank), rank));
        }

        public void DrawCard()
        {
            sb.Begin();
            Rectangle rec = new Rectangle(0, 0, 660, 1000);
            if (isBlack)
            {
                sb.Draw(textureBlackSide, location, rec, Color.White, rotate, new Vector2(0, 0), 0.15f, SpriteEffects.None, 0);
            }
            else
            {
                sb.Draw(textureRegularSide, location, rec, Color.White, rotate, new Vector2(0, 0), 0.15f, SpriteEffects.None, 0);
            }
            sb.End();
        }

        /// <summary>
        /// updates the trumpFlag and moves the card when the game is on and isMoving is true
        /// </summary>
        public void Move()
        {
            if (this.suit == (int)Desk.cozir)
            {
                trumpFlag = true;
            }
            else
            {
                trumpFlag = false;
            }

            if (!isMoving)
                return;

            if (!Game1.gameOn)
                return;

            if (Math.Abs(this.destX - this.location.X) < speed)
            {
                this.location.X = this.destX;
            }
            if (this.destX > this.location.X)
            {
                this.location.X += speed;
            }
            else if(this.destX < this.location.X)
            {
                this.location.X -= speed;
            }
            else if(this.destX == this.location.X)
            {
                if (Math.Abs(this.destY - this.location.Y) < speed)
                {
                    this.location.Y = this.destY;
                }
                if (this.destY > this.location.Y)
                {
                    this.location.Y += speed;
                }
                else if (this.destY < this.location.Y)
                {
                    this.location.Y -= speed;
                }
                else if (this.destY == this.location.Y) //this means that the card is standing and not moving
                {
                    isMoving = false;
                }
            }
        }        
    }
}
