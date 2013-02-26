using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.Data.Common;

namespace DAM
{
    public class DataAccess : IDisposable 
    {

        /// <summary>
        /// The current transaction.
        /// </summary>
        private SQLiteTransaction currTransaction;

        /// <summary>
        /// The connection to the database.
        /// </summary>
        private SQLiteConnection conn;

        public DataAccess()
        {
        }

        ~DataAccess()
        {
            Dispose();
        }

        /// <summary>
        /// Support for the IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            if (conn != null)
            {
                conn.Close();
                conn = null;
            }
        }

        /// <summary>
        /// Open a connection to the database if necessary.
        /// </summary>
        /// <returns>A database connection</returns>
        private SQLiteConnection GetConnection()
        {
            if (conn == null)
            {
                try
                {
                    conn = new SQLiteConnection("Data Source=" + AppSettings.Default.setAccountDir + "\\dsam.db; Cache Size=2000;Synchronous=Off;count_changes=0;temp_store=0;Version=3;Compress=True;");
                    conn.Open();

                    CreateTableIfNeeded(conn, "CharacterList", new string[] {
                        "CharPath text NOT NULL",
                        "AccDir text",
                        "AccID text",
                        "CharName text",
                        "IsDeleted bool",
                        "Location text",
                        "Ship text",
                        "Money integer",
                        "Rank integer",
                        "Created datetime",
                        "Updated datetime",
                        "OnLineSecs integer",
                        "LastOnLine datetime"
                    }, "PRIMARY KEY(CharPath) ON CONFLICT REPLACE");

                    CreateTableIfNeeded(conn, "BanList", new string[] {
                        "AccDir text NOT NULL",
                        "AccID text",
                        "BanReason text",
                        "BanStart datetime",
                        "BanEnd datetime",
                        "Existent bool"
                    }, "PRIMARY KEY(AccDir) ON CONFLICT REPLACE");

                    CreateTableIfNeeded(conn, "IPList", new string[] {
                        "AccDir text NOT NULL",
                        "IP text NOT NULL",
                        "AccessTime datetime"
                    }, "PRIMARY KEY(AccDir, IP) ON CONFLICT REPLACE");

                    CreateTableIfNeeded(conn, "LoginIDList", new string[] {
                        "AccDir text NOT NULL",
                        "LoginID text NOT NULL",
                        "AccessTime datetime"
                    }, "PRIMARY KEY(AccDir, LoginID) ON CONFLICT REPLACE");

                    CreateTableIfNeeded(conn, "LoginIDBanList", new string[] {
                        "LoginID text NOT NULL",
                        "Reason text"
                    }, "PRIMARY KEY(LoginID) ON CONFLICT REPLACE");

                    CreateTableIfNeeded(conn, "GeneralStatistics", new string[] {
                        "Description text NOT NULL",
                        "Result text",
                        "SQL text",
                    }, "PRIMARY KEY(Description) ON CONFLICT REPLACE");

                    CreateTableIfNeeded(conn, "HashList", new string[] {
                        "ItemHash integer",
                        "ItemNickName text",
                        "ItemType text",
                        "IDSName text",
                        "IDSInfo text",
                        "IDSInfo1 text",
                        "IDSInfo2 text",
                        "IDSInfo3 text",
                        "ItemKeys text",
                    }, null);

                    CreateTableIfNeeded(conn, "HardPointList", new string[] {
                        "ShipHash integer",
                        "HPName text",
                        "HPType text",
                        "MountableTypes text",
                        "DefaultItemHash integer",
                    }, null);

                    CreateTableIfNeeded(conn, "EquipInfoList", new string[] {
                        "EquipHash integer",
                        "ItemType text",
                        "MountableType text",
                    }, null);

                    CreateTableIfNeeded(conn, "ShipInfoList", new string[] {
                        "ShipHash integer",
                        "DefaultSound integer",
                        "DefaultEngine integer",
                        "DefaultPowerPlant integer",
                    }, null);
                                    }
                catch (Exception e)
                {
                    if (conn != null)
                        conn.Close();
                    conn = null;
                    throw e;
                }
            }
            return conn;
        }

