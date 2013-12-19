using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading;
using System.Windows.Forms;

namespace MAH
{
    class ReqParama
    {
        public string ParamaName;
        public string ParamaValue;
        public ReqParama(string name, string val)
        {
            ParamaName = name;
            ParamaValue = val;
        }
    }

    class MA
    {
        public static string host = "game1-CBT.ma.sdo.com:10001";
        public static int server_host = 0; //0 cn        1 tw
        public static int proxy_enable = 0;
        public static string http_proxy = null;
       
        public static XmlDocument document = new XmlDocument();

        public static string myfairy_serial_id = "";
        public static int remaining_rewards = 0;
        public static int user_id = 0;
        public static int ap = -1;
        public static int bc = -1;
        public static int apmax = -1;
        public static int bcmax = -1;
        public static int tea_ap_count = 0;
        public static int tea_bc_count = 0;
        public static string user_name = "";
        public static int town_level = -1;
        public static int gold = -1;
        public static int friendship_point = -1;
        public static int rewards = 0;
        public static int get_exp = -1;
        public static int next_exp = -1;
        public static int max_card_num = 0;

        public static int fairy_floor_lv = -1;
        public static int fairy_floor_hp = -1;
        public static int fairy_floor_hp_max = -1;

        public static int yinzi_win = 0;
        public static int yinzi_lose = 0;

        public static List<string> sb250 = new List<string>();

        public class MA_Card
        {
            public int serial_id;
            public int master_card_id;
            public int lv;
            public int power;
            public int hp;
            public int cp;
            public string name; //服务器不提供update时才会有
            public int holography;

            public MA_Card Copy()
            {
                MA_Card card = new MA_Card();
                card.serial_id = serial_id;
                card.master_card_id = master_card_id;
                card.lv = lv;
                card.power = power;
                card.hp = hp;
                card.cp = cp;
                card.name = name;
                return card;
            }
        }

        public class MA_fairy_event
        {
            public int user_id;
            public string user_name;
            public int fairy_serial_id;
            public string fairy_name;
            public int fairy_lv;
            public int fairy_time_limit;
            public int fairy_put_down;   //  1扑街 2战斗中
            public int fucked;
            public int try_time;
            public int touched;
            public int start_time;
            public int touch;//用于更新时判断是否还在列表中
        }

        public static List<MA_Card> cardlst = new List<MA_Card>();
        public static List<MA_fairy_event> fairylst = new List<MA_fairy_event>();

        //package parmas by aes
        public static string ma_prepare_request(List<ReqParama> reqparams = null)
        {
            string postdata = "";

            AES.InitKey();

            foreach (ReqParama reqparma in reqparams)
            {

                if (postdata != "")
                    postdata += "&";
                
                postdata += reqparma.ParamaName + "=" + System.Uri.EscapeDataString(AES.Encrypt(reqparma.ParamaValue)) +"%0A";
            }
            return postdata;
        }

        public static string ma_prepare_login_request(List<ReqParama> reqparams = null)
        {
            string postdata = "";

            AES.InitKey();

            foreach (ReqParama reqparma in reqparams)
            {

                if (postdata != "")
                    postdata += "&";

                string aes = AES.Encrypt(reqparma.ParamaValue);
                string rsa = RSA.RSAEncrypt(aes);

                postdata += reqparma.ParamaName + "=" + System.Uri.EscapeDataString(rsa) + "%0A";
            }

            return postdata;
        }

