using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SutherlandScript
{
    public class SabreUtilities
    {
        public static string sqlCon = "";

        public static string getAirlineName(string _var)
        {
            try
            {
                string queryString = "SELECT AirlineName FROM dbo.Airlines where AirlineCode='" + _var + "'";

                string al = "";

                using (SqlConnection connection = new SqlConnection(sqlCon))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            al = reader[0].ToString();
                        }
                    }
                }

                //sqlCon = null;

                return al;
            }
            catch (Exception ex)
            {
                throw new LibraryErrorException("sabreUtilities" + ex.Message);
            }
        }

        public static string getCityName(string _var)
        {
            //string sqlCon = "Data Source=(local)\\SQLExpress; Initial Catalog = Travcom; User ID = sa";
            string queryString = "SELECT CITY_DESCR FROM dbo.tbl_AIRPORT where STATUS ='Y' and CITY_CODE='" + _var + "'";

            string al = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(sqlCon))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            al = reader[0].ToString();
                        }
                    }
                }

                //sqlCon = null;

                return al;
            }
            catch (Exception ex)
            {
                 throw new LibraryErrorException("sabreUtilities" + ex.Message);
            }
        }

        public static string getDateMonthDay(string _var, string varFormat)
        {
            string strDateData = _var.Substring(0, 10).Split('-')[2] + "/" + _var.Substring(0, 10).Split('-')[1] + "/" + _var.Substring(0, 9).Split('-')[0];

            return Convert.ToDateTime(strDateData).ToString(varFormat);
        }

        public static string getTime(string _var, bool is24Time)
        {
            int _index = _var.IndexOf(@"T") + 1;
            string strDateData = _var.Substring(_index, 5);
            DateTime dateTime = DateTime.ParseExact(strDateData, "HH:mm", CultureInfo.InvariantCulture);

            //return dateTime.ToString("H:mm tt", CultureInfo.CurrentCulture);
            string x;
            if (is24Time)
            {
                x = dateTime.ToString("HH:mm");
            }
            else
            {
                x = dateTime.ToString("hh:mm tt");
            }
            return x;
        }

        public static string deCodeStatusFind(string statusCode)
        {
            if (statusCode.Contains("HK"))
                return "CONFIRMED";
            else if (statusCode.Contains("GK") || statusCode.Contains("DK"))
                return "ON REQUEST";
            else if (statusCode.Contains("TK") || statusCode.Contains("KK"))
                return "WITH CHANGES";
            else if (statusCode.Contains("KL"))
                return "CONFIRMED FROM WAITLIST";
            else if (statusCode.Contains("RR"))
                return "RECONFIRMED";
            else if (statusCode.Contains("HL") || statusCode.Contains("GL"))
                return "WAITLISTED";
            else if (statusCode.Contains("UC"))
                return "UNABLE TO CONFIRM";
            else if (statusCode.Contains("HX"))
                return "CANCELLED";
            else
                return "UNITDENTIFIED";
        }
    }
}
