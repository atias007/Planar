using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Planner.Calendar.Hebrew
{
    #region Enums

    public enum HebrewEventType
    {
        None,
        ErevRoshHashana,
        RoshHashana,
        ZomGdalya,
        ErevYomHakipurim,
        YomHakipurim,
        ErevSukot,
        Sukot,
        SukotHolHamoedDay1,
        SukotHolHamoedDay2,
        SukotHolHamoedDay3,
        SukotHolHamoedDay4,
        SukotHolHamoedDay5,
        HoshanaRaba,
        ShminiAzeret,
        YomHazikaronLeRabin,
        Hanuka,
        AsaraBeTevet,
        TooBeshvat,
        YomHamishpaha,
        YomHazikaronMakomKvuraLoNoda,
        TaanitEster,
        Purim,
        ShushanPurim,
        Pesah,
        ErevPesah,
        PesahHolHamoed1,
        PesahHolHamoed2,
        PesahHolHamoed3,
        PesahHolHamoed4,
        PesahHolHamoed5,
        ShviaiShelPesah,
        Hamimuna,
        YomHazikaronLashoaaVelagvura,
        YomHazikaronLehalaleyMaarhotIsrael,
        YomHaAzmaaut,
        LagBaomer,
        YomYerushalayim,
        ErevShavuot,
        Shavuot,
        ZomShivaAsarBetamuz,
        TishaBeav,
        TooBeav
    }

    public enum HebrewEventDetail
    {
        None,
        RoshHashanaDay1,
        RoshHashanaDay2,
        HanukaDay1,
        HanukaDay2,
        HanukaDay3,
        HanukaDay4,
        HanukaDay5,
        HanukaDay6,
        HanukaDay7,
        HanukaDay8,
        YomHakadishHaklali,
        ErevShviaiShelPesah,
        ErevShminiAzeret,
    }

    public enum HebrewMonth
    {
        תשרי, חשוון, כסלו, טבת, שבט, אדר_ב, אדר, ניסן, אייר, סיון, תמוז, אב, אלול
    }

    public enum HebrewDay
    {
        א, ב, ג, ד, ה, ו, ז, ח, ט, י, יא, יב, יג, יד, טו, טז, יז, יח, יט, כ, כא, כב, כג, כד, כה, כו, כז, כח, כט, ל
    }

    #endregion Enums

    #region Util

    public class HebrewEvent
    {
        /// <summary>
        /// Gets event by type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <returns></returns>
        public static HebrewEventInfo GetEvent(HebrewEventType eventType)
        {
            return GetEvent(eventType, DateTime.Now.Year);
        }

        /// <summary>
        /// Gets the close event by type.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <returns></returns>
        public static HebrewEventInfo GetCloseEvent(HebrewEventType eventType)
        {
            var startDate = DateTime.Now;
            HebrewEventInfo info;

            do
            {
                info = new HebrewEventInfo(startDate);
                if (info.IsEvent && info.EventType == eventType)
                {
                    return info;
                }
                startDate = startDate.AddDays(1);
            } while (true);
        }

        /// <summary>
        /// Gets event by type and year.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="year">The year.</param>
        /// <returns></returns>
        public static HebrewEventInfo GetEvent(HebrewEventType eventType, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            while (startDate.Year == year)
            {
                var info = new HebrewEventInfo(startDate);
                if (info.IsEvent && info.EventType == eventType)
                {
                    return info;
                }
                startDate = startDate.AddDays(1);
            }

            return null;
        }

        /// <summary>
        /// Gets the hebrew event info of today.
        /// </summary>
        /// <returns></returns>
        public static HebrewEventInfo GetHebrewEventInfo()
        {
            return new HebrewEventInfo();
        }

        /// <summary>
        /// Gets the hebrew event info by date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static HebrewEventInfo GetHebrewEventInfo(DateTime date)
        {
            return new HebrewEventInfo(date);
        }

        /// <summary>
        /// Gets the hebrew date.
        /// </summary>
        /// <returns></returns>
        public static HebrewDate GetHebrewDate()
        {
            return new HebrewDate();
        }

        /// <summary>
        /// Gets the hebrew date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static HebrewDate GetHebrewDate(DateTime date)
        {
            return new HebrewDate(date);
        }

        /// <summary>
        /// Gets the events by filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public static IEnumerable<HebrewEventInfo> GetEvents(HebrewEventFilter filter)
        {
            if (filter.Year == null) filter.Year = DateTime.Now.Year;

            var result = new List<HebrewEventInfo>();
            var startDate = new DateTime(filter.Year.Value, filter.Month.HasValue ? filter.Month.Value : 1, 1);

            while (startDate.Year == filter.Year.Value && (filter.Month == null || startDate.Month == filter.Month.Value))
            {
                var info = new HebrewEventInfo(startDate);
                if (info.IsEvent)
                {
                    var match =
                        (string.IsNullOrEmpty(filter.EventTitle) || info.EventTitle.Contains(filter.EventTitle)) &&
                        (filter.IsHolyDate == null || info.IsHoliday == filter.IsHolyDate.Value) &&
                        (filter.IsHolyDateEvening == null || info.IsHolidayEve == filter.IsHolyDateEvening.Value) &&
                        (filter.IsSabaton == null || info.IsSabbaton == filter.IsSabaton.Value) &&
                        (filter.IsZom == null || info.IsZom == filter.IsZom.Value);

                    if (match)
                        result.Add(info);
                }

                startDate = startDate.AddDays(1);
            }

            return result;
        }

        /// <summary>
        /// Determines whether [is holy date].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is holy date]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsHolyDate()
        {
            return IsHolyDate(DateTime.Now);
        }

        /// <summary>
        /// Determines whether [is holy date] [the specified date].
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>
        ///   <c>true</c> if [is holy date] [the specified date]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsHolyDate(DateTime date)
        {
            var info = GetHebrewEventInfo(date);
            if (info.IsHoliday) return true;
            if (info.IsHolidayEve || date.DayOfWeek == DayOfWeek.Friday)
            {
                return date.Hour > 16;
            }
            return false;
        }
    }

    #endregion Util

    #region Data Structures

    public class HebrewEventFilter
    {
        /// <summary>
        /// Gets or sets the event title.
        /// </summary>
        /// <value>
        /// The event title.
        /// </value>
        public string EventTitle { get; set; }

        /// <summary>
        /// Gets or sets the is sabaton.
        /// </summary>
        /// <value>
        /// The is sabaton.
        /// </value>
        public bool? IsSabaton { get; set; }

        /// <summary>
        /// Gets or sets the is holy date.
        /// </summary>
        /// <value>
        /// The is holy date.
        /// </value>
        public bool? IsHolyDate { get; set; }

        /// <summary>
        /// Gets or sets the is zom.
        /// </summary>
        /// <value>
        /// The is zom.
        /// </value>
        public bool? IsZom { get; set; }

        /// <summary>
        /// Gets or sets the is holy date evening.
        /// </summary>
        /// <value>
        /// The is holy date evening.
        /// </value>
        public bool? IsHolyDateEvening { get; set; }

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>
        /// The year.
        /// </value>
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets the month.
        /// </summary>
        /// <value>
        /// The month.
        /// </value>
        public int? Month { get; set; }
    }

    public class HebrewEventInfo
    {
        private readonly DateTime _date;
        private readonly CultureInfo _ci = CultureInfo.CreateSpecificCulture("he-IL");

        /// <summary>
        /// Initializes a new instance of the <see cref="HebrewEventInfo"/> class.
        /// </summary>
        public HebrewEventInfo()
            : this(DateTime.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HebrewEventInfo"/> class.
        /// </summary>
        /// <param name="date">The date.</param>
        public HebrewEventInfo(DateTime date)
        {
            _ci.DateTimeFormat.Calendar = new System.Globalization.HebrewCalendar();
            _date = date;
            HebrewDate = HebrewEvent.GetHebrewDate(date);
            Analyze(date);
            SetTitle();
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        public DateTime Date
        {
            get { return _date.Date; }
        }

        /// <summary>
        /// Gets the hebrew date.
        /// </summary>
        public HebrewDate HebrewDate { get; private set; }

        /// <summary>
        /// Gets the event title.
        /// </summary>
        public string EventTitle { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is event.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is event; otherwise, <c>false</c>.
        /// </value>
        public bool IsEvent { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is sabaton.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is sabaton; otherwise, <c>false</c>.
        /// </value>
        public bool IsSabbaton { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is holy date.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is holy date; otherwise, <c>false</c>.
        /// </value>
        public bool IsHoliday { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is zom.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is zom; otherwise, <c>false</c>.
        /// </value>
        public bool IsZom { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is shabat.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is shabat; otherwise, <c>false</c>.
        /// </value>
        public bool IsShabat { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is holy date evening.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is holy date evening; otherwise, <c>false</c>.
        /// </value>
        public bool IsHolidayEve { get; private set; }

        /// <summary>
        /// Gets the type of the event.
        /// </summary>
        /// <value>
        /// The type of the event.
        /// </value>
        public HebrewEventType EventType { get; private set; }

        /// <summary>
        /// Gets the event detail.
        /// </summary>
        public HebrewEventDetail EventDetail { get; private set; }

        /// <summary>
        /// Sets the title.
        /// </summary>
        private void SetTitle()
        {
            switch (EventType)
            {
                case HebrewEventType.None:
                    break;

                case HebrewEventType.ErevRoshHashana:
                    EventTitle = "ערב ראש השנה";
                    break;

                case HebrewEventType.RoshHashana:
                    if (EventDetail == HebrewEventDetail.RoshHashanaDay1) EventTitle = "ראש השנה (יום ראשון)";
                    else if (EventDetail == HebrewEventDetail.RoshHashanaDay2) EventTitle = "ראש השנה (יום שני)";
                    break;

                case HebrewEventType.ZomGdalya:
                    EventTitle = "צום גדליה";
                    break;

                case HebrewEventType.ErevYomHakipurim:
                    EventTitle = "ערב יום כיפור";
                    break;

                case HebrewEventType.YomHakipurim:
                    EventTitle = "יום כיפור";
                    break;

                case HebrewEventType.ErevSukot:
                    EventTitle = "ערב סוכות";
                    break;

                case HebrewEventType.Sukot:
                    EventTitle = "סוכות";
                    break;

                case HebrewEventType.SukotHolHamoedDay1:
                    EventTitle = "חול המועד סוכות (יום ראשון)";
                    break;

                case HebrewEventType.SukotHolHamoedDay2:
                    EventTitle = "חול המועד סוכות (יום שני)";
                    break;

                case HebrewEventType.SukotHolHamoedDay3:
                    EventTitle = "חול המועד סוכות (יום שלישי)";
                    break;

                case HebrewEventType.SukotHolHamoedDay4:
                    EventTitle = "חול המועד סוכות (יום רביעי)";
                    break;

                case HebrewEventType.SukotHolHamoedDay5:
                    EventTitle = "חול המועד סוכות (יום חמישי)";
                    break;

                case HebrewEventType.HoshanaRaba:
                    EventTitle = "הושענה רבא";
                    break;

                case HebrewEventType.ShminiAzeret:
                    EventTitle = "שמיני עצרת";
                    break;

                case HebrewEventType.YomHazikaronLeRabin:
                    EventTitle = "יום הזיכרון ליצחק רבין";
                    break;

                case HebrewEventType.Hanuka:
                    switch (EventDetail)
                    {
                        case HebrewEventDetail.HanukaDay1:
                            EventTitle = "חנוכה (יום ראשון)";
                            break;

                        case HebrewEventDetail.HanukaDay2:
                            EventTitle = "חנוכה (יום שני)";
                            break;

                        case HebrewEventDetail.HanukaDay3:
                            EventTitle = "חנוכה (יום שלישי)";
                            break;

                        case HebrewEventDetail.HanukaDay4:
                            EventTitle = "חנוכה (יום רביעי)";
                            break;

                        case HebrewEventDetail.HanukaDay5:
                            EventTitle = "חנוכה (יום חמישי)";
                            break;

                        case HebrewEventDetail.HanukaDay6:
                            EventTitle = "חנוכה (יום שישי)";
                            break;

                        case HebrewEventDetail.HanukaDay7:
                            EventTitle = "חנוכה (יום שביעי)";
                            break;

                        case HebrewEventDetail.HanukaDay8:
                            EventTitle = "חנוכה (יום שמיני)";
                            break;
                    }
                    break;

                case HebrewEventType.AsaraBeTevet:
                    EventTitle = "עשרה בטבת (יום הקדיש הכללי)";
                    break;

                case HebrewEventType.TooBeshvat:
                    EventTitle = "טו בשבט";
                    break;

                case HebrewEventType.YomHamishpaha:
                    EventTitle = "יום המשפחה";
                    break;

                case HebrewEventType.YomHazikaronMakomKvuraLoNoda:
                    EventTitle = "יום הזיכרון לחללי מערכות ישראל שמקום קבורתם לא נודע";
                    break;

                case HebrewEventType.TaanitEster:
                    EventTitle = "תענית אסתר";
                    break;

                case HebrewEventType.Purim:
                    EventTitle = "פורים";
                    break;

                case HebrewEventType.ShushanPurim:
                    EventTitle = "שושן פורים";
                    break;

                case HebrewEventType.Pesah:
                    EventTitle = "פסח (ליל הסדר)";
                    break;

                case HebrewEventType.ErevPesah:
                    EventTitle = "ערב פסח";
                    break;

                case HebrewEventType.PesahHolHamoed1:
                    EventTitle = "פסח (חול המועד יום ראשון)";
                    break;

                case HebrewEventType.PesahHolHamoed2:
                    EventTitle = "פסח (חול המועד יום שני)";
                    break;

                case HebrewEventType.PesahHolHamoed3:
                    EventTitle = "פסח (חול המועד יום שלישי)";
                    break;

                case HebrewEventType.PesahHolHamoed4:
                    EventTitle = "פסח (חול המועד יום רביעי)";
                    break;

                case HebrewEventType.PesahHolHamoed5:
                    EventTitle = "פסח (חול המועד יום חמישי)";
                    break;

                case HebrewEventType.ShviaiShelPesah:
                    EventTitle = "שביעי של פסח";
                    break;

                case HebrewEventType.Hamimuna:
                    EventTitle = "המימונה";
                    break;

                case HebrewEventType.YomHazikaronLashoaaVelagvura:
                    EventTitle = "יום הזיכרון לשואה ולגבורה";
                    break;

                case HebrewEventType.YomHazikaronLehalaleyMaarhotIsrael:
                    EventTitle = "יום הזיכרון לחללי מערכות ישראל";
                    break;

                case HebrewEventType.YomHaAzmaaut:
                    EventTitle = "יום העצמאות";
                    break;

                case HebrewEventType.LagBaomer:
                    EventTitle = "ל\"ג בעומר";
                    break;

                case HebrewEventType.YomYerushalayim:
                    EventTitle = "יום ירושליים";
                    break;

                case HebrewEventType.ErevShavuot:
                    EventTitle = "ערב שבועות";
                    break;

                case HebrewEventType.Shavuot:
                    EventTitle = "שבועות";
                    break;

                case HebrewEventType.ZomShivaAsarBetamuz:
                    EventTitle = "צום שבעה עשר בתמוז";
                    break;

                case HebrewEventType.TishaBeav:
                    EventTitle = "תשעה באב";
                    break;

                case HebrewEventType.TooBeav:
                    EventTitle = "ט\"ו באב (חג האהבה)";
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool IsLeapYear { get; private set; }

        private static readonly int[] LeapYears = new[] { 0, 3, 6, 8, 11, 14, 17 };

        private bool IsLeapYearInner(int year)
        {
            var modYear = year % 19;
            var result = LeapYears.Contains(modYear);
            return result;
        }

        /// <summary>
        /// Analyzes the specified date.
        /// </summary>
        /// <param name="date">The date.</param>
        private void Analyze(DateTime date)
        {
            this.IsShabat = date.DayOfWeek == DayOfWeek.Saturday;
            this.IsHoliday = this.IsShabat;
            this.IsSabbaton = this.IsShabat;
            this.IsLeapYear = IsLeapYearInner(date.Year);

            IsEvent = true;
            var d = HebrewDate.ToString("dd MMM");
            switch (d)
            {
                case "י' טבת":
                    EventType = HebrewEventType.AsaraBeTevet;
                    EventDetail = HebrewEventDetail.YomHakadishHaklali;
                    IsZom = true;
                    break;

                case "ט\"ו שבט":
                    EventType = HebrewEventType.TooBeshvat;
                    break;

                case "ל' שבט":
                    EventType = HebrewEventType.YomHamishpaha;
                    break;

                case "ז' אדר":
                    if (IsLeapYear)
                    {
                        IsEvent = false;
                    }
                    else
                    {
                        EventType = HebrewEventType.YomHazikaronMakomKvuraLoNoda;
                    }
                    break;

                case "י\"ג אדר":
                    if (IsLeapYear)
                    {
                        IsEvent = false;
                    }
                    else
                    {
                        EventType = HebrewEventType.TaanitEster;
                        IsZom = true;
                    }
                    break;

                case "י\"ד אדר":
                    if (IsLeapYear)
                    {
                        IsEvent = false;
                    }
                    else
                    {
                        EventType = HebrewEventType.Purim;
                    }
                    break;

                case "ט\"ו אדר":
                    if (IsLeapYear)
                    {
                        IsEvent = false;
                    }
                    else
                    {
                        EventType = HebrewEventType.ShushanPurim;
                    }
                    break;

                case "ז' אדר ב":
                    EventType = HebrewEventType.YomHazikaronMakomKvuraLoNoda;
                    break;

                case "י\"ג אדר ב":
                    EventType = HebrewEventType.TaanitEster;
                    IsZom = true;
                    break;

                case "י\"ד אדר ב":
                    EventType = HebrewEventType.Purim;
                    break;

                case "ט\"ו אדר ב":
                    EventType = HebrewEventType.ShushanPurim;
                    break;

                case "י\"ד ניסן":
                    EventType = HebrewEventType.ErevPesah;
                    IsHolidayEve = true;
                    break;

                case "ט\"ו ניסן":
                    EventType = HebrewEventType.Pesah;
                    IsSabbaton = true;
                    IsHoliday = true;
                    break;

                case "ט\"ז ניסן":
                    EventType = HebrewEventType.PesahHolHamoed1;
                    break;

                case "י\"ז ניסן":
                    EventType = HebrewEventType.PesahHolHamoed2;
                    break;

                case "י\"ח ניסן":
                    EventType = HebrewEventType.PesahHolHamoed3;
                    break;

                case "י\"ט ניסן":
                    EventType = HebrewEventType.PesahHolHamoed4;
                    break;

                case "כ' ניסן":
                    EventType = HebrewEventType.PesahHolHamoed5;
                    EventDetail = HebrewEventDetail.ErevShviaiShelPesah;
                    IsHolidayEve = true;
                    break;

                case "כ\"א ניסן":
                    EventType = HebrewEventType.ShviaiShelPesah;
                    IsSabbaton = true;
                    IsHoliday = true;
                    break;

                case "כ\"ב ניסן":
                    EventType = HebrewEventType.Hamimuna;
                    break;

                case "כ\"ו ניסן":
                    if (date.DayOfWeek == DayOfWeek.Thursday)
                    {
                        EventType = HebrewEventType.YomHazikaronLashoaaVelagvura;
                    }
                    else
                    {
                        IsEvent = false;
                    }
                    break;

                case "כ\"ז ניסן":
                    if (date.DayOfWeek != DayOfWeek.Friday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        EventType = HebrewEventType.YomHazikaronLashoaaVelagvura;
                    }
                    else
                    {
                        IsEvent = false;
                    }
                    break;

                case "כ\"ח ניסן":
                    if (date.DayOfWeek == DayOfWeek.Monday)
                    {
                        EventType = HebrewEventType.YomHazikaronLashoaaVelagvura;
                    }
                    else
                    {
                        IsEvent = false;
                    }
                    break;

                case "ב' אייר":
                    if (date.DayOfWeek == DayOfWeek.Wednesday)
                    {
                        EventType = HebrewEventType.YomHazikaronLehalaleyMaarhotIsrael;
                    }
                    else
                    {
                        IsEvent = false;
                    }
                    break;

                case "ג' אייר":
                    if (date.DayOfWeek == DayOfWeek.Wednesday)
                    {
                        EventType = HebrewEventType.YomHazikaronLehalaleyMaarhotIsrael;
                    }
                    else if (date.DayOfWeek == DayOfWeek.Thursday)
                    {
                        EventType = HebrewEventType.YomHaAzmaaut;
                        IsSabbaton = true;
                    }
                    else
                    {
                        IsEvent = false;
                    }
                    break;

                case "ד' אייר":
                    if (date.DayOfWeek == DayOfWeek.Thursday)
                    {
                        EventType = HebrewEventType.YomHaAzmaaut;
                        IsSabbaton = true;
                    }
                    else
                    {
                        EventType = HebrewEventType.YomHazikaronLehalaleyMaarhotIsrael;
                    }
                    break;

                case "ה' אייר":
                    if (date.DayOfWeek == DayOfWeek.Monday)
                    {
                        EventType = HebrewEventType.YomHazikaronLehalaleyMaarhotIsrael;
                    }
                    else if (date.DayOfWeek != DayOfWeek.Friday && date.DayOfWeek != DayOfWeek.Saturday)
                    {
                        EventType = HebrewEventType.YomHaAzmaaut;
                        IsSabbaton = true;
                    }
                    else
                    {
                        IsEvent = false;
                    }
                    break;

                case "ו' אייר":
                    if (date.DayOfWeek == DayOfWeek.Tuesday)
                    {
                        EventType = HebrewEventType.YomHaAzmaaut;
                        IsSabbaton = true;
                    }
                    else
                    {
                        IsEvent = false;
                    }
                    break;

                case "י\"ח אייר":
                    EventType = HebrewEventType.LagBaomer;
                    break;

                case "כ\"ח אייר":
                    EventType = HebrewEventType.YomYerushalayim;
                    break;

                case "ה' סיון":
                    EventType = HebrewEventType.ErevShavuot;
                    IsHolidayEve = true;
                    break;

                case "ו' סיון":
                    EventType = HebrewEventType.Shavuot;
                    IsSabbaton = true;
                    IsHoliday = true;
                    break;

                case "י\"ז תמוז":
                    EventType = HebrewEventType.ZomShivaAsarBetamuz;
                    IsZom = true;
                    break;

                case "ט' אב":
                    EventType = HebrewEventType.TishaBeav;
                    IsZom = true;
                    break;

                case "ט\"ו אב":
                    EventType = HebrewEventType.TooBeav;
                    break;

                case "כ\"ט אלול":
                    EventType = HebrewEventType.ErevRoshHashana;
                    IsHolidayEve = true;
                    break;

                case "א' תשרי":
                    EventType = HebrewEventType.RoshHashana;
                    EventDetail = HebrewEventDetail.RoshHashanaDay1;
                    IsSabbaton = true;
                    IsHoliday = true;
                    break;

                case "ב' תשרי":
                    EventType = HebrewEventType.RoshHashana;
                    EventDetail = HebrewEventDetail.RoshHashanaDay2;
                    IsSabbaton = true;
                    IsHoliday = true;
                    break;

                case "ג' תשרי":
                    EventType = HebrewEventType.ZomGdalya;
                    IsZom = true;
                    break;

                case "ט' תשרי":
                    EventType = HebrewEventType.ErevYomHakipurim;
                    IsHolidayEve = true;
                    break;

                case "י' תשרי":
                    EventType = HebrewEventType.YomHakipurim;
                    IsSabbaton = true;
                    IsHoliday = true;
                    IsZom = true;
                    break;

                case "י\"ד תשרי":
                    EventType = HebrewEventType.ErevSukot;
                    IsHolidayEve = true;
                    break;

                case "ט\"ו תשרי":
                    EventType = HebrewEventType.Sukot;
                    IsSabbaton = true;
                    IsHoliday = true;
                    break;

                case "ט\"ז תשרי":
                    EventType = HebrewEventType.SukotHolHamoedDay1;
                    break;

                case "י\"ז תשרי":
                    EventType = HebrewEventType.SukotHolHamoedDay2;
                    break;

                case "י\"ח תשרי":
                    EventType = HebrewEventType.SukotHolHamoedDay3;
                    break;

                case "י\"ט תשרי":
                    EventType = HebrewEventType.SukotHolHamoedDay4;
                    break;

                case "כ' תשרי":
                    EventType = HebrewEventType.SukotHolHamoedDay5;
                    break;

                case "כ\"א תשרי":
                    EventType = HebrewEventType.HoshanaRaba;
                    EventDetail = HebrewEventDetail.ErevShminiAzeret;
                    IsHolidayEve = true;
                    break;

                case "כ\"ב תשרי":
                    EventType = HebrewEventType.ShminiAzeret;
                    IsSabbaton = true;
                    IsHoliday = true;
                    break;

                case "י\"ב חשון":
                    EventType = HebrewEventType.YomHazikaronLeRabin;
                    break;

                case "כ\"ה כסלו":
                    EventType = HebrewEventType.Hanuka;
                    EventDetail = HebrewEventDetail.HanukaDay1;
                    break;

                case "כ\"ו כסלו":
                    EventType = HebrewEventType.Hanuka;
                    EventDetail = HebrewEventDetail.HanukaDay2;
                    break;

                case "כ\"ז כסלו":
                    EventType = HebrewEventType.Hanuka;
                    EventDetail = HebrewEventDetail.HanukaDay3;
                    break;

                case "כ\"ח כסלו":
                    EventType = HebrewEventType.Hanuka;
                    EventDetail = HebrewEventDetail.HanukaDay4;
                    break;

                case "כ\"ט כסלו":
                    EventType = HebrewEventType.Hanuka;
                    EventDetail = HebrewEventDetail.HanukaDay5;
                    break;

                case "ל' כסלו":
                    EventType = HebrewEventType.Hanuka;
                    EventDetail = HebrewEventDetail.HanukaDay6;
                    break;

                case "א' טבת":
                    EventType = HebrewEventType.Hanuka;

                    var d1 = date.AddDays(-1).ToString("dd MMM", _ci);
                    EventDetail =
                        d1 == "ל' כסלו" ?
                        HebrewEventDetail.HanukaDay7 :
                        HebrewEventDetail.HanukaDay6;
                    break;

                case "ב' טבת":
                    EventType = HebrewEventType.Hanuka;

                    var d2 = date.AddDays(-2).ToString("dd MMM", _ci);
                    EventDetail =
                        d2 == "ל' כסלו" ?
                        HebrewEventDetail.HanukaDay7 :
                        HebrewEventDetail.HanukaDay8;
                    break;

                default:
                    IsEvent = false;
                    break;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(EventTitle) ? string.Empty : EventTitle;
        }
    }

    public class HebrewDate
    {
        private readonly CultureInfo _ci = CultureInfo.CreateSpecificCulture("he-IL");

        /// <summary>
        /// Initializes a new instance of the <see cref="HebrewDate"/> class.
        /// </summary>
        public HebrewDate()
            : this(DateTime.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HebrewDate"/> class.
        /// </summary>
        /// <param name="date">The date.</param>
        public HebrewDate(DateTime date)
        {
            _ci.DateTimeFormat.Calendar = new System.Globalization.HebrewCalendar();

            var dayString = date.ToString("dd", _ci).Replace("'", string.Empty).Replace("\"", string.Empty);
            var monthString = date.ToString("MMM", _ci)
                .Replace(" ", "_");

            Day = (HebrewDay)Enum.Parse(typeof(HebrewDay), dayString);
            Month = (HebrewMonth)Enum.Parse(typeof(HebrewMonth), monthString);
            _year = DateTime.Now.ToString("yyyy", _ci);
        }

        /// <summary>
        /// Gets or sets the day.
        /// </summary>
        /// <value>
        /// The day.
        /// </value>
        public HebrewDay Day { get; set; }

        /// <summary>
        /// Gets or sets the month.
        /// </summary>
        /// <value>
        /// The month.
        /// </value>
        public HebrewMonth Month { get; set; }

        private string _year;

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>
        /// The year.
        /// </value>
        public string Year
        {
            get { return _year; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Year can not be null or empty");

                const string val = "א' תשרי ";
                if (!value.Contains("\"") && value.Length > 1)
                {
                    value = val.Substring(0, value.Length - 1) + "\"" + value[^1];
                }

                var dateString = val + value;
                DateTime result;
                if (!DateTime.TryParse(dateString, _ci, DateTimeStyles.None, out result))
                {
                    throw new ArgumentException("Year is not valid hebrew year");
                }

                _year = val;
            }
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <returns></returns>
        public DateTime GetDate()
        {
            var dt = GetDateString();
            var result = DateTime.Parse(dt, _ci);
            return result;
        }

        /// <summary>
        /// Gets the date string.
        /// </summary>
        /// <returns></returns>
        public string GetDateString()
        {
            var day = Day.ToString();
            day = day.Length == 1 ? day + "'" : day[0] + "\"" + day[1];
            var month = Month.ToString().Replace("_", " ");
            var dateString = day + " " + month + " " + Year;
            return dateString;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return GetDate().ToString("dd MMM yyyy", _ci);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            return GetDate().ToString(format, _ci);
        }
    }

    #endregion Data Structures
}