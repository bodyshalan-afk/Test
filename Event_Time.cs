using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MrRayzo
{
    public class Event_Time
    {
        public static DateTime now
        {
            get
            {
                return DateTime.Now;
            }
        }

        public class Start
        {
          
           
            public static bool TreasureBox
            {
                get
                {
                    return (now.Hour >= 22 && now.Minute == 32 &&  now.Second == 8);
                }
            }
        
            public static bool WorldCup
            {
                get
                {
                    return (now.DayOfWeek == DayOfWeek.Thursday && now.Hour == 20) && now.Minute == 00 && now.Second == 1;
                }
            }

            public static bool Unionwar
            {
                get
                {
                    return (now.Hour == 20 ) && now.Minute == 00 && now.Second == 21;
                }
            }


            public static bool EliteGW
            {
                get
                {
                  
                    return (now.Hour == 17 && now.Minute == 00);
                }
            }
          
            public static bool SuperGuildWar
            {
                get
                {
                    return (now.DayOfWeek == DayOfWeek.Tuesday && now.Hour == 19 && now.Minute == 1); 
                }
            }
            public static bool GuildWar
            {
                get
                {
                    return (now.DayOfWeek == DayOfWeek.Friday && now.Hour == 19 && now.Minute == 1);
                }
            }
            public static bool CTF
            {
                get
                {
                    DateTime Now64 = DateTime.Now;
                    return (Now64.DayOfWeek == DayOfWeek.Sunday && now.Hour == 20) && now.Minute == 1;
                }
            }
            public static bool NobiltyWarPole
            {
                get
                {
                    return (now.DayOfWeek == DayOfWeek.Monday && now.Hour == 22 && now.Minute == 1); 
                }
            }
            public static bool PowerX
            {
                get
                {
                    return (now.Hour == 11 && now.Minute == 00 && now.Second == 5);
                }
            }
            public static bool StatuesWar
            {
                get
                {
                    return ( now.Hour == 21 && now.Minute == 0) && (now.Second == 8);
                }
            }
            public static bool GuildScoreWar
            {
                get
                {
                    return (now.Hour == 16 && now.Minute == 0 && now.Second == 50); 
                }
            }
            public static bool ThunderScoreWar
            {
                get
                {
                    return (now.Hour == 08 && now.Minute == 00 && now.Second == 4);
                }
            }
            public static bool ClassPoleWar
            {
                get
                {
                    return ( now.Hour == 14 && now.Minute == 00 && now.Second == 4); 
                }
            }
            public static bool HeroOfGame
            {
                get
                {
                    return (DateTime.Now.Hour == 18 && DateTime.Now.Minute == 30 && DateTime.Now.Second == 5);
                }
            }



        }
        //انتهاء الحروب 
        public class End
        {
            public static bool SuperGuildWar
            {
                get
                {
                    return (now.DayOfWeek == DayOfWeek.Tuesday && now.Hour == 21 && now.Second == 1);
                }
            }
            public static bool GuildWar
            {
                get
                {
                    return (now.DayOfWeek == DayOfWeek.Friday && now.Hour == 21 && now.Second == 1);
                }
            }
            public static bool WorldCup
            {
                get
                {
                    return (now.DayOfWeek == DayOfWeek.Thursday && now.Hour == 20) && now.Minute == 59 && now.Second == 59;
                }
            }
            public static bool TreasureBox
            {
                get
                {
                    return (now.Hour >= 22 && now.Minute == 40);
                }
            }

            public static bool Unionwar
            {
                get
                {
                    return (now.Hour == 20 && now.Minute == 59 && now.Second == 59);
                }
            }
            public static bool EliteGW
            {
                get
                {
                    DateTime Now64 = DateTime.Now;
                    return (now.Hour == 17 && now.Minute == 59 && now.Second == 59); 
                }
            }
         
        }

    }
}
   
