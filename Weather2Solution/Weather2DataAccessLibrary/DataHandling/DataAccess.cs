using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Weather2DataAccessLibrary.Models;

namespace Weather2DataAccessLibrary.DataAccess
{
    public static class DataAccess
    {
        public static Sensor GetSensor(int id)
        {
            Sensor sensor = new Sensor();

            using (Weather2Context context = new Weather2Context())
            {
                sensor = context.Sensors
                    .Where(s => s.Id == id)
                    .Include(s => s.Records)
                    .FirstOrDefault();
            }

            return sensor;
        }

        public static List<Record> GetRecordsForSensor(Sensor sensor)
        {
            var records = new List<Record>();

            using (Weather2Context context = new Weather2Context())
            {
                records = context.Records
                    .Where(r => r.SensorId == sensor.Id)
                    .ToList();
            }

            return records;
        }

        public static DailyAverages GetAveragesForSelectedSensorAndDay(Sensor sensor, DateTime date)
        {
            DailyAverages dailyAverages = new DailyAverages();

            using (Weather2Context context = new Weather2Context())
            {
                dailyAverages = context.Records
                    .Where(r => r.SensorId == sensor.Id)
                    .GroupBy(r => r.Time.Date)
                    .Where(g => g.Key == date)
                    .Select(g => new DailyAverages
                    {
                        Day = g.Key,
                        AverageTemperature = g.Average(r => r.Temperature),
                        AverageHumidity = g.Average(r => r.Humidity),
                        FungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity))
                    })
                    .FirstOrDefault();
            }