        //post request to ma webserver and load XML
        public static void ma_request(string uri, string postdata)
        {

            if (MA.host != "game.ma.mobimon.com.tw:10001")
            { 
                string key = "K=" + System.Uri.EscapeDataString(RSA.RSAEncrypt(Convert.ToBase64String(AES.rDel.Key))) + "%0A";
                if (postdata.Length > 0)
                    postdata = key + "&" + postdata;
                else
                postdata = key;
            }

            byte[] undecryptbyte = HTTP.HttpPost1("http://" + host + uri, postdata, Script.frm.cookie, Script.frm.cookie2, 1);

            try
            {
                if (undecryptbyte == null)
                    return;

                string decryptstr = AES.Decrypt(undecryptbyte);

                if (decryptstr.IndexOf("<?xml") != 0)
                    throw new Exception("Not XML File");

                document.LoadXml(decryptstr);
                XmlNodeList error = document.SelectNodes("/response/header/error");
                if (error.Count != 0)
                {
                    if (error[0].SelectNodes("./code").Count != 0)
                    {
                        //出售
                        if (error[0].SelectNodes("./code")[0].InnerText != "0" && error[0].SelectNodes("./code")[0].InnerText != "1010")
                        {
                            if (error[0].SelectNodes("./message").Count != 0)
                            {
                                throw new Exception("Message:" + error[0].SelectNodes("./message")[0].InnerText + " Code:" + error[0].SelectNodes("./code")[0].InnerText);
                            }
                            throw new Exception(error[0].SelectNodes("./code")[0].InnerText);
                        }
                    }
                }
                else
                {
                    throw new Exception("Unkown Data format!");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //..
        private static void exploration_area()
        {
            ma_request("/connect/app/exploration/area?cyt=1", "");
        }

        //return a first finded area id by names list
        public static string get_area_id(string [] area_names)
        {
            exploration_area();
            XmlNodeList area_info_nodes = document.SelectNodes("/response/body/exploration_area/area_info_list/area_info");

            if (area_names != null)
            {
                foreach (string area_name in area_names)
                {
                    foreach (XmlNode node in area_info_nodes)
                    {
                        string text = node.SelectNodes("./name")[0].InnerText;
                        if (text == area_name)
                            return node.SelectNodes("./id")[0].InnerText;
                    }
                }
            }

            if (area_info_nodes[0].SelectNodes("./id").Count != 0)
                return area_info_nodes[0].SelectNodes("./id")[0].InnerText;
            return null;
        }

        public static void get_event_area_id_floor_id(string keyword, out string area_id, out string floor_id, out string area_name, out int cost)
        {
            exploration_area();
            XmlNodeList area_info_nodes = document.SelectNodes("/response/body/exploration_area/area_info_list/area_info");

            Thread.Sleep(3000);

            //优先查找匹配关键字
            if (keyword != null && keyword != "")
            {
                if (keyword.IndexOf(',') == -1)
                {
                    keyword += ',';
                }
                string[] sz = keyword.Split(',');
                if (sz != null)
                {
                    foreach (string str in sz)
                    {
                        foreach (XmlNode node in area_info_nodes)
                        {
                            if (node["name"].InnerText.IndexOf(str) != -1)
                            {
                                //找到要优先探索的区域
                                area_id = node["id"].InnerText;
                                area_name = node["name"].InnerText;
                                exploration_floor(area_id);
                                XmlNodeList floor_info_nodes1 = document.SelectNodes("/response/body/exploration_floor/floor_info_list/floor_info");
                                if (floor_info_nodes1.Count != 0)
                                {
                                    floor_id = null;
                                    cost = 0;
                                    foreach (XmlNode floor_node in floor_info_nodes1)
                                    {
                                        if (int.Parse(floor_node["progress"].InnerText) < 100)
                                        {
                                            floor_id = floor_node["id"].InnerText;
                                            cost = int.Parse(floor_node["cost"].InnerText);
                                            return;
                                        }
                                        if (int.Parse(floor_node["progress"].InnerText) == 100)
                                        {
                                            floor_id = floor_node["id"].InnerText;
                                            cost = int.Parse(floor_node["cost"].InnerText);
                                        }
                                    }
                                    if (MA_Client.force_explore_area_next == 0 && floor_info_nodes1.Count>0)
                                    {
                                        floor_id = floor_info_nodes1[0]["progress"].InnerText;
                                        cost = int.Parse(floor_info_nodes1[0]["cost"].InnerText);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            foreach (XmlNode node in area_info_nodes)
            {
                if (int.Parse(node["area_type"].InnerText) == 1)
                {
                    if (int.Parse(node["prog_area"].InnerText) == 100)
                    {
                        //此区域全部跑完
                        continue;
                    }
                }
                else
                {
                    if (int.Parse(node["prog_area"].InnerText) > 95)
                    {
                        //此区域全部跑完
                        continue;
                    }
                }

                area_id = node["id"].InnerText;
                area_name = node["name"].InnerText;

                exploration_floor(area_id);
                XmlNodeList floor_info_nodes = document.SelectNodes("/response/body/exploration_floor/floor_info_list/floor_info");
                foreach (XmlNode floor_node in floor_info_nodes)
                {
                    if (int.Parse(floor_node["progress"].InnerText) < 100)
                    {
                        floor_id = floor_node["id"].InnerText;
                        cost = int.Parse(floor_node["cost"].InnerText);
                        return;
                    }
                }
            }


            area_id = area_info_nodes[0]["id"].InnerText;
            area_name = area_info_nodes[0]["name"].InnerText;
            exploration_floor(area_id);
            XmlNodeList floor_info_nodes2 = document.SelectNodes("/response/body/exploration_floor/floor_info_list/floor_info");
            if (floor_info_nodes2.Count != 0)
            {
                floor_id = floor_info_nodes2[0]["id"].InnerText;
                cost = int.Parse(floor_info_nodes2[0]["cost"].InnerText);
                foreach (XmlNode floor_node in floor_info_nodes2)
                {
                    if (int.Parse(floor_node["progress"].InnerText) == 100)
                    {
                        floor_id = floor_node["id"].InnerText;
                        cost = int.Parse(floor_node["cost"].InnerText);
                    }
                }
                return;
            }

            cost = 0;
            area_id = null;
            floor_id = null;
            area_name = null;
        }

        //return a area which not completed
        public static string get_area_id_first()
        {
            exploration_area();
            XmlNodeList area_info_nodes = document.SelectNodes("/response/body/exploration_area/area_info_list/area_info");
            foreach (XmlNode node in area_info_nodes)
            {
                return node.SelectNodes("./id")[0].InnerText;
            }
            return null;
        }

        //return a area which not completed
        public static string get_area_id_by_prog_area()
        {
            exploration_area();

            XmlNodeList area_info_nodes = document.SelectNodes("/response/body/exploration_area/area_info_list/area_info");
            foreach (XmlNode node in area_info_nodes)
            {
                string text = node.SelectNodes("./prog_area")[0].InnerText;
                int n = 0;
                int.TryParse(text, out n);
                if (n<100)
                {
                    return node.SelectNodes("./id")[0].InnerText;
                }
            }
            if (area_info_nodes[0].SelectNodes("./id").Count != 0)
                return area_info_nodes[0].SelectNodes("./id")[0].InnerText;
            return null;
        }

        public static void exploration_get_floor(string area_id, string floor_id, string check = "1")
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("area_id", area_id));
            reqparams.Add(new ReqParama("check", check));
            reqparams.Add(new ReqParama("floor_id", floor_id));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/exploration/get_floor?cyt=1", postdata);

            update();
        }

        //do explpre
        //0 None
        //1 卡
        //4 基友
        //8 妖精
        public static int exploration_explore(string area_id, string floor_id, string auto_build = "1")
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("area_id", area_id));
            reqparams.Add(new ReqParama("auto_build", auto_build));
            reqparams.Add(new ReqParama("floor_id", floor_id));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/exploration/explore?cyt=1", postdata);

            update();

            int ret = 0;
            XmlNodeList area_info_nodes = document.SelectNodes("/response/body/explore/fairy");
            if (area_info_nodes.Count != 0)
            {
                ret += 8;
                myfairy_serial_id = area_info_nodes[0].SelectNodes("./serial_id")[0].InnerText;
            }

            return ret;
        }

        //kill fairy
        public static void exploration_explore_fairy()
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("serial_id", myfairy_serial_id));
            reqparams.Add(new ReqParama("user_id", user_id.ToString()));

            string postdata = ma_prepare_request(reqparams);

            try
            {
                ma_request("/connect/app/exploration/fairybattle?cyt=1", postdata);
                update();
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("Padding") != -1 || ex.Message.IndexOf("填充无效") != -1)
                    return;
                else
                    throw;
            }     
        }



        public static void exploration_explore_fairy(int serial_id, int user_id)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("serial_id", serial_id.ToString()));
            reqparams.Add(new ReqParama("user_id", user_id.ToString()));

            string postdata = ma_prepare_request(reqparams);

            try
            {
                ma_request("/connect/app/exploration/fairybattle?cyt=1", postdata);
                update();
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("Padding") != -1 || ex.Message.IndexOf("填充无效") != -1)
                    return;
                else
                    throw;
            }
        }

