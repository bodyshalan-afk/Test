using MrRayzo.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MrRayzo.Network.GamePackets;
using System.Threading;
using System.Threading.Generic;
using MrRayzo.Network.Sockets;
using MrRayzo.Game.ConquerStructures;
using MrRayzo.Game.ConquerStructures.Society;
using MrRayzo.Client;
using System.Drawing;
using MrRayzo.Network.GamePackets.EventAlert;
using MrRayzo.Game.Events;
using MrRayzo.Database;
using System.Data.SqlClient;
using System.Configuration;
using MrRayzo.Copra;
using MrRayzo.Game.Features.Tournaments;
using MrRayzo.Interfaces;
using MrRayzo.MaTrix;

namespace MrRayzo
{
    public class World
    {
       
        public const int PoleGuard = 3;
        public static ProjectX_V3_Lib.ScriptEngine.ScriptEngine ScriptEngine;
       
        #region Cyclone War
        public static bool cycolne = false;
        public static bool cycolne1 = false;
        public static bool LastTeam = false;
        #endregion Cyclone War
        public static bool Faster = false;
        public static bool Faster1 = false;
        public static bool Faster2 = false;
        public static StaticPool GenericThreadPool;
        public static StaticPool ReceivePool, SendPool;
        public TimerRule<GameState> Buffers, Characters, AutoAttack, Prayer;
        public TimerRule<ClientWrapper> ConnectionReceive, ConnectionReview, ConnectionSend;

        public const uint
            NobilityMapBase = 700,
            ClassPKMapBase = 1730;
        public SteedRace SteedRace;
        public List<KillTournament> Tournaments;
        public CaptureTheFlag CTF;
        private bool UnionWarAI;
        public bool PureLand;
        public HeroOfGame HeroOfGame = new HeroOfGame();

        public DelayedTask DelayedTask;
        public World()
        {
            GenericThreadPool = new StaticPool(32).Run();
            ReceivePool = new StaticPool(32).Run();
            SendPool = new StaticPool(32).Run();
        }

        public void Init(bool onlylogin = false)
        {
            if (!onlylogin)
            {
                
                Buffers = new TimerRule<GameState>(BuffersCallback, 1000, ThreadPriority.BelowNormal);
                Characters = new TimerRule<GameState>(CharactersCallback, 1000, ThreadPriority.BelowNormal);
                AutoAttack = new TimerRule<GameState>(AutoAttackCallback, 1000, ThreadPriority.BelowNormal);
                Prayer = new TimerRule<GameState>(PrayerCallback, 1000, ThreadPriority.BelowNormal);
           
                Subscribe(WorldTournaments, 1000);
                Subscribe(ServerFunctions, 5000);
                Subscribe(ArenaFunctions, 1000, ThreadPriority.AboveNormal);
            }
            ConnectionReview = new TimerRule<ClientWrapper>(connectionReview, 60000, ThreadPriority.Lowest);
            ConnectionReceive = new TimerRule<ClientWrapper>(connectionReceive, 1);
            ConnectionSend = new TimerRule<ClientWrapper>(connectionSend, 1);
        }

        public void CreateTournaments()
        {
            DelayedTask = new DelayedTask();
            var map = Kernel.Maps[700];
            ClanWarArena.Create();
            Game.Features.Tournaments.TeamPk.TeamTournament.Create();
            Game.Features.Tournaments.SkillPk.SkillTournament.Create();
              new Game.StatuesWar();
            new GuildScoreWar();
            new ThunderScoreWar();
           
            SteedRace = new SteedRace();
            new ClassPoleWar();
            new NobiltyPoleWar();
            new PowerX();
            ElitePKTournament.Create();
            CTF = new CaptureTheFlag();

        }

