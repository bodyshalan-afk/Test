using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MrRayzo.Network;
using MrRayzo.Database;
using MrRayzo.Network.Sockets;
using MrRayzo.Network.AuthPackets;
using MrRayzo.Game;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Text;
using MrRayzo.Network.GamePackets;
using MrRayzo.Client;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using MrRayzo;
using MrRayzo.Network.GamePackets.Union;
using ProtoBuf;
using MrRayzo.MaTrix;
using System.Security.Cryptography;

namespace MrRayzo
{
    class Program
    {
      
       
        public static MemoryCompressor MCompressor = new MemoryCompressor();
        public static string GameIP, Maze;
        public static Encoding Encoding = ASCIIEncoding.Default;
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public static Thread2 GHRooms = new Thread2(1000);
        public static Time32 MemoryStamp;
        public static int PlayerCap = 1000;
        public static long MaxOn = 0;
        public static Counter EntityUID;
        public static string GetProcess = "0";
        public static DayOfWeek Today;
        public static ServerSocket[] AuthServer;
        public static ushort GamePort;
        public static ServerSocket GameServer;
        public static List<ushort> AuthPort;
        public static DateTime StartDate;
        public static uint ThunderScoreWar = 0;
        public static DateTime RestartDate = DateTime.Now.AddHours(24);
        public static uint ScreenColor = 0;
        public static World World;
        #region Rayzo
        public static uint VIP1, VIP2, VIP3, VIP4, VIP5, VIP6, VIP7;
        public static uint Autohunt;
        public static uint BigMonster, TopInHour;
        public static uint GuildLeader, CTF;
        public static uint MrConquer;
        public static uint TopSpouse;
        public static uint HeroOfGame;
        public static uint Weekly, DropMoney;
        public static uint ElitePk1;
        public static uint ElitePk2;
        public static uint ElitePk3;
        public static uint ElitePk8;
        public static uint TeamPK1;
        public static uint TeamPK2;
        public static uint TeamPK3;
        public static uint TeamPK8;
        public static uint SkillTeam1;
        public static uint SkillTeam2;
        public static uint SkillTeam3;
        public static uint SkillTeam8;
        public static uint guilwar, SguildwarMoney, GuildWarMoney;
        public static uint EliteGw;
        public static uint Sguildwar;
        public static uint unionWar,ChiSeller,WorldCup,JiangSeller;
        public static uint clanwar;
        public static uint ThunderScOreWar;
        public static uint statuesWar;
        public static uint classPoleWar;
        public static uint nobiltyPoleWar;
        public static uint powerX;
        public static uint King, Baron, Earl, Duke, prince;
        public static uint guildScoreWar;
        public static uint classPk;
        public static uint Dameg;
        public static uint ChangeSex;
        public static uint ChangeName;
        public static uint OblivionDew;
        #endregion
        public static Client.GameState[] GamePool = new Client.GameState[0];
        public static Client.GameState[] Values = new Client.GameState[0];
        public static VariableVault Vars;
        public static long WeatherType = 0L;
        public static bool TestingMode = false;
        public static bool SnowBa = true;
        public static bool Vampira = true;
        public static bool Nemesis = true;
        public static bool Legendary = true;
        public static bool Nobility = false;
        public static bool LostMan = true;
        public static bool GuildWarMonster = true;
        public static bool DeadLady = true;
        public static bool NemesisTyrantSpanwed = false;
        public static bool Shangi = true;
        public static bool DeadMan = true;
        public static bool SwordMaster = true;
        public static bool TeratoDragon = true;
        public static bool ThrillingSpook = true;
        public static bool SnowBanshee = true;
        public static bool Destructive = true;
        public static bool SnowSoul = true;
        public static int RandomSeed = 0;
        public static void addiplog(string Player, string ip)
        {

            String folderN = Player + DateTime.Now.Year + "-" + DateTime.Now.Month,
                Path = "gmlogs\\Accountsiplog\\",
                NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            if (!File.Exists(NewPath + "\\" + DateTime.Now.Day + ".txt"))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(NewPath + "\\" + DateTime.Now.Day + ".txt"))
                {
                    fs.Close();
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(NewPath + "\\" + DateTime.Now.Day + ".txt", true))
            {
                file.WriteLine(Player + ip);
            }
        }
    
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Application_ThreadException);
            Time32 Start = Time32.Now;
            RandomSeed = Convert.ToInt32(DateTime.Now.Ticks.ToString().Remove(DateTime.Now.Ticks.ToString().Length / 2));
            Kernel.Random = new FastRandom(RandomSeed);
            StartDate = DateTime.Now;
            Console.Title = "Mr.Rayzo"; Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.ForegroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("|  ......... Mr~Rayzo.........    |", ConsoleColor.Red);
            Console.WriteLine(@"|          01140071296            |", ConsoleColor.Blue);
            Console.WriteLine(@"|          Privte 2D              |", ConsoleColor.Blue);
            Console.WriteLine(@"+---------------------------------+", ConsoleColor.Blue);
            System.Console.ForegroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Load server configuration");
            string ConfigFileName = "Mr.Rayzo.dll";
            IniFile IniFile = new IniFile(ConfigFileName);
            GameIP = IniFile.ReadString("configuration", "IP","127.0.0.1" );
            GamePort = IniFile.ReadUInt16("configuration", "GamePort");
            AuthPort = new List<ushort>()
            {
                 IniFile.ReadUInt16("configuration", "AuthPort"),
            };
            Constants.ServerName = IniFile.ReadString("configuration", "ServerName");
            Database.DataHolder.CreateConnection(
                IniFile.ReadString("MySql", "Host", "localhost"),
                IniFile.ReadString("MySql", "Username", "root"),
                 IniFile.ReadString("MySql", "Password", "123456789"),
                 IniFile.ReadString("MySql", "Database", "cq")
                );

            EntityUID = new Counter(0);
            bool x = false;
            using (MySqlCommand cmd = new MySqlCommand(MySqlCommandType.SELECT))
            {
                cmd.Select("configuration").Where("Server", Constants.ServerName);
                using (MySqlReader r = new MySqlReader(cmd))
                {
                    if (r.Read())
                    {
                        EntityUID = new Counter(r.ReadUInt32("EntityID"));
                        Game.ConquerStructures.Society.Guild.GuildCounter = new MrRayzo.Counter(r.ReadUInt32("GuildID"));
                        ConquerItem.ItemUID = new Counter(r.ReadUInt32("ItemUID"));
                        Constants.ExtraExperienceRate = r.ReadUInt32("ExperienceRate");
                        Constants.ExtraSpellRate = r.ReadUInt32("SpellExperienceRate");
                        Constants.ExtraProficiencyRate = r.ReadUInt32("ProficiencyExperienceRate");
                        
                        Constants.MoneyDropRate = r.ReadUInt32("MoneyDropRate");
                        Constants.MoneyDropMultiple = r.ReadUInt32("MoneyDropMultiple");
                        if (r.ReadByte("LastDailySignReset") != DateTime.Now.Month) x = true;
                        Constants.ConquerPointsDropRate = r.ReadUInt32("ConquerPointsDropRate");
                        Constants.ConquerPointsDropMultiple = r.ReadUInt32("ConquerPointsDropMultiple");
                        Constants.ItemDropRate = r.ReadUInt32("ItemDropRate");
                        Constants.ItemDropQualityRates = r.ReadString("ItemDropQualityString").Split('~');
                        #region rayzo
                        VIP1 = r.ReadUInt32("VIP1");
                        VIP2 = r.ReadUInt32("VIP2");
                        VIP3 = r.ReadUInt32("VIP3");
                        VIP4 = r.ReadUInt32("VIP4");
                        VIP5 = r.ReadUInt32("VIP5");
                        VIP6 = r.ReadUInt32("VIP6");
                        VIP7 = r.ReadUInt32("VIP7");
                        Autohunt = r.ReadUInt32("Autohunt");
                        BigMonster = r.ReadUInt32("BigMonster");
                        TopInHour = r.ReadUInt32("TopInHour");
                        GuildLeader = r.ReadUInt32("GuildLeader");
                        MrConquer = r.ReadUInt32("MrConquer");
                        TopSpouse = r.ReadUInt32("TopSpouse");
                        HeroOfGame = r.ReadUInt32("HeroOfGame");
                        Weekly = r.ReadUInt32("Weekly");
                        ElitePk1 = r.ReadUInt32("ElitePk1");
                        ElitePk2 = r.ReadUInt32("ElitePk2");
                        ElitePk3 = r.ReadUInt32("ElitePk3");
                        ElitePk8 = r.ReadUInt32("ElitePk8");
                        SkillTeam1 = r.ReadUInt32("SkillTeam1");
                        SkillTeam2 = r.ReadUInt32("SkillTeam1");
                        SkillTeam3 = r.ReadUInt32("SkillTeam3");
                        SkillTeam8 = r.ReadUInt32("SkillTeam8");
                        DropMoney = r.ReadUInt32("DropMoney");
                        TeamPK1 = r.ReadUInt32("TeamPK1");
                        TeamPK2 = r.ReadUInt32("TeamPK2");
                        TeamPK3 = r.ReadUInt32("TeamPK3");
                        TeamPK8 = r.ReadUInt32("TeamPK8");
                        King = r.ReadUInt32("King");
                        prince = r.ReadUInt32("prince");
                        Duke = r.ReadUInt32("Duke");
                        Earl = r.ReadUInt32("Earl");
                        Baron = r.ReadUInt32("Baron");
                        CTF = r.ReadUInt32("CTF");
                        SguildwarMoney = r.ReadUInt32("SguildwarMoney");
                        GuildWarMoney = r.ReadUInt32("GuildWarMoney");
                        guilwar = r.ReadUInt32("guilwar");
                        Sguildwar = r.ReadUInt32("Sguildwar");
                        EliteGw = r.ReadUInt32("EliteGw");
                        unionWar = r.ReadUInt32("unionWar");
                        clanwar = r.ReadUInt32("clanwar");
                        ThunderScOreWar = r.ReadUInt32("ThunderScOreWar");
                        statuesWar = r.ReadUInt32("statuesWar");
                        classPoleWar = r.ReadUInt32("classPoleWar");
                        nobiltyPoleWar = r.ReadUInt32("nobiltyPoleWar");
                        powerX = r.ReadUInt32("powerX");
                        guildScoreWar = r.ReadUInt32("guildScoreWar");
                        classPk = r.ReadUInt32("classPk");
                        Dameg = r.ReadUInt32("Dameg");
                        ChangeSex = r.ReadUInt32("ChangeSex");
                        ChangeName = r.ReadUInt32("ChangeName");
                        OblivionDew = r.ReadUInt32("OblivionDew");
                        WorldCup = r.ReadUInt32("WorldCup");
                        ChiSeller = r.ReadUInt32("ChiSeller");
                        JiangSeller = r.ReadUInt32("JiangSeller");
                        #endregion
                        Constants.WebAccExt = r.ReadString("AccountWebExt");
                        Constants.WebVoteExt = r.ReadString("VoteWebExt");
                        Constants.WebDonateExt = r.ReadString("DonateWebExt");
                        Constants.ServerWebsite = r.ReadString("ServerWebsite");
                        Constants.ServerGMPass = r.ReadString("ServerGMPass");
                        PlayerCap = r.ReadInt32("PlayerCap");
                        Union.UnionCounter = new Counter(r.ReadUInt32("UnionID"));
                        Database.EntityVariableTable.Load(0, out Vars);
                    }
                }
            }
            if (EntityUID.Now == 0)
            {
                Console.Clear();
                Console.WriteLine("Database error. Please check your MySQL. Server will now close.");
                Console.ReadLine();
                return;
            }

            else
            {

                {

                    \u0043onsole.WriteLine("Initializing Database.");
                    
                    ProjectX_V3_Game.Database.ScriptDatabase.LoadSettings();
                    ProjectX_V3_Game.Database.ScriptDatabase.LoadNPCScripts();
                    Console.WriteLine("Loading The Hard Things");
                    Database.ConquerItemInformation.Load();
                    Database.ConquerItemTable.ClearNulledItems();
                    InnerPowerTable.LoadDBInformation();
                    InnerPowerTable.Load();
                    PerfectionTable.LoadItemRefineAttribute();
                    if (x) MsgSignIn.Reset();
                    PerfectionTable.LoadItemRefineEffect();
                    PerfectionTable.LoadItemRefineEffectEX();
                    Database.Flowers.LoadFlowers();
                    Database.SignInTable.Load();
                    Database.MonsterInformation.Load();
                    Database.MapsTable.Load();
                    MrRayzo.MaTrix.SoulProtection.Load();
                    World = new World();
                    Database.BannedTable.Load();
                    World.Init();
                    Map.CreateTimerFactories();
                    Database.SignInTable.Load();
                    Database.DMaps.LoadMapPaths();
                    Database.DMaps.LoadMap(700);
                    DMaps.LoadMap
                        (2068);
                    Database.DMaps.LoadMap(3868);
                    
                    Database.DMaps.LoadMap(3935);
                    Copra.QuestInfo.Load();
                    Database.SpellTable.Load();
                    Database.ShopFile.Load();
                    Database.HonorShop.Load();
                    Database.RacePointShop.Load();
                    Database.ChampionShop.Load();
                    Database.EShopFile.Load();
                    Database.EShopV2File.Load();
                    StorageManager.Load();
                    Database.AddingInformationTable.Load();
                    Database.LotteryTable.Load();
                    Database.vipLottery.Load();
                    Copra.Roulette.Database.Roulettes.Load();
                    Copra.Way2Heroes.Load();
                    Database.ConquerItemTable.ClearNulledItems();
                    Refinery.Load();
                    Values = new Client.GameState[0];
                    new Game.Map(3820, Database.DMaps.MapPaths[3820]);
                    new Game.Map(1015, Database.DMaps.MapPaths[1015]);
                    new Game.Map(1002, Database.DMaps.MapPaths[1002]);
                    new Game.Map(1038, Database.DMaps.MapPaths[1038]);
                    new Game.Map(2071, Database.DMaps.MapPaths[2071]);
                    if (DMaps.LoadMap(1038))
                        Game.GuildWar.Initiate();
                  
                    if (DMaps.LoadMap(10380))
                        Game.SuperGuildWar.Initiate();
                    // new Game.Map(1509, Database.DMaps.MapPaths[1509]);
                    new Game.Map(10002, 2021, Database.DMaps.MapPaths[2021]);
                    new Game.Map(8883, 1004, Database.DMaps.MapPaths[1004]);
                    Constants.PKFreeMaps.Add(8883);

                    if (DMaps.LoadMap(1510))
                        Game.UnionWar.Initiate();
                    Game.EliteGuildWar.EliteGwint();

                    ////Database.Vote.Load();
                    Database.DataHolder.ReadStats();
                    Database.IPBan.Load();
                 //   PlayersVot.LoadPlayersVots();
                    Database.JiangHu.LoadStatus();
                    Database.JiangHu.LoadJiangHu();
                    Database.NobilityTable.Load();
                    Database.ArenaTable.Load();
                    Database.TeamArenaTable.Load();
                    Database.GuildTable.Load();
                    UnionTable.Load();
                    MagicTypeOP.Load();
                    AuctionBase.Load();
                    Database.ChiTable.LoadAllChi();
                    StorageManager.Load();
                    Database.WardrobeTable.Load();
                    Clan.LoadClans();
                    new MsgUserAbilityScore().GetRankingList();
                    new MsgEquipRefineRank().UpdateRanking();
                    new MsgRankMemberShow().UpdateBestEntity();
                    Game.Screen.CreateTimerFactories();
                    Network.Cryptography.AuthCryptography.PrepareAuthCryptography();
                    World.CreateTournaments();
                   
                    new MySqlCommand(MySqlCommandType.UPDATE).Update("entities").Set("Online", 0).Execute();
                    Console.WriteLine("Initializing Sockets.");
                    AuthServer = new ServerSocket[AuthPort.Count];
                    for (int i = 0; i < AuthServer.Length; i++)
                    {
                        AuthServer[i] = new ServerSocket();
                        AuthServer[i].OnClientConnect += AuthServer_OnClientConnect;
                        AuthServer[i].OnClientReceive += AuthServer_OnClientReceive;
                        AuthServer[i].OnClientDisconnect += AuthServer_OnClientDisconnect;
                        AuthServer[i].Enable(AuthPort[i], "0.0.0.0");
                        Console.WriteLine("Auth " + i + " server  online.");
                    }
                    Console.WriteLine("Auth server online.");
                    GameServer = new ServerSocket();
                    GameServer.OnClientConnect += OnClientConnect;
                    GameServer.OnClientReceive += GameServer_OnClientReceive;
                    GameServer.OnClientDisconnect += OnClientDisconnect;
                    GameServer.Enable(GamePort, "0.0.0.0");
                    Console.WriteLine("Game server online.");
                    {
                        Copra.Pet.CreateTimerFactories();
                        AI.CreateTimerFactories();
                        var client = new GameState(null);
                        client.Player = new Entity(EntityFlag.Monster, false);
                        client.Player.MapID = 1002;
                        Npcs npc = new Npcs(client);
                        var req = new NpcRequest();
                        req.Deserialize(new byte[28]);
                        Npcs.GetDialog(req, client);
                        client = null;
                        MrRayzo.Booths.Load();

                    }
                    Console.WriteLine("Server loaded in " + (Time32.Now - Start) + " milliseconds.");
                    Console.Clear();
                    new MrRayzo.Game.ServerStatus().ShowDialog();
                    Console.WriteLine("------------------------------------------------------------------", ConsoleColor.DarkGreen);
                    Console.WriteLine("[********************** [ Mr~Rayzo ] **********************]", ConsoleColor.Red);
                    Console.WriteLine("------------------------------------------------------------------", ConsoleColor.DarkGreen);
                    RayzoHandler += RayzoConsole_CloseEvent;
                    SetConsoleCtrlHandler(RayzoHandler, true);
                    GC.Collect();
                    WorkConsole();
                }

            }

        }
        #region Closing Events
        private static bool RayzoConsole_CloseEvent(CtrlType sig)
        {
            return !Save();
        }
        private static Native.ConsoleEventHandler RayzoHandler;
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(Native.ConsoleEventHandler handler, bool add);
        private delegate bool EventHandler(CtrlType sig);
        #endregion
        public static bool Save(bool Exit = false)
        {
            try
            {
                Database.JiangHu.SaveJiangHu();
                using (var conn = Database.DataHolder.MySqlConnection)
                {
                    conn.Open();
                    foreach (Client.GameState client in Program.Values)
                    {
                        client.Account.Save();
                        Database.ChiTable.Save(client);
                        Database.EntityTable.SaveEntity(client);
                        Database.SkillTable.SaveProficiencies(client);
                        Database.ArenaTable.SaveArenaStatistics(client.ArenaStatistic);
                        Database.TeamArenaTable.SaveArenaStatistics(client.TeamArenaStatistic);
                    }
                }
                Database.InnerPowerTable.Save();
                BannedTable.Save();//Ayman
                //Database.Flowers.SaveFlowers();
                AuctionBase.Save();
                using (MySqlCommand cmd = new MySqlCommand(MySqlCommandType.SELECT).Select("configuration").Where("Server", Constants.ServerName))
                {
                    using (MySqlReader r = new MySqlReader(cmd))
                    {
                        if (r.Read())
                        {
                            new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("serveronline", 0).Where("Server", Constants.ServerName).Execute();
                        }
                    }
                }
                using (MySqlCommand cmd = new MySqlCommand(MySqlCommandType.SELECT).Select("configuration").Where("Server", Constants.ServerName))
                {
                    using (MySqlReader r = new MySqlReader(cmd))
                    {
                        if (r.Read())
                        {
                            new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("EntityID", EntityUID.Now).Set("ServerKingdom", Kernel.ServerKingdom).Set("GuildID", Game.ConquerStructures.Society.Guild.GuildCounter.Now).Where("Server", Constants.ServerName).Execute();
                            if (r.ReadByte("LastDailySignReset") != DateTime.Now.Month) MsgSignIn.Reset();
                        }
                    }
                }
                using (var cmd = new MySqlCommand(MySqlCommandType.UPDATE).Update("configuration"))
                    cmd.Set("LastDailySignReset", DateTime.Now.Month).Execute();
                if (Exit)
                    Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }
        #region Exceptions & Logs

        public static void AddTradeLog(Game.ConquerStructures.Trade first, String firstN, Game.ConquerStructures.Trade second, String secondN)
        {
            String folderN = DateTime.Now.Year + "-" + DateTime.Now.Month,
                Path = "database\\Security\\Trade\\",
                NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            if (!File.Exists(NewPath + "\\" + DateTime.Now.Day + ".txt"))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(NewPath + "\\" + DateTime.Now.Day + ".txt"))
                {
                    fs.Close();
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(NewPath + "\\" + DateTime.Now.Day + ".txt", true))
            {
                file.WriteLine("************************************************************************************");
                file.WriteLine("First Person TradeLog ({0}) -", firstN);
                file.WriteLine("Gold Traded: " + first.Money);
                file.WriteLine("Conquer Points Traded: " + first.ConquerPoints);

                for (int i = 0; i < first.Items.Count; i++)
                {
                    file.WriteLine("------------------------------------------------------------------------------------");
                    file.WriteLine("Item : " + first.Items[i].ToLog());
                    file.WriteLine("------------------------------------------------------------------------------------");
                }
                file.WriteLine("Second Person TradeLog ({0}) -", secondN);
                file.WriteLine("Gold Traded: " + second.Money);
                file.WriteLine("Conquer Points Traded: " + second.ConquerPoints);

                for (int i = 0; i < second.Items.Count; i++)
                {
                    file.WriteLine("------------------------------------------------------------------------------------");
                    file.WriteLine("Item : " + second.Items[i].ToLog());
                    file.WriteLine("------------------------------------------------------------------------------------");
                }
                file.WriteLine("************************************************************************************");
            }
        }
        public static void AddTradeLog(MrRayzo.Game.ConquerStructures.Trade first, String firstN, MrRayzo.Game.ConquerStructures.Trade second, String secondN, String firstip, String secondip)
        {
            String folderN = DateTime.Now.Year + "-" + DateTime.Now.Month,
                Path = "gmlogs\\tradelogs\\",
                NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            if (!File.Exists(NewPath + "\\" + DateTime.Now.Day + ".txt"))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(NewPath + "\\" + DateTime.Now.Day + ".txt"))
                {
                    fs.Close();
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(NewPath + "\\" + DateTime.Now.Day + ".txt", true))
            {
                file.WriteLine("************************************************************************************");
                file.WriteLine("**************************Gived***********************");
                file.WriteLine("First Person TradeLog ( {0} ) -", firstN);
                file.WriteLine("First Person TradeLog and his ip ( {0} ) -", firstip);
                file.WriteLine("Gold Traded: " + first.Money);
                file.WriteLine("Conquer Points Traded: " + first.ConquerPoints);

                for (int i = 0; i < first.Items.Count; i++)
                {
                    file.WriteLine("------------------------------------------------------------------------------------");
                    file.WriteLine("Item : " + first.Items[i].ToLog());
                    if (first.Items[i].item_id / 1000 == 202)
                        file.WriteLine("Tower ");
                    if (first.Items[i].item_id / 1000 == 201)
                        file.WriteLine("Fan ");
                    if (first.Items[i].item_id / 1000 == 500)
                        file.WriteLine("Bow ");
                    if (first.Items[i].item_id / 1000 == 900)
                        file.WriteLine("Shield ");
                    if (first.Items[i].item_id / 1000 == 620)
                        file.WriteLine("EpicBackSword ");
                    if (first.Items[i].item_id / 1000 == 421)
                        file.WriteLine("BackSword ");
                    if (first.Items[i].item_id / 1000 == 410)
                        file.WriteLine("Blade ");
                    if (first.Items[i].item_id / 1000 == 204)
                        file.WriteLine("Wing ");
                    if (first.Items[i].item_id / 1000 == 420)
                        file.WriteLine("Sword ");
                    if (first.Items[i].item_id / 1000 == 601)
                        file.WriteLine("Katana-Ninja ");
                    if (first.Items[i].item_id / 1000 == 610)
                        file.WriteLine("Monk-Beeds ");
                    if (first.Items[i].item_id / 1000 == 203)
                        file.WriteLine("Crop ");
                    if (first.Items[i].item_id / 1000 == 181 || first.Items[i].item_id / 1000 == 182)
                        file.WriteLine("Germant ");
                    if (first.Items[i].item_id / 1000 == 2100)
                        file.WriteLine("Cup ");
                    if (first.Items[i].item_id / 1000 == 150)
                        file.WriteLine("Ring ");
                    if (first.Items[i].item_id / 1000 == 160)
                        file.WriteLine("Boot");
                    if (first.Items[i].item_id / 1000 == 200)
                        file.WriteLine("Mount ");
                    if (first.Items[i].item_id / 1000 == 120)
                        file.WriteLine("Necklace ");
                    if (first.Items[i].item_id / 1000 == 152)
                        file.WriteLine("Bracelet ");
                    if (first.Items[i].item_id / 1000 == 350 || first.Items[i].item_id / 1000 == 360 || first.Items[i].item_id / 1000 == 370 || first.Items[i].item_id / 1000 == 380)
                        file.WriteLine("Accessory ");
                    if (first.Items[i].item_id / 1000 == 152)
                        file.WriteLine("Bag ");
                    if (first.Items[i].item_id / 1000 == 622)
                        file.WriteLine("EpicMonk ");
                    if (first.Items[i].item_id / 1000 == 616)
                        file.WriteLine("EpicNinja ");
                    if (first.Items[i].item_id / 1000 == 619)
                        file.WriteLine("Hossu ");
                    if (first.Items[i].item_id / 1000 == 136)
                        file.WriteLine("Armor-Monk ");
                    if (first.Items[i].item_id / 1000 == 138)
                        file.WriteLine("Armor-Dragonwarrior ");
                    if (first.Items[i].item_id / 1000 == 139)
                        file.WriteLine("Armor-Pirate ");
                    if (first.Items[i].item_id / 1000 == 141)
                        file.WriteLine("Hat-Warrior ");
                    if (first.Items[i].item_id / 1000 == 142)
                        file.WriteLine("Hat-Archer ");
                    if (first.Items[i].item_id / 1000 == 143)
                        file.WriteLine("Hat-Monk ");

                    if (first.Items[i].item_id / 1000 == 144)
                        file.WriteLine("Hat-Pirate ");
                    if (first.Items[i].item_id / 1000 == 145)
                        file.WriteLine("Hat-Pirate ");
                    if (first.Items[i].item_id / 1000 == 148)
                        file.WriteLine("Hat-DragonWarrior ");
                    if (first.Items[i].item_id / 1000 == 170)
                        file.WriteLine("Hat-WindWalker ");
                    if (first.Items[i].item_id / 1000 == 142)
                        file.WriteLine("Hat-Archer ");
                    if (first.Items[i].item_id / 1000 == 143)
                        file.WriteLine("Hat-Monk ");



                    file.WriteLine("------------------------------------------------------------------------------------");
                }
                file.WriteLine("**************************Gived***********************");
                file.WriteLine("Second Person TradeLog ( {0} ) -", secondN);
                file.WriteLine("Second Person TradeLog and his ip ( {0} ) -", secondip);
                file.WriteLine("Gold Traded: " + second.Money);
                file.WriteLine("Conquer Points Traded: " + second.ConquerPoints);

                for (int i = 0; i < second.Items.Count; i++)
                {
                    file.WriteLine("------------------------------------------------------------------------------------");
                    file.WriteLine("Item : " + second.Items[i].ToLog());
                    if (second.Items[i].item_id / 1000 == 202)
                        file.WriteLine("Tower ");
                    if (second.Items[i].item_id / 1000 == 201)
                        file.WriteLine("Fan ");
                    if (second.Items[i].item_id / 1000 == 500)
                        file.WriteLine("Bow ");
                    if (second.Items[i].item_id / 1000 == 900)
                        file.WriteLine("Shield ");
                    if (second.Items[i].item_id / 1000 == 620)
                        file.WriteLine("EpicBackSword ");
                    if (second.Items[i].item_id / 1000 == 421)
                        file.WriteLine("BackSword ");
                    if (second.Items[i].item_id / 1000 == 410)
                        file.WriteLine("Blade ");
                    if (second.Items[i].item_id / 1000 == 204)
                        file.WriteLine("Wing ");
                    if (second.Items[i].item_id / 1000 == 420)
                        file.WriteLine("Sword ");
                    if (second.Items[i].item_id / 1000 == 601)
                        file.WriteLine("Katana-Ninja ");
                    if (second.Items[i].item_id / 1000 == 610)
                        file.WriteLine("Monk-Beeds ");
                    if (second.Items[i].item_id / 1000 == 203)
                        file.WriteLine("Crop ");
                    if (second.Items[i].item_id / 1000 == 181 || second.Items[i].item_id / 1000 == 182)
                        file.WriteLine("Germant ");
                    if (second.Items[i].item_id / 1000 == 2100)
                        file.WriteLine("Cup ");
                    if (second.Items[i].item_id / 1000 == 150)
                        file.WriteLine("Ring ");
                    if (second.Items[i].item_id / 1000 == 160)
                        file.WriteLine("Boot");
                    if (second.Items[i].item_id / 1000 == 200)
                        file.WriteLine("Mount ");
                    if (second.Items[i].item_id / 1000 == 120)
                        file.WriteLine("Necklace ");
                    if (second.Items[i].item_id / 1000 == 152)
                        file.WriteLine("Bracelet ");
                    if (second.Items[i].item_id / 1000 == 350 || second.Items[i].item_id / 1000 == 360 || second.Items[i].item_id / 1000 == 370 || second.Items[i].item_id / 1000 == 380)
                        file.WriteLine("Accessory ");
                    if (second.Items[i].item_id / 1000 == 152)
                        file.WriteLine("Bag ");
                    if (second.Items[i].item_id / 1000 == 622)
                        file.WriteLine("EpicMonk ");
                    if (second.Items[i].item_id / 1000 == 616)
                        file.WriteLine("EpicNinja ");
                    if (second.Items[i].item_id / 1000 == 619)
                        file.WriteLine("Hossu ");
                    if (second.Items[i].item_id / 1000 == 136)
                        file.WriteLine("Armor-Monk ");
                    if (second.Items[i].item_id / 1000 == 138)
                        file.WriteLine("Armor-Dragonwarrior ");
                    if (second.Items[i].item_id / 1000 == 139)
                        file.WriteLine("Armor-Pirate ");
                    if (second.Items[i].item_id / 1000 == 141)
                        file.WriteLine("Hat-Warrior ");
                    if (second.Items[i].item_id / 1000 == 142)
                        file.WriteLine("Hat-Archer ");
                    if (second.Items[i].item_id / 1000 == 143)
                        file.WriteLine("Hat-Monk ");

                    if (second.Items[i].item_id / 1000 == 144)
                        file.WriteLine("Hat-Pirate ");
                    if (second.Items[i].item_id / 1000 == 145)
                        file.WriteLine("Hat-Pirate ");
                    if (second.Items[i].item_id / 1000 == 148)
                        file.WriteLine("Hat-DragonWarrior ");
                    if (second.Items[i].item_id / 1000 == 170)
                        file.WriteLine("Hat-WindWalker ");
                    if (second.Items[i].item_id / 1000 == 142)
                        file.WriteLine("Hat-Archer ");
                    if (second.Items[i].item_id / 1000 == 143)
                        file.WriteLine("Hat-Monk ");

                    file.WriteLine("------------------------------------------------------------------------------------");
                }
                file.WriteLine("************************************************************************************");
            }
        }
        static void Application_ThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            SaveException(e.ExceptionObject as Exception);
           
        }
        public static void SaveException(Exception e)
        {
            Console.WriteLine(e);
            
            var dt = DateTime.Now;
            string date = dt.Month + "-" + dt.Day + "//";

            if (!Directory.Exists(Application.StartupPath + Constants.UnhandledExceptionsPath))
                Directory.CreateDirectory(Application.StartupPath + "\\" + Constants.UnhandledExceptionsPath);
            if (!Directory.Exists(Application.StartupPath + "\\" + Constants.UnhandledExceptionsPath + date))
                Directory.CreateDirectory(Application.StartupPath + "\\" + Constants.UnhandledExceptionsPath + date);
            if (!Directory.Exists(Application.StartupPath + "\\" + Constants.UnhandledExceptionsPath + date + e.TargetSite.Name))
                Directory.CreateDirectory(Application.StartupPath + "\\" + Constants.UnhandledExceptionsPath + date + e.TargetSite.Name);

            string fullPath = Application.StartupPath + "\\" + Constants.UnhandledExceptionsPath + date + e.TargetSite.Name + "\\";

            string date2 = dt.Hour + "-" + dt.Minute;
            List<string> Lines = new List<string>();

            Lines.Add("----Exception message----");
            Lines.Add(e.Message);
            Lines.Add("----End of exception message----\r\n");

            Lines.Add("----Stack trace----");
            Lines.Add(e.StackTrace);
            Lines.Add("----End of stack trace----\r\n");

            //Lines.Add("----Data from exception----");
            //foreach (KeyValuePair<object, object> data in e.Data)
            //    Lines.Add(data.Key.ToString() + "->" + data.Value.ToString());
            //Lines.Add("----End of data from exception----\r\n");

            File.WriteAllLines(fullPath + date2 + ".txt", Lines.ToArray());
        }

        private static void WorkConsole()
        {
            while (true)
            {
                try
                {
                    CommandsAI(Console.ReadLine());
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }
        public static DateTime LastRandomReset = DateTime.Now;
        public static Network.GamePackets.BlackSpotPacket BlackSpotPacket = new Network.GamePackets.BlackSpotPacket();
        public static bool MyPC = true;
        public static void CommandsAI(string command)
        {
            try
            {
                if (command == null)
                    return;
                string[] data = command.Split(' ');
                switch (data[0])
                {
                    case "@clear1":  //شفرة 
                        {
                            Program.Clear();
                            Console.WriteLine("Consle Clear + Memory Optimizeed!!");
                            break;
                        }
                    case "@donation":
                        {
                            try
                            {
                                uint rx = uint.Parse(data[1]);
                                using (var sel = new MySqlCommand(MySqlCommandType.SELECT).Select("nobility"))
                                using (var reader = sel.CreateReader())
                                {
                                    while (reader.Read())
                                    {
                                        using (var upd = new MySqlCommand(MySqlCommandType.UPDATE).Update("nobility"))
                                            upd.Set("donation", reader.ReadUInt64("Donation") / rx).Where("entityuid", reader.ReadUInt32("EntityUID"))
                                                .Execute();
                                    }
                                }
                            }
                            catch { }
                            Database.NobilityTable.Load();

                            break;
                        }
                    case "@reloadnpc":
                        {
                            World.ScriptEngine.Check_Updates();
                            Console.WriteLine("New System's Npc Reloaded.");
                            break;
                        }
                    case "cp":
                        {
                            new MrRayzo.Game.ServerStatus().ShowDialog();
                            break;
                        }
                    case "@1":
                        {
                            MrRayzo.Characters cp = new Characters();
                            cp.ShowDialog();
                            break;
                        }

                    case "@2":
                        {
                            new MrRayzo.Game.RayzoEditNpc().ShowDialog();
                            break;
                        }
                    case "@3":
                        {
                            new MrRayzo.JiangHu().ShowDialog();
                            break;
                        }
                    case "@4":
                        {
                            new MrRayzo.Game.RayzoMonsterSpawns().ShowDialog();
                            break;
                        }
                    case "@5":
                        {
                            new MrRayzo.RayzoServerDrop().ShowDialog();
                            break;
                        }
                    case "@6":
                        {
                            new MrRayzo.Game.FormatSQL().ShowDialog();
                            break;
                        }

                    case "@7":
                        {
                            new MrRayzo.MapControl().ShowDialog();
                            break;
                        }
                    case "@8":
                        {
                            new MrRayzo.RayzoAddNpc().ShowDialog();
                            break;
                        }

                    case "@clear":
                        {

                            Console.Clear();
                            MrRayzo.Console.WriteLine("Consle and program Cleared ");
                            break;
                        }
                    case "@flushbans":
                        {
                            Database.IPBan.Load();
                            break;
                        }
                    case "@Alivetime":
                        {
                            DateTime now = DateTime.Now;
                            TimeSpan t2 = new TimeSpan(StartDate.ToBinary());
                            TimeSpan t1 = new TimeSpan(now.ToBinary());
                            Console.WriteLine("The server has been online " + (int)(t1.TotalHours - t2.TotalHours) + " hours, " + (int)((t1.TotalMinutes - t2.TotalMinutes) % 1) + " minutes.");
                            break;
                        }
                    case "@online":
                        {
                            Console.WriteLine("Online Entitys count: " + Kernel.GamePool.Count);
                            string line = "";
                            foreach (Client.GameState pClient in Program.Values)
                                line += pClient.Player.Name + ",";
                            if (line != "")
                            {
                                line = line.Remove(line.Length - 1);
                                Console.WriteLine("Entitys: " + line);
                            }
                            break;
                        }
                    case "@memoryusage":
                        {
                            var proc = System.Diagnostics.Process.GetCurrentProcess();
                            Console.WriteLine("Thread count: " + proc.Threads.Count);
                            Console.WriteLine("Memory set(MB): " + ((double)((double)proc.WorkingSet64 / 1024)) / 1024);
                            proc.Close();
                            break;
                        }

                    case "@PlayerCap":
                        {
                            try
                            {
                                PlayerCap = int.Parse(data[1]);
                            }
                            catch
                            {

                            }
                            break;
                        }
                    case "@skill":
                        {
                            Game.Features.Tournaments.SkillPk.SkillTournament.Open();
                            foreach (var clien in Kernel.GamePool.Values)
                            {
                                if (clien.Team == null)
                                    clien.Team = new Game.ConquerStructures.Team(clien);
                                Game.Features.Tournaments.SkillPk.SkillTournament.Join(clien, 3);
                            }
                            break;
                        }
                    case "@team":
                        {
                            Game.Features.Tournaments.TeamPk.TeamTournament.Open();
                            foreach (var clien in Kernel.GamePool.Values)
                            {
                                if (clien.Team == null)
                                    clien.Team = new Game.ConquerStructures.Team(clien);
                                Game.Features.Tournaments.TeamPk.TeamTournament.Join(clien, 3);
                            }
                            break;
                        }
                    case "@exit":
                        {
                            CommandsAI("@save");
                            new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
                            Database.EntityVariableTable.Save(0, Vars);

                            var WC = Program.Values.ToArray();
                            Parallel.ForEach(Program.Values, client =>
                            {
                                client.Send(" Server will exit for 5 min to Solve The Problem, please be paitent ");
                                client.Disconnect();
                            });

                            Kernel.SendWorldMessage(new Network.GamePackets.Message(string.Concat(new object[] { " Server will exit for 5 min to Solve The Problem, please be paitent " }), System.Drawing.Color.Black, 0x7db), Program.Values);


                            if (GuildWar.IsWar)
                                GuildWar.End();
                            new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
                            Environment.Exit(0);
                        }
                        break;
                    case "serverpass":
                        {
                            using (MySqlCommand cmd = new MySqlCommand(MySqlCommandType.SELECT))
                            {
                                cmd.Select("configuration").Where("Server", Constants.ServerName);
                                using (MySqlReader r = new MySqlReader(cmd))
                                {
                                    if (r.Read())
                                        Constants.ServerGMPass = r.ReadString("ServerGMPass");
                                }

                            }
                            break;
                        }
                    case "@pressure":
                        {
                            Console.WriteLine("Genr: " + World.GenericThreadPool.ToString());
                            Console.WriteLine("Send: " + World.SendPool.ToString());
                            Console.WriteLine("Recv: " + World.ReceivePool.ToString());
                            break;
                        }
                    case "@test":
                        {
                            Console.WriteLine("Server will restart after 5 minutes.");
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 5 minute, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 4 minute 30 second, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 4 minute, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 3 minute 30 second, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 3 minute, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 2 minute 30 second, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 2 minute, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 1 minute 30 second, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 1 minute, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The server will be brought down for maintenance in 30 second, Please exit the game now.", System.Drawing.Color.Orange, 2011), Program.GamePool);
                            Console.WriteLine("Server will exit after 1 minute.");
                            CommandsAI("@save");
                            System.Threading.Thread.Sleep(30000);
                            Kernel.SendWorldMessage(new MrRayzo.Network.GamePackets.Message("The Server restarted, Please log in after 2 minutes! ", System.Drawing.Color.Orange, 0x7db), Program.GamePool);
                            try
                            {
                                CommandsAI("@restart");
                            }
                            catch
                            {
                                Console.WriteLine("Server cannot exit");
                            }
                            break;
                        }
                    case "@Loader1":
                    case "@loader1":
                        {
                            GetProcess = "1";
                            Console.WriteLine("Now You Can Get Player Processer");
                            break;
                        }
                    case "@Loader0":
                    case "@loader0":
                        {
                            GetProcess = "0";
                            Console.WriteLine("Now Close Get Player Processer");
                            break;
                        }
                    case "@restart":
                        {
                            try
                            {
                                Kernel.SendWorldMessage(new Network.GamePackets.Message(string.Concat(new object[] { "Server Will Be Restart Now !" }), System.Drawing.Color.Black, 0x7db), Program.Values);
                                CommandsAI("@save");
                                new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
                                var WC = Program.Values.ToArray();
                                foreach (Client.GameState client in WC)
                                {
                                    client.Send(" Server Will Be Restart Now ");
                                    client.Disconnect();
                                }

                                if (GuildWar.IsWar)
                                    GuildWar.End();
                                new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
                                Application.Restart();
                                Environment.Exit(0);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                Console.ReadLine();
                            }
                        }
                        break;
                    case "@account":
                        {
                            Database.AccountTable account = new AccountTable(data[1]);
                            account.Password = data[2];
                            account.State = AccountTable.AccountState.Entity;
                            account.Save();
                        }
                        break;
                    case "@save":
                        {
                            Save();
                        }
                        break;
                    case "@process":
                        {
                            HandleClipboardPacket(command);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Clear()
        {
            throw new NotImplementedException();
        }
        public static void WriteLine(string Line)
        {
            try
            {
                Console.WriteLine(Line);
            }
            catch { }
        }
        public static void HandleClipboardPacket(string cmd)
        {
            string[] pData = cmd.Split(' ');
            long off = 0, type = 0, val = 0;
            if (pData.Length > 1)
            {
                string[] oData = pData[1].Split(':');
                if (oData.Length == 3)
                {
                    off = long.Parse(oData[0]);
                    type = long.Parse(oData[1]);
                    if (oData[2] == "u")
                        val = 1337;
                    else
                        val = long.Parse(oData[2]);
                }
            }
            string Data = OSClipboard.GetText();
            string[] num = Data.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            byte[] packet = new byte[num.Length + 8];
            for (int i = 0; i < num.Length; i++)
                packet[i] = byte.Parse(num[i], System.Globalization.NumberStyles.HexNumber);
            Writer.WriteUInt16((ushort)(packet.Length - 8), 0, packet);
            if (off != 0)
            {
                switch (type)
                {
                    case 1:
                        {
                            packet[(int)off] = (byte)val;
                            break;
                        }
                    case 2:
                        {
                            Writer.WriteUInt16((ushort)val, (int)off, packet);
                            break;
                        }
                    case 4:
                        {
                            Writer.WriteUInt32((uint)val, (int)off, packet);
                            break;
                        }
                    case 8:
                        {
                            Writer.WriteUInt64((ulong)val, (int)off, packet);
                            break;
                        }
                }
            }
            foreach (var client in Program.Values)
            {
                if (val == 1337 && type == 4)
                    Writer.WriteUInt32(client.Player.UID, (int)off, packet);
                client.Send(packet);
            }
        }
        static void GameServer_OnClientReceive(byte[] buffer, int length, ClientWrapper obj)
        {
            if (obj.Owner == null)
            {
                obj.Disconnect();
                return;
            }
            Client.GameState Client = obj.Owner as Client.GameState;
            if (Client.Exchange)
            {
                Client.Exchange = false;
                Client.Action = 1;
                var crypto = new Network.Cryptography.GameCryptography(System.Text.Encoding.Default.GetBytes(Constants.GameCryptographyKey));
                byte[] otherData = new byte[length];
                Array.Copy(buffer, otherData, length);
                crypto.Decrypt(otherData, length);

                bool extra = false;
                int pos = 0;
                if (BitConverter.ToInt32(otherData, length - 140) == 128)//no extra packet
                {
                    pos = length - 140;
                    Client.Cryptography.Decrypt(buffer, length);
                }
                else if (BitConverter.ToInt32(otherData, length - 176) == 128)//extra packet
                {
                    pos = length - 176;
                    extra = true;
                    Client.Cryptography.Decrypt(buffer, length - 36);
                }
                int len = BitConverter.ToInt32(buffer, pos); pos += 4;
                if (len != 128)
                {
                    Client.Disconnect();
                    return;
                }
                byte[] pubKey = new byte[128];
                for (int x = 0; x < len; x++, pos++) pubKey[x] = buffer[pos];

                string PubKey = Program.Encoding.GetString(pubKey);
                Client.Cryptography = Client.DHKeyExchange.HandleClientKeyPacket(PubKey, Client.Cryptography);

                if (extra)
                {
                    byte[] data = new byte[36];
                    Buffer.BlockCopy(buffer, length - 36, data, 0, 36);
                    processData(data, 36, Client);
                }
            }
            else
            {
                processData(buffer, length, Client);
            }
        }
        private static void processData(byte[] buffer, int length, Client.GameState Client)
        {
            Client.Cryptography.Decrypt(buffer, length);
            Client.Queue.Enqueue(buffer, length);
            if (Client.Queue.CurrentLength > 1224)
            {
                Console.WriteLine("[Disconnect]Reason:The packet size is too big. " + Client.Queue.CurrentLength);
                Client.Disconnect();
                return;
            }
            while (Client.Queue.CanDequeue())
            {
                byte[] data = Client.Queue.Dequeue();
                Network.PacketHandler.HandlePacket(data, Client);
            }
        }
        static void OnClientConnect(ClientWrapper obj)
        {
            Client.GameState client = new Client.GameState(obj);
            client.Send(client.DHKeyExchange.CreateServerKeyPacket());
            obj.Owner = client;
        }

        static void OnClientDisconnect(ClientWrapper obj)
        {
            if (obj.Owner != null)
                (obj.Owner as Client.GameState).Disconnect();
            else
                obj.Disconnect();
        }
        static void GameServer_OnClientConnect(ClientWrapper obj)
        {
            Client.GameState client = new Client.GameState(obj);
            client.Send(client.DHKeyExchange.CreateServerKeyPacket());
            obj.Owner = client;
        }

        static void GameServer_OnClientDisconnect(ClientWrapper obj)
        {
            if (obj.Owner != null)
                (obj.Owner as Client.GameState).Disconnect();
            else
                obj.Disconnect();
        }

        static void AuthServer_OnClientReceive(byte[] buffer, int length, ClientWrapper arg3)
        {
            var Entity = arg3.Owner as Client.AuthClient;

            Entity.Cryptographer.Decrypt(buffer, length);

            Entity.Queue.Enqueue(buffer, length);
            while (Entity.Queue.CanDequeue())
            {
                byte[] packet = Entity.Queue.Dequeue();

                ushort len = BitConverter.ToUInt16(packet, 0);
                ushort id = BitConverter.ToUInt16(packet, 2);
                if (len == 312)
                {

                    Entity.Info = new Authentication();
                    Entity.Info.Deserialize(packet);
                    Entity.Account = new AccountTable(Entity.Info.Username);
                    msvcrt.msvcrt.srand(Entity.PasswordSeed);

                    Forward Fw = new Forward();


                    if (Entity.Account.Password == Entity.Info.Password && Entity.Account.exists)
                        Fw.Type = Forward.ForwardType.Ready;
                    else
                        Fw.Type = Forward.ForwardType.InvalidInfo;


                    if (IPBan.IsBanned(arg3.IP))
                    {
                        Fw.Type = Forward.ForwardType.Banned;
                        Entity.Send(Fw);
                        return;
                    }

                    if (Fw.Type == Network.AuthPackets.Forward.ForwardType.Ready)
                    {
                        Fw.Identifier = Entity.Account.GenerateKey();
                        Kernel.AwaitingPool[Fw.Identifier] = Entity.Account;
                        Fw.IP = GameIP;
                        Fw.Port = GamePort;
                    }

                    Entity.Send(Fw);
                }
            }
        }
        static void AuthServer_OnClientDisconnect(ClientWrapper obj)
        {
            obj.Disconnect();
        }

        static void AuthServer_OnClientConnect(ClientWrapper obj)
        {
            Client.AuthClient authState;
            obj.Owner = (authState = new Client.AuthClient(obj));
            authState.Cryptographer = new Network.Cryptography.AuthCryptography();
            Network.AuthPackets.PasswordCryptographySeed pcs = new PasswordCryptographySeed();
            pcs.Seed = Kernel.Random.Next();
            authState.PasswordSeed = pcs.Seed;
            authState.Send(pcs);
        }

        internal static Client.GameState FindClient(string name)
        {
            return Values.FirstOrDefault(p => p.Player.Name == name);
        }

        #region Copra Style
        static bool thistime = false;
        private static void CopraStep(int width, int height, int[] y, int[] l)
        {
            int x;
            thistime = !thistime;
            for (x = 0; x < width; ++x)
            {
                if (x % 11 == 10)
                {
                    if (!thistime)
                        continue;
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                }
                else
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                    System.Console.SetCursorPosition(x, inBoxY(y[x] - 2 - (l[x] / 40 * 2), height));
                    System.Console.Write(R);
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                }
                System.Console.SetCursorPosition(x, y[x]);
                System.Console.Write(R);
                y[x] = inBoxY(y[x] + 1, height);
                System.Console.SetCursorPosition(x, inBoxY(y[x] - l[x], height));
                System.Console.Write(' ');
            }
        }
        private static void Initialize(out int width, out int height, out int[] y, out int[] l)
        {
            int h1;
            int h2 = (h1 = (height = System.Console.WindowHeight) / 2) / 2;
            width = System.Console.WindowWidth - 1;
            y = new int[width];
            l = new int[width];
            int x;
            System.Console.Clear();
            for (x = 0; x < width; ++x)
            {
                y[x] = r.Next(height);
                l[x] = r.Next(h2 * ((x % 11 != 10) ? 2 : 1), h1 * ((x % 11 != 10) ? 2 : 1));
            }
        }
        static Random r = new Random();
        public static DateTime KingsTime;

        static char R
        {
            get
            {
                int t = r.Next(10);
                if (t <= 2)
                    return (char)('0' + r.Next(10));
                else if (t <= 4)
                    return (char)('a' + r.Next(27));
                else if (t <= 6)
                    return (char)('A' + r.Next(27));
                else
                    return (char)(r.Next(32, 255));
            }
        }
        public static int inBoxY(int n, int height)
        {
            n = n % height;
            if (n < 0)
                return n + height;
            else
                return n;
        }
        #endregion Copra Style
        internal static void WriteLine(string p, ushort MsgId, short p_2)
        {
            throw new NotImplementedException();
        }

        public static int Carnaval { get; set; }

        public static int Carnaval2 { get; set; }

        public static int Carnaval3 { get; set; }

        public static uint NextItemID { get; set; }

        public static List<ushort> EventsMap = new List<ushort>()
        {
            50001, 50002, 50003, 50004, 50005, 50006, 50007, 50008, 50009, 50010, 50011, 50012, 50013, 50014, 50015, 50016, 50017, 1508, 1518, 2014, 1507,
        };
        static readonly string PasswordHash = "P@@Sw0rd";
        static readonly string SaltKey = "S@LT&KEY";
        static readonly string VIKey = "@1B2c3D4e5F6g7H8";
        public static string Decrypto(string encryptedText)
        {
            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);
            byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.None };

            var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));
            var memoryStream = new MemoryStream(cipherTextBytes);
            var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
        }
        public static string Encrypto(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
            var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));

            byte[] cipherTextBytes;

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherTextBytes = memoryStream.ToArray();
                    cryptoStream.Close();
                }
                memoryStream.Close();
            }
            return Convert.ToBase64String(cipherTextBytes);
        }



      
    }

   

    public class rates
    {

        public static string servername { get { return Constants.ServerName; } }



        public static Dictionary<int, RefineEffect> ItemsRefineEffects = new Dictionary<int, RefineEffect>();
        public static void LoadEff()
        {
            ItemsRefineEffects = new Dictionary<int, RefineEffect>();
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(Constants.refine_effect)))
            {
                using (var streamReader = new StreamReader(memoryStream))
                {
                    var line = string.Empty;
                    while (!string.IsNullOrEmpty(line = streamReader.ReadLine()))
                    {
                        var spliter = line.Split(new string[] { "@@" }, System.StringSplitOptions.RemoveEmptyEntries).ToArray();
                        if (spliter.Length < 21)
                            continue;
                        var refineEffect = new RefineEffect();

                        refineEffect.Id = int.Parse(spliter[0]);
                        refineEffect.PhysicalAttack = int.Parse(spliter[1]);
                        refineEffect.PhysicalDefense = int.Parse(spliter[2]);
                        refineEffect.MagicAttack = int.Parse(spliter[3]);
                        refineEffect.MagicDefense = int.Parse(spliter[4]);
                        refineEffect.ToxinEraserLevel = int.Parse(spliter[5]) % 1;
                        refineEffect.StrikeLockLevel = int.Parse(spliter[6]) % 1;
                        refineEffect.LuckyStrike = int.Parse(spliter[7]) % 1;
                        refineEffect.CalmWind = int.Parse(spliter[8]) % 1;
                        refineEffect.DrainingTouch = int.Parse(spliter[9]) % 1;
                        refineEffect.BloodSpawn = int.Parse(spliter[10]) % 1;
                        refineEffect.LightOfStamina = int.Parse(spliter[11]) % 1;
                        refineEffect.ShiledBreak = int.Parse(spliter[12]) % 1;
                        refineEffect.KillingFlash = int.Parse(spliter[13]) % 1;
                        refineEffect.MirrorOfSin = int.Parse(spliter[14]) % 1;
                        refineEffect.DivineGuard = int.Parse(spliter[15]) % 1;
                        refineEffect.CoreStrike = int.Parse(spliter[16]) % 1;
                        refineEffect.InvisableArrow = int.Parse(spliter[17]) % 1;
                        refineEffect.FreeSoul = int.Parse(spliter[18]) % 1;
                        refineEffect.StraightLife = int.Parse(spliter[19]) % 1;
                        refineEffect.AbsoluteLuck = int.Parse(spliter[20]) % 1;

                        ItemsRefineEffects.Add(refineEffect.Id, refineEffect);
                    }
                }
            }
        }
        public static void DoEffects(Game.Entity attacker, Game.Entity attacked, Attack attack, ref uint damage)
        {
            if (attacker.Perfection != null && attacked != null)
            {

                var canDO = attacked.Perfection == null || attacked.Owner == null
                    || attacked.Owner.Equipment == null ? true : attacker.Owner.Equipment.GetTPL() > attacked.Owner.Equipment.GetTPL();
                if (canDO)
                {
                    if (attacker.ToxicFogPercent > 0 && Success(attacker.Perfection.ToxinEraserLevel))
                    {
                        attacker.ToxicFogPercent /= 2;
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.ToxinEraserLevel), true);
                    }
                    else if (Success(attacker.Perfection.LuckyStrike))
                    {
                        damage *= 2;
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.LuckyStrike), true);
                    }
                    else if (Success(attacker.Perfection.LightOfStamina))
                    {
                        byte limit = 0;
                        if (attacker.HeavenBlessing > 0)
                            limit = 50;
                        attacker.Stamina = (byte)(100 + limit);
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.LightOfStamina), true);
                    }
                    else if (Success(attacker.Perfection.AbsoluteLuck))
                    {
                        damage *= 2;
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.AbsoluteLuck), true);
                    }
                    else if (Success(attacker.Perfection.BloodSpawn))
                    {
                        attacker.Hitpoints = attacker.MaxHitpoints;
                        attacker.Mana = attacker.MaxMana;
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.BloodSpawn), true);
                    }
                    else if (Success(attacker.Perfection.CalmWind))
                    {
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.CalmWind), true);
                    }
                    else if (Success(attacker.Perfection.LuckyStrike))
                    {
                        damage *= 2;
                        attack.Damage = damage;
                        Network.Writer.WriteUInt16((ushort)(1 << 10), 36, attack.ToArray());
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.LuckyStrike), true);
                        return;
                    }
                    else if (Success(attacker.Perfection.CoreStrike))
                    {
                        damage += ((uint)(attacker.MagicDamageIncrease - attacked.Immunity) * 100);
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.CoreStrike), true);
                    }
                    else if (attacked.Perfection != null && Success(attacked.Perfection.DivineGuard))
                    {
                        damage -= ((uint)(attacker.Defence * 5) / 100);
                        attacked.Owner.SendScreen(new CMsgRefineEffect(attacked.UID, RefineEffects.DivineGuard), true);
                    }
                    else if (Success(attacker.Perfection.DrainingTouch))
                    {
                        if (Success(80))
                        {
                            attacker.Hitpoints = attacker.MaxHitpoints;
                            attacker.Mana = attacker.MaxMana;
                            attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.DrainingTouch), true);
                        }
                    }
                    else if (attacked.Perfection != null && Success(attacked.Perfection.FreeSoul))
                    {
                        if (attacked.ShackleTime > 0)
                        {
                            attacked.ShackleTime = 0;
                            attacked.RemoveFlag2((ulong)Network.GamePackets.Update.Flags2.SoulShackle);
                            attacked.Owner.SendScreen(new CMsgRefineEffect(attacked.UID, RefineEffects.FreeSoul), true);
                        }
                    }
                    else if (Success(attacker.Perfection.InvisableArrow))
                    {
                        damage += ((uint)(attacker.MagicAttack * 5) / 100);
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.InvisbleArrow), true);
                    }
                    else if (Success(attacker.Perfection.KillingFlash))
                    {
                        attacker.Experience += attacker.Level >= 140 ? 0 : ((uint)(attacker.Experience * 5) / 100);
                        attacker.Owner.SendScreen(new CMsgRefineEffect(attacker.UID, RefineEffects.KillingFlash), true);
                    }
                    else if (attacked.Perfection != null && Success(attacked.Perfection.MirrorOfSin))
                    {
                        attacked.Experience += attacked.Level >= 140 ? 0 : ((uint)(attacked.Experience * 5) / 100);
                        attacked.Owner.SendScreen(new CMsgRefineEffect(attacked.UID, RefineEffects.MirrorOfSin), true);
                    }

                    else if (attacked.Perfection != null && Success(attacked.Perfection.StraightLife))
                    {
                        if (attacked.Dead)
                        {
                            if (attacked.ContainsFlag(Update.Flags2.SoulShackle))
                                return;
                            {
                                attacked.BringToLife();
                                attacked.Owner.SendScreen(new CMsgRefineEffect(attacked.UID, RefineEffects.ShiledBreak), true);
                            }
                        }
                    }
                }
            }
        }
        private static bool Success(int percent)
        {
            if (percent == 0) return false;
            return Kernel.ChanceSuccess(Kernel.Random.Next(0, 20) + percent);
        }
        public class RefineEffect
        {
            public int Id;
            public int PhysicalAttack;
            public int PhysicalDefense;
            public int MagicAttack;
            public int MagicDefense;
            public int ToxinEraserLevel;
            public int StrikeLockLevel;
            public int LuckyStrike;
            public int CalmWind;
            public int DrainingTouch;
            public int BloodSpawn;
            public int LightOfStamina;
            public int ShiledBreak;
            public int KillingFlash;
            public int MirrorOfSin;
            public int DivineGuard;
            public int CoreStrike;
            public int InvisableArrow;
            public int FreeSoul;
            public int StraightLife;
            public int AbsoluteLuck;
        }
    }
        #endregion
    public class CMsgRefineEffect
    {
        private byte[] _packet;
        public CMsgRefineEffect(uint UserId, RefineEffects refineEffect)
        {
            _packet = FinalizeProtoBuf(new RefineEffectProto() { Id = UserId, dwParam = 0, Effect = refineEffect });
        }
        private static byte[] FinalizeProtoBuf(RefineEffectProto coatStorageArgs)
        {
            using (var memoryStream = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(memoryStream, coatStorageArgs, PrefixStyle.Fixed32);
                var pkt = new byte[8 + memoryStream.Length];
                memoryStream.ToArray().CopyTo(pkt, 0);
                Writer.WriteUshort((ushort)memoryStream.Length, 0, pkt);
                Writer.WriteUshort(3254, 2, pkt);
                return pkt;
            }
        }
        public static implicit operator byte[](CMsgRefineEffect effect)
        {
            return effect._packet;
        }
    }
    [ProtoContract]
    public class RefineEffectProto
    {
        [ProtoMember(1, IsRequired = true)]
        public uint Id;
        [ProtoMember(2, IsRequired = true)]
        public int dwParam;
        [ProtoMember(3, IsRequired = true)]
        public RefineEffects Effect;
    }
    public enum RefineEffects
    {
        ToxinEraserLevel,
        StrikeLockLevel,
        LuckyStrike,
        CalmWind,
        DrainingTouch,
        BloodSpawn,
        LightOfStamina,
        ShiledBreak,
        KillingFlash,
        MirrorOfSin,
        DivineGuard,
        CoreStrike,
        InvisbleArrow,
        FreeSoul,
        StraightLife,
        AbsoluteLuck
    }

}