            return dailyAverages;
        }

        public static List<DailyAverages> GetDailyAveragesForSensor(Sensor sensor)
        {
            List<DailyAverages> daily = new List<DailyAverages>();

            using (Weather2Context context = new Weather2Context())
            {
                daily = context.Records
                   .Where(r => r.SensorId == sensor.Id)
                    .GroupBy(r => r.Time.Date)
                    .Select(g => new DailyAverages
                    {
                        Day = g.Key,
                        AverageTemperature = g.Average(r => r.Temperature),
                        AverageHumidity = g.Average(r => r.Humidity),
                        FungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity))
                    })
                    .ToList();
            }

            return daily;

        }
        public static bool DateHasRecords(DateTime date)
        {
            bool foundRecord = false;

            using (Weather2Context context = new Weather2Context())
            {

                var q = context.Records
                    .FirstOrDefault(r => r.Time.Date == date.Date);

                foundRecord = q == null ? false : true;
            }

            return foundRecord;
        }

        // Villkor för höst:
        // - Första dygnet av 5 dygn i rad med medeltemperatur under 10,0C
        // - Kan starta tidigast 1:a augusti

        // Villkor för vinter:
        // - Första dygnet av 5 dygn i rad med medeltemperatur under 0,0C
        // - Kan starta tidigast 1:a augusti

        // Det finns inte data för alla dagar, kollar 5 dagar tillbaka av de som har data
        public static DateTime GetStartOfSeason(Sensor sensor, string season, int year)
        {
            List<Record> records = DataAccess.GetRecordsForSensor(sensor);

            DateTime earliestStartForAutumnOrWinter = new DateTime(year, 8, 1);

            DailyAverages[] dailyAveragesArray = records
               .GroupBy(r => r.Time.Date)
               .Where(g => g.Key >= earliestStartForAutumnOrWinter)
               .Select(g => new DailyAverages
               {
                   Day = g.Key,
                   AverageTemperature = g.Average(r => r.Temperature)
               })
               .OrderBy(a => a.Day)
               .ToArray();

            double startTemp = 0.0;

            switch (season.ToLower())
            {
                case "höst":
                    startTemp = 10.0;
                    break;
                case "vinter":
                    startTemp = 0.0;
                    break;
                default:
                    break;
            }

            DateTime seasonStart = new DateTime();

            for (int i = 4; i < dailyAveragesArray.Length; i++)
            {
                if (dailyAveragesArray[i].AverageTemperature < startTemp &&
                    dailyAveragesArray[i - 1].AverageTemperature < startTemp &&
                    dailyAveragesArray[i - 2].AverageTemperature < startTemp &&
                    dailyAveragesArray[i - 3].AverageTemperature < startTemp &&
                    dailyAveragesArray[i - 4].AverageTemperature < startTemp)
                {
                    seasonStart = dailyAveragesArray[i - 4].Day;
                    break;
                }
            }

            return seasonStart;
        }

        public static List<DailyInOutTempDifference> GetDifferenceInTempInAndOut(int insideSensorId, int outsideSensorId)
        {
            Sensor insideSensor = GetSensor(insideSensorId);

            var insideDailyTemp = GetRecordsForSensor(insideSensor)
                .GroupBy(r => r.Time.Date)
                .Select(g => new
                {
                    day = g.Key,
                    temp = g.Average(r => r.Temperature)
                });

            Sensor outsideSensor = GetSensor(outsideSensorId);

            var outsideDailyTemp = GetRecordsForSensor(outsideSensor)
                .GroupBy(r => r.Time.Date)
                .Select(g => new
                {
                    day = g.Key,
                    temp = g.Average(r => r.Temperature)
                });


            var differenceList = insideDailyTemp
                .Select(inside => new DailyInOutTempDifference
                {
                    Day = inside.day.Date,
                    InsideTemperature = inside.temp,
                    OutsideTemperature = outsideDailyTemp
                        .Where(outside => outside.day.Date == inside.day.Date)
                        .FirstOrDefault()
                        .temp,
                    TempDifference = inside.temp - outsideDailyTemp
                        .Where(outside => outside.day.Date == inside.day.Date)
                        .FirstOrDefault()
                        .temp
                })
                .Where(d => d.InsideTemperature != null && d.OutsideTemperature != null)
                .OrderByDescending(a => a.TempDifference)
                .ToList();

            return differenceList;
        }


        public static int CreateSensors(string[] fileContent)
        {
            int numberOfNewSensors = 0;

            using (Weather2Context context = new Weather2Context())
            {
                // Skapar lista för att för vardera avläsning kolla om sensor finns, antar
                //att det inte går att kolla mot context.Sensors innan SaveChanges körts.
                List<Sensor> sensors = context.Sensors.ToList();

                foreach (string line in fileContent)
                {
                    string[] values = line.Split(',');
                    // [0]:Time [1]:Inne/Ute = SensorName [2]:Temp [3]:Humidity

                    if (sensors.Where(s => s.SensorName == values[1]).Count() == 0)
                    {
                        Sensor newSensor = new Sensor();
                        newSensor.SensorName = values[1];

                        sensors.Add(newSensor);
                        context.Sensors.Add(newSensor);
                    }
                }

                numberOfNewSensors = context.SaveChanges();
            }

            return numberOfNewSensors;
        }

        public static List<Sensor> GetSensorList()
        {
            List<Sensor> sensors = new List<Sensor>();

            using (Weather2Context context = new Weather2Context())
            {
                sensors = context.Sensors.ToList();
            }

            return sensors;
        }


        public static int LoadData(string[] fileContent)
        {
            int numberOfRecords = 0;

            using (Weather2Context context = new Weather2Context())
            {
                if (context.Records.Count() == 0)
                {
                    // Skapar lista med alla sensorer för att inte behöva hämta SensorId från context för alla avläsningar
                    List<Sensor> sensors = context.Sensors.ToList();

                    foreach (string line in fileContent)
                    {
                        // [0]:Time [1]:Inne/Ute = SensorName [2]:Temp [3]:Humidity
                        string[] values = line.Split(',');

                        Record record = new Record();

                        record.SensorId = sensors.FirstOrDefault(s => s.SensorName == values[1]).Id; // Kollade först mot context för varja avläsning men det tog väldigt lång tid

                        if (DateTime.TryParse(values[0], out DateTime time))
                        {
                            record.Time = time;
                        }

                        // Hade först problem med minustecken och decimaltecken, det löstes av NumberStyles och CultureInfo
                        if (double.TryParse(values[2], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double temp))
                        {
                            record.Temperature = temp;
                        }

                        // Här skulle man kunna ha en int som håller reda på hur många värden som inte kommer med, dvs har fel format
                        //else
                        //{
                        //    numberOfFailedTemperature++;
                        //}


                        if (int.TryParse(values[3], out int humidity))
                        {
                            record.Humidity = humidity;
                        }

                        // Här skulle man kunna ha en int som håller reda på hur många värden som inte kommer med, dvs har fel format
                        //else
                        //{
                        //    numberOfFailedHumidity++;
                        //}


                        if (record.Time != null && record.SensorId != 0)  // Temperature och Humidity tillåter null men inte Time och SensorId
                        {
                            context.Records.Add(record);
                        }

                        // Här skulle man kunna ha en int som håller reda på hur många rader som inte kommer med, dvs saknar Time eller SensorId
                        //else
                        //{
                        //    numberOfFailedRecords++;
                        //}
                    }
                }

                numberOfRecords = context.SaveChanges();
            }

            return numberOfRecords;
        }

        public static int GetNumberOfRecordsForSensor(Sensor sensor)
        {
            int numberOfRecordsecords = 0;

            using (Weather2Context context = new Weather2Context())
            {
                numberOfRecordsecords = context.Records
                    .Where(r => r.SensorId == sensor.Id)
                    .Count();
            }

            return numberOfRecordsecords;
        }

        public static double? GetFungusRisk(double? temp, double? humidity)
        {
            double? risk = (humidity - 78) * (temp / 15) / 0.22;

            return risk;
        }

        static void GetTimeForOpenBalconyDoor(int insideSensorId, int outsideSensorId, DateTime date)
        {

            //Jag tänkte man kunde göra något sådant här:
            // Medelvärde med 5 min intervall för inne och ute i varsin array.
            // Hitta tid för inne där temp sjunker
            // Kolla om temp ökar vid samma tid för ute
            // I så fall är balkongdörren öppen
            // Sedan omvänt för att hitta tid när dörren stängs

            // Har startat lite men inte fått det klart

            Sensor insideSensor = DataAccess.GetSensor(insideSensorId);

            var insideRecordsForDay = DataAccess.GetRecordsForSensor(insideSensor)
                .Where(r => r.Time.Date == date.Date)
                .OrderBy(r => r.Time)
                .GroupBy(g =>                   // gruppera records med 5 min intervall
                {
                    DateTime t = g.Time;
                    t = t.AddMinutes(-(t.Minute % 5));
                    t = t.AddMilliseconds(-t.Millisecond - 1000 * t.Second);
                    return t;
                })
               .Select(g => new
               {
                   time = g.Key,
                   temp = g.Average(r => r.Temperature)
               })
               .OrderBy(a => a.time)
               .ToArray();


            Sensor outsideSensor = DataAccess.GetSensor(outsideSensorId);

            var outsideRecordsForDay = DataAccess.GetRecordsForSensor(outsideSensor)
                .Where(r => r.Time.Date == date.Date)
                .GroupBy(g =>                   // gruppera records med 5 min intervall
                {
                    DateTime t = g.Time;
                    t = t.AddMinutes(-(t.Minute % 5));
                    t = t.AddMilliseconds(-t.Millisecond - 1000 * t.Second);
                    return t;
                })
                .Select(g => new
                {
                    time = g.Key,
                    temp = g.Average(r => r.Temperature)
                })
                .OrderBy(a => a.time)
                .ToArray();

            bool isOpen = false;
            List<DateTime> openTimes = new List<DateTime>();

            for (int i = 0; i < insideRecordsForDay.Length - 1; i++)
            {
                if (isOpen == true)
                {
                    break;
                }

                if (insideRecordsForDay[i].temp > insideRecordsForDay[i + 1].temp)
                {
                    for (int j = 0; j < outsideRecordsForDay.Length; j++)
                    {
                        if (outsideRecordsForDay[j].time.TimeOfDay.TotalMinutes == insideRecordsForDay[i].time.TimeOfDay.TotalMinutes &&
                            outsideRecordsForDay[j].time.TimeOfDay.TotalMinutes < insideRecordsForDay[i].time.TimeOfDay.TotalMinutes + 5 &&
                            outsideRecordsForDay[j].temp < outsideRecordsForDay[j + 1].temp)
                        {
                            openTimes.Add(insideRecordsForDay[i].time);
                        }
                    }
                }
            }

            for (int i = 0; i < openTimes.Count(); i++)
            {
                Console.WriteLine($"öppen {openTimes[i]}");
            }
        }
    }
}
