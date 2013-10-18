using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MAH
{
    class XML
    {
        public static XmlDocument document = new XmlDocument();

        public static void parsexml(string xmldata)
        {
            xmldata = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><response><header><error><code>0</code></error><session_id>pb9qdcjmknq2r195mlhbsp7n32</session_id><revision><card_rev>167</card_rev><boss_rev>169</boss_rev><item_rev>161</item_rev><card_category_rev>167</card_category_rev><gacha_rev>170</gacha_rev><privilege_rev>161</privilege_rev><combo_rev>170</combo_rev><eventbanner_rev>170</eventbanner_rev><resource_rev><revision>161</revision><filename>res</filename></resource_rev><resource_rev><revision>148</revision><filename>sound</filename></resource_rev><resource_rev><revision>148</revision><filename>advbg</filename></resource_rev><resource_rev><revision>148</revision><filename>cmpsheet</filename></resource_rev><resource_rev><revision>170</revision><filename>gacha</filename></resource_rev><resource_rev><revision>148</revision><filename>privilege</filename></resource_rev><resource_rev><revision>170</revision><filename>eventbanner</filename></resource_rev></revision><next_scene>6100</next_scene><lock_unlock><scenario_voice>0</scenario_voice></lock_unlock></header><body><exploration_area><area_info_list><area_info><id>90116</id><name>断魂的西夏皇宫冰窖</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>100</prog_item><area_type>1</area_type></area_info><area_info><id>90115</id><name>返老还童的灵鹫之宫</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>64</prog_item><area_type>1</area_type></area_info><area_info><id>90114</id><name>天长地久的缥缈之峰</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>66</prog_item><area_type>1</area_type></area_info><area_info><id>90113</id><name>唯我独尊的天山之巅</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>83</prog_item><area_type>1</area_type></area_info><area_info><id>90112</id><name>星罗棋布的七十二岛</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>70</prog_item><area_type>1</area_type></area_info><area_info><id>90111</id><name>错综复杂的三十六洞</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>87</prog_item><area_type>1</area_type></area_info><area_info><id>50002</id><name>灯火摇曳的漫步之园</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>80</prog_item><area_type>1</area_type></area_info><area_info><id>6</id><name>授予祝福的山</name><x>50</x><y>1</y><prog_area>74</prog_area><prog_item>72</prog_item><area_type>0</area_type></area_info><area_info><id>5</id><name>猛兽的沙丘</name><x>-33</x><y>-51</y><prog_area>100</prog_area><prog_item>91</prog_item><area_type>0</area_type></area_info><area_info><id>4</id><name>睿智的草原</name><x>-100</x><y>-51</y><prog_area>100</prog_area><prog_item>83</prog_item><area_type>0</area_type></area_info><area_info><id>3</id><name>错乱的平原</name><x>-110</x><y>-68</y><prog_area>100</prog_area><prog_item>80</prog_item><area_type>0</area_type></area_info><area_info><id>2</id><name>磷光的湖</name><x>-51</x><y>-103</y><prog_area>100</prog_area><prog_item>73</prog_item><area_type>0</area_type></area_info><area_info><id>1</id><name>人鱼的断崖</name><x>-29</x><y>-129</y><prog_area>100</prog_area><prog_item>100</prog_item><area_type>0</area_type></area_info></area_info_list></exploration_area></body></response>";
            document.LoadXml(xmldata);
        }

        public static XmlNodeList xmlpath(string path)
        {
            return document.SelectNodes(path);
        }
    }
}
