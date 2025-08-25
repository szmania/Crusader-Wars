using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CrusaderWars.twbattle
{

    /* --------------
     * NOT BEING USED 
     * ______________
    
    public class Date
    {
        public int Year;
        public int Month;
        public int Day;
        public Date(int year, int month, int day) 
        {
            Year = year;
            Month = month;
            Day = day;
        }
        public Date AddDays(int days)
        {
            DateTime dateTime = new DateTime(Year, Month, Day);
            dateTime = dateTime.AddDays(days);
            return new Date(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        public Date SubtractDays(int days)
        {
            return AddDays(-days);
        }

        public bool IsGreaterThan(Date other)
        {
            return CompareTo(other) > 0;
        }

        public bool IsLessThan(Date other)
        {
            return CompareTo(other) < 0;
        }

        public bool IsEqualTo(Date other)
        {
            return CompareTo(other) == 0;
        }
        private int CompareTo(Date other)
        {
            if (Year != other.Year)
                return Year.CompareTo(other.Year);

            if (Month != other.Month)
                return Month.CompareTo(other.Month);

            return Day.CompareTo(other.Day);
        }

    }
    */

 
}
