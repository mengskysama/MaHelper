using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace MAH
{
    class CardInfo
    {
        public class Card
        {
            public int master_card_id;
            public string name;
            public int star;
            public int cost;
        }

        static public List<Card> cardlst = new List<Card>();

        public static int getCardcost(int master_card_id)
        {
            foreach (Card card in CardInfo.cardlst)
            {
                if (card.master_card_id == master_card_id)
                    return card.cost;
            }
            return 999;
        }

        public static string getCardname(int master_card_id)
        {
            foreach (Card card in CardInfo.cardlst)
            {
                if (card.master_card_id == master_card_id)
                    return card.name;
            }
            return "未知";
        }

        public static void readData(string dir)
        {
            using (StreamReader sr = new StreamReader(new FileStream(dir, FileMode.Open, FileAccess.Read)))
            {
                while (sr.Peek() > 0)
                {
                    string s = sr.ReadLine();
                    string[] sz = s.Split(',');
                    Card card = new Card();
                    card.master_card_id = int.Parse(sz[0]);
                    card.name = sz[1];
                    card.star = int.Parse(sz[2]);
                    card.cost = int.Parse(sz[3]);
                    cardlst.Add(card);
                }
                sr.Close();
            }
        }

    }
}
