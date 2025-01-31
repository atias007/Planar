﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Service.Calendars;

public static class CalendarInfo
{
    internal static readonly IReadOnlyDictionary<string, string> Items = new SortedDictionary<string, string>()
    {
        { DefaultCalendar.Name , "DF"},
        { IsraelCalendar.Name , "IL"},
        { "andorra" ,"AD"},
        { "albania" ,"AL"},
        { "argentina" ,"AR"},
        { "austria" ,"AT"},
        { "australia" ,"AU"},
        { "aland" ,"AX"},
        { "bosnia" , "BA"},
        { "herzegovina" ,"BA"},
        { "barbados" , "BB"},
        { "belgium" ,"BE"},
        { "bulgaria" , "BG"},
        { "benin" ,"BJ"},
        { "bolivia" ,"BO"},
        { "brazil" , "BR"},
        { "bahamas" ,"BS"},
        { "botswana" , "BW"},
        { "belarus" ,"BY"},
        { "belize" , "BZ"},
        { "canada" , "CA"},
        { "switzerland" ,"CH"},
        { "chile" ,"CL"},
        { "china" ,"CN"},
        { "colombia" , "CO"},
        { "costa rica" , "CR"},
        { "cuba" , "CU"},
        { "cyprus" , "CY"},
        { "czech republic" , "CZ"},
        { "germany" ,"DE"},
        { "denmark" ,"DK"},
        { "dominican republic" , "DO"},
        { "ecuador" ,"EC"},
        { "estonia" ,"EE"},
        { "egypt" ,"EG"},
        { "spain" ,"ES"},
        { "finland" ,"FI"},
        { "faroe islands" ,"FO"},
        { "france" , "FR"},
        { "gabon" ,"GA"},
        { "united kingdom" , "GB"},
        { "grenada" ,"GD"},
        { "gibraltar" ,"GI"},
        { "greenland" ,"GL"},
        { "gambia" , "GM"},
        { "greece" , "GR"},
        { "guatemala" ,"GT"},
        { "guernsey" , "GG"},
        { "guyana" , "GY"},
        { "honduras" , "HN"},
        { "croatia" ,"HR"},
        { "haiti" ,"HT"},
        { "hungary" ,"HU"},
        { "ireland" ,"IE"},
        { "indonesia" ,"ID"},
        { "isle of man" ,"IM"},
        { "iceland" ,"IS"},
        { "italy" ,"IT"},
        { "liechtenstein" ,"LI"},
        { "lesotho" ,"LS"},
        { "lithuania" ,"LT"},
        { "luxembourg" , "LU"},
        { "latvia" , "LV"},
        { "jersey" , "JE"},
        { "jamaica" ,"JM"},
        { "japan" ,"JP"},
        { "south korea" ,"KR"},
        { "morocco" ,"MA"},
        { "monaco" , "MC"},
        { "moldova" ,"MD"},
        { "montenegro" , "ME"},
        { "madagascar" , "MG"},
        { "macedonia" ,"MK"},
        { "mongolia" , "MN"},
        { "montserrat" , "MS"},
        { "malta" ,"MT"},
        { "mexico" , "MX"},
        { "mozambique" , "MZ"},
        { "namibia" ,"NA"},
        { "niger" ,"NE"},
        { "nigeria" ,"NG"},
        { "nicaragua" ,"NI"},
        { "netherlands" ,"NL"},
        { "norway" , "NO"},
        { "new zealand" ,"NZ"},
        { "panama" , "PA"},
        { "peru" , "PE"},
        { "papua new guinea" , "PG"},
        { "poland" , "PL"},
        { "puerto rico" ,"PR"},
        { "portugal" , "PT"},
        { "paraguay" , "PY"},
        { "romania" ,"RO"},
        { "serbia" , "RS"},
        { "russia" , "RU"},
        { "sweden" , "SE"},
        { "singapore" ,"SG"},
        { "slovenia" , "SI"},
        { "svalbard" , "SJ"},
        { "jan mayen" ,"SJ"},
        { "slovakia" , "SK"},
        { "san marino" , "SM"},
        { "suriname" , "SR"},
        { "el salvador" ,"SV"},
        { "tunisia" ,"TN"},
        { "turkey" , "TR"},
        { "ukraine" ,"UA"},
        { "united states" ,"US"},
        { "uruguay" ,"UY"},
        { "vatican city" , "VA"},
        { "venezuela" ,"VE"},
        { "vietnam" ,"VN"},
        { "south africa" , "ZA"},
        { "zimbabwe" , "ZW"}
    };

    public static bool Contains(string name) => Items.Any(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase));
}