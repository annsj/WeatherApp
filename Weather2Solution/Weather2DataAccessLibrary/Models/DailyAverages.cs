using System;
using System.Collections.Generic;
using System.Text;

namespace Weather2DataAccessLibrary.Models
{
    public class DailyAverages
    {
        public DateTime Day { get; set; }
        public double? AverageTemperature { get; set; }
        public double? AverageHumidity { get; set; }
        public double? FungusRisk { get; set; }

        //public int NumberOfTemperatureRecords { get; set; }
        //public int NumberOfHumidityRecords { get; set; }


        public override string ToString()
        {
            string printString = $"{Day.ToShortDateString()}\t";

            printString += AverageTemperature != null ?
                $"{Math.Round((double)AverageTemperature, 1)}\t\t" :
                $"*\t\t";

            //printString += NumberOfTemperatureRecords + "\t\t\t";

            printString += AverageHumidity != null ?
                $"{Math.Round((double)AverageHumidity)}\t\t" :
                $"*\t\t";

            if (FungusRisk < 1)
                printString += "0";
            else if (FungusRisk == null)
                printString += $"*";
            else
                printString += $"{Math.Round((double)FungusRisk)}";

            //printString += NumberOfHumidityRecords;

            return printString;
        }

       
    }
}