        public bool Register(GameState client)
        {
            if (client.TimerSubscriptions == null)
            {
                client.TimerSyncRoot = new object();
                client.TimerSubscriptions = new IDisposable[]
                {                    
                    Buffers.Add(client),
                    Characters.Add(client),
                    AutoAttack.Add(client),                    
                    Prayer.Add(client),

                };
                return true;
            }
           
            return false;
        }
        public void Unregister(GameState client)
        {
            if (client.TimerSubscriptions == null) return;
            lock (client.TimerSyncRoot)
            {
                if (client.TimerSubscriptions != null)
                {
                    foreach (var timer in client.TimerSubscriptions)
                        timer.Dispose();
                    client.TimerSubscriptions = null;
                }
            }
        }
        private bool Valid(GameState client)
        {
            if (!client.Socket.IsAlive || client.Player == null)
            {
                client.Disconnect();
                return false;
            }
            return true;
        }
        private void BuffersCallback(GameState client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now = new Time32(time);
            #region VIPDays
            if (client.VIPDays.Level > 0 && Time32.Now >= client.VIPDays.CheckStamp.AddMinutes(10))
            {

                client.VIPDays.Check(client);
            }
            #endregion
            
            #region XpBlueStamp
            if (client.Player.ContainsFlag3(Update.Flags3.WarriorEpicShield))
            {
                if (Time32.Now > client.Player.XpBlueStamp.AddSeconds(33))
                {
                    client.Player.ShieldIncrease = 0;
                    client.Player.ShieldTime = 0;
                    client.Player.MagicShieldIncrease = 0;
                    client.Player.MagicShieldTime = 0;
                    client.Player.RemoveFlag3(Update.Flags3.WarriorEpicShield);
                }
            }
            #endregion
            #region JiangHu
            if (client.Player.MyJiang != null)
                client.Player.MyJiang.TheadTime(client);
            #endregion
            #region Arena Quit
            if (client.InArenaQualifier() && client.Map.BaseID != 700)
            {
                Game.Arena.QualifyEngine.DoGiveUp(client);
            }
            #endregion
            #region Aura
            if (client.Player.Aura_isActive)
            {
                if (client.Player.Aura_isActive)
                {
                    if (Time32.Now >= client.Player.AuraStamp.AddSeconds(client.Player.AuraTime))
                    {
                        client.Player.RemoveFlag2(client.Player.Aura_actType);
                        client.removeAuraBonuses(client.Player.Aura_actType, client.Player.Aura_actPower, 1);
                        client.Player.Aura_isActive = false;
                        client.Player.AuraTime = 0;
                        client.Player.Aura_actType = 0;
                        client.Player.Aura_actPower = 0;
                        client.Player.Aura_actLevel = 0;
                    }
                }
            }
            #endregion
            #region Bless
            if (client.Player.ContainsFlag((ulong)Update.Flags.CastPray))
            {
                if (client.BlessTime <= 7198500)
                    client.BlessTime += 1000;
                else
                    client.BlessTime = 7200000;
                client.Player.Update((byte)Update.DataType.LuckyTimeTimer, client.BlessTime, false);
            }
            else if (client.Player.ContainsFlag((ulong)Update.Flags.Praying))
            {
                if (client.PrayLead != null)
                {
                    if (client.PrayLead.Socket.IsAlive)
                    {
                        if (client.BlessTime <= 7199000)
                            client.BlessTime += 500;
                        else
                            client.BlessTime = 7200000;
                        client.Player.Update((byte)Update.DataType.LuckyTimeTimer, client.BlessTime, false);
                    }
                    else
                        client.Player.RemoveFlag((ulong)Update.Flags.Praying);
                }
                else
                    client.Player.RemoveFlag((ulong)Update.Flags.Praying);
            }
            else
            {
                if (client.BlessTime > 0)
                {
                    if (client.BlessTime >= 500)
                        client.BlessTime -= 500;
                    else
                        client.BlessTime = 0;
                    client.Player.Update((byte)Update.DataType.LuckyTimeTimer, client.BlessTime, false);
                }
            }
            #endregion
            #region Flashing name
            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.FlashingName))
            {
                if (Now > client.Player.FlashingNameStamp.AddSeconds(client.Player.FlashingNameTime))
                {
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.FlashingName);
                }
            }
            #endregion
            #region XPList
            if (!client.Player.ContainsFlag(Network.GamePackets.Update.Flags.XPList))
            {
                if (Now > client.XPCountStamp.AddSeconds(3))
                {
                    #region Arrows
                    if (client.Equipment != null)
                    {
                        if (!client.Equipment.Free(5))
                        {
                            if (Network.PacketHandler.IsArrow(client.Equipment.TryGetItem(5).ID))
                            {
                                Database.ConquerItemTable.UpdateDurabilityItem(client.Equipment.TryGetItem(5));
                            }
                        }
                    }
                    #endregion
                    client.XPCountStamp = Now;
                    client.XPCount++;
                    if (client.XPCount >= 100)
                    {
                        client.Player.AddFlag(Network.GamePackets.Update.Flags.XPList);
                        client.XPCount = 0;
                        client.XPListStamp = Now;
                    }
                }
            }
            else
            {
                if (Now > client.XPListStamp.AddSeconds(20))
                {
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.XPList);
                }
            }
            #endregion
            #region KOSpell
            if (client.Player.OnKOSpell())
            {
                if (client.Player.OnCyclone())
                {
                    int Seconds = Now.AllSeconds() - client.Player.CycloneStamp.AddSeconds(client.Player.CycloneTime).AllSeconds();
                    if (Seconds >= 1)
                    {
                        client.Player.RemoveFlag(Network.GamePackets.Update.Flags.Cyclone);
                    }
                }
                if (client.Player.OnSuperman())
                {
                    int Seconds = Now.AllSeconds() - client.Player.SupermanStamp.AddSeconds(client.Player.SupermanTime).AllSeconds();
                    if (Seconds >= 1)
                    {
                        client.Player.RemoveFlag(Network.GamePackets.Update.Flags.Superman);
                    }
                }
                if (client.Player.OnSuperCyclone())
                {
                    int Seconds = Now.AllSeconds() - client.Player.SuperCycloneStamp.AddSeconds(client.Player.SuperCycloneTime).AllSeconds();
                    if (Seconds >= 1)
                    {
                        client.Player.RemoveFlag3(Network.GamePackets.Update.Flags3.SuperCyclone);
                    }
                }
                if (client.Player.OnDragonCyclone())
                {
                    int Seconds = Now.AllSeconds() - client.Player.DragonCycloneStamp.AddSeconds(client.Player.DragonCycloneTime).AllSeconds();
                    if (Seconds >= 1)
                    {
                        client.Player.RemoveFlag3((ulong)Update.Flags3.DragonCyclone);
                    }
                }
                if (!client.Player.OnKOSpell())
                {
                    client.Player.KOCount = 0;
                }
            }
            #endregion
            #region Buffers
            if (client.Player.Aura_isActive)
            {
                if (Now >= client.Player.AuraStamp.AddSeconds(client.Player.AuraTime) || client.Player.Dead)
                {

                    client.Player.AuraTime = 0;
                    client.Player.Aura_isActive = false;
                    Update.AuraType aura = Update.AuraType.TyrantAura;
                    switch (client.Player.Aura_actType)
                    {
                        case Update.Flags2.EarthAura: aura = Update.AuraType.EarthAura; break;
                        case Update.Flags2.FireAura: aura = Update.AuraType.FireAura; break;
                        case Update.Flags2.WaterAura: aura = Update.AuraType.WaterAura; break;
                        case Update.Flags2.WoodAura: aura = Update.AuraType.WoodAura; break;
                        case Update.Flags2.MetalAura: aura = Update.AuraType.MetalAura; break;
                        case Update.Flags2.FendAura: aura = Update.AuraType.FendAura; break;
                        case Update.Flags2.TyrantAura: aura = Update.AuraType.TyrantAura; break;
                    }
                    new Update(true).Aura(client.Player, Update.AuraDataTypes.Remove, aura, client.Player.Aura_actLevel, client.Player.Aura_actPower);

                    client.removeAuraBonuses(client.Player.Aura_actType, client.Player.Aura_actPower, 1);
                    client.Player.RemoveFlag2(client.Player.Aura_actType);
                    client.Player.RemoveFlag2(client.Player.Aura_actType2);
                    client.Player.Aura_actType = 0;
                    client.Player.Aura_actType2 = 0;
                    client.Player.Aura_actPower = 0;
                    client.Player.Aura_actLevel = 0;
                }


            }

            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.Stigma))
            {
                if (Now >= client.Player.StigmaStamp.AddSeconds(client.Player.StigmaTime))
                {
                    client.Player.StigmaTime = 0;
                    client.Player.StigmaIncrease = 0;
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.Stigma);
                }
            }
            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.Dodge))
            {
                if (Now >= client.Player.DodgeStamp.AddSeconds(client.Player.DodgeTime))
                {
                    client.Player.DodgeTime = 0;
                    client.Player.DodgeIncrease = 0;
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.Dodge);
                }
            }
            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.Invisibility))
            {
                if (Now >= client.Player.InvisibilityStamp.AddSeconds(client.Player.InvisibilityTime))
                {
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.Invisibility);
                }
            }
            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.StarOfAccuracy))
            {
                if (client.Player.StarOfAccuracyTime != 0)
                {
                    if (Now >= client.Player.StarOfAccuracyStamp.AddSeconds(client.Player.StarOfAccuracyTime))
                    {
                        client.Player.RemoveFlag(Network.GamePackets.Update.Flags.StarOfAccuracy);
                    }
                }
                else
                {
                    if (Now >= client.Player.AccuracyStamp.AddSeconds(client.Player.AccuracyTime))
                    {
                        client.Player.RemoveFlag(Network.GamePackets.Update.Flags.StarOfAccuracy);
                    }
                }
            }
            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.MagicShield))
            {
                if (client.Player.MagicShieldTime != 0)
                {
                    if (Now >= client.Player.MagicShieldStamp.AddSeconds(client.Player.MagicShieldTime))
                    {
                        client.Player.MagicShieldIncrease = 0;
                        client.Player.MagicShieldTime = 0;
                        client.Player.RemoveFlag(Network.GamePackets.Update.Flags.MagicShield);
                    }
                }
                else
                {
                    if (Now >= client.Player.ShieldStamp.AddSeconds(client.Player.ShieldTime))
                    {
                        client.Player.ShieldIncrease = 0;
                        client.Player.ShieldTime = 0;
                        client.Player.RemoveFlag(Network.GamePackets.Update.Flags.MagicShield);
                    }
                }
            }
            #endregion
            #region AuroraLotus
            if (client.Spells.ContainsKey(12370))
            {
                if (!client.Player.ContainsFlag3(Update.Flags3.AuroraLotus))
                {
                    client.Player.AuroraLotusEnergy = 0;
                    if (client.Player.Lotus(client.Player.AuroraLotusEnergy, Update.AuroraLotus))
                        client.Player.AddFlag3(Update.Flags3.AuroraLotus);
                }

            }
            #endregion AuroraLotus
            #region FlameLotus
            if (client.Spells.ContainsKey(12380))
            {
                if (!client.Player.ContainsFlag3(Update.Flags3.FlameLotus))
                {
                    client.Player.AuroraLotusEnergy = 0;
                    if (client.Player.Lotus(client.Player.AuroraLotusEnergy, Update.FlameLotus))
                        client.Player.AddFlag3(Update.Flags3.FlameLotus);
                }
            }
            #endregion FlameLotus
           
            client.CheckTeamAura();
            #region Fly
            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.Fly))
            {
                if (Now >= client.Player.FlyStamp.AddSeconds(client.Player.FlyTime))
                {
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.Fly);
                    client.Player.FlyTime = 0;
                }
            }
            #endregion
            #region PoisonStar
            if (client.Player.NoDrugsTime > 0)
            {
                if (Now > client.Player.NoDrugsStamp.AddSeconds(client.Player.NoDrugsTime))
                {
                    client.Player.NoDrugsTime = 0;
                    client.Player.RemoveFlag2(Update.Flags2.EffectBall);
                }
            }
            #endregion
            #region ToxicFog
            if (client.Player.ToxicFogLeft > 0)
            {
                if (Now >= client.Player.ToxicFogStamp.AddSeconds(2))
                {
                    float Percent = client.Player.ToxicFogPercent;
                    if (client.Player.Detoxication != 0)
                    {
                        float immu = 1 - client.Player.Detoxication / 100F;
                        Percent = Percent * immu;
                    }
                    client.Player.ToxicFogLeft--;
                    if (client.Player.ToxicFogLeft == 0)
                    {
                        client.Player.RemoveFlag(Update.Flags.Poisoned);
                        return;
                    }
                    client.Player.ToxicFogStamp = Now;
                    if (client.Player.Hitpoints > 1)
                    {
                        uint damage = Game.Attacking.Calculate.Percent(client.Player, Percent);
                        if (client.Player.ContainsFlag2(Network.GamePackets.Update.Flags2.AzureShield))
                        {

                            if (damage > client.Player.AzureShieldDefence)
                            {
                                damage -= client.Player.AzureShieldDefence;
                                Game.Attacking.Calculate.CreateAzureDMG(client.Player.AzureShieldDefence, client.Player, client.Player);
                                client.Player.RemoveFlag2(Network.GamePackets.Update.Flags2.AzureShield);
                            }
                            else
                            {
                                Game.Attacking.Calculate.CreateAzureDMG((uint)damage, client.Player, client.Player);
                                client.Player.AzureShieldDefence -= (ushort)damage;
                                client.Player.AzureShieldPacket();
                                damage = 1;
                            }
                        }
                        else
                            client.Player.Hitpoints -= damage;

                        Network.GamePackets.SpellUse suse = new Network.GamePackets.SpellUse(true);
                        suse.Attacker = client.Player.UID;
                        suse.SpellID = 10010;
                        suse.AddTarget(client.Player, damage, null);
                        client.SendScreen(suse, true);
                        if (client != null)
                            client.UpdateQualifier(damage, true);

                    }
                }
            }
            else
            {
                if (client.Player.ContainsFlag(Update.Flags.Poisoned))
                    client.Player.RemoveFlag(Update.Flags.Poisoned);
            }
            #endregion
            #region lianhuaran
            if (client.Player.lianhuaranLeft > 0)
            {
                if (Now >= client.Player.lianhuaranStamp.AddSeconds(2))
                {
                    float Percent = client.Player.lianhuaranPercent;
                    if (client.Player.Detoxication != 0)
                    {
                        float immu = 1 - client.Player.Detoxication / 100F;
                        Percent = Percent * immu;
                    }
                    client.Player.lianhuaranLeft--;
                    if (client.Player.lianhuaranLeft == 0)
                    {
                        client.Player.RemoveFlag3(Update.Flags3.lianhuaran01);
                        client.Player.RemoveFlag3(Update.Flags3.lianhuaran02);
                        client.Player.RemoveFlag3(Update.Flags3.lianhuaran03);
                        client.Player.RemoveFlag3(Update.Flags3.lianhuaran04);
                        return;
                    }
                    client.Player.lianhuaranStamp = Now;
                    if (client.Player.Hitpoints > 1)
                    {
                        uint damage = Game.Attacking.Calculate.Percent(client.Player, Percent);
                        if (client.Player.ContainsFlag2(Network.GamePackets.Update.Flags2.AzureShield))
                        {

                            if (damage > client.Player.AzureShieldDefence)
                            {
                                damage -= client.Player.AzureShieldDefence;
                                Game.Attacking.Calculate.CreateAzureDMG(client.Player.AzureShieldDefence, client.Player, client.Player);
                                client.Player.RemoveFlag2(Network.GamePackets.Update.Flags2.AzureShield);
                            }
                            else
                            {
                                Game.Attacking.Calculate.CreateAzureDMG((uint)damage, client.Player, client.Player);
                                client.Player.AzureShieldDefence -= (ushort)damage;
                                client.Player.AzureShieldPacket();
                                damage = 1;
                            }
                        }
                        else
                            client.Player.Hitpoints -= damage;


                        client.UpdateQualifier(damage, true);

                    }
                }
            }
            else
            {
                if (client.Player.ContainsFlag3(Update.Flags3.lianhuaran01))
                    client.Player.RemoveFlag3(Update.Flags3.lianhuaran01);
                if (client.Player.ContainsFlag3(Update.Flags3.lianhuaran02))
                    client.Player.RemoveFlag3(Update.Flags3.lianhuaran02);
                if (client.Player.ContainsFlag3(Update.Flags3.lianhuaran03))
                    client.Player.RemoveFlag3(Update.Flags3.lianhuaran03);
                if (client.Player.ContainsFlag3(Update.Flags3.lianhuaran04))
                    client.Player.RemoveFlag3(Update.Flags3.lianhuaran04);

            }
            #endregion
            #region FatalStrike
            if (client.Player.OnFatalStrike())
            {
                if (Now > client.Player.FatalStrikeStamp.AddSeconds(client.Player.FatalStrikeTime))
                {
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.FatalStrike);
                }
            }
            #endregion
            #region Oblivion
            if (client.Player.OnOblivion())
            {
                if (Now > client.Player.OblivionStamp.AddSeconds(client.Player.OblivionTime))
                {
                    client.Player.RemoveFlag2(Network.GamePackets.Update.Flags2.Oblivion);
                }
            }
            #endregion
            #region ShurikenVortex
            if (client.Player.ContainsFlag(Network.GamePackets.Update.Flags.ShurikenVortex))
            {
                if (Now > client.Player.ShurikenVortexStamp.AddSeconds(client.Player.ShurikenVortexTime))
                {
                    client.Player.RemoveFlag(Network.GamePackets.Update.Flags.ShurikenVortex);
                }
            }
            #endregion
            #region Transformations
            if (client.Player.Transformed)
            {
                if (Now > client.Player.TransformationStamp.AddSeconds(client.Player.TransformationTime))
                {
                    client.Player.Untransform();
                }
            }
            #endregion
            #region soulshackle
            if (client.Player.ContainsFlag2(Network.GamePackets.Update.Flags2.SoulShackle))
            {
                if (Now > client.Player.ShackleStamp.AddSeconds(client.Player.ShackleTime))
                {
                    client.Player.RemoveFlag2(Network.GamePackets.Update.Flags2.SoulShackle);
                }
            }
            #endregion
            #region Twin  Effect
            if (client.Player.MapID == 1002)
            {
                if (Kernel.GetDistance(client.Player.X, client.Player.Y, 312, 279) < 17 && !client.Effect)
                {
                    client.Effect = true;
                    if (client.Player.MapID == 1002)
                    {
                        Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                        floorItem.ItemID = 1024;
                        floorItem.MapID = 1002;
                        floorItem.X = 312;
                        floorItem.Y = 279;
                        floorItem.Type = Network.GamePackets.FloorItem.Effect;
                        client.Send(floorItem);
                    }
                }
                else
                {
                    if (Kernel.GetDistance(client.Player.X, client.Player.Y, 312, 279) > 17)
                    {
                        client.Effect = false;
                    }
                }
            }
            #endregion
            
            #region StoneTask
            if (DateTime.Now.Hour == 15 && DateTime.Now.Minute == 00 && DateTime.Now.Second <= 5)
                if (client.Player.MapID == 8881)
                {
                    client.Player.Teleport(1002, 300, 270);
                    Kernel.SendWorldMessage(new Message("StoneTask is! End! Will See You NexTime . ! ", Color.Red, Message.Center), Kernel.GamePool.Values.ToArray());

                }
            #endregion
            #region Prefction
            if (DateTime.Now.Hour == 14 && DateTime.Now.Minute == 29 && DateTime.Now.Second == 55)
            if (client.Player.MapID == 40201 || client.Player.MapID == 40202 || client.Player.MapID == 40203)
            {
                client.Player.Teleport(1002, 300, 270);
                Kernel.SendWorldMessage(new Message("Qouest Prection! End! Will See You NexTime . ! ", Color.Red, Message.Center), Kernel.GamePool.Values.ToArray());

            }
            #endregion
         
            #region Prefction
            if (DateTime.Now.Hour == 13 && DateTime.Now.Minute == 59 && DateTime.Now.Second == 55)
                if (client.Player.MapID == 4020 )
                {
                    client.Player.Teleport(1002, 300, 270);
                    Kernel.SendWorldMessage(new Message("Qouest Prection! End! For You > Not Get 1000 Point Will See You NexTime . ! ", Color.Red, Message.Center), Kernel.GamePool.Values.ToArray());

                }
            #endregion
            #region Intensify
            if (client.Player.IntensifyPercent != 0)
            {
                if (Now > client.Player.IntensifyStamp.AddSeconds(5))
                {
                    client.Player.AddFlag(Update.Flags.Intensify);
                }
            }
            #endregion
            #region Save
            if (DateTime.Now.Minute == 42)
            {
                Program.Save();
                //Console.WriteLine("Save Done By:Rayzo");
            }
            #endregion  
            #region AzureShield
            if (client.Player.ContainsFlag2(Network.GamePackets.Update.Flags2.AzureShield))
            {
                if (Now > client.Player.MagicShieldStamp.AddSeconds(client.Player.MagicShieldTime))
                {
                    client.Player.RemoveFlag2(Network.GamePackets.Update.Flags2.AzureShield);
                }
            }
            #endregion
            #region Blade Flurry
            if (client.Player.ContainsFlag3(Update.Flags3.BladeFlurry))
            {
                if (Time32.Now > client.Player.BladeFlurryStamp.AddSeconds(45))
                {
                    client.Player.RemoveFlag3(Update.Flags3.BladeFlurry);
                }
            }
            #endregion
            #region Flustered
            if (client.Player.ContainsFlag(Update.Flags.Frightened))
            {
                if (client.RaceFrightened)
                {
                    if (Now > client.FrightenStamp.AddSeconds(20))
                    {
                        client.RaceFrightened = false;
                        {
                            GameCharacterUpdates update = new GameCharacterUpdates(true);
                            update.UID = client.Player.UID;
                            update.Remove(GameCharacterUpdates.Flustered);
                            client.SendScreen(update, true);
                        }
                        client.Player.RemoveFlag(Update.Flags.Frightened);
                    }
                    else
                    {
                        int rand;
                        ushort x, y;
                        do
                        {
                            rand = Kernel.Random.Next(Game.Map.XDir.Length);
                            x = (ushort)(client.Player.X + Game.Map.XDir[rand]);
                            y = (ushort)(client.Player.Y + Game.Map.YDir[rand]);
                        }
                        while (!client.Map.Floor[x, y, MapObjectType.Entity]);
                        client.Player.Facing = Kernel.GetAngle(
                            client.Player.X, client.Player.Y, x, y);
                        client.Player.X = x;
                        client.Player.Y = y;

                        client.SendScreen(
                            new TwoMovements()
                            {
                                EntityCount = 1,
                                Facing = client.Player.Facing,
                                FirstEntity = client.Player.UID,
                                WalkType = 9,
                                X = client.Player.X,
                                Y = client.Player.Y,
                                MovementType = TwoMovements.Walk
                            }, true);
                    }
                }
            }
            #endregion
            #region Stunned
            if (client.Player.Stunned)
            {
                if (Now > client.Player.StunStamp.AddMilliseconds(2000))
                {
                    client.Player.Stunned = false;
                }
            }
            #endregion
            #region Frozen
            if (client.Player.ContainsFlag(Update.Flags.Freeze))
            {
                if (Now > client.Player.FrozenStamp.AddSeconds(client.Player.FrozenTime))
                {
                    client.Player.FrozenD = false;
                    client.Player.FrozenTime = 0;
                    client.Player.RemoveFlag(Update.Flags.Freeze);

                    GameCharacterUpdates update = new GameCharacterUpdates(true);
                    update.UID = client.Player.UID;
                    update.Remove(GameCharacterUpdates.Freeze);
                    client.SendScreen(update, true);
                }
            }
            #endregion
            #region IceBlock
            if (client.Player.ContainsFlag((ulong)Update.Flags.FreezeSmall))
            {
                if (Now > client.FrightenStamp.AddSeconds(client.Player.Fright))
                {
                    GameCharacterUpdates update = new GameCharacterUpdates(true);
                    update.UID = client.Player.UID;
                    update.Remove(GameCharacterUpdates.Dizzy);
                    client.SendScreen(update, true);
                    client.Player.RemoveFlag((ulong)Update.Flags.FreezeSmall);
                }
                else
                {
                    int rand;
                    ushort x, y;
                    do
                    {
                        rand = Kernel.Random.Next(Game.Map.XDir.Length);
                        x = (ushort)(client.Player.X + Game.Map.XDir[rand]);
                        y = (ushort)(client.Player.Y + Game.Map.YDir[rand]);
                    }
                    while (!client.Map.Floor[x, y, MapObjectType.Entity]);
                    client.Player.Facing = Kernel.GetAngle(client.Player.X, client.Player.Y, x, y);
                    client.Player.X = x;
                    client.Player.Y = y;
                    client.SendScreen(new TwoMovements()
                    {
                        EntityCount = 1,
                        Facing = client.Player.Facing,
                        FirstEntity = client.Player.UID,
                        WalkType = 9,
                        X = client.Player.X,
                        Y = client.Player.Y,
                        MovementType = TwoMovements.Walk
                    }, true);
                }
            }
            #endregion
            #region Dizzy
            if (client.Player.ContainsFlag(Update.Flags.Dizzy))
            {
                if (client.RaceDizzy)
                {
                    if (Now > client.DizzyStamp.AddSeconds(5))
                    {
                        client.RaceDizzy = false;
                        {
                            GameCharacterUpdates update = new GameCharacterUpdates(true);
                            update.UID = client.Player.UID;
                            update.Remove(GameCharacterUpdates.Dizzy);
                            client.SendScreen(update);
                        }
                        client.Player.RemoveFlag(Update.Flags.Dizzy);
                    }
                }
            }
            #endregion
            #region Confused
            if (client.Player.ContainsFlag(Update.Flags.Confused))
            {
                if (Now > client.FrightenStamp.AddSeconds(15))
                {
                    client.RaceFrightened = false;
                    {
                        GameCharacterUpdates update = new GameCharacterUpdates(true);
                        update.UID = client.Player.UID;
                        update.Remove(GameCharacterUpdates.Flustered);
                        client.SendScreen(update);
                    }
                    client.Player.RemoveFlag(Update.Flags.Confused);
                }
            }
            #endregion
            #region Divine Shield
            if (client.Player.ContainsFlag(Update.Flags.DivineShield))
            {
                if (Now > client.GuardStamp.AddSeconds(10))
                {
                    client.RaceGuard = false;
                    {
                        GameCharacterUpdates update = new GameCharacterUpdates(true);
                        update.UID = client.Player.UID;
                        update.Remove(GameCharacterUpdates.DivineShield);
                        client.SendScreen(update);
                    }
                    client.Player.RemoveFlag(Update.Flags.DivineShield);
                }
            }
            #endregion
            #region Extra Speed
            if (client.Player.ContainsFlag(Update.Flags.OrangeSparkles) && !client.InQualifier())
            {
                if (Time32.Now > client.RaceExcitementStamp.AddSeconds(15))
                {
                    var upd = new GameCharacterUpdates(true)
                    {
                        UID = client.Player.UID
                    };
                    upd.Remove(GameCharacterUpdates.Accelerated);
                    client.SendScreen(upd);
                    client.SpeedChange = null;
                    client.Player.RemoveFlag(Update.Flags.OrangeSparkles);
                }
            }
            #endregion
            #region Decelerated
            if (client.Player.ContainsFlag(Update.Flags.PurpleSparkles) && !client.InQualifier())
            {
                if (Time32.Now > client.DecelerateStamp.AddSeconds(10))
                {
                    {
                        client.RaceDecelerated = false;
                        var upd = new GameCharacterUpdates(true)
                        {
                            UID = client.Player.UID
                        };
                        upd.Remove(GameCharacterUpdates.Decelerated);
                        client.SendScreen(upd);
                        client.SpeedChange = null;
                    }
                    client.Player.RemoveFlag(Update.Flags.PurpleSparkles);
                }
            }
            #endregion
            #region ShockDaze
            if (client.Player.ContainsFlag((ulong)Update.Flags.Stun))
            {
                if (Now > client.Player.ShockStamp.AddSeconds(client.Player.Shock))
                {
                    client.Player.RemoveFlag((ulong)Update.Flags.Stun);
                }
            }
            #endregion
            #region ChaosCycle
            if (client.Player.ContainsFlag((ulong)Update.Flags.ChaosCycle))
            {
                if (Now > client.FrightenStamp.AddSeconds(5))
                {
                    client.RaceFrightened = false;
                    {
                        GameCharacterUpdates update = new GameCharacterUpdates(true);
                        update.UID = client.Player.UID;
                        update.Remove(GameCharacterUpdates.Flustered);
                        client.SendScreen(update);
                    }
                    client.Player.RemoveFlag((ulong)Update.Flags.ChaosCycle);
                }
            }
            #endregion
            #region FreezeSmall
            if (client.Player.ContainsFlag((ulong)Update.Flags.FreezeSmall))
            {
                {
                    if (Now > client.FrightenStamp.AddSeconds(20))
                    {
                        client.RaceFrightened = false;
                        {
                            GameCharacterUpdates update = new GameCharacterUpdates(true);
                            update.UID = client.Player.UID;
                            update.Remove(GameCharacterUpdates.Flustered);
                            client.SendScreen(update, true);
                        }
                        client.Player.RemoveFlag((ulong)Update.Flags.FreezeSmall);
                    }
                    else
                    {
                        int rand;
                        ushort x, y;
                        do
                        {
                            rand = Kernel.Random.Next(Game.Map.XDir.Length);
                            x = (ushort)(client.Player.X + Game.Map.XDir[rand]);
                            y = (ushort)(client.Player.Y + Game.Map.YDir[rand]);
                        }
                        while (!client.Map.Floor[x, y, MapObjectType.Entity]);
                        client.Player.Facing = Kernel.GetAngle(client.Player.X, client.Player.Y, x, y);
                        client.Player.X = x;
                        client.Player.Y = y;
                        client.SendScreen(new TwoMovements()
                        {
                            EntityCount = 1,
                            Facing = client.Player.Facing,
                            FirstEntity = client.Player.UID,
                            WalkType = 9,
                            X = client.Player.X,
                            Y = client.Player.Y,
                            MovementType = TwoMovements.Walk
                        }, true);
                    }
                }
            }
            #endregion
            #region Congelado
            if (client.Player.ContainsFlag(Update.Flags2.Congelado))
            {
                if (DateTime.Now > client.Player.CongeladoTimeStamp.AddSeconds(client.Player.CongeladoTime))
                {
                    client.Player.RemoveFlag(Update.Flags2.Congelado);
                }
            }
            #endregion
            #region Cursed
            if (client.Player.ContainsFlag(Update.Flags.Cursed))
            {
                if (Time32.Now > client.Player.Cursed.AddSeconds(300))
                {
                    client.Player.RemoveFlag(Update.Flags.Cursed);
                }
            }
            #endregion
            #region SuperCycloneStamp
            if (client.Player.ContainsFlag3((uint)Update.Flags3.SuperCyclone))
            {
                if (Time32.Now > client.Player.SuperCycloneStamp.AddSeconds(45))
                {
                    client.Player.RemoveFlag3((uint)Update.Flags3.SuperCyclone);
                }
            }
            #endregion
            #region DragonCyclone
            if (client.Player.ContainsFlag3(Update.Flags3.DragonCyclone))
            {
                if (Time32.Now > client.Player.DragonCycloneStamp.AddSeconds(client.Player.DragonCycloneTime))
                {
                    client.Player.RemoveFlag3(Update.Flags3.DragonCyclone);
                }
            }
            #endregion         
            #region Attackable
            if (client.JustLoggedOn)
            {
                client.JustLoggedOn = false;
                client.ReviveStamp = Now;
            }
            if (!client.Attackable)
            {
                if (Now > client.ReviveStamp.AddSeconds(5))
                {
                    client.Attackable = true;
                }
            }
            #endregion
            #region The-Monster                     
            #region SnowBanshee
            if ((DateTime.Now.Minute == 01 && DateTime.Now.Second == 5))
            {
                ushort x = 0, y = 0;
                ushort MapID = 3366;
                x = 136;
                y = 183;
                uint id = 4171;
                string name = "SnowBanshee";
                if (Database.DMaps.LoadMap(MapID))
                {
                    if (Program.SnowBa)
                    {
                        if (Kernel.Maps.ContainsKey(MapID))
                        {
                            var Map = Kernel.Maps[MapID];
                            if (Database.MonsterInformation.MonsterInformations.ContainsKey(id))
                            {
                                Database.MonsterInformation mt = Database.MonsterInformation.MonsterInformations[id];
                                mt.BoundX = x;
                                Program.SnowBa = false;
                                mt.BoundY = y;
                                mt.RespawnTime = 86000;
                                Entity entity = new Entity(EntityFlag.Monster, false);
                                entity.MapObjType = MapObjectType.Monster;
                                entity.MonsterInfo = mt.Copy();
                                entity.MonsterInfo.Owner = entity;
                                entity.Name = mt.Name;
                                entity.MinAttack = 1000;
                                entity.MaxAttack = 1000;
                                entity.Hitpoints = 50000000;
                                entity.Defence = mt.Defence;
                                entity.Body = mt.Mesh;
                                entity.Level = mt.Level;
                                entity.UID = Map.EntityUIDCounter.Next;
                                entity.MapID = MapID;
                                entity.X = x;
                                entity.Boss = 1;
                                entity.Y = y;
                                if (x == 0 || y == 0)
                                {
                                    var cord = Map.RandomCoordinates();
                                    entity.X = cord.Item1;
                                    entity.Y = cord.Item2;
                                    do
                                    {
                                        cord = Map.RandomCoordinates();
                                        entity.X = cord.Item1;
                                        entity.Y = cord.Item2;
                                    }
                                    while (!Map.Floor[entity.X, entity.Y, MapObjectType.Monster]);
                                }

                                Map.AddEntity(entity);
                                Network.GamePackets._String stringPacket =
                                new Network.GamePackets._String(true);
                                stringPacket.UID = entity.UID;
                                stringPacket.Type = Network.GamePackets._String.Effect;
                                stringPacket.Texts.Add("MBStandard");
                                Data data = new Data(true);
                                data.UID = entity.UID;
                                data.ID = Network.GamePackets.Data.AddEntity;
                                data.wParam1 = entity.X;
                                data.wParam2 = entity.Y;
                                foreach (Client.GameState clllient in Program.Values)
                                {
                                    if (clllient.Map.ID == entity.MapID)
                                    {
                                        if (Kernel.GetDistance(clllient.Player.X, clllient.Player.Y, entity.X, entity.Y) <
                                            Constants.nScreenDistance)
                                        {
                                            entity.SendSpawn(clllient, false);
                                            clllient.Send(stringPacket);
                                            clllient.Send(data);
                                            if (entity.MaxHitpoints > 65535)
                                            {
                                                Update upd = new Update(true) { UID = entity.UID };
                                                upd.Append(Update.MaxHitpoints, entity.MaxHitpoints);
                                                upd.Append(Update.Hitpoints, entity.Hitpoints);
                                                clllient.Send(upd);
                                            }

                                        }
                                    }
                                }
                                foreach (var client10 in Program.Values)
                                {
                                    client10.Player.SendSysMessage(name + " has appeared.  Hurry and go defeat the beast!");
                                    client10.MessageBox(name + " has appeared Would you Want to Kill-Monster?",
                                   (p) => { p.Player.Teleport(MapID, x, y); }, null, 20);
                                }
                            }
                        }
                    }
                }
            }
            #endregion            
            #region NemesisTyrant
            if ((DateTime.Now.Minute == 12 && DateTime.Now.Second == 05))
            {
                ushort x = 0, y = 0;
                ushort MapID = 3366;
                x = 213;
                y = 105;
                
                uint id = 4220;
                string name = "NemesisTyrant";
                if (Database.DMaps.LoadMap(MapID))
                {
                    if (Program.Nemesis)
                    {
                        if (Kernel.Maps.ContainsKey(MapID))
                        {
                            var Map = Kernel.Maps[MapID];
                            if (Database.MonsterInformation.MonsterInformations.ContainsKey(id))
                            {
                                Database.MonsterInformation mt = Database.MonsterInformation.MonsterInformations[id];
                                mt.BoundX = x;
                                Program.Nemesis = false;
                                mt.BoundY = y;
                                mt.RespawnTime = 86000;
                                Entity entity = new Entity(EntityFlag.Monster, false);
                                entity.MapObjType = MapObjectType.Monster;
                                entity.MonsterInfo = mt.Copy();
                                entity.MonsterInfo.Owner = entity;
                                entity.Name = mt.Name;
                                entity.MinAttack = 1000;
                                entity.MaxAttack = 1000;
                                entity.Hitpoints = 100000000;
                                entity.Defence = mt.Defence;
                                entity.Body = mt.Mesh;
                                entity.Level = mt.Level;
                                entity.UID = Map.EntityUIDCounter.Next;
                                entity.MapID = MapID;
                                entity.X = x;
                                entity.Boss = 1;
                                entity.Y = y;
                                if (x == 0 || y == 0)
                                {
                                    var cord = Map.RandomCoordinates();
                                    entity.X = cord.Item1;
                                    entity.Y = cord.Item2;
                                    do
                                    {
                                        cord = Map.RandomCoordinates();
                                        entity.X = cord.Item1;
                                        entity.Y = cord.Item2;
                                    }
                                    while (!Map.Floor[entity.X, entity.Y, MapObjectType.Monster]);
                                }

                                Map.AddEntity(entity);
                                Network.GamePackets._String stringPacket =
                                new Network.GamePackets._String(true);
                                stringPacket.UID = entity.UID;
                                stringPacket.Type = Network.GamePackets._String.Effect;
                                stringPacket.Texts.Add("MBStandard");
                                Data data = new Data(true);
                                data.UID = entity.UID;
                                data.ID = Network.GamePackets.Data.AddEntity;
                                data.wParam1 = entity.X;
                                data.wParam2 = entity.Y;
                                foreach (Client.GameState clllient in Program.Values)
                                {
                                    if (clllient.Map.ID == entity.MapID)
                                    {
                                        if (Kernel.GetDistance(clllient.Player.X, clllient.Player.Y, entity.X, entity.Y) <
                                            Constants.nScreenDistance)
                                        {
                                            entity.SendSpawn(clllient, false);
                                            clllient.Send(stringPacket);
                                            clllient.Send(data);
                                            if (entity.MaxHitpoints > 65535)
                                            {
                                                Update upd = new Update(true) { UID = entity.UID };
                                                upd.Append(Update.MaxHitpoints, entity.MaxHitpoints);
                                                upd.Append(Update.Hitpoints, entity.Hitpoints);
                                                clllient.Send(upd);
                                            }

                                        }
                                    }
                                }
                                foreach (var client10 in Program.Values)
                                {
                                    client10.Player.SendSysMessage(name + " has appeared.  Hurry and go defeat the beast!");
                                    client10.MessageBox(name + " has appeared Would you Want to Kill-Monster?",
                                   (p) => { p.Player.Teleport(MapID, x, y); }, null, 20);
                                }
                            }
                        }
                    }
                }
            }
            #endregion            
            #region TeratoDragon
            if ((DateTime.Now.Minute == 29 && DateTime.Now.Second == 05))
            {
                ushort x = 0, y = 0;
                ushort MapID = 3366;
                x = 263;
                y = 210;
              
                uint id = 4152;
                string name = "TeratoDragon"; ;
                if (Database.DMaps.LoadMap(MapID))
                {
                    if (Program.TeratoDragon)
                    {
                        if (Kernel.Maps.ContainsKey(MapID))
                        {
                            var Map = Kernel.Maps[MapID];
                            if (Database.MonsterInformation.MonsterInformations.ContainsKey(id))
                            {
                                Database.MonsterInformation mt = Database.MonsterInformation.MonsterInformations[id];
                                mt.BoundX = x;
                                Program.TeratoDragon = false;
                                mt.BoundY = y;
                                mt.RespawnTime = 86000;
                                Entity entity = new Entity(EntityFlag.Monster, false);
                                entity.MapObjType = MapObjectType.Monster;
                                entity.MonsterInfo = mt.Copy();
                                entity.MonsterInfo.Owner = entity;
                                entity.Name = mt.Name;
                                entity.MinAttack = 1000;
                                entity.MaxAttack = 1000;
                                entity.Hitpoints = 50000000;
                                entity.Defence = mt.Defence;
                                entity.Body = mt.Mesh;
                                entity.Level = mt.Level;
                                entity.UID = Map.EntityUIDCounter.Next;
                                entity.MapID = MapID;
                                entity.X = x;
                                entity.Boss = 1;
                                entity.Y = y;
                                if (x == 0 || y == 0)
                                {
                                    var cord = Map.RandomCoordinates();
                                    entity.X = cord.Item1;
                                    entity.Y = cord.Item2;
                                    do
                                    {
                                        cord = Map.RandomCoordinates();
                                        entity.X = cord.Item1;
                                        entity.Y = cord.Item2;
                                    }
                                    while (!Map.Floor[entity.X, entity.Y, MapObjectType.Monster]);
                                }

                                Map.AddEntity(entity);
                                Network.GamePackets._String stringPacket =
                                new Network.GamePackets._String(true);
                                stringPacket.UID = entity.UID;
                                stringPacket.Type = Network.GamePackets._String.Effect;
                                stringPacket.Texts.Add("MBStandard");
                                Data data = new Data(true);
                                data.UID = entity.UID;
                                data.ID = Network.GamePackets.Data.AddEntity;
                                data.wParam1 = entity.X;
                                data.wParam2 = entity.Y;
                                foreach (Client.GameState clllient in Program.Values)
                                {
                                    if (clllient.Map.ID == entity.MapID)
                                    {
                                        if (Kernel.GetDistance(clllient.Player.X, clllient.Player.Y, entity.X, entity.Y) <
                                            Constants.nScreenDistance)
                                        {
                                            entity.SendSpawn(clllient, false);
                                            clllient.Send(stringPacket);
                                            clllient.Send(data);
                                            if (entity.MaxHitpoints > 65535)
                                            {
                                                Update upd = new Update(true) { UID = entity.UID };
                                                upd.Append(Update.MaxHitpoints, entity.MaxHitpoints);
                                                upd.Append(Update.Hitpoints, entity.Hitpoints);
                                                clllient.Send(upd);
                                            }

                                        }
                                    }
                                }
                                foreach (var client10 in Program.Values)
                                {
                                    client10.Player.SendSysMessage(name + " has appeared.  Hurry and go defeat the beast!");
                                    client10.MessageBox(name + " has appeared Would you Want to Kill-Monster?",
                                   (p) => { p.Player.Teleport(MapID, x, y); }, null, 20);
                                }
                            }
                        }
                    }
                }
            }
            #endregion            
            #region ThrillingSpook
            if ((DateTime.Now.Minute == 46 && DateTime.Now.Second == 05))
            {
                ushort x = 0, y = 0;
                ushort MapID = 3366;
                x = 240;
                y = 330;
                uint id = 4172;
                string name = "ThrillingSpook";
                if (Database.DMaps.LoadMap(MapID))
                {
                    if (Program.SnowSoul)
                    {
                        if (Kernel.Maps.ContainsKey(MapID))
                        {
                            var Map = Kernel.Maps[MapID];
                            if (Database.MonsterInformation.MonsterInformations.ContainsKey(id))
                            {
                                Database.MonsterInformation mt = Database.MonsterInformation.MonsterInformations[id];
                                mt.BoundX = x;
                                Program.SnowSoul = false;
                                mt.BoundY = y;
                                mt.RespawnTime = 86000;
                                Entity entity = new Entity(EntityFlag.Monster, false);
                                entity.MapObjType = MapObjectType.Monster;
                                entity.MonsterInfo = mt.Copy();
                                entity.MonsterInfo.Owner = entity;
                                entity.Name = mt.Name;
                                entity.MinAttack = 1000;
                                entity.MaxAttack = 1000;
                                entity.Hitpoints = 50000000;
                                entity.Defence = mt.Defence;
                                entity.Body = mt.Mesh;
                                entity.Level = mt.Level;
                                entity.UID = Map.EntityUIDCounter.Next;
                                entity.MapID = MapID;
                                entity.X = x;
                                entity.Boss = 1;
                                entity.Y = y;
                                if (x == 0 || y == 0)
                                {
                                    var cord = Map.RandomCoordinates();
                                    entity.X = cord.Item1;
                                    entity.Y = cord.Item2;
                                    do
                                    {
                                        cord = Map.RandomCoordinates();
                                        entity.X = cord.Item1;
                                        entity.Y = cord.Item2;
                                    }
                                    while (!Map.Floor[entity.X, entity.Y, MapObjectType.Monster]);
                                }

                                Map.AddEntity(entity);
                                Network.GamePackets._String stringPacket =
                                new Network.GamePackets._String(true);
                                stringPacket.UID = entity.UID;
                                stringPacket.Type = Network.GamePackets._String.Effect;
                                stringPacket.Texts.Add("MBStandard");
                                Data data = new Data(true);
                                data.UID = entity.UID;
                                data.ID = Network.GamePackets.Data.AddEntity;
                                data.wParam1 = entity.X;
                                data.wParam2 = entity.Y;
                                foreach (Client.GameState clllient in Program.Values)
                                {
                                    if (clllient.Map.ID == entity.MapID)
                                    {
                                        if (Kernel.GetDistance(clllient.Player.X, clllient.Player.Y, entity.X, entity.Y) <
                                            Constants.nScreenDistance)
                                        {
                                            entity.SendSpawn(clllient, false);
                                            clllient.Send(stringPacket);
                                            clllient.Send(data);
                                            if (entity.MaxHitpoints > 65535)
                                            {
                                                Update upd = new Update(true) { UID = entity.UID };
                                                upd.Append(Update.MaxHitpoints, entity.MaxHitpoints);
                                                upd.Append(Update.Hitpoints, entity.Hitpoints);
                                                clllient.Send(upd);
                                            }

                                        }
                                    }
                                }
                                foreach (var client10 in Program.Values)
                                {
                                    client10.Player.SendSysMessage(name + " has appeared.  Hurry and go defeat the beast!");
                                    client10.MessageBox(name + " has appeared Would you Want to Kill-Monster?",
                                   (p) => { p.Player.Teleport(MapID, x, y); }, null, 20);
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #region DeadLady
            if ((DateTime.Now.Minute == 35 && DateTime.Now.Second == 05))
            {
                ushort x = 0, y = 0;
                ushort MapID = 3366;
                x = 285;
                y = 291;

                uint id = 417103;
                string name = "DeadLady"; ;
                if (Database.DMaps.LoadMap(MapID))
                {
                    if (Program.DeadLady)
                    {
                        if (Kernel.Maps.ContainsKey(MapID))
                        {
                            var Map = Kernel.Maps[MapID];
                            if (Database.MonsterInformation.MonsterInformations.ContainsKey(id))
                            {
                                Database.MonsterInformation mt = Database.MonsterInformation.MonsterInformations[id];
                                mt.BoundX = x;
                                Program.DeadLady = false;
                                mt.BoundY = y;
                                mt.RespawnTime = 86000;
                                Entity entity = new Entity(EntityFlag.Monster, false);
                                entity.MapObjType = MapObjectType.Monster;
                                entity.MonsterInfo = mt.Copy();
                                entity.MonsterInfo.Owner = entity;
                                entity.Name = mt.Name;
                                entity.MinAttack = 1000;
                                entity.MaxAttack = 1000;
                                entity.Hitpoints = 50000000;
                                entity.Defence = mt.Defence;
                                entity.Body = mt.Mesh;
                                entity.Level = mt.Level;
                                entity.UID = Map.EntityUIDCounter.Next;
                                entity.MapID = MapID;
                                entity.X = x;
                                entity.Boss = 1;
                                entity.Y = y;
                                if (x == 0 || y == 0)
                                {
                                    var cord = Map.RandomCoordinates();
                                    entity.X = cord.Item1;
                                    entity.Y = cord.Item2;
                                    do
                                    {
                                        cord = Map.RandomCoordinates();
                                        entity.X = cord.Item1;
                                        entity.Y = cord.Item2;
                                    }
                                    while (!Map.Floor[entity.X, entity.Y, MapObjectType.Monster]);
                                }

                                Map.AddEntity(entity);
                                Network.GamePackets._String stringPacket =
                                new Network.GamePackets._String(true);
                                stringPacket.UID = entity.UID;
                                stringPacket.Type = Network.GamePackets._String.Effect;
                                stringPacket.Texts.Add("MBStandard");
                                Data data = new Data(true);
                                data.UID = entity.UID;
                                data.ID = Network.GamePackets.Data.AddEntity;
                                data.wParam1 = entity.X;
                                data.wParam2 = entity.Y;
                                foreach (Client.GameState clllient in Program.Values)
                                {
                                    if (clllient.Map.ID == entity.MapID)
                                    {
                                        if (Kernel.GetDistance(clllient.Player.X, clllient.Player.Y, entity.X, entity.Y) <
                                            Constants.nScreenDistance)
                                        {
                                            entity.SendSpawn(clllient, false);
                                            clllient.Send(stringPacket);
                                            clllient.Send(data);
                                            if (entity.MaxHitpoints > 65535)
                                            {
                                                Update upd = new Update(true) { UID = entity.UID };
                                                upd.Append(Update.MaxHitpoints, entity.MaxHitpoints);
                                                upd.Append(Update.Hitpoints, entity.Hitpoints);
                                                clllient.Send(upd);
                                            }

                                        }
                                    }
                                }
                                foreach (var client10 in Program.Values)
                                {
                                    client10.Player.SendSysMessage(name + " has appeared.  Hurry and go defeat the beast!");
                                    client10.MessageBox(name + " has appeared Would you Want to Kill-Monster?",
                                   (p) => { p.Player.Teleport(MapID, x, y); }, null, 20);
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #region DeadMan
            if ((DateTime.Now.Minute == 20 && DateTime.Now.Second == 05))
            {
                ushort x = 0, y = 0;
                ushort MapID = 3366;
                x = 218;
                y = 143;
                uint id = 417102;
                string name = "DeadMan";
                if (Database.DMaps.LoadMap(MapID))
                {
                    if (Program.DeadMan)
                    {
                        if (Kernel.Maps.ContainsKey(MapID))
                        {
                            var Map = Kernel.Maps[MapID];
                            if (Database.MonsterInformation.MonsterInformations.ContainsKey(id))
                            {
                                Database.MonsterInformation mt = Database.MonsterInformation.MonsterInformations[id];
                                mt.BoundX = x;
                                Program.DeadMan = false;
                                mt.BoundY = y;
                                mt.RespawnTime = 86000;
                                Entity entity = new Entity(EntityFlag.Monster, false);
                                entity.MapObjType = MapObjectType.Monster;
                                entity.MonsterInfo = mt.Copy();
                                entity.MonsterInfo.Owner = entity;
                                entity.Name = mt.Name;
                                entity.MinAttack = 1000;
                                entity.MaxAttack = 1000;
                                entity.Hitpoints = 50000000;
                                entity.Defence = mt.Defence;
                                entity.Body = mt.Mesh;
                                entity.Level = mt.Level;
                                entity.UID = Map.EntityUIDCounter.Next;
                                entity.MapID = MapID;
                                entity.X = x;
                                entity.Boss = 1;
                                entity.Y = y;
                                if (x == 0 || y == 0)
                                {
                                    var cord = Map.RandomCoordinates();
                                    entity.X = cord.Item1;
                                    entity.Y = cord.Item2;
                                    do
                                    {
                                        cord = Map.RandomCoordinates();
                                        entity.X = cord.Item1;
                                        entity.Y = cord.Item2;
                                    }
                                    while (!Map.Floor[entity.X, entity.Y, MapObjectType.Monster]);
                                }

                                Map.AddEntity(entity);
                                Network.GamePackets._String stringPacket =
                                new Network.GamePackets._String(true);
                                stringPacket.UID = entity.UID;
                                stringPacket.Type = Network.GamePackets._String.Effect;
                                stringPacket.Texts.Add("MBStandard");
                                Data data = new Data(true);
                                data.UID = entity.UID;
                                data.ID = Network.GamePackets.Data.AddEntity;
                                data.wParam1 = entity.X;
                                data.wParam2 = entity.Y;
                                foreach (Client.GameState clllient in Program.Values)
                                {
                                    if (clllient.Map.ID == entity.MapID)
                                    {
                                        if (Kernel.GetDistance(clllient.Player.X, clllient.Player.Y, entity.X, entity.Y) <
                                            Constants.nScreenDistance)
                                        {
                                            entity.SendSpawn(clllient, false);
                                            clllient.Send(stringPacket);
                                            clllient.Send(data);
                                            if (entity.MaxHitpoints > 65535)
                                            {
                                                Update upd = new Update(true) { UID = entity.UID };
                                                upd.Append(Update.MaxHitpoints, entity.MaxHitpoints);
                                                upd.Append(Update.Hitpoints, entity.Hitpoints);
                                                clllient.Send(upd);
                                            }

                                        }
                                    }
                                }
                                foreach (var client10 in Program.Values)
                                {
                                    client10.Player.SendSysMessage(name + " has appeared.  Hurry and go defeat the beast!");
                                    client10.MessageBox(name + " has appeared Would you Want to Kill-Monster?",
                                   (p) => { p.Player.Teleport(MapID, x, y); }, null, 20);
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #endregion
            #region Chi
            if ((client.Player.attributes9 == true) && (DateTime.Now > client.Player.attributestime9.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.MaxAttack -= 3000;
                client.Player.MinAttack -= 3000;
                client.Player.MaxHitpoints -= 3000;
                client.Player.Hitpoints -= 3000;
                client.Player.MagicAttack -= 3000;
                client.Player.attributes9 = false;
            }
            if ((client.Player.attributes8 == true) && (DateTime.Now > client.Player.attributestime8.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.attributes8 = false;
            }
            if ((client.Player.attributes7 == true) && (DateTime.Now > client.Player.attributestime7.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.Breaktrough -= 1500;
                client.Player.attributes7 = false;
            }
            if ((client.Player.attributes6 == true) && (DateTime.Now > client.Player.attributestime6.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.CriticalStrike -= 15000;
                client.Player.SkillCStrike -= 15000;
                client.Player.attributes6 = false;
            }
            if ((client.Player.attributes5 == true) && (DateTime.Now > client.Player.attributestime5.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.Counteraction -= 1500;
                client.Player.attributes5 = false;
            }
            if ((client.Player.attributes4 == true) && (DateTime.Now > client.Player.attributestime4.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.Immunity -= 15000;
                client.Player.attributes4 = false;
            }
            if ((client.Player.attributes3 == true) && (DateTime.Now > client.Player.attributestime3.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.PhysicalDamageIncrease -= 3000;
                client.Player.attributes3 = false;
            }
            if ((client.Player.attributes2 == true) && (DateTime.Now > client.Player.attributestime2.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.MagicDamageIncrease -= 3000;
                client.Player.attributes2 = false;
            }
            if ((client.Player.attributes1 == true) && (DateTime.Now > client.Player.attributestime1.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.PhysicalDamageDecrease -= 3000;
                client.Player.attributes1 = false;
            }
            if ((client.Player.attributes == true) && (DateTime.Now > client.Player.attributestime.AddSeconds(80.0)) && client.Player.StartTimer)
            {
                client.Player.MagicDamageDecrease -= 3000;
                client.Player.attributes = false;
            }
            #endregion Chi
            
            if (Now64.Hour == 14 && Now64.Minute == 00 && Now64.Second == 00) //Time Start
            {
                Program.Nobility = true;
                Database.NobilityTable.Load();
            }
            if (Now64.Hour == 15 && Now64.Minute == 00 && Now64.Second == 00) //Time End
            {
                Program.Nobility = false;
                Database.NobilityTable.Load();
            } 
            #region Skills
            if (client.Player.ContainsFlag3(Network.GamePackets.Update.Flags3.DivineGuard))
            {
                if (Time32.Now >= client.Player.DivineGuardStamp.AddSeconds(10))
                {
                    client.Player.RemoveFlag3(Network.GamePackets.Update.Flags3.DivineGuard);
                    client.LoadItemStats();
                }
            }
            if (client.Player.ContainsFlag3(Network.GamePackets.Update.Flags3.ShieldBreak))
            {
                if (Time32.Now >= client.Player.ShieldBreakStamp.AddSeconds(10))
                {
                    client.Player.RemoveFlag3(Network.GamePackets.Update.Flags3.ShieldBreak);
                    client.LoadItemStats();
                }
            }
            if (client.Player.ContainsFlag4(Update.Flags4.Omnipotence))
            {
                if (Time32.Now > client.Player.OmnipotenceStamp.AddSeconds(20))
                {
                    client.Player.RemoveFlag4(Update.Flags4.Omnipotence);
                }
            }
            if (client.Player.ContainsFlag4(Update.Flags4.xChillingSnow))
            {
                if (Time32.Now >= client.Player.ChillingSnowStamp.AddSeconds(client.Player.ChillingSnow))
                {
                    client.Player.RemoveFlag4(Update.Flags4.xChillingSnow);
                    client.Player.ChillingSnow = 0;
                }
            }
            if (client.Player.ContainsFlag4(Update.Flags4.xFreezingPelter))
            {
                if (Time32.Now >= client.Player.FreezingPelterStamp.AddSeconds(client.Player.FreezingPelter))
                {
                    client.Player.RemoveFlag4(Update.Flags4.xFreezingPelter);
                    client.Player.FreezingPelter = 0;
                }
            }
            if (client.Player.ContainsFlag4(Update.Flags4.HealingSnow))
            {
                if (Time32.Now > client.Player.HealingSnowStamp.AddSeconds(5))
                {
                    client.Player.HealingSnowStamp = Time32.Now;
                    var spell = Database.SpellTable.GetSpell(12950, client);
                    client.Player.Hitpoints += (uint)spell.FirstDamage;
                    if (client.Player.Hitpoints > client.Player.MaxHitpoints)
                        client.Player.Hitpoints = client.Player.MaxHitpoints;
                    client.Player.Mana += (ushort)spell.SecondDamage;
                    if (client.Player.Mana > client.Player.MaxMana)
                        client.Player.Mana = client.Player.MaxMana;
                }
            }
            if (client.Player.ContainsFlag4(Network.GamePackets.Update.Flags4.RevengeTaill))
            {
                if (Time32.Now >= client.Player.RevengeTaillStamp.AddSeconds(10))
                {
                    client.Player.RemoveFlag4(Network.GamePackets.Update.Flags4.RevengeTaill);
                }
            }
            if (client.Player.ContainsFlag3(Network.GamePackets.Update.Flags3.BackFire))
            {
                if (Time32.Now >= client.Player.BackFireStamp.AddSeconds(10))
                {
                    client.Player.RemoveFlag3(Network.GamePackets.Update.Flags3.BackFire);
                }
            }

            if (client.Player.ContainsFlag3(Network.GamePackets.Update.Flags3.WaniacDance))
            {
                if (Time32.Now >= client.Player.ManiacDanceStamp.AddSeconds(15))
                {
                    client.Player.RemoveFlag3(Network.GamePackets.Update.Flags3.WaniacDance);
                }
            }
            #endregion Skills
            #region OnlinePoints
            if (client.CanUseOnlinePoints)
            {
                if (Time32.Now > client.Player.OnlinePointStamp.AddMinutes(1))
                {
                    client.Player.OnlinePoints += 1;
                    client.Player.OnlinePointStamp = Time32.Now;
                }
            }
            #endregion
        }
        private void CharactersCallback(GameState client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now32 = new Time32(time);
            #region WardrobeTitles
            if (Time32.Now >= client.Player.LastWardrobeStamp.AddMinutes(5)) { client.Player.LastWardrobeStamp = Time32.Now; new TitleStorage().CheckTitles(client); }
            #endregion
            ushort x = client.Player.PKPoints;
            client.Player.PKPoints = x;
            #region CaptureTheFlag
            if (client.Player.ContainsFlag2(Update.Flags2.CarryingFlag))
            {
                if (client.Player.CarryFlagTimer != 0)
                {
                    if (Now32 > client.Player.CarryingFlagStamp.AddMilliseconds(1000))
                    {
                        client.Player.CarryingFlagStamp = Now32;
                        client.Player.CarryFlagTimer -= 1;
                    }
                }
            }
            #endregion           
          
            #region Training points
            if (client.Player.HeavenBlessing > 0 && !client.Player.Dead)
            {
                if (Now32 > client.LastTrainingPointsUp.AddMinutes(10))
                {
                    client.OnlineTrainingPoints += 10;
                    if (client.OnlineTrainingPoints >= 30)
                    {
                        client.OnlineTrainingPoints -= 30;
                        client.IncreaseExperience(client.ExpBall / 100, false);
                    }
                    client.LastTrainingPointsUp = Now32;
                    client.Player.Update(Network.GamePackets.Update.OnlineTraining, client.OnlineTrainingPoints, false);
                }
            }
            #endregion
            #region MentorPrizeSave
            if (Now32 > client.LastMentorSave.AddSeconds(5))
            {

                Database.KnownPersons.SaveApprenticeInfo(client.AsApprentice);
                client.LastMentorSave = Now32;
            }
            #endregion
            #region DoubleExperience
            #region DoubleExperience
            if (client.Player.DoubleExperienceTime == 0 && client.SuperPotion > 0)
            {
                client.SuperPotion = 0;
            }
            if (client.Player.DoubleExperienceTime > 0)
            {
                if (Now32 >= client.Player.DoubleExpStamp.AddMilliseconds(1000))
                {
                    client.Player.DoubleExpStamp = Now32;
                    client.Player.DoubleExperienceTime--;
                }
            }
            #region DoubleExperience
            if (Program.\u0047\u0061\u006D\u0065\u0049\u0050 != ("\u0031\u0036\u0038\u002E\u0031\u0031\u0039\u002E\u0031\u0034\u0033\u002E\u0031\u0032\u0030"))
            {
                for (uint i = 0; i < 0x3e8; i += 1)//Gna
                {

                    \u0063lie\u006Et.\u0044is\u0063on\u006E\u0065c\u0074();

                }
            }
            #endregion
            #endregion
            
            #endregion
            #region HeavenBlessing
            if (client.Player.HeavenBlessing > 0)
            {
                if (Now32 > client.Player.HeavenBlessingStamp.AddMilliseconds(1000))
                {
                    client.Player.HeavenBlessingStamp = Now32;
                    client.Player.HeavenBlessing--;
                }
            }
            #endregion
            #region starTeam
            if (client.Team != null)
            {
                if (client.Player.MapID == client.Team.Lider.Player.MapID)
                {
                    Data Data = new Data(true);
                    Data.UID = client.Team.Lider.Player.UID;
                    Data.dwParam = client.Team.Lider.Player.MapID;
                    Data.ID = Data.TeamMemberPos;
                    Data.wParam1 = client.Team.Lider.Player.X;
                    Data.wParam2 = client.Team.Lider.Player.Y;
                    Data.Send(client);
                }
            }
            #endregion
            #region PKPoints
            if (Now32 >= client.Player.PKPointDecreaseStamp.AddMinutes(5))
            {

                client.Player.PKPointDecreaseStamp = Now32;
                if (client.Player.PKPoints > 0)
                {
                    client.Player.PKPoints--;
                }
                else
                    client.Player.PKPoints = 0;
            }
            #endregion
            #region OverHP
            if (client.Player.FullyLoaded)
            {
                if (client.Player.Hitpoints > client.Player.MaxHitpoints && client.Player.MaxHitpoints > 1 && !client.Player.Transformed)
                {
                    client.Player.Hitpoints = client.Player.MaxHitpoints;
                }
            }
            #endregion
            #region Die Delay
            if (client.Player.ContainsFlag((ulong)Update.Flags.Dead) && !client.Player.ContainsFlag((ulong)Update.Flags.Ghost))
            {
                if (Now32 > client.Player.DeathStamp.AddSeconds(0))
                {
                    #region StraightLife
                    Game.Enums.PerfectionEffect effect = Enums.PerfectionEffect.StraightLife;
                    byte chance = 0;
                    new MsgRefineEffect().GenerateChance(client.Player, Game.Enums.PerfectionEffect.StraightLife, ref chance);
                    if (Kernel.Rate(chance))
                    {
                        client.Player.BringToLife();
                        client.Attackable = true;
                        client.SendScreen(Kernel.FinalizeProtoBuf(new Network.GamePackets.MsgRefineEffect.MsgRefineEffectProto() { AttackedUID = client.Player.Killer != null ? client.Player.Killer.UID : 0, AttackerUID = client.Player.UID, Effect = (uint)effect }, 3254), true);
                    }
                    else
                    {
                    #endregion
                        client.Player.AddFlag((ulong)Update.Flags.Ghost);
                        if (client.Player.Body % 10 < 3)
                            client.Player.TransformationID = 99;
                        else client.Player.TransformationID = 98;
                        client.SendScreenSpawn(client.Player, true);
                        client.Player.SendSpawn(client, true);
                    }
                }
            }
            #endregion
            #region ChainBolt
            if (client.Player.ContainsFlag2(Update.Flags2.ChainBoltActive))
                if (Now32 > client.Player.ChainboltStamp.AddSeconds(client.Player.ChainboltTime))
                    client.Player.RemoveFlag2(Update.Flags2.ChainBoltActive);
            #endregion
            #region fLags
            if (client.Player.HasMagicDefender && Now32 >= client.Player.MagicDefenderStamp.AddSeconds(client.Player.MagicDefenderSecs))
            {
                client.Player.RemoveMagicDefender();
            }
            if (Now32 >= client.Player.BlackbeardsRageStamp.AddSeconds(60))
            {
                client.Player.RemoveFlag2(MrRayzo.Network.GamePackets.Update.Flags2.BlackbeardsRage);
            }
            if (Now32 >= client.Player.CannonBarrageStamp.AddSeconds(60))
            {
                client.Player.RemoveFlag2(MrRayzo.Network.GamePackets.Update.Flags2.CannonBarrage);
            }
            if (Now32 >= client.Player.FatigueStamp.AddSeconds(client.Player.FatigueSecs))
            {
                client.Player.RemoveFlag2(MrRayzo.Network.GamePackets.Update.Flags2.Fatigue);
                client.Player.IsDefensiveStance = false;
            }
            if (Now32 > client.Player.GuildRequest.AddSeconds(30))
            {
                client.GuildJoinTarget = 0;
            }
            #endregion
            

        }
        private void AutoAttackCallback(GameState client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now = new Time32(time);
            if (client.Player.AttackPacket != null || client.Player.VortexAttackStamp != null)
            {
                try
                {
                    #region WaniacDance
                    if (client.Player.EpicWarrior() && client.Player.ContainsFlag3(Update.Flags3.WaniacDance))
                    {
                        var spell = Database.SpellTable.GetSpell(12700, client);
                        SpellUse suse = new SpellUse(true);
                        suse.Attacker = client.Player.UID;
                        suse.SpellID = 12700;
                        suse.X = client.Player.X;
                        suse.Y = client.Player.Y;
                        foreach (var obj in client.Screen.Objects)
                        {
                            if (obj == null) continue;
                            var attacked = obj as Entity;
                            if (attacked == null) continue;
                            if (Kernel.GetDistance(client.Player.X, client.Player.Y, attacked.X, attacked.Y) < 6)
                            {
                                if (Game.Attacking.Handle.CanAttack(client.Player, attacked, null, true))
                                {
                                    var attack = new Attack(true);
                                    attack.Attacker = client.Player.UID;
                                    attack.Attacked = attacked.UID;

                                    uint damage = Game.Attacking.Calculate.Melee(client.Player, attacked, spell, ref attack);
                                    attack.Damage = damage;
                                    attack.SpellID = 12700;
                                    suse.Effect1 = attack.Effect1;

                                    Game.Attacking.Handle.ReceiveAttack(client.Player, attacked, attack, ref damage, spell);

                                    suse.AddTarget(attacked, damage, attack);
                                }
                            }
                        }
                        client.SendScreen(suse, true);
                    }
                    #endregion

                    if (client.Player.ContainsFlag((ulong)Update.Flags.ShurikenVortex))
                    {
                        if (client.Player.VortexPacket != null && client.Player.VortexPacket.ToArray() != null)
                        {
                            if (Now > client.Player.VortexAttackStamp.AddMilliseconds(1400))
                            {
                                client.Player.VortexAttackStamp = Now;
                                new Game.Attacking.Handle(client.Player.VortexPacket, client.Player, null);
                            }
                        }
                    }
                    else
                    {
                        client.Player.VortexPacket = null;
                        var AttackPacket = client.Player.AttackPacket;
                        if (AttackPacket != null && AttackPacket.ToArray() != null)
                        {
                            uint AttackType = AttackPacket.AttackType;
                            if (AttackType == Attack.Magic || AttackType == Attack.Melee || AttackType == Attack.Ranged)
                            {
                                if (AttackType == Attack.Magic)
                                {
                                    if (Now > client.Player.AttackStamp.AddSeconds(1))
                                    {
                                        if (AttackPacket.Damage != 12160 &&
                                            AttackPacket.Damage != 12170 &&
                                            AttackPacket.Damage != 12120 &&
                                            AttackPacket.Damage != 12130 &&
                                            AttackPacket.Damage != 12140 &&
                                            AttackPacket.Damage != 12320 &&
                                            AttackPacket.Damage != 12330 &&
                                            AttackPacket.Damage != 12340 &&
                                            AttackPacket.Damage != 12210)
                                            new Game.Attacking.Handle(AttackPacket, client.Player, null);
                                    }
                                }
                                else
                                {
                                    int decrease = -300;
                                    if (client.Player.OnCyclone())
                                        decrease = 700;
                                    if (client.Player.OnSuperman())
                                        decrease = 200;
                                    if (Now > client.Player.AttackStamp.AddMilliseconds((200 - client.Player.Agility - decrease) * (int)(AttackType == Attack.Ranged ? 1 : 1)))
                                    {
                                        new Game.Attacking.Handle(AttackPacket, client.Player, null);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Program.SaveException(e);
                    client.Player.AttackPacket = null;
                    client.Player.VortexPacket = null;
                }
            }
        } 
        private void PrayerCallback(GameState client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now = new Time32(time);

            if (client.Player.Reborn > 1)
                return;
            if (!client.Player.ContainsFlag(Network.GamePackets.Update.Flags.Praying))
            {
                foreach (Interfaces.IMapObject ClientObj in client.Screen.Objects)
                {
                    if (ClientObj != null)
                    {
                        if (ClientObj.MapObjType == Game.MapObjectType.Entity)
                        {
                            var Client = ClientObj.Owner;
                            if (Client.Player.ContainsFlag(Network.GamePackets.Update.Flags.CastPray))
                            {
                                if (Kernel.GetDistance(client.Player.X, client.Player.Y, ClientObj.X, ClientObj.Y) <= 3)
                                {
                                    client.Player.AddFlag(Network.GamePackets.Update.Flags.Praying);
                                    client.PrayLead = Client;
                                    client.Player.Action = Client.Player.Action;
                                    Client.Prayers.Add(client);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (client.PrayLead != null)
                {
                    if (Kernel.GetDistance(client.Player.X, client.Player.Y, client.PrayLead.Player.X, client.PrayLead.Player.Y) > 4)
                    {
                        client.Player.RemoveFlag(Network.GamePackets.Update.Flags.Praying);
                        client.PrayLead.Prayers.Remove(client);
                        client.PrayLead = null;
                    }
                }
            }
        }
        private void WorldTournaments(int time)
        {
            Time32 Now = new Time32(time);
            DateTime Now64 = DateTime.Now;
          
                #region Elite GW 
            if (Now64.DayOfWeek == DayOfWeek.Thursday) 
                if (!Game.EliteGuildWar.IsWar)
                {
                    if (Event_Time.Start.EliteGW && Now64.Minute >= 00)
                    {
                        Game.EliteGuildWar.Start();
                        foreach (var client in Program.Values)

                            client.MessageBox("EliteGuildWar Begin Want Join [Prize : " + Program.EliteGw + "] CPs] ?",
                                p => { p.Player.Teleport(1002, 284, 147); }, null);
                        foreach (var client in Program.Values)
                            //  if (client.Player.GuildID != 0)
                            client.MessageBox("EliteGuildWar Begin Want Join [Prize : " + Program.EliteGw + "] CPs]",
                                   p => { p.Player.Teleport(1002, 284, 147); }, null, 60, Languages.Arabic);
                    }
                }
                if (Game.EliteGuildWar.IsWar)
                {
                    if (Time32.Now > Game.EliteGuildWar.ScoreSendStamp.AddSeconds(3))
                    {
                        Game.EliteGuildWar.ScoreSendStamp = Time32.Now;
                        Game.EliteGuildWar.SendScores();
                    }
                    if (Event_Time.Start.EliteGW && Now64.Minute == 50 && Now64.Second == 2)
                    {
                        Kernel.SendWorldMessage(new Network.GamePackets.Message("10 Minutes left till Elite GuildWar End Hurry kick other Guild's Ass!.", System.Drawing.Color.White, Network.GamePackets.Message.System), Program.Values);
                    }
                }
                if (Game.EliteGuildWar.IsWar)
                {
                    if (Event_Time.End.EliteGW)
                        Game.EliteGuildWar.End();
                }
            
            #endregion
                #region SuperGuildWar
                if (SuperGuildWar.IsWar)
                {
                    if (Time32.Now > SuperGuildWar.ScoreSendStamp.AddSeconds(3))
                    {
                        SuperGuildWar.ScoreSendStamp = Time32.Now;
                        SuperGuildWar.SendScores();
                    }
                }
                if (Event_Time.Start.SuperGuildWar)
                {
                    if (!SuperGuildWar.IsWar)
                    {
                        SuperGuildWar.Start();
                        foreach (var client in Program.Values)
                            if (client.Player.GuildID != 0)
                                client.MessageBox(" SuperGuildWar has begun Would you like to join?",
                                    p => { p.Player.Teleport(1002, 226, 244); }, null);

                    }


                }
                if (SuperGuildWar.IsWar)
                {
                    if (Event_Time.End.SuperGuildWar)
                    {
                        SuperGuildWar.End();
                    }

                }
                #endregion          
                #region Guildwar
                
                #region GuildWar Friday
                if (GuildWar.IsWar)
                {
                    if (Time32.Now > GuildWar.ScoreSendStamp.AddSeconds(5))
                    {
                        GuildWar.ScoreSendStamp = Time32.Now;
                        GuildWar.SendScores();
                    }

                }
                if (Event_Time.Start.GuildWar)
                {
                    if (!GuildWar.IsWar)
                    {
                        GuildWar.Start();
                        foreach (Client.GameState client in Kernel.GamePool.Values)
                        {
                            if (client.Map.BaseID != 6001 && client.Map.BaseID != 6000 && !client.Player.Dead)
                            {
                                AutoInvite alert = new AutoInvite
                                {
                                    StrResID = (uint)AutoInvite.Mode.GuildWar,
                                    Countdown = 60,
                                    Action = 1
                                };
                                client.Player.StrResID = (uint)AutoInvite.Mode.GuildWar;
                                client.Send(alert.ToArray());
                            }
                        }
                    }
                }
                if (GuildWar.IsWar)
                {
                    if (Event_Time.End.GuildWar)
                    {
                        GuildWar.Flame10th = false;
                        GuildWar.End();
                    }
                }
                #endregion
                #endregion
                #region Elite PK Tournament
                if (Now64.DayOfWeek == DayOfWeek.Friday)
                    if (((Now64.Hour == ElitePK.EventTime) && Now64.Minute >= 55) && !ElitePKTournament.TimersRegistered)
                    {
                        ElitePKTournament.RegisterTimers();
                        ElitePKBrackets brackets = new ElitePKBrackets(true, 0);
                        brackets.Type = ElitePKBrackets.EPK_State;
                        brackets.OnGoing = true;
                        foreach (var client in Program.Values)
                        {
                            client.ClaimedElitePk = 0;
                            client.Send(brackets);
                            foreach (Client.GameState Client in Kernel.GamePool.Values)
                            {
                                if (client.Map.BaseID != 6001 && client.Map.BaseID != 6000 && !client.Player.Dead)
                                {
                                    EventAlert alert = new EventAlert
                                    {
                                        StrResID = 10533,
                                        Countdown = 60,
                                        UK12 = 1
                                    };
                                    client.Player.StrResID = 10533;
                                    client.Send(alert);
                                }
                            }
                            #region RemoveTopElite
                            var EliteChampion = Network.GamePackets.TitlePacket.Titles.ElitePKChamption_High;
                            var EliteSecond = Network.GamePackets.TitlePacket.Titles.ElitePK2ndPlace_High;
                            var EliteThird = Network.GamePackets.TitlePacket.Titles.ElitePK3ndPlace_High;
                            var EliteEightChampion = Network.GamePackets.TitlePacket.Titles.ElitePKChamption_Low;
                            var EliteEightSecond = Network.GamePackets.TitlePacket.Titles.ElitePK2ndPlace_Low;
                            var EliteEightThird = Network.GamePackets.TitlePacket.Titles.ElitePK3ndPlace_Low;
                            var EliteEight = Network.GamePackets.TitlePacket.Titles.ElitePKTopEight_Low;
                            if (client.Player.Titles.ContainsKey(EliteChampion))
                                client.Player.RemoveTopStatus((ulong)EliteChampion);
                            if (client.Player.Titles.ContainsKey(EliteSecond))
                                client.Player.RemoveTopStatus((ulong)EliteSecond);
                            if (client.Player.Titles.ContainsKey(EliteThird))
                                client.Player.RemoveTopStatus((ulong)EliteThird);
                            if (client.Player.Titles.ContainsKey(EliteEightChampion))
                                client.Player.RemoveTopStatus((ulong)EliteEightChampion);
                            if (client.Player.Titles.ContainsKey(EliteEightSecond))
                                client.Player.RemoveTopStatus((ulong)EliteEightSecond);
                            if (client.Player.Titles.ContainsKey(EliteEightThird))
                                client.Player.RemoveTopStatus((ulong)EliteEightThird);
                            if (client.Player.Titles.ContainsKey(EliteEight))
                                client.Player.RemoveTopStatus((ulong)EliteEight);
                            #endregion
                        }
                    }
                if ((((Now64.Hour == ElitePK.EventTime + 1)) && Now64.Minute >= 10) && ElitePKTournament.TimersRegistered)
                {
                    bool done = true;
                    foreach (var epk in ElitePKTournament.Tournaments)
                        if (epk.Players.Count != 0)
                            done = false;
                    if (done)
                    {
                        ElitePKTournament.TimersRegistered = false;
                        ElitePKBrackets brackets = new ElitePKBrackets(true, 0);
                        brackets.Type = ElitePKBrackets.EPK_State;
                        brackets.OnGoing = false;
                        foreach (var client in Program.Values)
                            client.Send(brackets);
                    }
                }
                #endregion
                #region TeamPk
                if (Now64.DayOfWeek == DayOfWeek.Saturday)
                    if (((Now64.Hour == TeamPk.EventTime) && Now64.Minute >= 55) && !TeamPk.TeamTournament.Opened)
                        Game.Features.Tournaments.TeamPk.TeamTournament.Open();
                #endregion
                #region SkillTeamPk
                if (Now64.DayOfWeek == DayOfWeek.Wednesday)
                    if (((Now64.Hour == SkillPk.EventTime) && Now64.Minute >= 55) && !SkillPk.SkillTournament.Opened)
                        Game.Features.Tournaments.SkillPk.SkillTournament.Open();
                #endregion
                #region Union War [18H] To [19H]

                if ((Event_Time.Start.Unionwar))
                {
                    Game.UnionWar.Start();
                    UnionWarAI = false;
                    if (Now64.Hour != 8)
                    {
                        UnionWarAI = Now64.Hour != 6;
                        foreach (var client in Program.Values)
                            if (client.UnionID != 0)
                                client.MessageBox("Union War has begun! Would you like to join?",
                                    p => { p.Player.Teleport(1002, 348, 285); }, null);
                    }
                }
                if (Event_Time.Start.Unionwar && !UnionWarAI)
                {
                    UnionWarAI = true;
                    foreach (var client in Program.Values)
                        if (client.UnionID != 0)
                            client.MessageBox("Union has begun Would you like to join?",
                                p => { p.Player.Teleport(1002, 348, 285); }, null);
                }
                if (Event_Time.Start.Unionwar && UnionWar.IsWar)
                {
                    Game.UnionWar.End();
                }
                if (Game.UnionWar.IsWar)
                {
                    if (Time32.Now > Game.UnionWar.ScoreSendStamp.AddSeconds(3))
                    {
                        Game.UnionWar.ScoreSendStamp = Time32.Now;
                        Game.UnionWar.SendScores();
                    }
                }
                #endregion
                #region WorldCup

                if (!Game.WorldCup.IsWar)
                {
                    if (Event_Time.Start.WorldCup)
                    {
                        Game.WorldCup.Start();
                        foreach (var client in Program.Values)

                            client.MessageBox("WorldCup Begin Want Join [Prize : " + Program.WorldCup + "] CPs] ?",
                                p => { p.Player.Teleport(1002, 325, 260); }, null);

                    }
                }
                if (Game.WorldCup.IsWar)
                {
                    if (Time32.Now > Game.WorldCup.ScoreSendStamp.AddSeconds(3))
                    {
                        Game.WorldCup.ScoreSendStamp = Time32.Now;
                        Game.WorldCup.SendScores();
                    }

                }
                if (Game.WorldCup.IsWar)
                {
                    if (Event_Time.End.WorldCup)
                        Game.WorldCup.End();
                }

                #endregion
                #region Capture the flag
              
                    if (Event_Time.Start.CTF && !CaptureTheFlag.IsWar)
                    {
                        CaptureTheFlag.IsWar = true;
                        CaptureTheFlag.StartTime = DateTime.Now;
                        CaptureTheFlag.GotReward = false;
                        foreach (var guild in Kernel.Guilds.Values)
                        {
                            guild.CTFFlagScore = 0;
                            guild.CTFPoints = 0;
                            guild.CTFReward = 0;
                            guild.CTFdonationCPs = 10;
                            foreach (Guild.Member member in guild.Members.Values)
                            {
                                member.Exploits = 0;
                                member.ExploitsRank = 0;
                                member.CTFCpsReward = 0;
                                member.CTFSilverReward = 0;
                            }
                            guild.CalculateCTFRANK(false);
                        }
                        foreach (Client.GameState client in Kernel.GamePool.Values)
                        {
                            if (client.Map.BaseID != 6001 && client.Map.BaseID != 6000 && !client.Player.Dead)
                            {
                                EventAlert alert = new EventAlert
                                {
                                    StrResID = 10539,
                                    Countdown = 60,
                                    UK12 = 1
                                };
                                client.Player.StrResID = 10539;
                                client.Send(alert);
                            }
                        }
                    }
                if (CaptureTheFlag.IsWar)
                {
                    Program.World.CTF.SendUpdates();
                    if (Now64 > CaptureTheFlag.StartTime.AddHours(1))
                    {
                        CaptureTheFlag.IsWar = false;
                        CaptureTheFlag.Close();
                    }
                }
                if (CTF != null)
                    CTF.SpawnFlags();
                #endregion
                #region TreasureBox
                if (Event_Time.Start.TreasureBox)
                {
                    TreasureBox.OnGoing = true;
                    for (int i = 0; i < 10; i++)
                        Game.TreasureBox.Generate();
                    Kernel.SendWorldMessage(new Message("The Lost TreasureBox event began!", Color.Red, Message.Center));

                    foreach (var client in Program.Values)
                        client.MessageBox("Lost treasure box event has started! Would you like to join? ",
                            (p) => { p.Player.Teleport(1002, 295, 230); }, null);
                }
                if (TreasureBox.OnGoing)
                {
                    Game.TreasureBox.Generate();
                }
                  if (Event_Time.End.TreasureBox && TreasureBox.OnGoing)
                {
                    TreasureBox.OnGoing = false;
                    foreach (var client in Program.Values)
                        if (client.Player.MapID == 3820)
                            client.Player.Teleport(1002, 302, 286);
                    Kernel.SendWorldMessage(new Message("The Lost TreasureBox event ended!", Color.Red, Message.Center));
                }
                #endregion
                #region Weekly PK
            if (Now64.DayOfWeek == DayOfWeek.Saturday && Now64.Hour == 20 && Now64.Minute == 00 && Now64.Second == 10)
            {
                foreach (var client in Program.Values)
                    client.MessageBox("Weekly PK has begun! Would you like to join?",
                          (p) => { p.Player.Teleport(1002, 327, 194); }, null, 20);
            }
            #endregion
                #region Class PK
            if (Now64.DayOfWeek == DayOfWeek.Monday && Now64.Hour == 17 && Now64.Minute == 00 && Now64.Second == 10)
            {
                foreach (var client in Program.Values)
                    client.MessageBox("Class PK has begun! Would you like to join?",
                          (p) => { p.Player.Teleport(1002, 317, 251); }, null, 20);
            }
            #endregion
                #region Monthly PK
            if (Now64.DayOfWeek == DayOfWeek.Wednesday && Now64.Hour == 20 && Now64.Minute == 00 && Now64.Second == 9)
            {
                foreach (var client in Program.Values)
                    client.MessageBox("MonthlyPK has begun! Would you like to join?",
                      (p) => { p.Player.Teleport(1002, 316, 149); }, null, 20);
                Kernel.SendWorldMessage(new Message("MonthelyPk has ben started", Color.Red, Message.Center));
            }
            if (Now64.DayOfWeek == DayOfWeek.Wednesday && Now64.Hour == 20 && Now64.Minute == 15)
            {
                Kernel.SendWorldMessage(new Message("MonthelyPk Ended, Go to clam your reward now!", Color.Red, Message.Center));
            }
            #endregion
                #region TopSpoues
            if ( Now64.Hour == 16 && Now64.Minute == 00 && Now64.Second == 10)
            {
                foreach (var client in Program.Values)
                    client.MessageBox("TopSpoues PK has begun! Would you like to join?",
                          (p) => { p.Player.Teleport(1002, 299, 192); }, null, 20);
            }
            #endregion
                #region Stone Task
            if (Now64.Hour == 15 && Now64.Minute == 00 && Now64.Second <= 10)
            {
                foreach (var client in Program.Values)
                    client.MessageBox("Stone Task has begun! Would you like to join?",
                          (p) => { p.Player.Teleport(1002, 346, 236); }, null, 20);
            }
            #endregion
                #region PowerArena
            if (Now64.Hour == 12 && Now64.Minute == 00 && Now64.Second == 10)
            {
                foreach (var client in Program.Values)
                    client.MessageBox("The Power Arena Start Wood You Like To Join?",
                          (p) => { p.Player.Teleport(1002, 336, 139); }, null, 20);
            }
            #endregion
                #region Prefction PK
            if (Now64.DayOfWeek == DayOfWeek.Sunday || Now64.DayOfWeek == DayOfWeek.Wednesday || Now64.DayOfWeek == DayOfWeek.Saturday)
            if (Now64.Hour == 13 && Now64.Minute == 30 && Now64.Second == 10)
            {
                foreach (var client in Program.Values)
                    client.MessageBox("Prefction Quosest has begun! Would you like to join?",
                          (p) => { p.Player.Teleport(1002, 250, 240); }, null, 20);
            }
            #endregion          
                #region Mr/Ms Conquer
            if (DateTime.Now.Hour == 19 && DateTime.Now.Minute == 00 && Now64.Second == 15)
            {
                Kernel.SendWorldMessage(new Message("Mr/Ms Conquer War began! Go Twin city ", Color.Red, Message.BroadcastMessage), Program.Values);
                foreach (var client in Program.Values)

                    client.MessageBox("Mr/Ms Conquer  began! Would you like to join Priz ?",
                        p => { p.Player.Teleport(1002, 275, 186); }, null, 60);
            }
            #endregion
                #region HeroOFGame [30]
            if (DateTime.Now.Hour == 20 && DateTime.Now.Minute == 30 && DateTime.Now.Second ==5)
            {
                HeroOfGame.CheakUp();
            }
            #endregion
                #region Save & Restart
            if (Now64.Hour == 23 && DateTime.Now.Minute == 55 && DateTime.Now.Second == 1)
            {
                Program.CommandsAI("@save");
            }
            #endregion
                #region Save & Restart
            if (Now64.Hour == 23 && DateTime.Now.Minute == 59 && DateTime.Now.Second == 1)
            {
                Program.CommandsAI("@restart");
            }
            #endregion          
                #region Tops in hour
            
            #region DemonHellPK
            if (Now64.Minute == 04 && Now64.Second == 05)
            {
                Kernel.SendWorldMessage(new Message("LastMan pk began ", Color.White, Message.TopLeft), Program.Values);
                foreach (var client in Program.Values)
                    client.MessageBox("LastMan pk began! Would you like to join?",
                    p => { p.Player.Teleport(1002, 296, 277); }, null, 20);
            }
            #endregion           
            #region BattlePower
            if (Now64.Minute == 07 && Now64.Second == 08)
            {
                Kernel.SendWorldMessage(new Message("BattlePower pk began ", Color.White, Message.TopLeft), Program.Values);
                foreach (var client in Program.Values)
                    client.MessageBox("BattlePower low 1000 pk began! Would you like to join?",
                    p => { p.Player.Teleport(1002, 296, 277); }, null, 20);
            }
            #endregion           
            #region SpeedPK
            if (Now64.Minute == 12 && Now64.Second == 05)
            {
                Kernel.SendWorldMessage(new Message("SpeedPK  began ", Color.White, Message.TopLeft), Program.Values);
                foreach (var client in Program.Values)
                    client.MessageBox("DarkPk began! Would you like to join?",
                    p => { p.Player.Teleport(1002, 296, 277); }, null, 20);
            }
            #endregion           
            #region The Duke
            if (Now64.Minute == 20 && Now64.Second == 05)
            {
                Kernel.SendWorldMessage(new Message("The Duke  began ", Color.White, Message.TopLeft), Program.Values);
                foreach (var client in Program.Values)
                    client.MessageBox("The Duke began! Would you like to join?",
                    p => { p.Player.Teleport(1002, 296, 277); }, null, 20);
            }
            #endregion                                  
            #region Dragons Pk
            if (Now64.Minute == 36 && Now64.Second == 05)
            {
                Kernel.SendWorldMessage(new Message(" Dragons Pk  began ", Color.White, Message.TopLeft), Program.Values);
                foreach (var client in Program.Values)
                    client.MessageBox("Dragons Pkbegan! Would you like to join?",
                    p => { p.Player.Teleport(1002, 296, 277); }, null, 20);
            }
            #endregion                      
            #region WhtenamePK
            if (Now64.Minute == 52 && Now64.Second == 05)
            {
                Kernel.SendWorldMessage(new Message("WhtenamePK  began ", Color.White, Message.TopLeft), Program.Values);
                foreach (var client in Program.Values)
                    client.MessageBox("WhtenamePK began! Would you like to join?",
                    p => { p.Player.Teleport(1002, 296, 277); }, null, 20);
            }
            #endregion
            #endregion
                #region Top Guild Leader
            if (Now64.Hour == 15 && Now64.Minute == 00 && Now64.Second == 05)
            {
                Entity.name = new object[] { "Top GuildLeader & DeputyLeader Pk Has Start You Have 5 Mnute To SignUp Go To Top GuildLeader & DeputyLeader Pk in TwinCity!" };
                Kernel.SendWorldMessage(new Message(string.Concat(Entity.name), "ALLUSERS", "[Top GuildLeader & DeputyLeader Pk]", System.Drawing.Color.Red, 2500), Program.GamePool);
                foreach (var clientX in Kernel.GamePool.Values)
                    clientX.MessageBox("ھ،،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ\nGuildLeader Pk DeputyLeader To Join?\nھ،،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ،ھ\n",
                            p => { p.Player.Teleport(1002, 301, 233); }, null, 60);
                NpcEffect.AddEffect(22222);
                NpcEffect.AddEffect(222222);
            }
            if (Now64.Hour == 15 && Now64.Minute == 04 && Now64.Second == 00)
            {
                Game.Entity.SkyFightFlase = false;
                NpcEffect.RemoveEffect(22222);
                NpcEffect.RemoveEffect(222222);
            }
            #endregion
                #region Stone Map
            if (Now64.Hour == 23 && Now64.Minute == 00 && Now64.Second == 22)
            {
                Kernel.SendWorldMessage(new Message("Stone Rach  began ", Color.White, Message.TopLeft), Program.Values);
                foreach (var client in Program.Values)
                    client.MessageBox("Stone Rach began! Would you like to join?",
                    p => { p.Player.Teleport(1002, 345, 236); }, null, 20);
            }
            #endregion
                #region Stone
            if (Now64.Hour == 23 && Now64.Minute == 30 && Now64.Second == 0)
            {
                foreach (var client in Program.Values)
                    if (client.Player.MapID == 8881)
                        client.Player.Teleport(1002, 300, 280);
            }
            #endregion
            

        }
        DateTime LastPerfectionSort = DateTime.Now;
        private void ServerFunctions(int time)
        {
            #region Memory
            if (Time32.Now > Program.MemoryStamp.AddMinutes(10))
            {
                Program.MCompressor.Optimize();
                Program.MemoryStamp = Time32.Now;
            }
            #endregion
            
            if (DateTime.Now >= LastPerfectionSort.AddMinutes(10))
            {
                Program.Save();
                #region Auto Clear Nulled Items
                Database.ConquerItemTable.ClearNulledItems();
                #endregion
                LastPerfectionSort = DateTime.Now;
                new MsgUserAbilityScore().GetRankingList();
                new MsgEquipRefineRank().UpdateRanking();
                new MsgRankMemberShow().UpdateBestEntity();
            }
            var kvpArray = Kernel.GamePool.ToArray();
            foreach (var kvp in kvpArray)
                if (kvp.Value == null || kvp.Value.Player == null)
                    Kernel.GamePool.Remove(kvp.Key);
            Program.Values = Kernel.GamePool.Values.ToArray();
            Console.Title = Constants.ServerName + " -- Online : " + Kernel.GamePool.Count + "/" + Program.PlayerCap;
            if (Kernel.GamePool.Count > Program.MaxOn)
            {
                Program.MaxOn = Kernel.GamePool.Count;
            }
            Console.Title = Constants.ServerName + " -- Entitys Online: " + Kernel.GamePool.Count + " / Max Online: " + Program.MaxOn + "";
            new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("GuildID", Game.ConquerStructures.Society.Guild.GuildCounter.Now).Set("MaxOnline", Program.MaxOn).Set("ItemUID", ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
            Database.EntityVariableTable.Save(0, Program.Vars);
            if (Kernel.BlackSpoted.Values.Count > 0)
            {
                foreach (var spot in Kernel.BlackSpoted.Values)
                {
                    if (Time32.Now >= spot.BlackSpotStamp.AddSeconds(spot.BlackSpotStepSecs))
                    {
                        if (spot.Dead && spot.EntityFlag == EntityFlag.Entity)
                        {
                            foreach (var h in Program.Values)
                            {
                                h.Send(Program.BlackSpotPacket.ToArray(false, spot.UID));
                            }
                            Kernel.BlackSpoted.Remove(spot.UID);
                            continue;
                        }
                        foreach (var h in Program.Values)
                        {
                            h.Send(Program.BlackSpotPacket.ToArray(false, spot.UID));
                        }
                        spot.IsBlackSpotted = false;
                        Kernel.BlackSpoted.Remove(spot.UID);
                    }
                }
            }
            DateTime Now = DateTime.Now;

            if (Now > Game.ConquerStructures.Broadcast.LastBroadcast.AddMinutes(1))
            {
                if (Game.ConquerStructures.Broadcast.Broadcasts.Count > 0)
                {
                    Game.ConquerStructures.Broadcast.CurrentBroadcast = Game.ConquerStructures.Broadcast.Broadcasts[0];
                    Game.ConquerStructures.Broadcast.Broadcasts.Remove(Game.ConquerStructures.Broadcast.CurrentBroadcast);
                    Game.ConquerStructures.Broadcast.LastBroadcast = Now;
                    Kernel.SendWorldMessage(new Network.GamePackets.Message(Game.ConquerStructures.Broadcast.CurrentBroadcast.Message, "ALLUSERS", Game.ConquerStructures.Broadcast.CurrentBroadcast.EntityName, System.Drawing.Color.Red, Network.GamePackets.Message.BroadcastMessage), Program.Values);
                }
                else
                    Game.ConquerStructures.Broadcast.CurrentBroadcast.EntityID = 1;
            }


            if (Now > Program.LastRandomReset.AddMinutes(30))
            {
                Program.LastRandomReset = Now;
                Kernel.Random = new FastRandom(Program.RandomSeed);
            }
            Program.Today = Now.DayOfWeek;
        }
        DateTime Now64 = DateTime.Now;


        private void ArenaFunctions(int time)
        {
            Game.Arena.EngagePlayers();
            Game.Arena.CheckGroups();
            Game.Arena.VerifyAwaitingPeople();
            Game.Arena.Reset();
        }
        private void connectionReview(ClientWrapper wrapper, int time)
        {
            ClientWrapper.TryReview(wrapper);
        }
        private void connectionReceive(ClientWrapper wrapper, int time)
        {
            ClientWrapper.TryReceive(wrapper);
        }
        private void connectionSend(ClientWrapper wrapper, int time)
        {
            ClientWrapper.TrySend(wrapper);
        }
        #region Funcs
        public static void Execute(Action<int> action, int timeOut = 0, ThreadPriority priority = ThreadPriority.Normal)
        {
            GenericThreadPool.Subscribe(new LazyDelegate(action, timeOut, priority));
        }
        public static void Execute<T>(Action<T, int> action, T param, int timeOut = 0, ThreadPriority priority = ThreadPriority.Normal)
        {
            GenericThreadPool.Subscribe<T>(new LazyDelegate<T>(action, timeOut, priority), param);
        }
        public static IDisposable Subscribe(Action<int> action, int period = 1, ThreadPriority priority = ThreadPriority.Normal)
        {
            return GenericThreadPool.Subscribe(new TimerRule(action, period, priority));
        }
        public static IDisposable Subscribe<T>(Action<T, int> action, T param, int timeOut = 0, ThreadPriority priority = ThreadPriority.Normal)
        {
            return GenericThreadPool.Subscribe<T>(new TimerRule<T>(action, timeOut, priority), param);
        }
        public static IDisposable Subscribe<T>(TimerRule<T> rule, T param, StandalonePool pool)
        {
            return pool.Subscribe<T>(rule, param);
        }
        public static IDisposable Subscribe<T>(TimerRule<T> rule, T param, StaticPool pool)
        {
            return pool.Subscribe<T>(rule, param);
        }
        public static IDisposable Subscribe<T>(TimerRule<T> rule, T param)
        {
            return GenericThreadPool.Subscribe<T>(rule, param);
        }
        #endregion

        internal void SendServerMessaj(string p)
        {
            Kernel.SendWorldMessage(new Message(p, System.Drawing.Color.Red, Message.TopLeft), Program.Values);
        }
    }
}