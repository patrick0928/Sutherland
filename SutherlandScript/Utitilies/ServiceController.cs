using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SutherlandScript
{
    public class ServiceController
    {
        /*
        List of Available Methods:
            - SearchRoundTripFlights ~ searching for roundtrip flights
            - SearchOneWayFlights ~ searching for one way flights
            - SearchMultiCityFlights ~ searching for multi-city flights
            - CreatePNR ~ creating reservation
            - RetrievePNR ~ retrieve reservation
            - CancelPNR ~ cancel reservation, removing itinerary XI command
            - CompareCurrentPrice ~ compare current price from the time of searching upto the click book now to revalidate current amount
            - AddDocumentPassport ~ add passport information to the existing reservation
            - GetFareRule ~ retrieve fare restrictions and rulings
 
        Rulings:
            FIND this variable ~ const string _url = "http://localhost:91"; //put your public ip here
            Origin, Destination ~ must be 3-LETTER AIRPORT/CITY CODE
            ValidatingCarrier ~ must be 2-LETTER AIRLINE CODE
            Departure,Arrival,Birthdate ~ must be this format 2019-05-28
            Pwd,Senior ~ Person w/ Disability and Senior Citizen is for domestic flights only, not applicable for international. Leave it 0.
            Cabin ~ Valid values are: 'PremiumFirst', 'First', 'PremiumBusiness', 'Business', 'PremiumEconomy', 'Economy'
            BaggageAdt,BaggageCnn,BaggageInf ~ '2P' meaning 2 piece/s of baggage (max 23kilos each) and '20K' meaning max 20 kilos of baggage allowance
            IncludeDoca ~ set to TRUE if you want to insert passport info, else FALSE
            PaxTitle ~ Valid values are: 'Mr.' and 'Ms.'
            PaxType ~ Valid values are: 'ADT' for Adult, 'CNN' for Child, 'INF' for Infant, 'SRC' for Senior Citizen, 'DIS' for PWD
            NameNumber ~ if 1 pax only '1.1' if 3 pax '1.1','2.1','3.1'
            PhoneType ~ if 'M' mobile, 'A' agency contact
 
        */

        public List<AirDetails> SearchRoundTripFlights(string origin, string destination, string departure, string arrival, int adult, int child, int infant, int senior = 0, int pwd = 0, bool isdirect = true, string cabin = "Economy")
        {

            CustomServiceRequest sv = new CustomServiceRequest(origin, destination, adult, child, infant, senior, pwd, cabin, departure, arrival, isdirect);
            string json = GetResponse(sv.LowFareSearchRT());
            List<AirDetails> result = new List<AirDetails>();
            if (!json.Contains("NOFLIGHT"))
            {
                result = JsonConvert.DeserializeObject<List<AirDetails>>(json);
            }
            return result;
        }

        public List<AirDetails> SearchOneWayFlights(string origin, string destination, string departure, int adult, int child, int infant, int senior = 0, int pwd = 0, bool isdirect = true, string cabin = "Economy")
        {
            CustomServiceRequest sv = new CustomServiceRequest(origin, destination, adult, child, infant, senior, pwd, cabin, departure, isdirect);
            string json = GetResponse(sv.LowFareSearchOW());
            List<AirDetails> result = new List<AirDetails>();
            if (!json.Contains("NOFLIGHT"))
            {
                result = JsonConvert.DeserializeObject<List<AirDetails>>(json);
            }
            return result;
        }

        public List<AirDetails> SearchMultiCityFlights(List<MultiFlightSegment> _segments, int adult, int child, int infant, int senior = 0, int pwd = 0, bool isdirect = true)
        {
            MultiCityData _req = new MultiCityData();
            _req.Adult = adult;
            _req.Child = child;
            _req.Infant = infant;
            _req.Senior = senior;
            _req.Pwd = pwd;
            _req.Origin = _segments[0].Origin;
            _req.Destination = _segments[0].Destination;
            _req.CabinClass = _segments[0].Class;
            _req.segments = _segments;
            _req.isDirect = isdirect;

            string postStr = "=" + JsonConvert.SerializeObject(_req).ToString();
            CustomServiceRequest svc = new CustomServiceRequest(postStr);
            string json = GetResponse(svc.LowFareSearchMC());
            List<AirDetails> result = new List<AirDetails>();
            if (!json.Contains("NOFLIGHT"))
            {
                result = JsonConvert.DeserializeObject<List<AirDetails>>(json);
            }
            return result;
        }

        public string CreatePNR(List<AirSegmentDetails> segments, List<PassengerName> passengers, List<PassengerContact> contacts, double totalAmount, string origin, string destination, string validatingCarrier, string requesterPaxTitle, string requesterFirstName, string requesterLastName, string email, string currency = "PHP", int createdBy = 0)
        {
            try
            {
                CreatePassengerNameRecordData _req = new CreatePassengerNameRecordData();

                _req.names = passengers;
                _req.contacts = contacts;
                _req.segments = segments;
                _req.isMulti = false;

                AgencyProfile ag = new AgencyProfile();

                // change this part if B2B
                ag.AgencyName = "BCD PHILSCAN TRAVEL"; //required put the name of agency. Max of 50 Chars. Special chars not allowed like & ( ) #  % * ^
                ag.City = "MAKATI CITY"; //required to put the address city of agency. Max of 20 Chars. Special chars not allowed like & ( ) #  % * ^
                ag.PostalCode = "1231"; //required to put the postal code.
                ag.Address = "2F ROYAL ENTERPRISE BLDG 2227 CHINO ROCES AVE"; //required to put the address. Max of 50 Chars. MSpecial chars not allowed like & ( ) #  % * ^
                _req.AgencyAddressLine = ag.AgencyName;
                _req.AgencyCityName = ag.City;
                _req.AgencyCountryCode = "PH";
                _req.AgencyPostalCode = ag.PostalCode;
                _req.AgencyStreetNmbr = ag.Address;
                _req.TicketingDeadline = "7TAW";
                _req.SpecialRequestRemarks = "";

                _req.PaxTitle = requesterPaxTitle; //Request Title. Optional, for database record only.
                _req.FirstName = requesterFirstName; //Requester First Name. Optional, for database record only.
                _req.LastName = requesterLastName; //Requester Last Name. Optional, for database record only.
                _req.Email = email; //Requester Email or Email of the First Passenger. Required.
                _req.Origin = origin; //Search Origin. Required.
                _req.Destination = destination; //Search Destination. Required.
                _req.Curr = currency; //Booking Currency. Required.
                _req.TotalAmountBeforeBook = totalAmount; //initial total amount before PNR creation. Optional, for database record only.
                _req.PricingCarrier = validatingCarrier; //Validating or the Pricing Carrier. Required.
                _req.CreatedBy = createdBy; //change this to EmpID if B2B. Optional, for database record only.


                string postStr = "=" + JsonConvert.SerializeObject(_req).ToString();
                CustomServiceRequest svc = new CustomServiceRequest(postStr);
                string reloc = GetResponse(JsonConvert.SerializeObject(svc.CreateReservation()));
                return reloc;
            }
            catch (Exception e)
            {
                return "FAILED - " + e.GetBaseException().ToString();
            }
        }

        public PnrMain RetrievePNR(string recordLocator)
        {
            PnrMain result = new PnrMain();
            try
            {
                CustomServiceRequest svc = new CustomServiceRequest(recordLocator, false);
                string json = GetResponse(JsonConvert.SerializeObject(svc.RetrieveReservation()));
                if (!json.Contains("ERROR"))
                {
                    result = JsonConvert.DeserializeObject<PnrMain>(json);
                }
            }
            catch (Exception x)
            {
                string errs = x.GetBaseException().ToString();
            }
            return result;
        }

        public string CancelPNR(string recordLocator, string receivedFrom)
        {
            CustomServiceRequest svc = new CustomServiceRequest(recordLocator, false, receivedFrom);
            string json = GetResponse(JsonConvert.SerializeObject(svc.CancelReservation()));
            string result = json;
            return result;
        }

        public List<AirDetails> CompareCurrentPrice(string origin, string destination, List<AirSegmentDetails> segments, int adult, int child, int infant, int senior = 0, int pwd = 0, bool isdirect = true)
        {
            ReValidateItineraryData _req = new ReValidateItineraryData();

            _req.Origin = origin;
            _req.Destination = destination;
            _req.Adult = adult;
            _req.Child = child;
            _req.Infant = infant;
            _req.Senior = senior;
            _req.Pwd = pwd;
            _req.isMulti = false;
            _req.segments = segments;

            string postStr = "=" + JsonConvert.SerializeObject(_req).ToString();
            CustomServiceRequest svc = new CustomServiceRequest(postStr);
            string json = GetResponse(JsonConvert.SerializeObject(svc.ComparePriceInformation()));
            List<AirDetails> result = new List<AirDetails>();
            if (!json.Contains("NOFLIGHT"))
            {
                result = JsonConvert.DeserializeObject<List<AirDetails>>(json);
            }
            return result;
        }

        public string AddDocumentPassport(string recordLocator, List<AirSegmentDetails> segments, List<PassengerName> passengers)
        {
            ModifySpecialServiceData _req = new ModifySpecialServiceData();
            _req.names = passengers;
            _req.segments = segments;
            _req.RecordLocator = recordLocator;
            string postStr = "=" + JsonConvert.SerializeObject(_req).ToString();

            CustomServiceRequest svc = new CustomServiceRequest(postStr);
            string json = GetResponse(JsonConvert.SerializeObject(svc.GetModifyDocumentInformation()));
            string result = json;
            return result;
        }

        public FareRule GetFareRule(string farebasis, string cabin, string carrier, string origin, string destination)
        {
            CustomServiceRequest sv = new CustomServiceRequest(farebasis, cabin, origin, destination, carrier);
            string json = GetResponse(sv.GetFareRuleInformation());
            FareRule result = new FareRule();
            if (!json.Contains("ERROR"))
            {
                result = JsonConvert.DeserializeObject<FareRule>(json);
            }
            return result;
        }

        public string AddRemark(string recordLocator, string receivedFrom, string command)
        {
            CustomServiceRequest svc = new CustomServiceRequest(recordLocator, receivedFrom, command, 1);
            string json = GetResponse(JsonConvert.SerializeObject(svc.AddRemarkInformation()));
            string result = json;
            return result;
        }

        private string GetResponse(string response)
        {
            //HttpContext.Current.Response.Clear();
            // HttpContext.Current.Response.ContentType = "application/json";
            // HttpContext.Current.Response.Write(response);
            //  HttpContext.Current.ApplicationInstance.CompleteRequest();
            //Response.Flush();
            //Response.SuppressContent = true;
            response = response.Replace(@"\", "").Trim('"');

            return response;
        }

        public string SabreCommand(string command)
        {
            CustomServiceRequest svc = new CustomServiceRequest(command);
            string json = GetResponse(JsonConvert.SerializeObject(svc.SendCommandInformation()));           
            string result = json;
            MessageBox.Show(result);
            return result;
        }

    }


    public class CustomServiceRequest : ISabreBusiness
    {
        //const string _url = "http://localhost:91";
        const string _url = "http://203.160.184.51:91/sabreapi"; //put your public ip here

        string _origin { get; set; }
        string _destination { get; set; }
        int _adult { get; set; }
        int _child { get; set; }
        int _infant { get; set; }
        int _senior { get; set; }
        int _pwd { get; set; }
        string _cabin { get; set; }
        string _departure_time { get; set; }
        string _arrival_time { get; set; }
        bool _is_direct { get; set; }
        bool _is_oneway { get; set; }
        string _farebasisfr { get; set; }
        string _cabinfr { get; set; }
        string _originfr { get; set; }
        string _destfr { get; set; }
        string _carrierfr { get; set; }


        int _ctype { get; set; }
        ResultType _type { get; set; }
        string _postStr { get; set; }
        string _locatorCode { get; set; }
        string _by { get; set; }
        string _ticketNo { get; set; }
        string _nameSelect { get; set; }
        string _priceQuote { get; set; }
        string _currfrom { get; set; }
        string _currto { get; set; }
        string _curramount { get; set; }
        string _remarks { get; set; }
        int _data { get; set; }

        public CustomServiceRequest()
        { }


        public CustomServiceRequest(string origin, string destination, int adult, int child, int infant, int senior, int pwd, string cabin, string departure, string arrival, bool isdirect)
        {
            _origin = origin;
            _destination = destination;
            _adult = adult;
            _child = child;
            _infant = infant;
            _senior = senior;
            _pwd = pwd;
            _cabin = cabin;
            _departure_time = departure;
            _arrival_time = arrival;
            _is_direct = isdirect;
        }

        public CustomServiceRequest(string origin, string destination, int adult, int child, int infant, int senior, int pwd, string cabin, string departure, bool isdirect)
        {
            _origin = origin;
            _destination = destination;
            _adult = adult;
            _child = child;
            _infant = infant;
            _senior = senior;
            _pwd = pwd;
            _cabin = cabin;
            _departure_time = departure;
            _is_direct = isdirect;
        }

        public CustomServiceRequest(string farebasisfr, string cabinfr, string originfr, string destfr, string carrierfr)
        {
            _farebasisfr = farebasisfr;
            _cabinfr = cabinfr;
            _originfr = originfr;
            _destfr = destfr;
            _carrierfr = carrierfr;
        }

        public CustomServiceRequest(string locatorcode, bool isObsolete)
        {
            _locatorCode = locatorcode;
        }

        public CustomServiceRequest(string locatorcode, bool isObsolete, string by)
        {
            _locatorCode = locatorcode;
            _by = by;
        }

        public CustomServiceRequest(string currfrom, string amount, string currto, DateTime currdate)
        {
            _currfrom = currfrom;
            _currto = currto;
            _curramount = amount;
        }

        public CustomServiceRequest(string locatorcode, string nameSelect, string priceQuote, string by)
        {
            _locatorCode = locatorcode;
            _nameSelect = nameSelect;
            _priceQuote = priceQuote;
            _by = by;
        }

        public CustomServiceRequest(string locatorcode, string tno, string by)
        {
            _locatorCode = locatorcode;
            _ticketNo = tno;
            _by = by;
        }

        public CustomServiceRequest(string locatorcode, string by, string remarks, int data)
        {
            _locatorCode = locatorcode;
            _remarks = remarks;
            _by = by;
            //_data = data;
        }

        public CustomServiceRequest(string postStr)
        {
            _postStr = postStr;
        }

        string Endpoint(RequestType type)
        {
            StringBuilder sb = new StringBuilder();

            switch (type)
            {
                case RequestType.LowFareSearchRoundTrip:
                    sb.AppendFormat("{0}/roundtripfares/origin/{1}/destination/{2}/from/{3}/end/{4}/adult/{5}/child/{6}/infant/{7}/senior/{8}/pwd/{9}/cabinclass/{10}/direct/{11}", _url, _origin, _destination, _departure_time, _arrival_time, _adult, _child, _infant, _senior, _pwd, _cabin, _is_direct);
                    break;
                case RequestType.LowFareSearchOneWay:
                    sb.AppendFormat("{0}/onewayfares/origin/{1}/destination/{2}/from/{3}/adult/{4}/child/{5}/infant/{6}/senior/{7}/pwd/{8}/cabinclass/{9}/direct/{10}", _url, _origin, _destination, _departure_time, _adult, _child, _infant, _senior, _pwd, _cabin, _is_direct);
                    break;
                case RequestType.LowFareSearchMultiCity:
                    sb.AppendFormat("{0}/multicityfares", _url);
                    break;
                case RequestType.CreatePNR:
                    sb.AppendFormat("{0}/pnr/create", _url);
                    break;
                case RequestType.RetrievePNR:
                    sb.AppendFormat("{0}/pnr/retrieve/{1}", _url, _locatorCode);
                    break;
                case RequestType.CancelPNR:
                    sb.AppendFormat("{0}/pnr/cancel/{1}/cancelby/{2}", _url, _locatorCode, _by);
                    break;
                case RequestType.ComparePrice:
                    sb.AppendFormat("{0}/comparecurrentfare", _url);
                    break;
                case RequestType.RetrieveFareRule:
                    sb.AppendFormat("{0}/farerule/farebasis/{1}/cabin/{2}/origin/{3}/dest/{4}/carrier/{5}/ticketdes/{6}", _url, _farebasisfr, _cabinfr, _originfr, _destfr, _carrierfr, _cabinfr);
                    break;
                case RequestType.AddPassportDocument:
                    sb.AppendFormat("{0}/pnr/document/passport", _url);
                    break;
                case RequestType.AddRemarksPNRSabre:
                    sb.AppendFormat("{0}/pnr/{1}/remarks/{2}/modifyby/{3}", _url, _locatorCode, _ticketNo, _by);
                    break;
                case RequestType.SabreCommand:
                    sb.AppendFormat("{0}/sabrecommand/", _url, _locatorCode, _ticketNo, _by);
                    break;
            }
            return sb.ToString();
        }

        string GetAPIResponse(RequestType _type, string _postdata) //http://203.160.184.51:91/sabreapi   HDDXFX
        {
            try
            {
                string _endpoint = this.Endpoint(_type);
                var request = (HttpWebRequest)WebRequest.Create(_endpoint);             
                var postData = _postdata;
                var data = Encoding.ASCII.GetBytes(postData);
                
                request.Method = "POST";

                string _auth = string.Format("{0}:{1}", "PHILSCAN", "1b02d815625b4585ba1abd2fd200b7f8");
                string _enc = Convert.ToBase64String(Encoding.ASCII.GetBytes(_auth));
                string _cred = string.Format("{0} {1}", "Basic", _enc);
                request.Headers[HttpRequestHeader.Authorization] = _cred;

                if (_type == RequestType.LowFareSearchRoundTrip || _type == RequestType.LowFareSearchOneWay || _type == RequestType.RetrievePNR)
                {
                    request.ContentType = "application/json";
                }
                else
                {
                    request.ContentType = "application/json";                   
                }

                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return responseString;
            }
            catch (Exception x)
            {
                string ex = x.GetBaseException().ToString();
                MessageBox.Show(x.ToString());
                return "";
            }
        }

        public string LowFareSearchRT()
        {
            return GetAPIResponse(RequestType.LowFareSearchRoundTrip, "");
        }

        public string LowFareSearchOW()
        {
            return GetAPIResponse(RequestType.LowFareSearchOneWay, "");
        }

        public string LowFareSearchMC()
        {
            return GetAPIResponse(RequestType.LowFareSearchMultiCity, _postStr);
        }

        public string CreateReservation()
        {
            return GetAPIResponse(RequestType.CreatePNR, _postStr);
        }

        public string RetrieveReservation()
        {
            return GetAPIResponse(RequestType.RetrievePNR, "");
        }

        public string CancelReservation()
        {
            return GetAPIResponse(RequestType.CancelPNR, "");
        }

        public string ComparePriceInformation()
        {
            return GetAPIResponse(RequestType.ComparePrice, _postStr);
        }

        public string GetFareRuleInformation()
        {
            return GetAPIResponse(RequestType.RetrieveFareRule, "");
        }

        public string GetModifyDocumentInformation()
        {
            return GetAPIResponse(RequestType.AddPassportDocument, _postStr);
        }

        public string AddRemarkInformation()
        {
            return GetAPIResponse(RequestType.AddRemarksPNRSabre, "");
        }

        public string SendCommandInformation()
        {
            return GetAPIResponse(RequestType.SabreCommand, "");
        }

    }

    public enum ResultType
    {
        NonQuery,
        Scalar,
        DataTable
    }

    public enum RequestType
    {
        LowFareSearchRoundTrip,
        LowFareSearchOneWay,
        LowFareSearchMultiCity,
        CreatePNR,
        RetrievePNR,
        CancelPNR,
        ComparePrice,
        RetrieveFareRule,
        AddPassportDocument,
        AddRemarksPNRSabre,
        SabreCommand
    }

    public interface ISabreBusiness
    {
        string LowFareSearchRT();
        string LowFareSearchOW();
        string CreateReservation();
    }

    public class AgencyProfile
    {
        public int AgencyCode { get; set; }
        public string AgencyName { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string TelNo { get; set; }
        public string TelNo1 { get; set; }
        public string MobNo { get; set; }
        public string MobNo1 { get; set; }
        public string ContactName { get; set; }
        public string PseudoCode { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
    }

    public class BargainFinderMaxData
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CabinClass { get; set; }
        public bool isDirect { get; set; }
        public bool isOneWay { get; set; }
        public int Adult { get; set; }
        public int Child { get; set; }
        public int Infant { get; set; }
        public int Senior { get; set; }
        public int Pwd { get; set; }
    }

    public class ReValidateItineraryData
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public int Adult { get; set; }
        public int Child { get; set; }
        public int Infant { get; set; }
        public int Senior { get; set; }
        public int Pwd { get; set; }
        public bool isMulti { get; set; }
        public List<AirSegmentDetails> segments { get; set; }
    }

    public class MultiCityData
    {
        public string CabinClass { get; set; }
        public bool isDirect { get; set; }
        public int Adult { get; set; }
        public int Child { get; set; }
        public int Infant { get; set; }
        public int Senior { get; set; }
        public int Pwd { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public List<MultiFlightSegment> segments { get; set; }
    }

    public class ModifySpecialServiceData
    {
        public string RecordLocator { get; set; }
        public List<PassengerName> names { get; set; }
        public List<AirSegmentDetails> segments { get; set; }
    }

    public class CreatePassengerNameRecordData
    {
        public string AgencyAddressLine { get; set; }
        public string AgencyCityName { get; set; }
        public string AgencyCountryCode { get; set; }
        public string AgencyPostalCode { get; set; }
        public string AgencyStateCountyProvCode { get; set; }
        public string AgencyStreetNmbr { get; set; }
        public string TicketingDeadline { get; set; }
        public string SpecialRequestRemarks { get; set; }
        public string Email { get; set; }
        public List<PassengerContact> contacts { get; set; }
        public List<PassengerName> names { get; set; }
        public List<AirSegmentDetails> segments { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Curr { get; set; }
        public double TotalAmountBeforeBook { get; set; }
        public string PaxTitle { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PricingCarrier { get; set; }
        public int CreatedBy { get; set; }
        public bool isMulti { get; set; }
    }

    public class PassengerContact
    {
        public string NameNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneType { get; set; }
    }

    public class PassengerName
    {
        public string NameNumber { get; set; }
        public string NameReference { get; set; }
        public string PaxType { get; set; }
        public string SurName { get; set; }
        public string GivenName { get; set; }
        public string PaxTitle { get; set; }
        public string Nationality { get; set; }
        public string Birthdate { get; set; }
        public string PassportNo { get; set; }
        public string DateOfExpiry { get; set; }
        public bool IncludeDoca { get; set; }
        public string RefID { get; set; }
    }

    public class MultiFlightSegment
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Departure { get; set; }
        public string Class { get; set; }
    }

    public class AirSegmentDetails
    {
        public string RecordLocator { get; set; }
        public string Key { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string DepartureDateTime { get; set; }
        public string ArrivalDateTime { get; set; }
        public string DepartureTerminal { get; set; }
        public string ArrivalTerminal { get; set; }
        public string OperatingCarrier { get; set; }
        public string MarketingCarrier { get; set; }
        public string OperatingCarrierImage { get; set; }
        public string MarketingCarrierImage { get; set; }
        public string OperatingCarrierIcon { get; set; }
        public string MarketingCarrierIcon { get; set; }
        public string FlightNumber { get; set; }
        public string Equipment { get; set; }
        public string CabinClass { get; set; }
        public string MarriageGrp { get; set; }
        public string FlightTime { get; set; } //ElapsedTime
        public string Distance { get; set; } //AirMilesFlown
        public string BaggageAdt { get; set; }
        public string BaggageCnn { get; set; }
        public string BaggageInf { get; set; }
        public string ClassOfService { get; set; }
        public string FareBasisCode { get; set; }
        public string ProviderCode { get; set; }
        public long StopQuantity { get; set; }
        public int Group { get; set; }
        public string StatusCode { get; set; }
        public string MealCode { get; set; }
        public bool ChangeOfGauge { get; set; }
        public bool ETicket { get; set; }
        public bool ScheduleChangeIndicator { get; set; }
        public string AirlineRefID { get; set; }
        public int NumberInParty { get; set; }
        public bool ConnectingFlight { get; set; }
        public List<AirSegmentStops> AirStops { get; set; }
    }

    public class AirSegmentStops
    {
        public string RecordLocator { get; set; }
        public string Key { get; set; }
        public string LocationCode { get; set; }
        public string DepartureDateTime { get; set; }
        public string ArrivalDateTime { get; set; }
        public string Duration { get; set; }
        public string Equipment { get; set; }
    }

    public class AirDetails
    {
        public string Key { get; set; }
        public double FinalTotalPrice { get; set; }
        public double OriginalTotalPrice { get; set; }
        public double BasePrice { get; set; }
        public double ComputedCommission { get; set; }
        public double TotalTaxes { get; set; }
        public string Currency { get; set; }
        public string BaseFareCurrency { get; set; }
        public List<AirSegmentDetails> Segments { get; set; }
        public List<JourneyTime> Journey { get; set; }
        public List<FareBasisCodes> FareBasisCodes { get; set; }
        public List<FareInfo> FareInfo { get; set; }
        public List<PaxFareBreakdownFromFlight> PaxFareBreakdown { get; set; }
        public long SequenceNumber { get; set; }
        public string PriceCarrier { get; set; }
        public string PriceCarrierIcon { get; set; }
        public string PriceCarrierImage { get; set; }
        public double BaseFareUSD { get; set; }
        public int IsPrivateFare { get; set; }
        public double ServiceCharge { get; set; }
    }

    public class JourneyTime
    {
        public string TravelTime { get; set; }
    }

    public class FareBasisCodes
    {
        public string FareBasis { get; set; }
        public string AirSegmentRef { get; set; }
        public string CabinClass { get; set; }
        public string DepartureAirportCode { get; set; }
        public string ArrivalAirportCode { get; set; }
        public string GovCarrier { get; set; }
    }

    public class FareInfo
    {
        public int SeatsRemaining { get; set; }
        public string CabinClass { get; set; }
        public string MealCode { get; set; }
        public string FareReference { get; set; }
    }

    public class PaxFareBreakdownFromFlight
    {
        public string PaxType { get; set; }
        public int Qty { get; set; }
        public double BaseFare { get; set; }
        public double TotalTaxes { get; set; }
        public double AirlineComm { get; set; }
        public double ServiceFee { get; set; }
        public string CurrCode { get; set; }
        public List<TaxBreakdownFromFlight> TaxBreakdown { get; set; }
    }

    public class TaxBreakdownFromFlight
    {
        public string TaxCode { get; set; }
        public double TaxAmount { get; set; }
        public string CountryCode { get; set; }
        public string CurrCode { get; set; }
    }

    public class PnrMain
    {
        public string RecordLocator { get; set; }
        public DateTime DateTimeCreated { get; set; }
        public string CreatedAgentID { get; set; }
        public DateTime FlightStartDate { get; set; }
        public DateTime FlightEndDate { get; set; }
        public string PseudoCityCode { get; set; }
        public string AgentSine { get; set; }
        public string ISOCountry { get; set; }
        public string AgentDutyCode { get; set; }
        public string AirlineVendorID { get; set; }
        public string SequenceNumber { get; set; }
        public string PocAirport { get; set; }
        public DateTime PocDeparture { get; set; }
        public int NumberInParty { get; set; }
        public int NumberOfInfants { get; set; }
        public int NumberOfChilds { get; set; }
        public int NumberOfAdults { get; set; }
        public string MainEmail { get; set; }
        public string AddressName { get; set; }
        public string AddressLine { get; set; }
        public string AddressCityCtry { get; set; }
        public string AddressZipCode { get; set; }
        public string ReceivedFrom { get; set; }
        public string PhoneNumber { get; set; }
        public string RequestorFirstName { get; set; }
        public string RequestorLastName { get; set; }
        public string PaymentSuccessCode { get; set; }
        public string PaymentRefNo { get; set; }
        public string RequestorPaxTitle { get; set; }
        public string RequesterOrigin { get; set; }
        public string RequesterDest { get; set; }
        public int IsOneWay { get; set; }
        public string TransStatus { get; set; }
        public string PriceCarrier { get; set; }
        public string PriceCarrierDesc { get; set; }
        public double FinalTotalPrice { get; set; }
        public double OriginalTotalPrice { get; set; }
        public double BasePrice { get; set; }
        public double ComputedCommission { get; set; }
        public double TotalTaxes { get; set; }
        public string Currency { get; set; }
        public string BaseFareCurrency { get; set; }
        public double ServiceCharge { get; set; }
        public double BaseFareUSD { get; set; }
        public double BaseFarePHP { get; set; }
        public int IsPrivateFare { get; set; }
        public int CreatedBy { get; set; }
        public DateTime OptionDateBooking { get; set; }
        public DateTime OptionDateAirline { get; set; }
        public double TotalTaxesUSD { get; set; }
        public double InitialTotalPrice { get; set; }
        public string AirlineLocator { get; set; }
        public int TicketType { get; set; }
        public List<PassengerNames> Passengers { get; set; }
        public List<AirSegmentDetails> Segments { get; set; }
        public List<PaxTypeFareBreakdown> Fares { get; set; }
        public List<TaxBreakdown> Taxes { get; set; }
        public DateTime IssueDate { get; set; }
        public int PassportIndicator { get; set; }
        public DateTime? PriceQuoteDate { get; set; }
        public bool HasHXSegment { get; set; }
        public string RequestorMultiCity { get; set; }
        public List<PnrRemarks> Remarks { get; set; }
    }

    public class PnrRemarks
    {
        public string Type { get; set; }
        public string Text { get; set; }
    }

    public class PassengerNames
    {
        public string RecordLocator { get; set; }
        public string NameNumber { get; set; }
        public string NameReference { get; set; }
        public string PaxType { get; set; }
        public string SurName { get; set; }
        public string GivenName { get; set; }
        public string PaxTitle { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string TicketNo { get; set; }
        public string TicketStatus { get; set; }
        public int TicketIndex { get; set; }
        public string RefID { get; set; }
        public string BaggageAllow { get; set; }
        public string BaggagePerSeg { get; set; }
        public string SeatNumber { get; set; }
        public double Taxes { get; set; }
        public int VoidedBy { get; set; }
        public int ReIssuedBy { get; set; }
        public int IsReIssue { get; set; }
        public int PriceQuote { get; set; }
        public bool IncludeDoca { get; set; }
        public List<PassengerDocuments> Documents { get; set; }
    }

    public class PassengerDocuments
    {
        public string RecordLocator { get; set; }
        public string NameNumber { get; set; }
        public string DocumentID { get; set; }
        public string DocumentType { get; set; }
        public string CountryOfIssue { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentNationalityCountry { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public DateTime DocumentExpirationDate { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
    }

    public class PaxTypeFareBreakdown
    {
        public string RecordLocator { get; set; }
        public int Item { get; set; }
        public string PaxTypeCode { get; set; }
        public int Quantity { get; set; }
        public double BasePrice { get; set; }
        public double OriginalTotalPrice { get; set; }
        public double TotalTax { get; set; }
        public string FareBasisCode { get; set; }
        public string FareCalc { get; set; }
        public string Status { get; set; }
        public int SequenceNumber { get; set; }
    }

    public class TaxBreakdown
    {
        public string RecordLocator { get; set; }
        public int Item { get; set; }
        public int SequenceNumber { get; set; }
        public string TaxCode { get; set; }
        public double TaxAmount { get; set; }
        public string TaxPaid { get; set; }
    }

    public class FareRule
    {
        public string FareBasisCode { get; set; }
        public string CabinClass { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Departure { get; set; }
        public string Carrier { get; set; }
        public List<FareRuleFields> Rules { get; set; }
    }

    public class FareRuleFields
    {
        public int RuleNo { get; set; }
        public string RuleTitle { get; set; }
        public string RuleDesc { get; set; }
    }
}



