﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public struct Season
    {
        public Time Start;
        public Time End;
        public double SpecialThreshold;
        
        public bool Durring(DateTime time)
        {
            DateTime st = Start;
            DateTime et = End;
            return ((time >= st) && (time < et));
        }
        public List<string> Specials;
        public string Something => (Specials.Count>0)? Specials[new Random((int)DateTime.Now.Ticks).Next(Specials.Count)]: throw new Exception("No Specials Availible"); 
        public Season() { }
        
    }
}