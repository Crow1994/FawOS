using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace DAM
{
    class StatisticsGenerator : IDisposable
    {
        DataAccess dataAccess;
        FLHookSocket m_flHookCmdr;
        FLGameData m_gameData;

        public StatisticsGenerator(FLGameData gameData, FLHookSocket flHookCmdr)
        {
            dataAccess = new DataAccess();
            m_gameData = gameData;
            m_flHookCmdr = flHookCmdr;
        }

        public void Dispose()
        {
            dataAccess.Dispose();
        }

        public void GenerateGeneralStatistics(BackgroundWorker bgWkr, LogRecorderInterface log)
        {
            GenerateOnlinePlayerStats(log);

            DateTime date = DateTime.Now;
            try
            {
                string filePath = String.Format("{0}\\general_{1:yyyyMMdd}.html", AppSettings.Default.setStatisticsDir, date);
                if (File.Exists(filePath))
                    return;

                using (DamDataSet ds = new DamDataSet())
                {
                    bgWkr.ReportProgress(60, "Creating general statistics...");
                    dataAccess.GetGeneralStatisticsTable((DamDataSet.GeneralStatisticsTableDataTable)ds.GeneralStatisticsTable);

                    bgWkr.ReportProgress(70, "Generating general reports...");
                    string[] colTitles = new string[] { "Description", "Result" };
                    string[] colNames = new string[] { "Description", "Result" };
                    string title = String.Format("General Statistics - {0}", date);
                    DataRow[] rows = ds.GeneralStatisticsTable.Select();
                    SaveAsHTML(filePath, title, rows, colTitles, colNames, 0, rows.Length);
                    File.Copy(filePath, AppSettings.Default.setStatisticsDir + "\\general.html", true);
                }

            }
            catch (Exception ex)
            {
                log.AddLog(String.Format("Error '{0}' when calculating general statistics", ex.Message));
            }
        }

        /// <summary>
        /// generate online statistics for online players ...
        /// </summary>
        private void GenerateOnlinePlayerStats(LogRecorderInterface log)
        {
            //***** assemble the sorted lists for both tables ...
            SortedDictionary<string, FLHookSocket.PlayerInfo> char_list = new SortedDictionary<string, FLHookSocket.PlayerInfo>();
            SortedDictionary<string, List<string>> system_list = new SortedDictionary<string, List<string>>();
            string[] playerlistFields = AppSettings.Default.setStatPlayerListShowFields.Split(';');
            bool column0 = true;
            int slots_in_use = -1;
            lock (m_flHookCmdr.playerInfoList)
            {
                slots_in_use = m_flHookCmdr.playerInfoList.Count;
                //***** store the hook characters information into both lists ...
                foreach (KeyValuePair<int, FLHookSocket.PlayerInfo> kvp in m_flHookCmdr.playerInfoList)
                {
                    string character = HtmlEncode(kvp.Value.charname);
                    if (character != "-")
                    {
                       string system = HtmlEncode(m_gameData.GetItemDescByNickNameX(kvp.Value.system));
                       char_list.Add(character, kvp.Value);
                       if (!system_list.ContainsKey(system))
                       {
                          system_list.Add(system, new List<string>());
                       }
                       system_list[system].Add(character);
                    }
                }
            }
            //*****   generate the html contents ...
            string contents = "<html><head><title>Players Online</title><style type=text/css>"; ;
            contents += ".Column0 {FONT-FAMILY: Tahoma; FONT-SIZE: 10pt;  TEXT-ALIGN: left; COLOR: #000000; BACKGROUND: #FFFFFF;}";
            contents += ".Column1 {FONT-FAMILY: Tahoma; FONT-SIZE: 10pt;  TEXT-ALIGN: left; COLOR: #000000; BACKGROUND: #FFFFFF;}";
            contents += "</style>";
            contents += "</head><body>";
            if (AppSettings.Default.setStatPlayerListTimeUTC)
                contents += "<i>last update: " + DateTime.UtcNow.ToString() + " [UTC]</i><br><br>";
            else
                contents += "<i>last update: " + DateTime.Now.ToString() + "</i><br><br>";
            contents += "<i>used slots: " + String.Format("{0}", slots_in_use) + " </i><br><br><br><br>";
            if (AppSettings.Default.setStatPlayerListShowCharsByName)
            {
                contents += "<size=5><b><u>Characters by Name</u></b></size><br><br>";
                contents += "<table width=\"90%\" border=\"1\" cellspacing=\"0\" cellpadding=\"0\">";
                contents += "<tr>";
                foreach (string field in playerlistFields)
                {
                    if (column0)
                        contents += "<th bgcolor=\"#ECE9D8\" align=\"left\"><font face=\"Tahoma\" color=\"#000000\" size=\"2\">";
                    else
                        contents += "<th bgcolor=\"#ECE9D8\" align=\"left\"><font face=\"Tahoma\" color=\"#000000\" size=\"2\">";
                    contents += field;
                    contents += "</font></th>";
                    column0 = !column0;
                }
                column0 = true;
                contents += "</tr>";
                foreach (KeyValuePair<string, FLHookSocket.PlayerInfo> kvp in char_list)
                {
                    contents += "<tr>";

                    foreach (string field in playerlistFields)
                    {
                        string toAdd = string.Empty;
                        switch (field)
                        {
                            case "Character": toAdd = kvp.Key; break;
                            case "System": toAdd = m_gameData.GetItemDescByNickNameX(kvp.Value.system); break;
                            case "ID": toAdd = kvp.Value.id.ToString(); break;
                            case "IP": toAdd = kvp.Value.ip.ToString(); break;
                            case "Ping": toAdd = kvp.Value.ping.ToString(); break;
                            case "Loss": toAdd = kvp.Value.loss.ToString(); break;
                            case "Fluct": toAdd = kvp.Value.ping_fluct.ToString(); break;
                            case "Saturation": toAdd = kvp.Value.saturation.ToString(); break;
                            case "TxQueue": toAdd = kvp.Value.txqueue.ToString(); break;
                            case "Lag": toAdd = kvp.Value.lag.ToString(); break;
                        }

                        //toAdd = HtmlEncode(toAdd);

                        contents += "<td class=\"column" + (column0 ? "0" : "1") + "\">";
                        contents += toAdd;
                        contents += "</td>";
                        column0 = !column0;
                    }

                    contents += "</tr>";
                }
                contents += "</table><br><br><br><br>";
            }
            if (AppSettings.Default.setStatPlayerListShowCharsBySys)
            {
                contents += "<size=5><b><u>Characters by System</u></b></size><br><br>";
                contents += "<table width=\"90%\" border=\"1\" cellspacing=\"0\" cellpadding=\"0\">";
                contents += "<tr><th bgcolor=\"#ECE9D8\" align=\"left\"><font face=\"Tahoma\" color=\"#000000\" size=\"2\">Character</font></th><th bgcolor=\"#ECE9D8\" align=\"left\"><font face=\"Tahoma\" color=\"#000000\" size=\"2\">System</font></th></tr>";
                foreach (KeyValuePair<string, List<string>> kvp in system_list)
                {
                    kvp.Value.Sort();
                    foreach (string character in kvp.Value)
                    {
                        contents += "<tr><td class=\"column0\">" + character + "</td><td class=\"column1\">" + kvp.Key + "</td></tr>";
                    }
                }
                contents += "</table>";
            }
            contents += "</body></html>";
            //*****   open the stream and write the contents ...
            String online_players_file = String.Format("{0}\\players_online.html", AppSettings.Default.setStatisticsDir);
            StreamWriter writer = new StreamWriter(online_players_file);
            try
            {
                writer.Write(contents);

            }
            catch (Exception ex)
            {
                log.AddLog(String.Format("Error '{0}' in GenerateOnlinePlayerStats", ex.Message));
            }
            finally
            {
                writer.Close();
            }
        }

        /// <summary>
        /// Generate player stats in a format similar to FLStat.
        /// </summary>
        /// <param name="bgWkr"></param>
        /// <param name="log"></param>
        public void GeneratePlayerStats(BackgroundWorker bgWkr, LogRecorderInterface log)
        {
            DateTime date = DateTime.Now;
            try
            {
                string filePath = String.Format("{0}\\players_{1:yyyyMMdd}.html", AppSettings.Default.setStatisticsDir, date);
                if (File.Exists(filePath))
                    return;


                using (DamDataSet ds = new DamDataSet())
                {
                    dataAccess.GetCharacterList(ds.CharacterList);

                    bgWkr.ReportProgress(75, "Generating player reports...");
                    //DataRow[] rows = ds.CharacterList.Select("IsDeleted = 'false'");

                    using (UIDataSet uds = new UIDataSet())
                    {
                        foreach (DamDataSet.CharacterListRow cr in ds.CharacterList)
                        {
                            if (!cr.IsDeleted)
                            {
                                uds.StatPlayer.AddStatPlayerRow(cr.CharName,
                                    new TimeSpan(0, 0, cr.OnLineSecs), cr.Money, cr.Rank, cr.LastOnLine, cr.OnLineSecs);
                            }
                        }

                        string[] colTitles = new string[] { "Name", "Rank", "Money", "Last On Line", "On Line Time" };
                        string[] colNames = new string[] { "Name", "Rank", "Money", "LastOnLine", "OnLineTime" };
                        string title = String.Format("Players - {0}", date);

                        SaveAsHTML(filePath, title, uds.StatPlayer.Select("", "Rank DESC"), colTitles, colNames, 0, 2500);
                        File.Copy(filePath, AppSettings.Default.setStatisticsDir + "\\players.html", true);
                    }
                }
            }
            catch (Exception ex)
            {
                log.AddLog(String.Format("Error '{0}' when calculating player statistics", ex.Message));
            }
        }

        /// <summary>
        /// Calculate and generate statistics for factions.
        /// </summary>
        /// <param name="bgWkr"></param>
        /// <param name="log"></param>
        public void GenerateFactionActivity(BackgroundWorker bgWkr, LogRecorderInterface log)
        {
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.Subtract(new TimeSpan(30, 0, 0, 0));

            // If statistics are disabled, do nothing.
            if (AppSettings.Default.setStatisticsDir.Length == 0)
                return;

            // Produce activity statistics if they don't exist.
            string filePath = String.Format("{0}\\activity_summary_{1:yyyyMMdd}.html", AppSettings.Default.setStatisticsDir, endDate);
            if (File.Exists(filePath))
                return;

            try
            {
                // Keep a summary table of all factions.
                UIDataSet summaryFactions = new UIDataSet();

                // Crunch the data.
                bgWkr.ReportProgress(80, "Creating activity statistics...");
                UIDataSet results = CalcActivity(startDate, endDate, bgWkr, log);

                // General overall activity report.
                string title = String.Format("Activity - {0} days to {1}", (endDate - startDate).Days, endDate);
                GenerateActivityReport(results, "", title, summaryFactions, endDate, true);

                // Save statistics for factions.
                bgWkr.ReportProgress(90, "Generating activity reports...");
                string[] factionsFilter = AppSettings.Default.setStatsFactions.Split(' ');
                foreach (string factionFilter in factionsFilter)
                {
                    string faction = factionFilter.Trim();
                    if (faction.Length > 0)
                    {
                        title = String.Format("Activity {0} - {1}", faction, endDate);
                        GenerateActivityReport(results, faction, title, summaryFactions, DateTime.MinValue, false);
                    }
                }

                // Write the index.html
                DataRow[] rows = summaryFactions.StatPlayer.Select("", "OnLineTime DESC");
                SaveAsHTML(filePath, String.Format("Activity Summary - {0} days to {1}", (endDate - startDate).Days, endDate),
                    rows, new string[] { "Name", "On Line Time" },
                    new string[] { "Name", "OnLineTime" }, 0, rows.Length);
                File.Copy(filePath, AppSettings.Default.setStatisticsDir + "\\activity_summary.html", true);
            }
            catch (Exception ex)
            {
                log.AddLog(String.Format("Error '{0}' when generating HTML", ex.Message));
                try { File.Delete(filePath); }
                catch { };
            }
        }

        /// <summary>
        /// Scan the historical character lists and build a list containing the time each players
        /// was online within the specified time period.
        /// </summary>
        /// <returns>Returns the path to an HTML file cot </returns>
        private UIDataSet CalcActivity(DateTime startDate, DateTime endDate, BackgroundWorker bgWkr, LogRecorderInterface log)
        {
            UIDataSet results = new UIDataSet();
            try
            {

                // Keep two tables; one containing the records when they are first detected and
                // the other with the most recent data.
                DamDataSet oldestData = new DamDataSet();
                DamDataSet newestData = new DamDataSet();
                Dictionary<string, int> onLineTimes = new Dictionary<string, int>();

                // Scan from oldest date to newest reading character lists from the database
                // and building the online time list.
                for (DateTime date = startDate.Subtract(new TimeSpan(90, 0, 0, 0)); date <= endDate; date = date.AddDays(1))
                {
                    if (bgWkr.CancellationPending)
                        return null;

                    using (DamDataSet tds = new DamDataSet())
                    {
                        if (dataAccess.GetCharacterHistory(date, tds.CharacterList))
                        {
                            foreach (DamDataSet.CharacterListRow tempCharRecord in tds.CharacterList)
                            {
                                if (bgWkr.CancellationPending)
                                    return null;

                                // Record the oldest data. Don't update if data is already present.
                                DamDataSet.CharacterListRow oldCharRecord = oldestData.CharacterList.FindByCharPath(tempCharRecord.CharPath);
                                if (oldCharRecord == null)
                                {
                                    oldestData.CharacterList.AddCharacterListRow(tempCharRecord.CharName,
                                        tempCharRecord.CharPath, tempCharRecord.Created, tempCharRecord.Updated,
                                        tempCharRecord.AccID, tempCharRecord.IsDeleted, tempCharRecord.Location,
                                        tempCharRecord.Rank, tempCharRecord.Money, tempCharRecord.Ship,
                                        tempCharRecord.AccDir, tempCharRecord.OnLineSecs, tempCharRecord.LastOnLine);
                                }
                                // Keep updating to find the newest record from before the startDate 
                                else if (tempCharRecord.LastOnLine < startDate)
                                {
                                    oldCharRecord.Created = tempCharRecord.Created;
                                    oldCharRecord.Updated = tempCharRecord.Updated;
                                    oldCharRecord.IsDeleted = tempCharRecord.IsDeleted;
                                    oldCharRecord.Location = tempCharRecord.Location;
                                    oldCharRecord.Rank = tempCharRecord.Rank;
                                    oldCharRecord.Money = tempCharRecord.Money;
                                    oldCharRecord.Ship = tempCharRecord.Ship;
                                    oldCharRecord.OnLineSecs = tempCharRecord.OnLineSecs;
                                    oldCharRecord.LastOnLine = tempCharRecord.LastOnLine;
                                }
                            }
                        }
                    }
                }

                newestData = oldestData.Copy() as DamDataSet;

                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (bgWkr.CancellationPending)
                        return null;

                    using (DamDataSet tds = new DamDataSet())
                    {
                        if (dataAccess.GetCharacterHistory(date, tds.CharacterList))
                        {
                            foreach (DamDataSet.CharacterListRow tempCharRecord in tds.CharacterList)
                            {
                                if (bgWkr.CancellationPending)
                                    return null; 
                                
                                // Record the newest data. Keep updating with the most recent records.
                                // Calculate the online seconds here to deal with the situation where
                                // a character has been deleted then re-created.
                                DamDataSet.CharacterListRow charRecord = newestData.CharacterList.FindByCharPath(tempCharRecord.CharPath);
                                if (charRecord == null)
                                {
                                    charRecord = newestData.CharacterList.AddCharacterListRow(tempCharRecord.CharName,
                                        tempCharRecord.CharPath, tempCharRecord.Created, tempCharRecord.Updated,
                                        tempCharRecord.AccID, tempCharRecord.IsDeleted, tempCharRecord.Location,
                                        tempCharRecord.Rank, tempCharRecord.Money, tempCharRecord.Ship,
                                        tempCharRecord.AccDir, tempCharRecord.OnLineSecs, tempCharRecord.LastOnLine);
                                }
                                else
                                {
                                    int onLineTime = tempCharRecord.OnLineSecs - charRecord.OnLineSecs;
                                    if (onLineTime < 0)
                                        onLineTime = 0;
                                    if (onLineTimes.ContainsKey(charRecord.CharPath))
                                        onLineTimes[charRecord.CharPath] += onLineTime;
                                    else
                                        onLineTimes[charRecord.CharPath] = onLineTime;

                                    charRecord.Created = tempCharRecord.Created;
                                    charRecord.Updated = tempCharRecord.Updated;
                                    charRecord.IsDeleted = tempCharRecord.IsDeleted;
                                    charRecord.Location = tempCharRecord.Location;
                                    charRecord.Rank = tempCharRecord.Rank;
                                    charRecord.Money = tempCharRecord.Money;
                                    charRecord.Ship = tempCharRecord.Ship;
                                    charRecord.OnLineSecs = tempCharRecord.OnLineSecs;
                                    charRecord.LastOnLine = tempCharRecord.LastOnLine;
                                }
                            }
                        }
                    }
                }

                // Build the results data set. This data set will be used to generate individual activity reports.
                foreach (DamDataSet.CharacterListRow newestCharRecord in newestData.CharacterList)
                {
                    if (bgWkr.CancellationPending)
                        return null;

                    DamDataSet.CharacterListRow oldestCharRecord = oldestData.CharacterList.FindByCharPath(newestCharRecord.CharPath);
                    if (oldestCharRecord != null)
                    {
                        int rankDelta = newestCharRecord.Rank - oldestCharRecord.Rank;
                        int moneyDelta = newestCharRecord.Money - oldestCharRecord.Money;
                        int onLineSecsDelta = 0;
                        if (onLineTimes.ContainsKey(oldestCharRecord.CharPath))
                            onLineSecsDelta = onLineTimes[oldestCharRecord.CharPath];

                        results.StatPlayer.AddStatPlayerRow(newestCharRecord.CharName,
                                            new TimeSpan(0, 0, onLineSecsDelta), moneyDelta, rankDelta,
                                            newestCharRecord.LastOnLine, onLineSecsDelta);
                    }
                }
            }
            catch (Exception ex)
            {
                log.AddLog(String.Format("Error '{0}' when calculating activity statistics", ex.Message));
                return null;
            }

            return results;
        }


        /// <summary>
        /// Generate from the results dataset for the specified charNameFilter
        /// </summary>
        void GenerateActivityReport(UIDataSet results, string charNameFilter, string title, UIDataSet summaryFactions, DateTime date, bool showChangedOnlineTimeOnly)
        {
            string[] colTitles = new string[] { "Name", "Last On Line", "On Line Time" };
            string[] colNames = new string[] { "Name", "LastOnLine", "OnLineTime" };

            // Prepare the filepath to save the HTML.
            string escapedFactionName = Regex.Replace(charNameFilter, @"[?:\\/*""<>|]", "");
            string filePath = String.Format("{0}\\activity_{1}.html", AppSettings.Default.setStatisticsDir, escapedFactionName);
            if (date > DateTime.MinValue)
                filePath = String.Format("{0}\\activity_{1:yyyyMMdd}.html", AppSettings.Default.setStatisticsDir, date);

            // Filter and sort the results table.
            string query = "(Name LIKE '" + FLUtility.EscapeLikeExpressionString(charNameFilter) + "%'" +
                            " OR Name LIKE '%" + FLUtility.EscapeLikeExpressionString(charNameFilter) + "')";
            if (showChangedOnlineTimeOnly)
                query += " AND OnLineSecs > 0";
            DataRow[] rows = results.StatPlayer.Select(query, "OnLineTime DESC");

            // Save it.
            SaveAsHTML(filePath, title, rows, colTitles, colNames, 0, rows.Length);

            // Update faction activity summary table to generate the index table later.
            string linkName = HtmlEncode(filePath.Substring(AppSettings.Default.setStatisticsDir.Length + 1));
            linkName = String.Format("<a href=\"{0}\">{1}</a>", HtmlEncode(linkName), HtmlEncode(linkName));
            TimeSpan totalOnLineTime = TimeSpan.Zero;
            foreach (UIDataSet.StatPlayerRow row in (UIDataSet.StatPlayerRow[])rows)
                totalOnLineTime += row.OnLineTime;
            summaryFactions.StatPlayer.AddStatPlayerRow(linkName, totalOnLineTime, 0, 0, DateTime.Now, (int)totalOnLineTime.TotalSeconds);
        }

        /// <summary>
        /// HTML-encodes a string and returns the encoded string.
        /// </summary>
        /// <param name="text">The text string to encode. </param>
        /// <returns>The HTML-encoded text.</returns>
        public static string HtmlEncode(string text)
        {
            if (text == null)
                return null;

            StringBuilder sb = new StringBuilder(text.Length);

            int len = text.Length;
            for (int i = 0; i < len; i++)
            {
                switch (text[i])
                {

                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    case '&':
                        sb.Append("&amp;");
                        break;
                    default:
                        if (text[i] > 159)
                        {
                            // decimal numeric entity
                            sb.Append("&#");
                            sb.Append(((int)text[i]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            sb.Append(";");
                        }
                        else
                            sb.Append(text[i]);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Save the dataset in HTML format.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="title"></param>
        /// <param name="rows"></param>
        /// <param name="colTitles"></param>
        /// <param name="colNames"></param>
        private void SaveAsHTML(string filePath, string title, DataRow[] rows, string[] colTitles, string[] colNames,
            int startRow, int count)
        {
            using (StreamWriter wr = new StreamWriter(filePath))
            {
                // Write the header
                wr.WriteLine(String.Format("<html><head><title>{0}</title>", HtmlEncode(title)));
                wr.WriteLine();
                wr.WriteLine("<style type=text/css>");
                for (int i = 0; i < colTitles.Length; i++)
                {
                    wr.WriteLine(String.Format(".Column{0} {{FONT-FAMILY: Tahoma; FONT-SIZE: 10pt;  TEXT-ALIGN: left; COLOR: #000000; BACKGROUND: #FFFFFF;}}", i));
                }
                wr.WriteLine("</style>");
                wr.WriteLine("</head><body>");
                wr.WriteLine();

                // Write the table header
                wr.WriteLine("<table width=\"90%\" border=\"1\" cellspacing=\"0\" cellpadding=\"0\">");
                wr.Write("<tr>");
                for (int i = 0; i < colTitles.Length; i++)
                {
                    wr.Write(String.Format("<th bgcolor=\"#ECE9D8\" align=\"left\"><font face=\"Tahoma\" color=\"#000000\" size=\"2\">{0}</font></th>", HtmlEncode(colTitles[i])));
                }
                wr.WriteLine("</tr>");

                // Write the table contents
                int endRow = startRow + count;
                for (int r = startRow; r < endRow; r++)
                {
                    wr.Write("<tr>");
                    for (int i = 0; i < colTitles.Length; i++)
                    {
                        wr.Write(String.Format("<td class=\"column{0}\">{1}</td>", i, rows[r][colNames[i]].ToString()));
                    }
                    wr.WriteLine("</tr>");
                }

                // Write the table footer
                wr.WriteLine("</table>");
                wr.WriteLine();
                wr.WriteLine("</body></html>");
                wr.Close();
            }
        }
    }
}
