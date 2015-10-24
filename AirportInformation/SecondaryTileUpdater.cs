//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SecondaryTileUpdater.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.UI;
    using Windows.UI.Notifications;
    using Windows.UI.StartScreen;
    using metar;

    internal static class SecondaryTileUpdater
    {
        internal async static Task<bool> UpdateAsync(SecondaryTile tile, METAR metar)
        {
            Debug.WriteLine(metar.raw_text);
            if (!SecondaryTile.Exists(tile.TileId))
            {
                await tile.RequestCreateAsync();
            }

            tile.VisualElements.BackgroundColor = ConvertFlightRuleToTileBackgroundColor(metar);
            tile.VisualElements.ShowNameOnSquare150x150Logo = true;
            tile.RoamingEnabled = true;
            var isUpdated = await tile.UpdateAsync();
            if (isUpdated)
            {
                var tileTpl = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Text03);
                if (metar != null)
                {
                    var attribs = tileTpl.GetElementsByTagName("text");
                    var localTime = DateTime.Parse(metar.observation_time).ToLocalTime();
                    string wind;
                    if (metar.wind_gust_ktSpecified && metar.wind_speed_ktSpecified)
                    {
                        wind = string.Format(
                            "{0:HH:mm}: {1}° @{2}kt G{3}",
                            localTime,
                            metar.wind_dir_degrees,
                            metar.wind_speed_kt,
                            metar.wind_gust_kt);
                    }
                    else
                    {
                        wind = string.Format(
                            "{0:HH:mm}: {1}° @{2}kt",
                            localTime,
                            metar.wind_dir_degrees,
                            metar.wind_speed_kt);
                    }

                    attribs[0].AppendChild(tileTpl.CreateTextNode(wind));
                    if (metar.sky_condition != null && metar.sky_condition.Length > 0)
                    {
                        // Retrieve the first sky condition reporting IFR ceiling.
                        var sky =
                            metar.sky_condition.FirstOrDefault(
                                s => s.sky_cover == "BKN" || s.sky_cover == "OVC" || s.sky_cover == "OVX");
                        if (sky == null)
                        {
                            sky = metar.sky_condition[0];
                        }

                        var cover = new StringBuilder(sky.sky_cover);
                        if (sky.cloud_base_ft_aglSpecified)
                        {
                            cover.AppendFormat(" {0} ft |", sky.cloud_base_ft_agl);
                        }

                        if (metar.visibility_statute_miSpecified)
                        {
                            cover.AppendFormat(" Vis {0} mi", metar.visibility_statute_mi);
                        }

                        attribs[1].AppendChild(tileTpl.CreateTextNode(cover.ToString()));
                    }

                    var temp = string.Format("T {0:0}°C Dew Pt {1:0}°C", metar.temp_c, metar.dewpoint_c);
                    attribs[2].AppendChild(tileTpl.CreateTextNode(temp));
                    if (metar.altim_in_hgSpecified)
                    {
                        var trend = string.Empty;
                        if (metar.three_hr_pressure_tendency_mbSpecified)
                        {
                            if (metar.three_hr_pressure_tendency_mb > 0)
                            {
                                trend = "↑";
                            }
                            else if (metar.three_hr_pressure_tendency_mb == 0)
                            {
                                trend = "=";
                            }
                            else
                            {
                                trend = "↓";
                            }
                        }

                        attribs[3].AppendChild(
                            tileTpl.CreateTextNode(
                                string.Format("{0:0.00} inHg {1} {2}", metar.altim_in_hg, trend, metar.wx_string)));
                    }
                }

                var tileUpdater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId);
                var notification = new TileNotification(tileTpl);
                tileUpdater.Update(notification);
                Debug.WriteLine("Tile {0} updated with {1}.", tile.TileId, tileTpl.GetXml());
            }

            return isUpdated;
        }

        internal static Color ConvertFlightRuleToTileBackgroundColor(METAR metar)
        {
            if (metar == null || metar.flight_category == null)
            {
                return Colors.DimGray;
            }

            Color col;
            if (metar.flight_category.Length > 0)
            {
                switch (metar.flight_category)
                {
                    case "VFR":
                        col = Colors.DarkGreen;
                        break;
                    case "MVFR":
                        col = Colors.DarkBlue;
                        break;
                    case "IFR":
                        col = Colors.DarkRed;
                        break;
                    case "LIFR":
                        col = Colors.DarkMagenta;
                        break;
                    default:
                        col = Colors.DimGray;
                        break;
                }
            }
            else
            {
                if (metar.visibility_statute_mi < 1 ||
                    (metar.sky_condition != null &&
                     metar.sky_condition.Any(
                         s => s.cloud_base_ft_agl < 500 && (s.sky_cover == "BKN" || s.sky_cover == "OVC"))))
                {
                    return Colors.DarkMagenta;
                }
                if (metar.visibility_statute_mi < 3 ||
                    (metar.sky_condition != null &&
                     metar.sky_condition.Any(
                         s => s.cloud_base_ft_agl < 1000 && (s.sky_cover == "BKN" || s.sky_cover == "OVC"))))
                {
                    return Colors.DarkRed;
                }
                if (metar.visibility_statute_mi <= 5 ||
                    (metar.sky_condition != null &&
                     metar.sky_condition.Any(
                         s =>
                             s.cloud_base_ft_agl <= 3000 &&
                             (s.sky_cover == "BKN" || s.sky_cover == "OVC" || s.sky_cover == "OVX"))))
                {
                    return Colors.DarkBlue;
                }
                if (metar.visibility_statute_mi > 5 ||
                    (metar.sky_condition != null && metar.sky_condition.Any(s => s.cloud_base_ft_agl > 1000)))
                {
                    return Colors.DarkGreen;
                }

                return Colors.DimGray;
            }

            return col;
        }
    }
}
