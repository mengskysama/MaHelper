using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MAH
{
    class MA_Client
    {


        public static string login_id;
        public static string login_password;
        public static int jx_time = 600;


        public static string card1 = null;
        public static string card2 = null;
        public static string card3 = null;
        public static string card4 = null;
        public static string card1l = null;
        public static string card2l = null;
        public static string card3l = null;
        public static string card4l = null;

        public static string sell = null;
        public static string force_explore_area = null;
        public static string wake_up_key = null;

        public static int tea_ap = 0;
        public static int tea_bc = 0;
        public static int noname_bc = 20;
        public static int noname_ap = 7;
        public static int noname_cost = 25;
        public static int useautorewards = 0;
        public static int useautocard_cp = 0;
        public static int yinzi_use_tea_bc = 0;
        public static int force_explore_area_next = 0;
        public static int yinzi = 0;
        public static int useautocard = 0;
        public static int usegetcard = 0;
        public static int usesell = 0;
        public static int useselln = 0;
        public static int useexplore = 0;
        public static int force_explore = 0;
        public static int explore_ap = 0;
        public static int explore_bc = 0;
        public static int explore_step = 0;
        public static int force_pg = 0;
        public static int force_pg_bc = 0;
        public static int jx_time_begin = 0;
        public static int jx_time_end = 0;
        public static int jx_bc = 0;
        public static int jx_wait = 0;
        public static int loop_time = 4000;
        public static int changecard = -1;

        static DateTime time_SendAPBC = DateTime.Now.AddHours(-1.0);
        static DateTime time_GetCard = DateTime.Now.AddHours(-1.0);

        public class CardDeck
        {
            public int power;
            public int hp;
            public int cp;
            public MA.MA_Card[] card = new MA.MA_Card[3];

            public CardDeck Copy()
            {
                CardDeck d = new CardDeck();
                d.power = power;
                d.hp = hp;
                d.cp = cp;

                if (card[0] != null)
                    d.card[0] = card[0].Copy();
                if (card[1] != null)
                    d.card[1] = card[1].Copy();
                if (card[2] != null)
                    d.card[2] = card[2].Copy();

                return d;
            }
        }

        // null
        // 找到最优解
        public static CardDeck SelectCard(int mybc, int addbc, int fcp, int fcp_max, int flv)
        {
            List<CardDeck> decklst = new List<CardDeck>();

            //我瞎猜的
            flv = (int)((float)flv * 1.19);

            Script.frm.LogUpdateFunction("SordCard NeedBC=" + (addbc + Math.Sqrt(fcp / (float)fcp_max) * flv) + " Lv=" + flv + " fcp=" + fcp);

            if (mybc - 4 < addbc + Math.Sqrt(fcp / (float)fcp_max) * flv)
            {
                Script.frm.LogUpdateFunction("NeedBC=" + (addbc + Math.Sqrt(fcp / (float)fcp_max) * flv) + "预算CP超出当放弃自动配卡");
                return null;
            }

            //得到能击杀妖精卡组
            //得到LV>10       CP + 4 < SQRT(FCP/FCP_MAX)*flv 全排列卡组

            int cardCnt = MA.cardlst.Count;
            for (int i = 0; i < cardCnt; i++)
            {
                CardDeck deck = new CardDeck();
                if (MA.cardlst[i].lv < 10)
                    continue;
                if (MA.cardlst[i].cp > 50)
                    continue;
                if (useautocard_cp > (MA.cardlst[i].hp + MA.cardlst[i].power) / MA.cardlst[i].cp)
                    continue;
                deck.card[0] = MA.cardlst[i];

                for (int j = 0; j < cardCnt; j++)
                {
                    if (i == j || MA.cardlst[j].master_card_id == deck.card[0].master_card_id)
                        continue;
                    if (MA.cardlst[j].lv < 10)
                        continue;
                    if (MA.cardlst[j].cp > 50)
                        continue;
                    if (useautocard_cp > (MA.cardlst[j].hp + MA.cardlst[j].power) / MA.cardlst[j].cp)
                        continue;
                    deck.card[1] = MA.cardlst[j];
                    deck.cp = deck.card[0].cp + deck.card[1].cp;
                    deck.power = deck.card[0].power + deck.card[1].power;
                    deck.hp = deck.card[0].hp + deck.card[1].hp;
                    if (deck.cp > addbc + Math.Sqrt(fcp / (float)fcp_max) * flv)
                        decklst.Add(deck.Copy());
                    for (int k = 0; k < cardCnt; k++)
                    {
                        if (i == k || MA.cardlst[k].master_card_id == deck.card[0].master_card_id)
                            continue;
                        if (i == k || MA.cardlst[k].master_card_id == deck.card[1].master_card_id)
                            continue;
                        if (MA.cardlst[k].lv < 10)
                            continue;
                        if (MA.cardlst[k].cp > 50)
                            continue;
                        if (useautocard_cp > (MA.cardlst[k].hp + MA.cardlst[k].power) / MA.cardlst[k].cp)
                            continue;
                        deck.card[2] = MA.cardlst[k];
                        deck.cp = deck.card[0].cp + deck.card[1].cp + deck.card[2].cp;
                        deck.power = deck.card[0].power + deck.card[1].power + deck.card[2].power;
                        deck.hp = deck.card[0].hp + deck.card[1].hp + deck.card[2].hp;
                        if (deck.cp > addbc + Math.Sqrt(fcp / (float)fcp_max) * flv)
                            decklst.Add(deck.Copy());
                    }
                    deck.card[2] = null;
                }
                deck.card[1] = null;
            }

            //
            if (decklst.Count == 0 || decklst[0].cp > mybc)
            {
                Script.frm.LogUpdateFunction("找不出能日死的组合??");
                return null;
            }

            if (decklst[0].cp > mybc)
            {
                Script.frm.LogUpdateFunction("NeedBC=" + (addbc + Math.Sqrt(fcp / (float)fcp_max) * flv) + " +8 计算CP超出当放弃自动配卡");
                return null;
            }

            //找出BC相同最大输出卡组
            //得到CP升序排列
            int cp = decklst[0].cp;
            int deck_i = 0;
            int deck_output = decklst[0].hp + decklst[0].power;
            if (decklst.Count >= 2)
            {
                for (int i = 1; i < decklst.Count; i++)
                {
                    if (decklst[i].cp != cp)
                    {
                        break;
                    }
                    int deck_o = decklst[i].hp + decklst[i].power;
                    if (deck_output < deck_o)
                    {
                        deck_i = i;
                        deck_output = deck_o;
                    }
                }
            }
            return decklst[deck_i];
        }


        public static void Explore(int step, int exception = 0)
        {
            try
            {
                string area_id = MA.get_area_id(Script.hkgarea);
                Thread.Sleep(4000);
                while (true)
                {
                    if ((MA.ap < 2 || MA.bc < 2) || step <= 0)
                        break;
                    int ret = MA.exploration_explore(area_id, "1");
                    Thread.Sleep(4000);
                    if (ret == 8)
                    {
                        Script.frm.LogUpdateFunction("切换探索卡牌");
                        MA.cardselect_savedeckcard(card3, card3l);
                        Script.frm.LogUpdateFunction("切换探索卡牌完成");
                        Thread.Sleep(4000);
                        Script.frm.LogUpdateFunction("日怪");
                        MA.exploration_explore_fairy();
                        Script.frm.LogUpdateFunction("日怪完成延时等待");
                        Thread t = new Thread(Script.SendCnt);
                        t.Start();
                        Thread.Sleep(19000);
                    }
                    Script.frm.LogUpdateFunction("探索完成一步");
                    step--;
                }
            }
            catch (Exception ex)
            {
                if (exception == 1)
                    throw;
            }
        }

        //Auto Explore Map
        //12345  12345
        public static void AutoExplore(int step, int exception = 0)
        {
            try
            {
                while (true)
                {
                    string area_id = null;
                    string floor_id = null;
                    string area_name = null;
                    int cost = 0;

                    MA.get_event_area_id_floor_id(force_explore_area, out area_id, out floor_id, out area_name, out cost);

                    if (area_id == null || floor_id == null || area_name == null)
                    {
                        Script.frm.LogUpdateFunction("选取探索区域失败");
                        return;
                    }
                    else
                    {
                        Script.frm.LogUpdateFunction("开始探索秘境:" + area_name + ",区域" + floor_id);
                    }

                    Thread.Sleep(2000);

                    MA.exploration_get_floor(area_id, floor_id);

                    Thread.Sleep(2000);

                    if ((MA.ap < cost || (yinzi == 0 && MA.bc < 2)) || step < 1)
                        break;
                    int ret = MA.exploration_explore(area_id, floor_id);
                    Script.frm.LogUpdateFunction("探索完成一步");

                    if (MA.bc == MA.bcmax)
                    {
                        Script.frm.LogUpdateFunction("BC已经溢出,退出探索");
                        break;
                    }

                    Thread.Sleep(3000);

                    if (ret == 8)
                    {
                        Script.frm.LogUpdateFunction("假装妖精出现");
                        Thread.Sleep(2000);
                        if (yinzi == 1)
                        {
                            if (changecard != 1)
                            {
                                Script.frm.LogUpdateFunction("进入配卡菜单");
                                MA.roundtable_edit();
                                Script.frm.LogUpdateFunction("假装在选卡");
                                Thread.Sleep(3000);
                                Script.frm.LogUpdateFunction("因子战模式保留BC准备进行舔怪");
                                MA.cardselect_savedeckcard(card1, card1l);
                                changecard = 1;
                                Script.frm.LogUpdateFunction("切换舔怪卡牌完成");
                                Thread.Sleep(2000);
                            }
                            Script.frm.LogUpdateFunction("舔怪");
                            MA.exploration_explore_fairy();
                            Script.frm.LogUpdateFunction("舔怪完成延时等待");
                            Thread t = new Thread(Script.SendCnt);
                            t.Start();
                            Thread.Sleep(20000);
                        }
                        else
                        {
                            //强制日怪
                            MA.exploration_fairy_floor(int.Parse(MA.myfairy_serial_id), MA.user_id);
                            //MA.bc
                            if (useautocard == 0)
                            {
                                if (changecard != 3)
                                {
                                    Script.frm.LogUpdateFunction("进入配卡菜单");
                                    MA.roundtable_edit();
                                    Script.frm.LogUpdateFunction("假装在选卡");
                                    Thread.Sleep(3000);
                                    MA.cardselect_savedeckcard(card3, card3l);
                                    Script.frm.LogUpdateFunction("切换探索卡牌完成");
                                    changecard = 3;
                                }
                            }
                            else
                            {
                                MA_Client.CardDeck deck = MA_Client.SelectCard(MA.bc, 8, MA.fairy_floor_hp, MA.fairy_floor_hp_max, MA.fairy_floor_lv);
                                if (deck == null)
                                {
                                    if (changecard != 3)
                                    {
                                        Script.frm.LogUpdateFunction("BC=" + MA.bc + "自动选卡失败切换探索卡组");
                                        MA.cardselect_savedeckcard(card3, card3l);
                                        Script.frm.LogUpdateFunction("切换探索卡牌完成");
                                        changecard = 3;
                                    }
                                }
                                else
                                {
                                    Script.frm.LogUpdateFunction("进入配卡菜单");
                                    MA.roundtable_edit();
                                    Script.frm.LogUpdateFunction("假装在选卡");
                                    Thread.Sleep(3000);
                                    MA.cardselect_savedeckcard(deck);
                                    Script.frm.LogUpdateFunction("自动选卡完成");
                                    changecard = 5;
                                }
                            }

                            
                            Script.frm.LogUpdateFunction("切换探索卡牌完成");
                            Thread.Sleep(3000);
                            Script.frm.LogUpdateFunction("日怪");
                            MA.exploration_explore_fairy();
                            Script.frm.LogUpdateFunction("日怪完成延时等待");
                            Thread t = new Thread(Script.SendCnt);
                            t.Start();
                            Thread.Sleep(20000);
                        }
                    }
                    step--;
                }
            }
            catch (Exception ex)
            {
                if (exception == 1)
                    throw;
            }
        }

        public static void sell_card()
        {
            Random rd = new Random();

            if (sell == null)
                return;

            MA.card_exchange();
            Script.frm.LogUpdateFunction("进入卖卡界面");

            redo:

            string sid = null;
            int count = 0;
            string [] sz = sell.Split(',');

            foreach (MA.MA_Card card in MA.cardlst)
            {
                //Script.frm.LogUpdateFunction("卡牌id:" + card.master_card_id + "是否在出售列表?");
                for (int i = 0; i < sz.Length; i++)
                {
                    if (card.holography == 1)
                    {
                        Script.frm.LogUpdateFunction("卧槽!差点卖掉闪卡~" + CardInfo.getCardname(card.master_card_id));
                        break;
                    }
                    if (card.master_card_id.ToString() == sz[i])
                    {
                        if (sid == null)
                            sid = card.serial_id.ToString();
                        else
                            sid += "," + card.serial_id.ToString();
                        Script.frm.LogUpdateFunction("找到出售卡牌:" + CardInfo.getCardname(card.master_card_id));
                        if (++count == 30)
                            break;
                    }
                }
                if (count == 30)
                    break;
            }

            Script.frm.LogUpdateFunction("假装在选择需要卖的卡");
            Thread.Sleep((int)(4000 + count * 500));

            if (sid != null)
            {
                Script.frm.LogUpdateFunction("出售卡牌");
                MA.trunk_sell(sid);
                Script.frm.LogUpdateFunction("出售卡牌完成,总计:" + count);
                if (count == 30)
                {
                    goto redo;
                }
            }
            else
            {
                Script.frm.LogUpdateFunction("没有满足出售列表的卡");
            }
        }

        public static void Do()
        {
            int login_try;
            int is_login;
            int time_out_try;
            int interrupt = 0;

            changecard = -1;
            tea_ap = 0;
            tea_bc = 0;
            

            Random rd = new Random();

            while (true)
            {
                is_login = 1;
                login_try = 3;
                time_out_try = 3;

            retry:
                int next_sell = 0;
            redo:
                try
                {//Script.hkgbs

                    if (is_login == 0)
                    {
                        MA.login(login_id, login_password, interrupt);
                        Script.frm.LogUpdateFunction("登陆成功");
                        Thread.Sleep(5000);
                        is_login = 1;
                        login_try = 3;
                    }

                    //获取MP等信息
                    MA.mainmenu();
                    Script.frm.LogUpdateFunction("获取基本信息完成");
                    Thread.Sleep(4000);

                    //重置尝试次数
                    time_out_try = 3;

                    if (tea_ap != 0)
                    {
                        tea_ap = 0;
                        Script.frm.LogUpdateFunction("手动磕绿!");
                        MA.itemuse(1);
                        Thread.Sleep(2000);
                    }
                    if (tea_bc != 0)
                    {
                        tea_bc = 0;
                        Script.frm.LogUpdateFunction("手动磕红!");
                        MA.itemuse(2);
                        Thread.Sleep(2000);
                    }

                    //出售卡牌
                    if (usesell == 1 && sell != null && MA.cardlst.Count > useselln)
                    {
                        Script.frm.LogUpdateFunction("满足条件准备出售");
                        if (next_sell == 0)
                        {
                            int n = MA.cardlst.Count;
                            sell_card();
                            if (MA.cardlst.Count == n)
                            {
                                Script.frm.LogUpdateFunction("尼玛!没一张卡要卖!");
                                next_sell = 20;
                            }
                            Thread.Sleep(2000);
                            MA.mainmenu();
                            Script.frm.LogUpdateFunction("返回主城");
                            Thread.Sleep(3000);
                        }
                        else
                        {
                            Script.frm.LogUpdateFunction("轮询:" + next_sell.ToString() + "次后继续卖卡");
                            next_sell--;
                        }
                    }

                    //AutoExplore(1, 1);


                    //领取礼物盒
                    if (MA.rewards == 1 && useautorewards == 1)
                    {
                        do
                        {
                            if (MA.friendship_point > 2000)
                            {
                                if (MA.cardlst.Count + 10 >= MA.max_card_num)
                                {
                                    Script.frm.LogUpdateFunction("[自动领取礼物][基点扭蛋][卡牌槽小于10放弃]");
                                    break;
                                }
                                Script.frm.LogUpdateFunction("[自动领取礼物][基点扭蛋]");
                                MA.gacha_buy(1, 1, 1);
                                Script.frm.LogUpdateFunction("[自动领取礼物][消耗基点][完成扭蛋]");
                                Script.frm.LogUpdateFunction("假装在扭蛋");
                                Thread.Sleep(6000);
                                goto retry;
                            }

                            if (MA.cardlst.Count >= MA.max_card_num)
                            {
                                Script.frm.LogUpdateFunction("[自动领取礼物][卡牌已满]");
                                break;
                            }

                            if (MA.friendship_point > 2000)
                            {
                                Script.frm.LogUpdateFunction("[自动领取礼物][基点大于2000放弃领取]");
                                break;
                            }
                            
                            MA.getrewardbox();
                            Thread.Sleep(3000);
                            goto retry;
                        }
                        while (false);
                    }

                    if (MA.bc >= 0 && yinzi_use_tea_bc == 1 && MA.tea_bc_count > 0 && MA.bc < MA_Client.noname_bc)
                    {
                        Script.frm.LogUpdateFunction("点名自动嗑红!");
                        MA.itemuse(2);
                        MA.bc = MA.bcmax;
                        Thread.Sleep(2000);
                    }

                    //获取妖精列表
                    MA.menu_fairyselect();
                    Script.frm.LogUpdateFunction("获取妖精列表完成");
                    Thread.Sleep(4000);

                    //领取卡片
                    if (usegetcard == 1 && MA.remaining_rewards > 0)
                    {
                        TimeSpan sps = new TimeSpan();
                        sps = DateTime.Now - time_GetCard;
                        //每30分钟领取一次
                        if (sps.TotalSeconds > 60 * 30)
                        {
                            time_GetCard = DateTime.Now;
                            MA.menu_fairyrewards();
                            Script.frm.LogUpdateFunction("假装在领取卡片");
                            Thread.Sleep((int)(2000 + MA.remaining_rewards * 200));
                            if (MA.cardlst.Count >= MA.max_card_num)
                            {
                                Script.frm.LogUpdateFunction("卡片槽已满!不能继续战斗!");
                                goto retry;
                            }
                        }
                    }

                    if (MA.bc >= 2)
                    {
                        int ihave = 0;
                        //先检查自己有没有
                        for (int i = 0; i < MA.fairylst.Count; i++)
                        {
                            if (MA.fairylst[i].user_id == MA.user_id && MA.fairylst[i].fairy_put_down == 1)
                            {
                                ihave = 1;
                                break;
                            }
                        }


                        //反正数据量不大暂时就先不删。233
                        for (int i = 0; i < MA.fairylst.Count; i++)
                        {
                            if (MA.fairylst[i].fairy_put_down == 1)
                            {
                                //战斗中
                                if (MA.fairylst[i].user_id == MA.user_id && MA.fairylst[i].fucked == 0)
                                {
                                    //自己的怪
                                    if (!isWakeUp(MA.fairylst[i].fairy_name))
                                    {
                                        //自己的怪 在探索时日过一次
                                        if (yinzi == 0)
                                            MA.fairylst[i].fucked = 1;
                                        else
                                            MA.fairylst[i].touched = 1;
                                    }
                                    else
                                    {
                                        //觉醒
                                    }
                                }
                                if (!isWakeUp(MA.fairylst[i].fairy_name))
                                {
                                    //不是觉醒
                                    if ((MA.bc > force_pg_bc && force_pg == 1) || (MA.fairylst[i].fucked == 0 && MA.fairylst[i].touched == 0))
                                    {
                                        //满足强制日怪 或者 没舔过&没日过
                                        if (MA.bc > force_pg_bc && force_pg == 1 && yinzi == 0)
                                        {
                                            //强制日怪
                                            if (ihave == 1 && MA.fairylst[i].user_id != MA.user_id)
                                            {
                                                continue;
                                            }
                                            MA.exploration_fairy_floor(MA.fairylst[i].fairy_serial_id, MA.fairylst[i].user_id);
                                            //MA.bc

                                            if (useautocard == 0)
                                            {
                                                if (changecard != 3)
                                                {
                                                    Script.frm.LogUpdateFunction("BC=" + MA.bc + "切换探索卡组");
                                                    Script.frm.LogUpdateFunction("进入配卡菜单");
                                                    MA.roundtable_edit();
                                                    Script.frm.LogUpdateFunction("假装在选卡");
                                                    Thread.Sleep(3000);
                                                    MA.cardselect_savedeckcard(card3, card3l);
                                                    changecard = 3;
                                                }
                                            }
                                            else
                                            {
                                                MA_Client.CardDeck deck = MA_Client.SelectCard(MA.bc, 8, MA.fairy_floor_hp, MA.fairy_floor_hp_max, MA.fairy_floor_lv);
                                                if (deck == null)
                                                {
                                                    if (changecard != 3)
                                                    {
                                                        Script.frm.LogUpdateFunction("BC=" + MA.bc + "BC满足强制日怪,自动选卡失败切换探索卡组");
                                                        Script.frm.LogUpdateFunction("进入配卡菜单");
                                                        MA.roundtable_edit();
                                                        Script.frm.LogUpdateFunction("假装在选卡");
                                                        Thread.Sleep(3000);
                                                        MA.cardselect_savedeckcard(card3, card3l);
                                                        changecard = 3;
                                                    }
                                                }
                                                else
                                                {
                                                    //if (changecard != 5)
                                                    //{
                                                        Script.frm.LogUpdateFunction("进入配卡菜单");
                                                        MA.roundtable_edit();
                                                        Script.frm.LogUpdateFunction("假装在选卡");
                                                        Thread.Sleep(2000);
                                                        MA.cardselect_savedeckcard(deck);
                                                        Script.frm.LogUpdateFunction("BC=" + MA.bc + "BC满足强制日怪,自动选卡完成");
                                                        changecard = 5;
                                                    //}
                                                }
                                            }

                                            Script.frm.LogUpdateFunction("切换探索卡组完成");
                                            Thread.Sleep(2000);
                                            Script.frm.LogUpdateFunction("日怪");
                                            MA.exploration_explore_fairy(MA.fairylst[i].fairy_serial_id, MA.fairylst[i].user_id);
                                            MA.fairylst[i].fucked++;
                                            Script.frm.LogUpdateFunction("日怪完成延时等待");
                                            Thread t = new Thread(Script.SendCnt);
                                            t.Start();
                                            Thread.Sleep(20000);
                                        }
                                        else if (MA.fairylst[i].try_time < 2 && MA.fairylst[i].touched == 0)
                                        {
                                            //舔怪
                                            if (changecard != 1)
                                            {
                                                Script.frm.LogUpdateFunction("进入配卡菜单");
                                                MA.roundtable_edit();
                                                Script.frm.LogUpdateFunction("假装在选卡");
                                                Thread.Sleep(3000);
                                                Script.frm.LogUpdateFunction("切换舔怪卡组");
                                                MA.cardselect_savedeckcard(card1, card1l);
                                                Script.frm.LogUpdateFunction("切换舔怪卡组完成");
                                                changecard = 1;
                                                Thread.Sleep(2000);
                                            }
                                            Script.frm.LogUpdateFunction("舔怪");
                                            if (MA.fairylst[i].try_time++ == 1)
                                            {
                                                Script.frm.LogUpdateFunction("尝试第2次后不再继续");
                                            }
                                            MA.exploration_explore_fairy(MA.fairylst[i].fairy_serial_id, MA.fairylst[i].user_id);
                                            MA.fairylst[i].touched = 1;
                                            Script.frm.LogUpdateFunction("舔怪完成延时等待");
                                            Thread t = new Thread(Script.SendCnt);
                                            t.Start();
                                            Thread.Sleep(20000);
                                        }
                                    }
                                }
                                else
                                {
                                    //觉醒
                                    int flag = 0;
                                    if (jx_time_begin < jx_time_end)
                                    {
                                        if (jx_time_begin <= DateTime.Now.Hour && jx_time_end > DateTime.Now.Hour)
                                            flag = 1;
                                    }
                                    else if (jx_time_begin > jx_time_end)
                                    {
                                        if (jx_time_begin <= DateTime.Now.Hour || jx_time_end > DateTime.Now.Hour)
                                            flag = 1;
                                    }
                                    else
                                    {//IFTIME S=0 E=0
                                        flag = 0;
                                    }

                                    if (yinzi == 0 && flag == 1 && MA.bc > jx_bc && (MA.fairylst[i].fairy_time_limit < 1800 - jx_wait * 60))
                                    {
                                        //到日觉醒时间 bc满足 觉醒存活时间满足
                                        if (MA.bc > force_pg_bc && force_pg == 1)
                                        {
                                            if (ihave == 1 && MA.fairylst[i].user_id != MA.user_id)
                                            {
                                                continue;
                                            }
                                            Script.frm.LogUpdateFunction("BC=" + MA.bc + "满足强制日怪,切换觉醒卡组");
                                        }
                                        else
                                        {
                                            Script.frm.LogUpdateFunction("切换觉醒卡组");
                                        }
                                        if (changecard != 2)
                                        {
                                            Script.frm.LogUpdateFunction("进入配卡菜单");
                                            MA.roundtable_edit();
                                            Script.frm.LogUpdateFunction("假装在选卡");
                                            Thread.Sleep(3000);
                                            MA.cardselect_savedeckcard(card2, card2l);
                                            Script.frm.LogUpdateFunction("切换觉醒卡组完成");
                                            changecard = 2;
                                            Thread.Sleep(2000);
                                        }
                                        Script.frm.LogUpdateFunction("日怪");
                                        MA.exploration_explore_fairy(MA.fairylst[i].fairy_serial_id, MA.fairylst[i].user_id);
                                        Script.frm.LogUpdateFunction("日怪完成延时等待");
                                        MA.fairylst[i].fucked++;
                                        Thread t = new Thread(Script.SendCnt);
                                        t.Start();
                                        Thread.Sleep(20000);
                                    }
                                    else if (MA.fairylst[i].fucked == 0 && MA.fairylst[i].touched == 0 && MA.fairylst[i].try_time < 2)
                                    {
                                        //未舔过
                                        if (changecard != 1)
                                        {
                                            Script.frm.LogUpdateFunction("进入配卡菜单");
                                            MA.roundtable_edit();
                                            Script.frm.LogUpdateFunction("假装在选卡");
                                            Thread.Sleep(3000);
                                            Script.frm.LogUpdateFunction("切换舔怪卡组");
                                            MA.cardselect_savedeckcard(card1, card1l);
                                            Script.frm.LogUpdateFunction("切换舔怪卡组完成");
                                            changecard = 1;
                                            Thread.Sleep(2000);
                                        }
                                        Script.frm.LogUpdateFunction("舔怪");
                                        if (MA.fairylst[i].try_time++ == 1)
                                        {
                                            Script.frm.LogUpdateFunction("尝试第2次后不再继续");
                                        }
                                        MA.exploration_explore_fairy(MA.fairylst[i].fairy_serial_id, MA.fairylst[i].user_id);
                                        MA.fairylst[i].touched = 1;
                                        Script.frm.LogUpdateFunction("舔怪完成延时等待");
                                        Thread t = new Thread(Script.SendCnt);
                                        t.Start();
                                        Thread.Sleep(20000);
                                    }
                                }
                            }
                        }//end for
                        if (yinzi == 0 && changecard != 2)
                        {
                            Script.frm.LogUpdateFunction("进入配卡菜单");
                            MA.roundtable_edit();
                            Script.frm.LogUpdateFunction("假装在选卡");
                            Thread.Sleep(3000);
                            Script.frm.LogUpdateFunction("切换觉醒卡组保护因子");
                            MA.cardselect_savedeckcard(card2, card2l);
                            Script.frm.LogUpdateFunction("切换觉醒卡组完成");
                            changecard = 2;
                            Thread.Sleep(2000);
                        }
                    }
                    else
                    {
                        Script.frm.LogUpdateFunction("BC<2 不进行判断,等待");
                    }


                    if (MA.bc >= noname_bc && yinzi == 1)
                    {
                        //因子战
                        if (changecard != 4)
                        {
                            Script.frm.LogUpdateFunction("准备刷无名亚瑟");
                            Script.frm.LogUpdateFunction("进入配卡菜单");
                            MA.roundtable_edit();
                            Script.frm.LogUpdateFunction("假装在选卡");
                            Thread.Sleep(3000);
                            MA.cardselect_savedeckcard(card4, card4l);
                            changecard = 4;
                            Thread.Sleep(2000);
                        }
                        MA.noname0048();
                    }

                    //满足探索条件
                    Script.frm.LogUpdateFunction("检查探索条件");
                    if (useexplore == 1 && ((yinzi == 1 && MA.ap > noname_ap) || (MA.ap > explore_ap && MA.bc > explore_bc)))
                    {
                        Script.frm.LogUpdateFunction("满足条件准备探索");
                        AutoExplore(explore_step, 1);
                        next_sell = 0;
                    }

                    if (force_explore == 1 && MA.ap + 1 >= MA.apmax)
                    {
                        string area_id = null;
                        string floor_id = null;
                        string area_name = null;
                        int cost; 

                        Script.frm.LogUpdateFunction("AP=" + MA.ap + " 强制探索一步!");

                        MA.get_event_area_id_floor_id(force_explore_area, out area_id, out floor_id, out area_name, out cost);
                        Thread.Sleep(3000);
                        if (area_id == null || floor_id == null || area_name == null)
                            Script.frm.LogUpdateFunction("选取探索区域失败");
                        else
                        {
                            Script.frm.LogUpdateFunction("开始探索秘境:" + area_name + ",区域" + floor_id);
                            MA.exploration_explore(area_id, floor_id);
                        }
                    }

                    TimeSpan sp = new TimeSpan();
                    sp = DateTime.Now - time_SendAPBC;
                    //每3分钟更新数据
                    if (sp.TotalSeconds > 60 * 3)
                    {
                        time_SendAPBC = DateTime.Now;
                        Thread t = new Thread(new ParameterizedThreadStart(Script.SendAPBC));
                        string apbc = ((float)MA.ap * 100 / MA.apmax).ToString() + "," + ((float)MA.bc * 100 / MA.bcmax).ToString();
                        t.Start(apbc);
                    }
                    Script.frm.LogUpdateFunction("成功更新用户数据");
                    Thread.Sleep(loop_time * 1000 * (int)(0.1 * rd.Next(8, 12)));
                    goto redo;
                }
                catch (Exception ex)
                {
                    Script.frm.LogUpdateFunction(ex.Message);
                    if (ex.Message.IndexOf("Code:9000") != -1)
                    {
                        //尝试三次错误
                        login_try--;
                        is_login = 0;
                        if (login_try == 0)
                        {
                            login_try = 3;
                            Script.frm.LogUpdateFunction("连续3次登陆失败,休息60秒重试");
                            Thread.Sleep(60 * 1000 * (int)(0.1 * rd.Next(8, 12)));
                        }
                        goto retry;
                    }
                    else if (ex.Message.IndexOf("Code:8000") != -1)
                    {
                        //现在不能战斗
                        Script.frm.LogUpdateFunction("延迟等待约20秒");
                        Thread.Sleep(20 * 1000 * (int)(0.1 * rd.Next(8, 12)));
                        goto retry;
                    }
                    else if (ex.Message.IndexOf("Code:1000") != -1)
                    {
                        Script.frm.LogUpdateFunction("密码错误,放弃治疗");
                        return;
                    }
                    else if (ex.Message.IndexOf("维护") != -1 || ex.Message.IndexOf("game") != -1)
                    {
                        interrupt = 0;
                        time_out_try--;
                        if (time_out_try == 0)
                        {
                            time_out_try = 3;
                            is_login = 0;
                            Script.frm.LogUpdateFunction("连续3次超时,可能是网络问题,120秒后重试");
                            Thread.Sleep(120 * 1000 * (int)(0.1 * rd.Next(8, 12)));
                        }
                        Thread.Sleep(30 * 1000 * (int)(0.1 * rd.Next(8, 12)));
                        goto retry;
                    }
                    else if(ex.Message.IndexOf("超时") != -1 || ex.Message.IndexOf("timed out") != -1)
                    {
                        interrupt = 1;
                        time_out_try--;
                        if (time_out_try == 0)
                        {
                            time_out_try = 3;
                            is_login = 0;
                            Script.frm.LogUpdateFunction("连续3次超时,可能是网络问题,60秒后重试");
                            Thread.Sleep(50 * 1000 * (int)(0.1 * rd.Next(8, 12)));
                        }
                        Thread.Sleep(15 * 1000 * (int)(0.1 * rd.Next(8, 12)));
                        goto retry;
                    }
                    Thread.Sleep(10000);
                    goto retry;
                }
            }
        }


        public static bool isWakeUp(string failry_name)
        {
            if (wake_up_key != null || wake_up_key != "")
            {
                string[] sz = wake_up_key.Split(',');
                if (sz != null)
                {
                    foreach (string str in sz)
                    {
                        if (failry_name.IndexOf(str) != -1)
                            return true;
                    }
                }
            }
            return false;
        }

        public static void Loop()
        {

        }
    }
}
