namespace CrusaderWars
{
    public static class Date
    {
        public static int Month { get; set; }
        public static int Year { get; set; }
        public static int Day { get; set; }
        
        public static string GetSeason()
        {
            int month = Month;
            string season = "summer";
            //Winter
            if ((month >= 1 && month <= 2) || month == 12)
            {
                season = "winter";
                return season;
            }
            //Spring
            else if (month >= 3 && month <= 5)
            {
                season = "spring";
                return season;
            }
            //Summer
            else if (month >= 6 && month <= 8)
            {
                season = "summer";
                return season;
            }
            //Autumn
            else if (month >= 9 && month <= 11)
            {
                season = "fall";
                return season;
            }

            return season;
        }

        public static void Reset()
        {
            Month = 0;
            Year = 0;
            Day = 0;
        }
    }

    
}