        public void ExecuteSQL(string sql)
        {
            SQLiteCommand cmd = new SQLiteCommand(sql, GetConnection());
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Rebuild the statistics table.
        /// </summary>
        public void RebuildStatistics()
        {
            SQLiteCommand cmd = new SQLiteCommand("DELETE FROM GeneralStatistics;", GetConnection());
            cmd.ExecuteNonQuery();

            UpdateGeneralStatistics("Active characters", "SELECT COUNT(*) FROM CharacterList WHERE NOT IsDeleted ");
            UpdateGeneralStatistics("Deleted characters", "SELECT COUNT(*) FROM CharacterList WHERE IsDeleted");
            UpdateGeneralStatistics("Active Accounts", "SELECT COUNT(DISTINCT(AccDir)) FROM CharacterList WHERE NOT IsDeleted");
            UpdateGeneralStatistics("Banned Accounts", "SELECT COUNT(*) FROM BanList WHERE Existent");
            UpdateGeneralStatistics("Unique Logins", "SELECT COUNT(DISTINCT(LoginID)) FROM LoginIDList");
            UpdateGeneralStatistics("Characters over rank 80", "SELECT COUNT(*) FROM CharacterList WHERE Rank > '80'");
            UpdateGeneralStatistics("Characters under rank 30", "SELECT COUNT(*) FROM CharacterList WHERE Rank < '30'");
        }

        /// <summary>
        /// Create the specified table. Eventually this will issue alter commands
        /// to allow the addition of columns.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="name"></param>
        /// <param name="cols"></param>
        private bool CreateTableIfNeeded(SQLiteConnection conn, string name, string[] cols, string constraint)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "select * from sqlite_master where name = '" + name + "'";
                if (cmd.ExecuteReader().HasRows)
                {
                    using (SQLiteCommand pragmaCmd = new SQLiteCommand(conn))
                    {
                        pragmaCmd.CommandText = "PRAGMA table_info('" + name + "')";
                        SQLiteDataReader reader = pragmaCmd.ExecuteReader();

                        List<string> cols_add = new List<string>(cols);

                        while (true)
                        {
                            if (!reader.Read())
                                break;

                            string s = reader.GetString(1);

                            for (int i = 0; i < cols_add.Count; i++)
                            {
                                if (cols_add[i].Split(' ')[0] == s)
                                {
                                    cols_add.RemoveAt(i);
                                    i--;
                                    break;
                                }
                            }

                        }

                        foreach (string col in cols_add)
                        {
                            using (SQLiteCommand alterCmd = new SQLiteCommand(conn))
                            {
                                alterCmd.CommandText = "ALTER TABLE " + name + " ADD COLUMN " + col;
                                alterCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    return false;
                }
            }

            using (SQLiteCommand cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "create table " + name + " (";
                for (int i = 0; i < cols.Length; i++)
                {
                    if (i != 0)
                        cmd.CommandText += ", ";
                    cmd.CommandText += cols[i];
                }
                if (constraint != null)
                    cmd.CommandText += ", " + constraint;
                cmd.CommandText += ");";
                cmd.ExecuteNonQuery();
            }

            return true;
        }

        public void LoadGameData(GameDataSet ds, LogRecorderInterface log)
        {
            try
            {
                ds.Clear();
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM HashList", GetConnection()))
                    adpt.Fill(ds.HashList);
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM HardPointList", GetConnection()))
                    adpt.Fill(ds.HardPointList);
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM EquipInfoList", GetConnection()))
                    adpt.Fill(ds.EquipInfoList);
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM ShipInfoList", GetConnection()))
                    adpt.Fill(ds.ShipInfoList);
            }
            catch (Exception e)
            {
                log.AddLog(String.Format("Error '{0}' when updating db", e.Message));
            }
        }

        public void CommitGameData(GameDataSet ds, LogRecorderInterface log)
        {
            try
            {
                BeginTransaction();
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM HashList", GetConnection()))
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(ds.HashList);
                }
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM HardPointList", GetConnection()))
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(ds.HardPointList);
                }
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM EquipInfoList", GetConnection()))
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(ds.EquipInfoList);
                }
                using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM ShipInfoList", GetConnection()))
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(ds.ShipInfoList);
                }
                EndTransaction();
            }
            catch (Exception e)
            {
                log.AddLog(String.Format("Error '{0}' when updating db", e.Message));
            }
        }

        public void ClearGameData(LogRecorderInterface log)
        {
            try
            {
                BeginTransaction();
                using (SQLiteCommand cmd = GetConnection().CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM HashList";
                    cmd.ExecuteNonQuery();
                }
                using (SQLiteCommand cmd = GetConnection().CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM HardPointList";
                    cmd.ExecuteNonQuery();
                }
                using (SQLiteCommand cmd = GetConnection().CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM EquipInfoList";
                    cmd.ExecuteNonQuery();
                }
                using (SQLiteCommand cmd = GetConnection().CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ShipInfoList";
                    cmd.ExecuteNonQuery();
                }
                EndTransaction();
            }
            catch (Exception e)
            {
                log.AddLog(String.Format("Error '{0}' when updating db", e.Message));
            }
        }

        /// <summary>
        /// Commit pending changes on the CharacterList, BanList, IPList and LoginID
        /// </summary>
        /// <param name="dsTable"></param>
        public void CommitChanges(DamDataSet dsTable, LogRecorderInterface log)
        {
            BeginTransaction();

            using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM CharacterList", GetConnection()))
            {
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                builder.ConflictOption = ConflictOption.OverwriteChanges;
                try
                {
                    adpt.Update(dsTable.CharacterList);
                }
                catch (Exception e)
                {
                    
                    log.AddLog(String.Format("Error '{0}' when updating db with {1} records",
                        e.Message, dsTable.CharacterList.Count)); 
                }
            }
            using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM BanList", GetConnection()))
            {
                try
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(dsTable.BanList);
                }
                catch (Exception e)
                {
                    log.AddLog("Update of database failed: " + e.Message);
                }
            }
            using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM IPList", GetConnection()))
            {
                try
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(dsTable.IPList);
                }
                catch (Exception e)
                {
                    log.AddLog("Update of database failed: " + e.Message);
                }
            }
            using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM LoginIDList", GetConnection()))
            {
                try
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(dsTable.LoginIDList);
                }
                catch (Exception e)
                {
                    log.AddLog("Update of database failed: " + e.Message);
                }
            }
            using (SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM LoginIDBanList", GetConnection()))
            {
                try
                {
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adpt);
                    builder.ConflictOption = ConflictOption.OverwriteChanges;
                    adpt.Update(dsTable.LoginIDBanList);
                }
                catch (Exception e)
                {
                    log.AddLog("Update of database failed: " + e.Message);
                }
            }
            EndTransaction();
        }

        /// <summary>
        /// Get all character records
        /// </summary>
        public void GetCharacterList(DamDataSet.CharacterListDataTable dsTable)
        {
            dsTable.Clear();
            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM CharacterList", GetConnection());
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the characters for the specified account
        /// </summary>
        /// <param name="accID">The account dir</param>
        /// <returns>DataSet containing IP records</returns>
        public void GetCharacterListByAccDir(DamDataSet.CharacterListDataTable dsTable, string accDir)
        {
            dsTable.Clear();
            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM CharacterList WHERE AccDir ='" + accDir + "'", GetConnection());
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get all existent ban list records.
        /// </summary>
        /// <param name="dsTable">The ban list.</param>
        public void GetBanList(DamDataSet.BanListDataTable dsTable)
        {
            string query = "SELECT * FROM BanList";

            dsTable.Clear();
            SQLiteDataAdapter adpt = new SQLiteDataAdapter(query, GetConnection());
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the ban list for the specified account
        /// </summary>
        /// <param name="dsTable">The ban list.</param>
        public void GetBanListByAccDir(DamDataSet.BanListDataTable dsTable, string accDir, bool onlyExistent)
        {
            dsTable.Clear();
            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM BanList WHERE AccDir ='" + accDir + "'", GetConnection());
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the IPList for the specified account
        /// </summary>
        /// <param name="accID">The account dir</param>
        /// <returns>DataSet containing IP records</returns>
        public void GetIPListByAccDir(DamDataSet.IPListDataTable dsTable, string accDir)
        {
            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM IPList WHERE AccDir ='" + accDir + "' ORDER BY AccessTime DESC, IP", GetConnection());
            dsTable.Clear();
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the IPList for the specified array of IP addresses
        /// </summary>
        /// <param name="accID">The IPs to search for.</param>
        /// <returns>DataSet containing IP records</returns>
        public void GetIPListByIP(DamDataSet.IPListDataTable dsTable, string[] IPs)
        {
            dsTable.Clear();
            if (IPs.Length == 0)
                return;

            string query = "SELECT * FROM IPList WHERE";
            for (int i = 0; i < IPs.Length; i++)
            {
                if (i != 0)
                    query += " OR";
                query += " IP = '" + IPs[i] + "'";
            }

            SQLiteDataAdapter adpt = new SQLiteDataAdapter(query, GetConnection());
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the IPList for the specified IP and fragment of IP.
        /// </summary>
        /// <param name="accID">The IPs to search for.</param>
        /// <returns>DataSet containing IP records</returns>
        public void GetIPListByIP(DamDataSet.IPListDataTable dsTable, string ipFrag)
        {
            dsTable.Clear();

            string query = "SELECT * FROM IPList WHERE IP LIKE '" + SafeSqlLiteral(ipFrag) + "%'";

            SQLiteDataAdapter adpt = new SQLiteDataAdapter(query, GetConnection());
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the LoginIDList for the specified account
        /// </summary>
        /// <param name="accID">The account dir</param>
        /// <returns>DataSet containing LoginID records</returns>
        public void GetLoginIDListByAccDir(DamDataSet.LoginIDListDataTable dsTable, string accDir)
        {
            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM LoginIDList WHERE AccDir ='" + accDir + "' ORDER BY AccessTime DESC, LoginID", GetConnection());
            dsTable.Clear();
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the LoginIDList for the specified loginID
        /// </summary>
        /// <param name="accID">The login ID to search for.</param>
        /// <returns>DataSet containing LoginID records</returns>
        public void GetLoginIDListByLoginID(DamDataSet.LoginIDListDataTable dsTable, params string[] loginIDs)
        {
            dsTable.Clear();
            if (loginIDs.Length == 0)
                return;

            string query = "SELECT * FROM LoginIDList WHERE";
            for (int i = 0; i < loginIDs.Length; i++)
            {
                if (i != 0)
                    query += " OR";
                query += " LoginID =  '" + loginIDs[i] + "'";
;
            }
            query += "";

            SQLiteDataAdapter adpt = new SQLiteDataAdapter(query, GetConnection());
            adpt.Fill(dsTable);
        }

        /// <summary>
        /// Get the latest LoginID
        /// </summary>
        /// <param name="accDir">The account dir</param>
        /// <returns>The row with the lastest loginID, null if no loginID exists</returns>
        public DamDataSet.LoginIDListRow GetLatestLoginIDRowByAccDir(string accDir)
        {
            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM LoginIDList WHERE AccDir ='" + accDir + "' ORDER BY AccessTime DESC LIMIT 0,1", GetConnection());
            DamDataSet.LoginIDListDataTable dsTable = new DamDataSet.LoginIDListDataTable();
            adpt.Fill(dsTable);
            if (dsTable.Count == 0)
                return null;
            return dsTable[0];
        }

        /// <summary>
        /// Get the LoginIDBanListRow of a LoginID
        /// </summary>
        /// <param name="loginID">The loginID</param>
        /// <returns>The row with of the loginID, null if no row was found</returns>
        public DamDataSet.LoginIDBanListRow GetLoginIDBanRowByLoginID(string loginID)
        {
            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM LoginIDBanList WHERE loginID ='" + loginID + "' LIMIT 0,1", GetConnection());
            DamDataSet.LoginIDBanListDataTable dsTable = new DamDataSet.LoginIDBanListDataTable();
            adpt.Fill(dsTable);
            if (dsTable.Count == 0)
                return null;
            return dsTable[0];
        }

        /// <summary>
        /// Get all statistics records
        /// </summary>
        public void GetGeneralStatisticsTable(DamDataSet.GeneralStatisticsTableDataTable dsTable)
        {
            dsTable.Clear();

            SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM GeneralStatistics", GetConnection());
            adpt.Fill(dsTable);

            foreach (DamDataSet.GeneralStatisticsTableRow row in dsTable)
            {
                SQLiteCommand cmd = new SQLiteCommand(row.SQL, GetConnection());
                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    row.Result = (long)rdr.GetValue(0);
                }
            }
        }

        /// <summary>
        /// Insert into the general statistics table.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="SQL"></param>
        public void UpdateGeneralStatistics(string description, string SQL)
        {
            SQLiteCommand cmd = new SQLiteCommand(GetConnection());
            cmd.CommandText =
                        "INSERT OR REPLACE INTO GeneralStatistics ( Description, SQL ) " +
                        "VALUES ( :Description, :SQL )";
            cmd.Parameters.Add(":Description", DbType.String);
            cmd.Parameters.Add(":SQL", DbType.String);
            cmd.Parameters[0].Value = description;
            cmd.Parameters[1].Value = SQL;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// The name of the character list history table for the specified date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static string GetCharacterHistoryName(DateTime date)
        {
            return String.Format("CharacterList_{0:yyyyMMdd}", date);
        }

        /// <summary>
        /// Return true if the CharacterHistory table exists on the specified day.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public bool CharacterHistoryExists(DateTime date)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(GetConnection()))
            {
                cmd.CommandText = "select * from sqlite_master where name = '" + GetCharacterHistoryName(date) + "'";
                if (cmd.ExecuteReader().HasRows)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks to see if the history table for yesterday exists. If it doesn't
        /// then return true otherwise false.
        /// </summary>
        public bool MakeActiveCharacterHistoryNeeded()
        {
            DateTime yesterday = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
            if (CharacterHistoryExists(yesterday))
                return false;
            return true;
        }

        /// <summary>
        /// Drops old history tables based on a backward horizon in days
        /// </summary>
        public void CleanUpCharacterHistory(int horizon)
        {
           DateTime last_week = DateTime.Now.Subtract(new TimeSpan(horizon, 0, 0, 0));
           string comparator = GetCharacterHistoryName(last_week);
           List<string> tlist = new List<string>();
           using (SQLiteCommand cmd = new SQLiteCommand(GetConnection()))
           {
              cmd.CommandText = "select name from sqlite_master where name like 'CharacterList_%' and name<'" + comparator + "'";
              SQLiteDataReader rdr = cmd.ExecuteReader();
              if (rdr.HasRows)
              {
                 while (rdr.Read())
                 {
                    tlist.Add(rdr.GetString(0));
                 }
                 rdr.Close();
              }
           }
           foreach(string tname in tlist)
           {
              BeginTransaction();
              using (SQLiteCommand cmd = new SQLiteCommand(GetConnection()))
              {
                 cmd.CommandText = "drop table if exists " + tname;
                 cmd.ExecuteNonQuery();
              }
              EndTransaction();              
           }
        }

        /// <summary>
        /// Builds the history table for yesterday by copying information about all
        /// currently active characters into the history table.
        /// </summary>
        public void MakeActiveCharacterHistory()
        {
            if (MakeActiveCharacterHistoryNeeded())
            {
                DateTime yesterday = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));                
                
                using (SQLiteCommand cmd = new SQLiteCommand(GetConnection()))
                {
                    cmd.CommandText = "select * from sqlite_master where name = 'CharacterList'";
                    SQLiteDataReader rdr = cmd.ExecuteReader();
                    if (!rdr.HasRows)
                        return;
                    using (SQLiteCommand cmd1 = new SQLiteCommand(GetConnection()))
                    {
                        cmd1.CommandText = rdr["sql"].ToString().Replace("CharacterList", GetCharacterHistoryName(yesterday));
                        cmd1.ExecuteNonQuery();
                    }
                }

                BeginTransaction();
                using (SQLiteCommand cmd = new SQLiteCommand(GetConnection()))
                {
                    cmd.CommandText = "INSERT OR REPLACE INTO " + GetCharacterHistoryName(yesterday) +
                        " SELECT * FROM CharacterList WHERE NOT IsDeleted AND (LastOnLine > Date('now','-1 day'))";
                    cmd.ExecuteNonQuery();
                }
                EndTransaction();
            }
        }

        /// <summary>
        /// Get all character history records for the specified date. Return true if the history
        /// table exists and was loaded into the dataset, otherwise return false.
        /// </summary>
        public bool GetCharacterHistory(DateTime date, DamDataSet.CharacterListDataTable dsTable)
        {
            if (date == DateTime.Today)
            {
                dsTable.Clear();
                SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM CharacterList WHERE NOT IsDeleted", GetConnection());
                adpt.Fill(dsTable);
                return true;
            }
            else if (CharacterHistoryExists(date))
            {
                dsTable.Clear();
                SQLiteDataAdapter adpt = new SQLiteDataAdapter("SELECT * FROM " + GetCharacterHistoryName(date), GetConnection());
                adpt.Fill(dsTable);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Start a new transaction. This is commit a pending transaction before
        /// starting a new one.
        /// </summary>
        public void BeginTransaction()
        {
            if (currTransaction != null)
            {
                currTransaction.Commit();
                currTransaction = null;
            }
            currTransaction = GetConnection().BeginTransaction();
        }

        /// <summary>
        /// End the current transaction and commit all changes.
        /// </summary>
        public void EndTransaction()
        {
            if (currTransaction != null)
            {
                currTransaction.Commit();
                currTransaction = null;
            }
        }

        /// <summary>
        /// Escape SQL text.
        /// </summary>
        /// <param name="inputSQL"></param>
        /// <returns></returns>
        public static string SafeSqlLiteral(string inputSQL)
        {
            return inputSQL.Replace("'", "''");
        }
    }
}