        private static void check_inspection()
        {
            Script.frm.cookie = null;
            Script.frm.cookie2 = null;
            ma_request("/connect/app/check_inspection?cyt=1", "");
        }

        //interupt 1 被其他客户端踢掉
        //interupt 0 维护或者重登陆
        public static void login(string login_id, string password, int interrupt = 0)
        {
            //if (HTTP.ua.IndexOf("Million") == -1)
             //   throw new Exception("UA Error");


            //check_inspection();

            //if (interrupt == 0)
            //    notification_post_devicetoken(login_id, password);

            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("login_id", login_id));
            reqparams.Add(new ReqParama("password", password));

            string postdata;
            if (MA.host == "game.ma.mobimon.com.tw:10001")
                postdata = ma_prepare_request(reqparams);
            else
                postdata = ma_prepare_login_request(reqparams);
            
            ma_request("/connect/app/login?cyt=1", postdata);

            update();
        }

        //
        public static void notification_post_devicetoken(string login_id, string password)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("S", "nosessionid"));
            reqparams.Add(new ReqParama("login_id", login_id));
            reqparams.Add(new ReqParama("password", password));
            reqparams.Add(new ReqParama("app", "and"));

            string token;
            Random rd = new Random();

            byte[] bytes = Encoding.ASCII.GetBytes(HTTP.EncryptMD5(HTTP.code+login_id) + rd.Next(1000000).ToString());
            token = Convert.ToBase64String(bytes).Replace("\n","");
            reqparams.Add(new ReqParama("token", token));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/notification/post_devicetoken?cyt=1", postdata);
        }

        //get card
        public static void menu_fairyrewards()
        {
            ma_request("/connect/app/menu/fairyrewards?cyt=1", "");
            update();
        }

        
        public static void gacha_buy(int auto_build, int bulk, int product_id)
        {
            ma_request("/connect/app/gacha/select/getcontents?cyt=1", "");
            Script.frm.LogUpdateFunction("假装进入扭蛋界面");
            Thread.Sleep(2000);

            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("auto_build", auto_build.ToString()));
            reqparams.Add(new ReqParama("bulk", bulk.ToString()));
            reqparams.Add(new ReqParama("product_id", product_id.ToString()));
            
            string postdata = ma_prepare_request(reqparams);

            ma_request("/connect/app/gacha/buy?cyt=1", postdata);
            update();
        }

        //get reward box
        public static void getrewardbox()
        {
            ma_request("/connect/app/menu/rewardbox?cyt=1", "");
            XmlNodeList rewardbox_nodes = document.SelectNodes("/response/body/rewardbox_list/rewardbox");
            string ids = null;
            string content = null;

            if(rewardbox_nodes != null)
            {
                int count = 0;
                foreach (XmlNode rewardbox_node in rewardbox_nodes)
                {
                    if (ids == null)
                    {
                        ids = rewardbox_node["id"].InnerText;
                        content = rewardbox_node["content"].InnerText;
                    }
                    else
                    {
                        ids += "," + rewardbox_node["id"].InnerText;
                        content += "\r\n" + rewardbox_node["content"].InnerText;
                    }
                    if (++count == 20)
                        break;
                }
            }

            Script.frm.LogUpdateFunction("奖励列表:\r\n" + content);

            Thread.Sleep(2000);

            if (ids != null)
            {
                List<ReqParama> reqparams = new List<ReqParama>();
                reqparams.Add(new ReqParama("notice_id", ids));
                string postdata = ma_prepare_request(reqparams);
                MA.rewards = 0;
                //return err 8000
                ma_request("/connect/app/menu/get_rewards?cyt=1", postdata);
            }

        }

        //reward box
        public static void menu_rewardbox()
        {
            ma_request("/connect/app/menu/rewardbox?cyt=1", "");
        }

        //fairyselect
        public static void menu_fairyselect()
        {
            ma_request("/connect/app/menu/fairyselect?cyt=1", "");

            XmlNodeList fairy_event_nodes = document.SelectNodes("/response/body/fairy_select/fairy_event");

            if (fairy_event_nodes.Count > 0)
            {
                for (int i = 0; i < fairylst.Count; i++)
                {
                    //用来确认列表中是否还存在
                    fairylst[i].touch = 0;
                }


                foreach (XmlNode node in fairy_event_nodes)
                {
                    int serial_id = int.Parse(node["fairy"]["serial_id"].InnerText);
                    int flag = 0;
                    for (int i = 0; i < fairylst.Count; i++)
                    {
                        if (fairylst[i].fairy_serial_id == serial_id)
                        {
                            //更新
                            fairylst[i].fairy_time_limit = int.Parse(node["fairy"]["time_limit"].InnerText);
                            fairylst[i].fairy_put_down = int.Parse(node["put_down"].InnerText);
                            fairylst[i].touch = 1;
                            flag = 1;
                            break;
                        }
                    }

                    if (flag == 0)
                    {
                        MA_fairy_event fairy_event = new MA_fairy_event();
                        fairy_event.user_id = int.Parse(node["user"]["id"].InnerText);
                        fairy_event.user_name = node["user"]["name"].InnerText;
                        fairy_event.fairy_lv = int.Parse(node["fairy"]["lv"].InnerText);
                        fairy_event.fairy_serial_id = int.Parse(node["fairy"]["serial_id"].InnerText);
                        fairy_event.fairy_name = node["fairy"]["name"].InnerText;
                        fairy_event.fairy_time_limit = int.Parse(node["fairy"]["time_limit"].InnerText);
                        fairy_event.fairy_put_down = int.Parse(node["put_down"].InnerText);
                        fairy_event.start_time = int.Parse(node["start_time"].InnerText);
                        fairy_event.touched = 0;
                        fairy_event.fucked = 0;
                        fairy_event.try_time = 0;
                        fairy_event.touch = 1;
                        fairylst.Add(fairy_event);
                    }
                }

                for (int i = 0; i < fairylst.Count; i++)
                {
                    if (fairylst[i].touch == 0)
                    {
                        fairylst.RemoveAt(i);
                        i--;
                    }
                }
            }

            update();
        }

        public static void mainmenu()
        {
            ma_request("/connect/app/mainmenu?cyt=1", "");
            update();
        }

        //编辑卡组
        public static void roundtable_edit()
        {
            ma_request("/connect/app/roundtable/edit?cyt=1", "move=HJQrxs%2FKaF3hyO81WS2jdA%3D%3D%0A");
        }

        //use yao
        //1 ap
        //2 bc
        public static void itemuse(int item_id)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("item_id", item_id.ToString()));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/item/use?cyt=1", postdata);
        }

        public static void exploration_floor(string area_id)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("area_id", area_id.ToString()));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/exploration/floor?cyt=1", postdata);
        }

        //get fairy info
        public static void exploration_fairy_floor(int serial_id, int user_id)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("check", "1"));
            reqparams.Add(new ReqParama("serial_id", serial_id.ToString()));
            reqparams.Add(new ReqParama("user_id", user_id.ToString()));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/exploration/fairy_floor?cyt=1", postdata);

            update();
            //result response here but i not compare.
        }

        //set my card deck . these param from winpacp:) I not anymore time to fix it.
        public static void cardselect_savedeckcard(string b64data)
        {
            ma_request("/connect/app/cardselect/savedeckcard?cyt=1", b64data);
        }

        //不选择因子战
        public static void battle_battle(string uid)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("user_id", uid));

            string postdata = "lake_id=NzgOGTK08BvkZN5q8XvG6Q%3D%3D%0A&parts_id=NzgOGTK08BvkZN5q8XvG6Q%3D%3D%0A&"
            + ma_prepare_request(reqparams);

            ma_request("/connect/app/battle/battle?cyt=1", postdata);

        }

        //没用，限制了
        public static void  hello()
        {

            List<ReqParama> reqparams = new List<ReqParama>();

            for (int i = 32400; i < 32600; i++)
            {
                reqparams.Add(new ReqParama("user_id", i.ToString()));
                string postdata = "dialog=HJQrxs%2FKaF3hyO81WS2jdA%3D%3D%0A&" + ma_prepare_request(reqparams);
                try
                {
                    ma_request("/connect/app/friend/like_user?cyt=1", postdata);
                }
                catch (Exception)
                {
                }
            }

        }


        //搜索无名亚瑟进行因子战
        public static void noname0048()
        {
            List<ReqParama> reqparams = new List<ReqParama>();

            if (MA.host == "game.ma.mobimon.com.tw:10001")
            {
                reqparams.Add(new ReqParama("name", "無名亞"));
            }
            else
            {
                reqparams.Add(new ReqParama("name", "无名亚"));
            }

            ma_request("/connect/app/menu/player_search?cyt=1", ma_prepare_request(reqparams));
            Thread.Sleep(2000);

            int cnt = 0;

            XmlNodeList user_nodes = document.SelectNodes("/response/body/player_search/user_list/user");
            foreach (XmlNode node in user_nodes)
            {
                if (int.Parse(node["cost"].InnerText) > MA_Client.noname_cost)
                {
                    continue;
                }
                if (MA.bc < MA_Client.noname_bc)
                {
                    Script.frm.LogUpdateFunction("BC不足,已经虚脱");
                    return;
                }

                string uid = node["id"].InnerText;

                if (sb250.Contains(uid))
                {
                    Script.frm.LogUpdateFunction("第几次遇到这傻逼二百五了?无视!");
                    continue;
                }
                else
                {
                    Script.frm.LogUpdateFunction("诶玛又遇到个二百五!");
                    sb250.Add(uid);
                }

                try
                {
                    battle_battle(uid);
                }
                catch (Exception ex)
                {
                    Script.frm.LogUpdateFunction(ex.Message);
                    continue;
                }

                XmlNodeList battle_result_nodes = document.SelectNodes("/response/body/battle_result");
                update();

                if (battle_result_nodes.Count == 1)
                {
                    if (battle_result_nodes[0]["winner"].InnerText == "1")
                    {
                        Script.frm.LogUpdateFunction("因子战胜利");
                        yinzi_win++;
                    }
                    else
                    {
                        Script.frm.LogUpdateFunction("因子战失败");
                        yinzi_lose++;
                    }
                }

                cnt++;
                Script.frm.LogUpdateFunction("战斗完成延迟等待");
                Thread.Sleep(20000);
            }

            if (cnt == 0 && MA.host == "game1-CBT.ma.sdo.com:10001")
            {
                Script.frm.LogUpdateFunction("从我的数据库获取一个无名亚瑟!");
                string ret = HTTP.HttpGet(HTTP.url + "dm123.php?t=get&u=" + HTTP.user + "&p=" + HTTP.pass);
                if (ret != null && ret.Length > 1)
                {
                    string uid = DES.DecryptDES(ret, "11111111");

                    try
                    {
                        battle_battle(uid);
                    }
                    catch (Exception ex)
                    {
                        Script.frm.LogUpdateFunction(ex.Message);
                    }

                    sb250.Add(uid);

                    XmlNodeList battle_result_nodes = document.SelectNodes("/response/body/battle_result");

                    if (battle_result_nodes.Count == 1)
                    {
                        if (battle_result_nodes[0]["winner"].InnerText == "1")
                        {
                            Script.frm.LogUpdateFunction("因子战胜利");
                            yinzi_win++;
                        }
                        else
                        {
                            Script.frm.LogUpdateFunction("因子战失败");
                            yinzi_lose++;
                        }
                        Thread.Sleep(20000);
                    }
                }
            }
        }

        //set my card deck 
        public static void cardselect_savedeckcard(string card, string leader)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("C", card));
            reqparams.Add(new ReqParama("lr", leader));
            reqparams.Add(new ReqParama("deck_id", "1"));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/cardselect/savedeckcard?cyt=1", postdata);
        }

        public static void cardselect_savedeckcard(MA_Client.CardDeck deck)
        {
            string leader = deck.card[0].serial_id.ToString();
            string cards = null;
            int n = 0;
            foreach (MA.MA_Card card in deck.card)
            {
                if (card != null)
                {
                    if (cards == null)
                        cards = card.serial_id.ToString();
                    else
                        cards = cards + "," + card.serial_id.ToString();
                    Script.frm.LogUpdateFunction("全排配卡:" + card.name);
                    n++;
                }
            }

            for (n = 12 - n; n > 0; n--)
            {
                cards += ",empty";
            }

            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("C", cards));
            reqparams.Add(new ReqParama("lr", leader));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/cardselect/savedeckcard?cyt=1", postdata);
        }


        public static void card_exchange()
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("mode", "1"));

            string postdata = ma_prepare_request(reqparams);
            ma_request("/connect/app/card/exchange?cyt=1", postdata);

            update();
        }

        //sell card
        public static void trunk_sell(string serial_id)
        {
            List<ReqParama> reqparams = new List<ReqParama>();
            reqparams.Add(new ReqParama("serial_id", serial_id));

            string postdata = ma_prepare_request(reqparams);
            try
            {
                ma_request("/connect/app/trunk/sell?cyt=1", postdata);
            }
            catch (Exception)
            {
                //一定先认定买成功了
            }
            cardlst.Clear();
            update();
        }


        public static void update()
        {
            XmlNodeList remaining_rewards_nodes = document.SelectNodes("/response/body/fairy_select/remaining_rewards");
            if (remaining_rewards_nodes.Count == 1)
                remaining_rewards = int.Parse(remaining_rewards_nodes[0].InnerText);

            XmlNodeList ap_nodes = document.SelectNodes("/response/header/your_data/ap/current");
            if (ap_nodes.Count == 1)
                ap = int.Parse(ap_nodes[0].InnerText);

            XmlNodeList bc_nodes = document.SelectNodes("/response/header/your_data/bc/current");
            if (bc_nodes.Count == 1)
                bc = int.Parse(bc_nodes[0].InnerText);

            XmlNodeList apmax_nodes = document.SelectNodes("/response/header/your_data/ap/max");
            if (apmax_nodes.Count == 1)
                apmax = int.Parse(apmax_nodes[0].InnerText);

            XmlNodeList bcmax_nodes = document.SelectNodes("/response/header/your_data/bc/max");
            if (bcmax_nodes.Count == 1)
                bcmax = int.Parse(bcmax_nodes[0].InnerText);

            XmlNodeList itemlist_nodes = document.SelectNodes("/response/header/your_data/itemlist");
            if (itemlist_nodes != null)
            {
                foreach (XmlNode node in itemlist_nodes)
                {
                    if (node["item_id"].InnerText == "1")
                        tea_ap_count = int.Parse(node["num"].InnerText);
                    if (node["item_id"].InnerText == "2")
                        tea_bc_count = int.Parse(node["num"].InnerText);
                }
            }


            XmlNodeList user_name_nodes = document.SelectNodes("/response/header/your_data/name");
            if (user_name_nodes.Count == 1)
                user_name = user_name_nodes[0].InnerText;

            XmlNodeList town_level_nodes = document.SelectNodes("/response/header/your_data/town_level");
            if (town_level_nodes.Count == 1)
                town_level = int.Parse(town_level_nodes[0].InnerText);

            XmlNodeList glod_nodes = document.SelectNodes("/response/header/your_data/gold");
            if (glod_nodes.Count == 1)
                gold = int.Parse(glod_nodes[0].InnerText);

            XmlNodeList max_card_num_nodes = document.SelectNodes("/response/header/your_data/max_card_num");
            if (max_card_num_nodes.Count == 1)
                max_card_num = int.Parse(max_card_num_nodes[0].InnerText);

            XmlNodeList friendship_point_node = document.SelectNodes("/response/header/your_data/friendship_point");
            if (friendship_point_node.Count == 1)
                friendship_point = int.Parse(friendship_point_node[0].InnerText);

            XmlNodeList rewards_node = document.SelectNodes("/response/body/mainmenu/rewards");
            if (rewards_node.Count == 1)
                rewards = int.Parse(rewards_node[0].InnerText);


            XmlNodeList next_exp_node = document.SelectNodes("/response/body/explore/next_exp");
            if (next_exp_node.Count == 1)
                next_exp = int.Parse(next_exp_node[0].InnerText);

            XmlNodeList battle_result_node = document.SelectNodes("/response/body/battle_result/after_exp");
            if (battle_result_node.Count == 1)
                next_exp = int.Parse(battle_result_node[0].InnerText);

            //进入exploration_fairy_floor
            XmlNodeList fairy_node = document.SelectNodes("/response/body/fairy_floor/explore/fairy");
            if (fairy_node.Count == 1)
            {
                //当前floor妖精信息
                fairy_floor_lv = int.Parse(fairy_node[0]["lv"].InnerText);
                fairy_floor_hp = int.Parse(fairy_node[0]["hp"].InnerText);
                fairy_floor_hp_max = int.Parse(fairy_node[0]["hp_max"].InnerText);
            }



            XmlNodeList user_card_nodes = document.SelectNodes("/response/header/your_data/owner_card_list/user_card");
            if (user_card_nodes.Count > 0)
            {
                cardlst.Clear();
                foreach (XmlNode node in user_card_nodes)
                {
                    MA_Card card = new MA_Card();
                    card.hp = int.Parse(node["hp"].InnerText);
                    card.lv = int.Parse(node["lv"].InnerText);
                    card.master_card_id = int.Parse(node["master_card_id"].InnerText);
                    card.cp = CardInfo.getCardcost(card.master_card_id);
                    card.name = CardInfo.getCardname(card.master_card_id);
                    card.power = int.Parse(node["power"].InnerText);
                    card.serial_id = int.Parse(node["serial_id"].InnerText);
                    card.holography = int.Parse(node["holography"].InnerText);
                    cardlst.Add(card);
                }
            }

            XmlNodeList user_id_nodes = document.SelectNodes("/response/body/login/user_id");
            if (user_id_nodes.Count == 1)
                user_id = int.Parse(user_id_nodes[0].InnerText);

            //更新数据
            string[] str = new string[12];

            str[0] = "角色: " + MA.user_name;
            str[1] = "Lv: " + MA.town_level;
            str[2] = "AP: " + MA.ap + " / " + MA.apmax;
            str[3] = "BC: " + MA.bc + " / " + MA.bcmax;
            str[4] = "绿药: " + MA.tea_ap_count;
            str[5] = "红药: " + MA.tea_bc_count;
            str[6] = "Card: " + MA.cardlst.Count + " / " + MA.max_card_num;
            str[7] = "Gold: " + MA.gold;
            str[8] = "Next Exp: " + MA.next_exp;
            str[9] = "Gay Point: " + MA.friendship_point;
            str[10] = "收到礼物: " + ((rewards == 1) ? "是" : "否");
            str[11] = "本次点名统计: " + "Win:" + MA.yinzi_win + "Lose:" + MA.yinzi_lose;

            Script.frm.UListUpdateFunction(str);


            //刷新信息
            int n = 0;
            foreach (MA.MA_fairy_event f in MA.fairylst)
            {
                string [] szstr = new string[6];
                szstr[0] = f.fairy_time_limit.ToString();
                szstr[1] = f.fairy_name.PadRight(10,' ') + " Lv." + f.fairy_lv;
                szstr[2] = f.user_name;
                szstr[3] = (f.fairy_put_down == 1) ? "战斗中" : "胜利";
                szstr[4] = (f.touched == 1) ? "是" : "否";
                szstr[5] = f.fucked.ToString();
                Script.frm.FListUpdateFunction(szstr, n++);
            }
            Script.frm.FListUpdateFunction(null, -1, n);
        }
    }
}
