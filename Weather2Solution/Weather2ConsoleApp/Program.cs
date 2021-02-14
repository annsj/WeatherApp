using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Weather2DataAccessLibrary.DataAccess;
using Weather2DataAccessLibrary.Models;

namespace Weather2ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] fileContent = File.ReadAllLines("TemperaturData.txt");
            //string[] fileContent = File.ReadAllLines("TestData.txt");

            Console.WriteLine("Skapar sensorer...");
            int newSensors = DataAccess.CreateSensors(fileContent);

            Console.WriteLine(newSensors != 0 ? $"{newSensors} nya sensorer skapades i databasen." :
                                                 "Sensorer finns redan i databasen");
            Console.WriteLine();
            Console.WriteLine("Alla sensorer med antal avläsningar:");
            PrintSensors();
            Console.WriteLine("\n");

            Console.WriteLine("Laddar upp data...");
            int newRecords = DataAccess.LoadData(fileContent);
            Console.WriteLine(newRecords != 0 ? $"{newRecords} nya avläsningar registrerades." :
                                                 "Avläsningarna finns redan i databasen");
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Alla sensorer med antal avläsningar:");
            PrintSensors();
            Console.WriteLine("\n");

            Console.WriteLine("Tryck på Enter för att gå vidare.");
            Console.ReadLine();

            Console.Clear();

            bool newSelection = true;

            while (newSelection)
            {
                int selectedService = SelectFromMainMenu();

                switch (selectedService)
                {
                    case 1:
                        Console.Clear();
                        int inOrOut = SelectInOrOut();
                        Sensor sensor = DataAccess.GetSensor(inOrOut);
                        DateTime date = SelectDate();
                        Console.Clear();
                        Console.WriteLine("\nHämtar data...\n");
                        PrintAveragesForSelectedSensorAndDay(sensor, date);
                        break;
                    case 2:
                        Console.Clear();
                        inOrOut = SelectInOrOut();
                        Console.WriteLine("Hämtar data...\n");
                        sensor = DataAccess.GetSensor(inOrOut);
                        PrintSortedDailyAverages(sensor);
                        break;
                    case 3:
                        Console.Clear();
                        Console.WriteLine("\nHämtar resultat för höst...");
                        sensor = DataAccess.GetSensor(6); // Sensor 6 är "Ute"
                        PrintStartOfSeason(sensor, "Höst", 2016);
                        Console.WriteLine("Hämtar resultat för vinter...");
                        PrintStartOfSeason(sensor, "Vinter", 2016);
                        Console.WriteLine("Tryck på Enter för att komma till huvudmenyn");
                        Console.ReadLine();
                        break;
                    case 4:
                        Console.Clear();
                        Console.WriteLine("\nHämtar data...");
                        PrintDifferenceInTempInAndOut(5, 6); // Sensor 5 är "Inne", sensor 6 är "Ute"
                        break;
                    case 5:
                        Console.Clear();
                        Console.WriteLine("\nMetoden Öppen balkongdörr finns inte.\n");                        
                        Console.WriteLine("Tryck på Enter för att komma till huvudmenyn");
                        Console.ReadLine();
                        break;
                    case 6:
                        Console.WriteLine("Avslutat");
                        newSelection = false;
                        break;
                    default:
                        break;
                }
            }
        }

        static int SelectFromMainMenu()
        {
            Console.Clear();

            Console.WriteLine("\nHuvudmeny\n*********\n" +
                "1. Medelvärden för en dag.\n" +
                "2. Lista med medelvärden för alla dagar.\n" +
                "3. Datum för meteoroligisk höst och vinter 2016.\n" +
                "4. Lista med temperaturskillnad mellan inne och ute.\n" +
                "5. Öppen balkongdörr.\n" +
                "6. Avsluta.\n");

            Console.Write("Välj tjänst med siffra 1-5 eller avsluta med 6  ");
            Console.WriteLine();

            ConsoleKeyInfo input = Console.ReadKey(true);
            string inputToString = input.KeyChar.ToString();
            int selection = 0;

            while (true)
            {
                if (int.TryParse(inputToString, out int number) &&
                    number > 0 && number < 7)
                {
                    selection = number;
                    Console.WriteLine($"Val: {selection}\n\n");
                    break;
                }
                else
                {
                    Console.WriteLine("Ange nummer 1-6");
                    inputToString = Console.ReadKey(true).KeyChar.ToString();
                }
            }

            return selection;
        }

        static int SelectInOrOut()
        {
            string heading = "\nVälj värden från inne eller ute";

            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n" +
               "5. Inne.\n" +
               "6. Ute.\n");

            Console.Write("Välj inne eller ute med siffra 5 eller 6  ");

            ConsoleKeyInfo input = Console.ReadKey(true);
            string inputToString = input.KeyChar.ToString();
            int selection = 0;

            while (true)
            {
                if (int.TryParse(inputToString, out int number) &&
                    number > 4 && number < 7)
                {
                    selection = number;
                    Console.WriteLine($"\nVald plats: {selection}\n\n");
                    break;
                }
                else
                {
                    Console.WriteLine("\nAnge nummer 5 eller 6");
                    inputToString = Console.ReadKey(true).KeyChar.ToString();
                }
            }

            return selection;
        }

        private static DateTime SelectDate()
        {
            DateTime selectedDate = new DateTime(2016, 5, 31);

            Console.WriteLine($"Tryck \"j\" + Enter för att välja förinställt datum (2016-05-31) eller Enter för att välja datum");
            string input = Console.ReadLine();
            Console.WriteLine();

            if (input == "j")
            {
                return selectedDate;
            }

            else
            {
                Console.Write("Välj en dag under 2016 med formatet åååå-mm-dd, bekräfta med Enter ");
                string selectedDay = Console.ReadLine();

                while (true)
                {
                    if (DateTime.TryParse(selectedDay, out DateTime d))
                    {
                        selectedDate = d;
                        Console.Clear();
                        Console.WriteLine($"Valt datum: {selectedDate.ToShortDateString()}\n\n");

                        if (DataAccess.DateHasRecords(selectedDate))
                        {
                            break;
                        }

                        else
                        {
                            Console.Clear();
                            Console.Write($"Det finns inga inmatningar för {selectedDate.ToShortDateString()}, välj ett annat datum: ");
                            selectedDay = Console.ReadLine();
                        }
                    }

                    else
                    {
                        Console.Clear();
                        Console.WriteLine("Felaktig inmatning. Välj en dag under 2016 med formatet yyyy-mm-dd");
                        selectedDay = Console.ReadLine();
                    }
                }
            }

            return selectedDate;
        }

        static void PrintAveragesForSelectedSensorAndDay(Sensor sensor, DateTime date)
        {
            DailyAverages dailyAverages = DataAccess.GetAveragesForSelectedSensorAndDay(sensor, date);

            string heading = $"Dygnsmedelvärden från sensor \"{sensor.SensorName}\" för valt datum";

            Console.Clear();
            Console.WriteLine($"\n{heading}\n{Utils.GetUnderline(heading)}");
            Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");
            Console.WriteLine(dailyAverages);           
            Console.WriteLine();
            Console.WriteLine("Tryck på Enter för att komma till huvudmenyn");
            Console.ReadLine();
        }

        static void PrintSortedDailyAverages(Sensor sensor)
        {
            List<DailyAverages> dailyAverages = DataAccess.GetDailyAveragesForSensor(sensor);

            bool newSelection = true;

            while (newSelection)
            {
                var sorton = SelectSorting();

                switch (sorton)
                {
                    case Sortingselection.Date:
                        dailyAverages = dailyAverages.OrderBy(a => a.Day).ToList();
                        break;
                    case Sortingselection.Temperature:
                        dailyAverages = dailyAverages.OrderByDescending(a => a.AverageTemperature).ToList();
                        break;
                    case Sortingselection.Humidity:
                        dailyAverages = dailyAverages.OrderBy(a => a.AverageHumidity).ToList();
                        break;
                    case Sortingselection.FungusRisk:
                        dailyAverages = dailyAverages.OrderByDescending(a => a.FungusRisk).ToList();
                        break;
                    case Sortingselection.Quit:
                        newSelection = false;
                        break;
                    default:
                        break;
                }

                if (sorton != Sortingselection.Quit)
                {
                    dailyAverages = dailyAverages
                        .Where(a => a.AverageTemperature != null && a.AverageHumidity != null && a.FungusRisk != null).ToList();

                    string heading = GetHeadingForSortedList(sorton, sensor.SensorName);

                    Console.Clear();
                    Console.WriteLine($"\n{heading}\n{Utils.GetUnderline(heading)}");
                    Console.WriteLine($"Visar resultat för {dailyAverages.Count()} dygn.\n");
                    Console.WriteLine("Tryck Enter för att göra ett nytt sorteringsval\n");
                    Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");

                    foreach (var day in dailyAverages)
                    {
                        Console.WriteLine(day);
                    }

                    Utils.ScrollToTop();
                    Console.Clear();
                }
            }
        }

        enum Sortingselection
        {
            [Display(Name = "Datum")]
            Date = 1,

            [Display(Name = "Temperatur")]
            Temperature,

            [Display(Name = "Fuktighet")]
            Humidity,

            [Display(Name = "Mögelrisk")]
            FungusRisk,

            [Display(Name = "Avsluta")]
            Quit
        }

        private static Sortingselection SelectSorting()
        {
            string heading = "Välj sorteringsordning";

            Console.WriteLine($"\n{heading}\n{Utils.GetUnderline(heading)}\n" +
                "1. Datum, tidigt till sent.\n" +
                "2. Temperatur, varmast till kallast.\n" +
                "3. Fuktighet, torrast till fuktigast.\n" +
                "4. Risk för mögel, högst till lägst risk.\n" +
                "5. Huvudmeny.\n");

            Console.Write("Välj med siffra 1-4 eller gå tillbaka till huvudmenyn med 5  ");
            Console.WriteLine();

            ConsoleKeyInfo input = Console.ReadKey(true);
            string inputToString = input.KeyChar.ToString();
            int selection = 0;

            while (true)
            {
                if (int.TryParse(inputToString, out int number) &&
                    number > 0 && number < 6)
                {
                    selection = number;
                    Console.WriteLine($"Vald sortering: {(Sortingselection)selection}\n\n");
                    break;
                }
                else
                {
                    Console.WriteLine("Ange nummer 1-5");
                    inputToString = Console.ReadKey(true).KeyChar.ToString();
                }
            }

            return (Sortingselection)selection;
        }

        static void PrintStartOfSeason(Sensor sensor, string season, int year)
        { 
            DateTime seasonStart = DataAccess.GetStartOfSeason(sensor, season, year);

            string printString = seasonStart != default ?
                   $"\n{season} blev det {seasonStart.ToShortDateString()}.\n" :
                   $"\nDet blev inte {season.ToLower()} innan {year} års slut.\n";

            Console.WriteLine(printString);
        }

        private static string GetHeadingForSortedList(Sortingselection sortOn, string sensorName)
        {
            string heading = $"Dygnsmedelvärden från sensor \"{sensorName}\" sorterat på";

            switch (sortOn)
            {
                case Sortingselection.Date:
                    heading += " datum";
                    break;
                case Sortingselection.Temperature:
                    heading += " temperatur, varmast till kallast";
                    break;
                case Sortingselection.Humidity:
                    heading += " fuktighet, torrast till fuktigast";
                    break;
                case Sortingselection.FungusRisk:
                    heading += $" risk för mögel, högst till lägst risk";
                    break;
                default:
                    heading = "Ingen tabell tillgänglig, felaktigt sorteringsval";
                    break;
            }

            return heading;
        }

        private static void PrintDifferenceInTempInAndOut(int insideSensorId, int outsideSensorId)
        {
            var differenceList = DataAccess.GetDifferenceInTempInAndOut(insideSensorId, outsideSensorId);

            string heading = "\nDygnsmedeltemperaturer och skillnad mellan inne och ute, sorterat på skillnad";

            Console.Clear();
            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n");
            Console.WriteLine("Tryck på Enter för att komma till huvudmenyn\n");
            Console.WriteLine("Datum\t\tInne\tUte\tSkillnad");

            foreach (var day in differenceList)
            {
                Console.WriteLine(day);
            }

            Utils.ScrollToTop();
        }



        static void PrintSensors()
        {
            List<Sensor> sensors = DataAccess.GetSensorList();

            foreach (var sensor in sensors)
            {
                Console.WriteLine(sensor);
            }
        }

        static void PrintRecords(List<Record> records)
        {
            foreach (var record in records)
            {
                Console.WriteLine(record);
            }
        }
    }
}
