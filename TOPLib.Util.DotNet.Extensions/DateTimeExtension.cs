using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class DateTimeExtension
    {
        private static readonly DateTime _SqlMinValue = DateTime.Parse("1/1/1753 12:00:00 AM");

        public static DateTime SqlMinValue
        {
            get
            {
                return _SqlMinValue;
            }
        }
    }
}
