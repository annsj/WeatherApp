using System;
using System.Collections.Generic;
using System.Text;

namespace Weather2DataAccessLibrary.Models
{
    public class DailyInOutTempDifference
    {
        public DateTime Day { get; set; }
        public double? InsideTemperature { get; set; }
        public double? OutsideTemperature { get; set; }
        public double? TempDifference { get; set; }

        public override string ToString()
        {
            string printString = $"{Day.ToShortDateString()}\t";

            printString += InsideTemperature != null ?
                $"{Math.Round((double)InsideTemperature, 1)}\t" :
                $"*\t";

            printString += OutsideTemperature != null ?
                $"{Math.Round((double)OutsideTemperature, 1)}\t" :
                $"*\t";

            printString += InsideTemperature != null && OutsideTemperature != null ?
                $"{Math.Round((double)TempDifference, 1)}" :
                $"*";

            return printString;                
        }
    }
}
