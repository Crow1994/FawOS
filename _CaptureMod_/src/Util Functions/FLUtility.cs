/*
 * Purpose: Miscellanous utility functions.
 * Author: Cannon
 * Date: Jan 2010
 * 
 * Item hash algorithm from flhash.exe by sherlog@t-online.de (2003-06-11)
 * Faction hash algorithm from flfachash.exe by Haenlomal (October 2006)
 * 
 * This is free software. Permission to copy, store and use granted as long
 * as this note remains intact.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

namespace DAM
{
    class FLUtility
    {
        public static DateTime INVALID_DATE = new DateTime(0);

        public static Dictionary<string, List<KeyValuePair<string, string>>> LOGIN_ID_FILES;

        /// <summary>
        /// Decode an ascii hex string into unicode
        /// </summary>
        /// <param name="encodedName">The encoded value</param>
        /// <returns>The deocded value</returns>
        public static string DecodeUnicodeHex(string encodedValue)
        {
            string name = "";
            while (encodedValue.Length > 0)
            {
                name += (char)System.Convert.ToUInt16(encodedValue.Substring(0, 4), 16);
                encodedValue = encodedValue.Remove(0, 4);
            }
            return name;
        }


        /// <summary>
        /// Decode a unicode string into ascii hex
        /// </summary>
        /// <param name="value">The value string to encode</param>
        /// <returns>The encoded value</returns>
        public static string EncodeUnicodeHex(string value)
        {
            return BitConverter.ToString(Encoding.BigEndianUnicode.GetBytes(value)).Replace("-","");
        }

        
        /// <summary>
        /// Look up table for faction id creation.
        /// </summary>
        private static uint[] createFactionIDTable = null;

        /// <summary>
        /// Function for calculating the Freelancer data nickname hash.
        /// Algorithm from flfachash.c by Haenlomal (October 2006).
        /// </summary>
        /// <param name="nickName"></param>
        /// <returns></returns>
        public static uint CreateFactionID(string nickName)
        {
            const uint FLFACHASH_POLYNOMIAL = 0x1021;
            const uint NUM_BITS = 8;
            const int HASH_TABLE_SIZE = 256;

            if (createFactionIDTable == null)
            {
                // The hash table used is the standard CRC-16-CCITT Lookup table 
                // using the standard big-endian polynomial of 0x1021.
                createFactionIDTable = new uint[HASH_TABLE_SIZE];
                for (uint i = 0; i < HASH_TABLE_SIZE; i++)
                {
                    uint x = i << (16 - (int)NUM_BITS);
                    for (uint j = 0; j < NUM_BITS; j++)
                    {
                        x = ((x & 0x8000) == 0x8000) ? (x << 1) ^ FLFACHASH_POLYNOMIAL : (x << 1);
                        x &= 0xFFFF;
                    }
                    createFactionIDTable[i] = x;
                }
            }

            byte[] tNickName = Encoding.ASCII.GetBytes(nickName.ToLowerInvariant());

            uint hash = 0xFFFF;
            for (uint i = 0; i < tNickName.Length; i++)
            {
                uint y = (hash & 0xFF00) >> 8;
                hash = y ^ (createFactionIDTable[(hash & 0x00FF) ^ tNickName[i]]);
            }

            return hash;
        }

        /// <summary>
        /// Look up table for id creation.
        /// </summary>
        private static uint[] createIDTable = null;

        /// <summary>
        /// Function for calculating the Freelancer data nickname hash.
        /// Algorithm from flhash.exe by sherlog@t-online.de (2003-06-11)
        /// </summary>
        /// <param name="nickName"></param>
        /// <returns></returns>
        public static uint CreateID(string nickName)
        {
            const uint FLHASH_POLYNOMIAL = 0xA001;
            const int LOGICAL_BITS = 30;
            const int PHYSICAL_BITS = 32;

            // Build the crc lookup table if it hasn't been created
            if (createIDTable == null)
            {
                createIDTable = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint x = i;
                    for (uint bit = 0; bit < 8; bit++)
                        x = ((x & 1) == 1) ? (x >> 1) ^ (FLHASH_POLYNOMIAL << (LOGICAL_BITS - 16)) : x >> 1;
                    createIDTable[i] = x;
                }
                if (2926433351 != CreateID("st01_to_st03_hole")) throw new Exception("Create ID hash algoritm is broken!");
                if (2460445762 != CreateID("st02_to_st01_hole")) throw new Exception("Create ID hash algoritm is broken!");
                if (2263303234 != CreateID("st03_to_st01_hole")) throw new Exception("Create ID hash algoritm is broken!");
                if (2284213505 != CreateID("li05_to_li01")) throw new Exception("Create ID hash algoritm is broken!");
                if (2293678337 != CreateID("li01_to_li05")) throw new Exception("Create ID hash algoritm is broken!");
            }

            byte[] tNickName = Encoding.ASCII.GetBytes(nickName.ToLowerInvariant());

            // Calculate the hash.
            uint hash = 0;
            for (int i = 0; i < tNickName.Length; i++)
                hash = (hash >> 8) ^ createIDTable[(byte)hash ^ tNickName[i]];
            // b0rken because byte swapping is not the same as bit reversing, but 
            // that's just the way it is; two hash bits are shifted out and lost
            hash = (hash >> 24) | ((hash >> 8) & 0x0000FF00) | ((hash << 8) & 0x00FF0000) | (hash << 24);
            hash = (hash >> (PHYSICAL_BITS - LOGICAL_BITS)) | 0x80000000;

            return hash;
        }

        /// <summary>
        /// Escape a string for an expression.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EscapeLikeExpressionString(string value)
        {
            string escapedText = value;
            escapedText = escapedText.Replace("[", "[[]");
            //filter = filter.Replace("]", "[]]");
            escapedText = escapedText.Replace("%", "[%]");
            escapedText = escapedText.Replace("*", "[*]");
            escapedText = escapedText.Replace("'", "''");
            return escapedText;
        }

        /// <summary>
        /// Escape a string for an expression.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EscapeEqualsExpressionString(string value)
        {
            string escapedText = value;
            escapedText = escapedText.Replace("'", "''");
            return escapedText;
        }

        /// <summary>
        /// Get the account id from the specified account directory.
        /// Will throw file open exceptions if the 'name' file cannot be opened.
        /// </summary>
        /// <param name="accDirPath">The account directory to search.</param>
        public static string GetAccountID(string accDirPath)
        {
            string accountIdFilePath = accDirPath + Path.DirectorySeparatorChar + "name";

            // Read a 'name' file into memory.
            FileStream fs = System.IO.File.OpenRead(accountIdFilePath);
            byte[] buf = new byte[fs.Length];
            fs.Read(buf, 0, (int)fs.Length);
            fs.Close();

            // Decode the account ID
            string accountID = "";
            for (int i = 0; i < buf.Length; i += 2)
            {
                switch (buf[i])
                {
                    case 0x43:
                        accountID += '-';
                        break;
                    case 0x0f:
                        accountID += 'a';
                        break;
                    case 0x0c:
                        accountID += 'b';
                        break;
                    case 0x0d:
                        accountID += 'c';
                        break;
                    case 0x0a:
                        accountID += 'd';
                        break;
                    case 0x0b:
                        accountID += 'e';
                        break;
                    case 0x08:
                        accountID += 'f';
                        break;
                    case 0x5e:
                        accountID += '0';
                        break;
                    case 0x5f:
                        accountID += '1';
                        break;
                    case 0x5c:
                        accountID += '2';
                        break;
                    case 0x5d:
                        accountID += '3';
                        break;
                    case 0x5a:
                        accountID += '4';
                        break;
                    case 0x5b:
                        accountID += '5';
                        break;
                    case 0x58:
                        accountID += '6';
                        break;
                    case 0x59:
                        accountID += '7';
                        break;
                    case 0x56:
                        accountID += '8';
                        break;
                    case 0x57:
                        accountID += '9';
                        break;
                    default:
                        accountID += '?';
                        break;
                }
            }

            return accountID;
        }

        /// <summary>
        /// Return the location string for the specified player
        /// </summary>
        /// <param name="gameData">The current game data.</param>
        /// <param name="charFile">The player's data file.</param>
        /// <returns>A string containing the location.</returns>
        public static string GetLocation(FLGameData gameData, FLDataFile charFile)
        {
            string location = gameData.GetItemDescByNickNameX(charFile.GetSetting("Player", "system").Str(0));
            if (charFile.SettingExists("Player", "pos"))
            {
                float posX = charFile.GetSetting("Player", "pos").Float(0);
                float posY = charFile.GetSetting("Player", "pos").Float(1);
                float posZ = charFile.GetSetting("Player", "pos").Float(2);
                location += String.Format(" in space {0}, {1}, {2}", posX, posY, posZ);
            }
            else
            {
                string basename = gameData.GetItemDescByNickNameX(charFile.GetSetting("Player", "base").Str(0));
                if (basename.Trim().Length==0)
                    basename += "-ERROR";
                location += " docked at " + basename + " [" + charFile.GetSetting("Player", "base").Str(0) + "]";
            }
            return location;
        }


        /// <summary>
        /// Return the ship string for the specified player
        /// </summary>
        /// <param name="gameData">The current game data.</param>
        /// <param name="charFile">The player's data file.</param>
        /// <returns>A string containing the ship name.</returns>
        public static string GetShip(FLGameData gameData, FLDataFile charFile, out Int64 shipArchType)
        {
            string nickNameOrHash = charFile.GetSetting("Player", "ship_archetype").Str(0);
            GameDataSet.HashListRow shipItem = gameData.GetItemByNickName(nickNameOrHash);
            if (shipItem != null)
            {
                shipArchType = shipItem.ItemHash;
            }
            else
            {
                shipArchType = charFile.GetSetting("Player", "ship_archetype").UInt(0);
            }
            return gameData.GetItemDescByHash(shipArchType);
        }

        /// <summary>
        /// Hack FL formatted xml into a RTF format.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static string FLXmlToRtf(string xml)
        {
            int xmlEnd = xml.IndexOf("</RDP>");
            if (xmlEnd >= 0)
                xml = xml.Substring(0, xmlEnd);
            xml = xml.Replace("<JUST loc=\"center\"/>", "\\qc ");
            xml = xml.Replace("<JUST loc=\"left\"/>", "\\pard ");
            xml = xml.Replace("<TRA data=\"1\" mask=\"1\" def=\"-2\"/>", "\\b ");
            xml = xml.Replace("<TRA data=\"1\" mask=\"1\" def=\"0\"/>", "\\b0 ");
            xml = xml.Replace("<TRA data=\"0\" mask=\"1\" def=\"-1\"/>", "\\b0 ");
            xml = xml.Replace("<PARA/>", "\\par ");
            xml = System.Text.RegularExpressions.Regex.Replace(xml, "<[^<>]*>", "");
            xml = xml.Replace("&gt;", ">");
            xml = xml.Replace("&lt;", "<");
            xml = xml.Trim();
            return xml;
        }

        /// <summary>
        /// Rrturn the charfile file access timestamp. This is the time
        /// this character was last accessed.
        /// </summary>
        /// <param name="charFile">The charfile to check.</param>
        /// <returns>The access time</returns>
        public static DateTime GetTimeStamp(FLDataFile charFile)
        {
            if (charFile.SettingExists("Player", "tstamp"))
            {
                long high = charFile.GetSetting("Player", "tstamp").UInt(0);
                long low = charFile.GetSetting("Player", "tstamp").UInt(1);
                return DateTime.FromFileTimeUtc(high << 32 | low);
            }
            return DateTime.Now;
        }

        /// <summary>
        /// Return the secs this character has been played on
        /// </summary>
        /// <param name="charFile">The charfile to check.</param>
        /// <returns>Seconds of in game time.</returns>
        public static uint GetOnLineTime(FLDataFile charfile)
        {
            if (charfile.SettingExists("mPlayer", "total_time_played"))
                return (uint)charfile.GetSetting("mPlayer", "total_time_played").Float(0);
            else
                return 0;
        }

        /// <summary>
        /// Scan all accounts from within a background worker.
        /// </summary>
        /// <param name="bgWkr">The worker. Progress updates are reported here.</param>
        /// <param name="log">The log interface</param>
        /// <param name="gameData">The freelancer game data</param>
        /// <returns></returns>
        public static DamDataSet CheckAccounts(System.ComponentModel.BackgroundWorker bgWkr,
            LogRecorderInterface log, FLGameData gameData)
        {
            FLHookSocket flsk = new FLHookSocket();

            // Drop the process priority while we're doing this.
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass =
                System.Diagnostics.ProcessPriorityClass.BelowNormal;

            DateTime updateTime = DateTime.Now.AddMilliseconds(100);

            DamDataSet tempDataStore = new DamDataSet();
            using (DataAccess da = new DataAccess())
            {
                log.AddLog("Loading database...");
                da.GetBanList(tempDataStore.BanList);
                da.GetCharacterList(tempDataStore.CharacterList);
                log.AddLog("Load complete "
                    + tempDataStore.CharacterList.Count + " characters, "
                    + tempDataStore.BanList.Count + " bans");


                // Clean obviously corrupt and old files
                if (AppSettings.Default.setAutomaticCharClean)
                {
                    int totalFilesDeleted = 0;
                    bgWkr.ReportProgress(0, "Cleaning accounts...");
                    log.AddLog("Cleaning accounts...");
                    string[] accDirs = Directory.GetDirectories(AppSettings.Default.setAccountDir, "??-????????");
                    for (int i = 0; i < accDirs.Length && !bgWkr.CancellationPending; i++)
                    {
                        if (updateTime < DateTime.Now)
                        {
                            updateTime = DateTime.Now.AddMilliseconds(1000);
                            bgWkr.ReportProgress((i * 100) / accDirs.Length);
                        }
                        string accDir = accDirs[i].Substring(AppSettings.Default.setAccountDir.Length + 1);
                        totalFilesDeleted += FLUtility.CleanCharAccount(tempDataStore, log, gameData, accDir);
                    }
                    log.AddLog("Removed " + totalFilesDeleted + " files");
                }

                // Read LoginID-Bans
                bgWkr.ReportProgress(0, "Reading LognID-Bans...");
                log.AddLog("Reading LognID-Bans...");
                bool retn = ReadLoginIDBans(tempDataStore, da, AppSettings.Default.setLoginIDBanFile);
                if (!retn)
                {
                    log.AddLog(tempDataStore.LoginIDBanList.Rows.Count + " LoginID-Bans in database");
                }
                else
                {
                    log.AddLog("Failed to load LoginID-Bans! Check your settings!");
                }

                // Check for new/changed character files.
                int totalFilesUpdated = 0;
                bgWkr.ReportProgress(0, "Checking for new or changed characters...");
                log.AddLog("Checking for new or changed characters...");
                string[] accDirs1 = Directory.GetDirectories(AppSettings.Default.setAccountDir, "??-????????");
                for (int i = 0; i < accDirs1.Length && !bgWkr.CancellationPending; i++)
                {
                    if (updateTime < DateTime.Now)
                    {
                        updateTime = DateTime.Now.AddMilliseconds(1000);
                        bgWkr.ReportProgress((i * 100) / accDirs1.Length);
                    }
                    string accDir = accDirs1[i].Substring(AppSettings.Default.setAccountDir.Length + 1);
                    totalFilesUpdated += FLUtility.ScanCharAccount(tempDataStore, log, gameData, accDir);
                }
                log.AddLog("Updated " + totalFilesUpdated + " files");


                // Check for deleted char files and accounts & remove inactive/uninterested players
                bgWkr.ReportProgress(0, "Checking for deleted and inactive/uninterested characters...");
                log.AddLog("Checking for deleted and inactive/uninterested characters...");
                int inactiveChars = 0;
                int uninterestedChars = 0;
                int len = tempDataStore.CharacterList.Rows.Count;
                for (int i = 0; i < len && !bgWkr.CancellationPending; i++)
                {
                    DamDataSet.CharacterListRow charRecord = tempDataStore.CharacterList[i];
                    if (updateTime < DateTime.Now)
                    {
                        updateTime = DateTime.Now.AddMilliseconds(1000);
                        bgWkr.ReportProgress((i * 100) / len);
                    }

                    string charFilePath = AppSettings.Default.setAccountDir + "\\" + charRecord.CharPath;
                    if (!charRecord.IsDeleted && !File.Exists(charFilePath))
                    {
                        charRecord.IsDeleted = true;
                    }

                    if (!charRecord.IsDeleted && AppSettings.Default.setAutomaticCharWipe)
                    {
                        double lastOnlineDaysAgo = DateTime.Now.Subtract(charRecord.LastOnLine).TotalDays;
                        if (lastOnlineDaysAgo > (double)AppSettings.Default.setDaysToDeleteInactiveChars)
                        {
                            if (!AppSettings.Default.setMoveUninterestedChars)
                            {
                                // Delete the character via FLHook too.
                                if (flsk.IsConnected())
                                    flsk.CmdDeleteChar(charRecord.CharName);
                                File.Delete(charFilePath);
                            }
                            else
                            {
                                MoveChar(charFilePath, AppSettings.Default.setMoveUninterestedCharsDir, log);
                            }
                            charRecord.IsDeleted = true;
                            inactiveChars++;
                        }
                        else if (lastOnlineDaysAgo > (uint)AppSettings.Default.setDaysInactiveToDeleteUninterestedChars
                            && charRecord.OnLineSecs < (uint)AppSettings.Default.setSecsToDeleteUninterestedChars)
                        {
                            if (!AppSettings.Default.setMoveUninterestedChars)
                            {
                                // Delete the character via FLHook too.
                                if (flsk.IsConnected())
                                    flsk.CmdDeleteChar(charRecord.CharName);
                                File.Delete(charFilePath);
                            }
                            else
                            {
                                MoveChar(charFilePath, AppSettings.Default.setMoveUninterestedCharsDir, log);
                            }
                            charRecord.IsDeleted = true;
                            uninterestedChars++;
                        }
                    }
                }
                log.AddLog("Removed " + inactiveChars + " inactive characters, "
                    + uninterestedChars + " uninterested characters");

                // Commit any database changes.
                log.AddLog("Saving database...");
                bgWkr.ReportProgress(50, "Saving database...");
                da.CommitChanges(tempDataStore, log);
                
                tempDataStore.AcceptChanges();
                log.AddLog("Save complete "
                    + tempDataStore.CharacterList.Count + " characters, "
                    + tempDataStore.BanList.Count + " bans");
            }
            
            return tempDataStore;
        }

        /// <summary>
        /// Moves one single char-file to an other directory.
        /// Also creates the account-directory in toPath if necessary.
        /// </summary>
        /// <param name="charFile">char-file to move</param>
        /// <param name="toPath">the destination</param>
        /// <param name="log">The LogRecorderInterface</param>
        public static void MoveChar(String charFile, String toPath, LogRecorderInterface log)
        {
            String[] oldName;
            String newFileName;

            // extracts the freelancer-id-directory from the full-path
            oldName = charFile.Split('\\', '/');
            // oldName.Length - 1 = charfile
            // oldName.Length - 2 = account-directory
            newFileName = toPath + "\\" + oldName[oldName.Length - 2] + "\\" + oldName[oldName.Length - 1];

            // create the freelancer-id-directory if it doesn't exist
            if (!Directory.Exists(toPath + "\\" + oldName[oldName.Length - 2]))
                Directory.CreateDirectory(toPath + "\\" + oldName[oldName.Length - 2]);

            try
            {
                if (File.Exists(newFileName)) // check to prevent an exception because of a existing file
                    File.Move(newFileName, newFileName + ".bak"); // make a backup if something goes wrong
                File.Move(charFile, newFileName); // move the charfile..
                // no file-exists-check because it doesn't throw an exception
                File.Delete(newFileName + ".bak"); // delete the backup
            }
            catch (Exception ex)
            {
                log.AddLog("Error (" + ex.Message + ") while moveing: " + charFile + "!");
                log.AddLog(ex.StackTrace);

                try
                {
                    if (File.Exists(newFileName + ".bak")) // try to revert the backup (something has gone wrong)
                        File.Move(newFileName + ".bak", newFileName);
                }
                catch (Exception ex2)
                {
                    log.AddLog("Error (" + ex.Message + ") trying to fix the previous error with " + charFile + "! Your old charfile should be at *charfilename*.bak");
                    log.AddLog(ex2.StackTrace);
                }
            }

        }

        public static bool ReadLoginIDBans(DamDataSet dataSet, DataAccess da, string filepath)
        {
            if (!File.Exists(filepath))
                return false;

            string[] bans = File.ReadAllLines(filepath);
            string tmpLoginID = null;
            string tmpReason = null;

            /*
             * If a file contains the same loginID twice, AddLoginIDBanListRow will crash.
             * This is because GetLoginIDBanRowByLogin reads from the file,
             * but AddLoginIDBanListRow isn't synced to the file.
             * 
             * This list stores all banned IDs.
             */
            List<string> bannedIDs = new List<string>(bans.Length);

            dataSet.LoginIDBanList.Rows.Clear();

            foreach (string ban in bans)
            {
                string[] parts = ban.Split('=');
                
                if (parts.Length == 0)
                    continue;

                tmpReason = null;
                tmpLoginID = parts[0].Trim();

                if (parts.Length == 2)
                {
                    tmpReason = parts[1].Trim();
                }

                if (bannedIDs.Contains(tmpLoginID))
                    continue;

                // check if the ban exists..
                DamDataSet.LoginIDBanListRow loginIDRecord = da.GetLoginIDBanRowByLoginID(tmpLoginID);
                if (loginIDRecord != null)
                {
                    // just update the reason
                    loginIDRecord.Reason = tmpReason;
                }
                else
                {
                    // add the ban
                    dataSet.LoginIDBanList.AddLoginIDBanListRow(tmpLoginID, tmpReason);
                    bannedIDs.Add(tmpLoginID);
                }
            }

            return true;
        }

        static uint[] crctbl = {
                0x00000000,0x00A00100,0x01400200,0x01E00300,0x02800400,0x02200500,0x03C00600,0x03600700,
                0x05000800,0x05A00900,0x04400A00,0x04E00B00,0x07800C00,0x07200D00,0x06C00E00,0x06600F00,
                0x0A001000,0x0AA01100,0x0B401200,0x0BE01300,0x08801400,0x08201500,0x09C01600,0x09601700,
                0x0F001800,0x0FA01900,0x0E401A00,0x0EE01B00,0x0D801C00,0x0D201D00,0x0CC01E00,0x0C601F00,
                0x14002000,0x14A02100,0x15402200,0x15E02300,0x16802400,0x16202500,0x17C02600,0x17602700,
                0x11002800,0x11A02900,0x10402A00,0x10E02B00,0x13802C00,0x13202D00,0x12C02E00,0x12602F00,
                0x1E003000,0x1EA03100,0x1F403200,0x1FE03300,0x1C803400,0x1C203500,0x1DC03600,0x1D603700,
                0x1B003800,0x1BA03900,0x1A403A00,0x1AE03B00,0x19803C00,0x19203D00,0x18C03E00,0x18603F00,
                0x28004000,0x28A04100,0x29404200,0x29E04300,0x2A804400,0x2A204500,0x2BC04600,0x2B604700,
                0x2D004800,0x2DA04900,0x2C404A00,0x2CE04B00,0x2F804C00,0x2F204D00,0x2EC04E00,0x2E604F00,
                0x22005000,0x22A05100,0x23405200,0x23E05300,0x20805400,0x20205500,0x21C05600,0x21605700,
                0x27005800,0x27A05900,0x26405A00,0x26E05B00,0x25805C00,0x25205D00,0x24C05E00,0x24605F00,
                0x3C006000,0x3CA06100,0x3D406200,0x3DE06300,0x3E806400,0x3E206500,0x3FC06600,0x3F606700,
                0x39006800,0x39A06900,0x38406A00,0x38E06B00,0x3B806C00,0x3B206D00,0x3AC06E00,0x3A606F00,
                0x36007000,0x36A07100,0x37407200,0x37E07300,0x34807400,0x34207500,0x35C07600,0x35607700,
                0x33007800,0x33A07900,0x32407A00,0x32E07B00,0x31807C00,0x31207D00,0x30C07E00,0x30607F00,
                0x50008000,0x50A08100,0x51408200,0x51E08300,0x52808400,0x52208500,0x53C08600,0x53608700,
                0x55008800,0x55A08900,0x54408A00,0x54E08B00,0x57808C00,0x57208D00,0x56C08E00,0x56608F00,
                0x5A009000,0x5AA09100,0x5B409200,0x5BE09300,0x58809400,0x58209500,0x59C09600,0x59609700,
                0x5F009800,0x5FA09900,0x5E409A00,0x5EE09B00,0x5D809C00,0x5D209D00,0x5CC09E00,0x5C609F00,
                0x4400A000,0x44A0A100,0x4540A200,0x45E0A300,0x4680A400,0x4620A500,0x47C0A600,0x4760A700,
                0x4100A800,0x41A0A900,0x4040AA00,0x40E0AB00,0x4380AC00,0x4320AD00,0x42C0AE00,0x4260AF00,
                0x4E00B000,0x4EA0B100,0x4F40B200,0x4FE0B300,0x4C80B400,0x4C20B500,0x4DC0B600,0x4D60B700,
                0x4B00B800,0x4BA0B900,0x4A40BA00,0x4AE0BB00,0x4980BC00,0x4920BD00,0x48C0BE00,0x4860BF00,
                0x7800C000,0x78A0C100,0x7940C200,0x79E0C300,0x7A80C400,0x7A20C500,0x7BC0C600,0x7B60C700,
                0x7D00C800,0x7DA0C900,0x7C40CA00,0x7CE0CB00,0x7F80CC00,0x7F20CD00,0x7EC0CE00,0x7E60CF00,
                0x7200D000,0x72A0D100,0x7340D200,0x73E0D300,0x7080D400,0x7020D500,0x71C0D600,0x7160D700,
                0x7700D800,0x77A0D900,0x7640DA00,0x76E0DB00,0x7580DC00,0x7520DD00,0x74C0DE00,0x7460DF00,
                0x6C00E000,0x6CA0E100,0x6D40E200,0x6DE0E300,0x6E80E400,0x6E20E500,0x6FC0E600,0x6F60E700,
                0x6900E800,0x69A0E900,0x6840EA00,0x68E0EB00,0x6B80EC00,0x6B20ED00,0x6AC0EE00,0x6A60EF00,
                0x6600F000,0x66A0F100,0x6740F200,0x67E0F300,0x6480F400,0x6420F500,0x65C0F600,0x6560F700,
                0x6300F800,0x63A0F900,0x6240FA00,0x62E0FB00,0x6180FC00,0x6120FD00,0x60C0FE00,0x6060FF00};

        public static string FLNameToFile(string idstr)
        {
            byte[] accid = UnicodeEncoding.Unicode.GetBytes(idstr.ToLowerInvariant());
            uint hash = 0;
            for (int pos = 0; pos < accid.Length; pos++)
            {
                hash = crctbl[(hash & 0xFF) ^ (uint)accid[pos]] ^ (hash >> 8);
            }
            uint result = (hash & 0xFF000000) >> 24 | (hash & 0x00FF0000) >> 8 | (hash & 0x0000FF00) << 8 | (hash & 0x000000FF) << 24;
            return String.Format("{0:x2}-{1:x8}", idstr.Length, result);
        }

        /// <summary>
        /// Scan a player's account directory and update the data in the database.
        /// </summary>
        /// <param name="dataSet">The database to update. This should contain both the CharacterList and BanList</param>
        /// <param name="log">The log interface.</param>
        /// <param name="log">The game data.</param>
        /// <param name="accDirPath">The account directory path to scan.</param>
        public static int ScanCharAccount(DamDataSet dataSet, LogRecorderInterface log, FLGameData gameData, string accDir)
        {
            int filesUpdated = 0;
            string accDirPath = AppSettings.Default.setAccountDir + "\\" + accDir;

            if (!Directory.Exists(accDirPath) || !File.Exists(accDirPath + "\\name"))
                return 0;

            // Check and update the account ban information. Remove bans for deleted accounts
            // and automatically create bans if they don't have any other information.
            bool banned = File.Exists(accDirPath + Path.DirectorySeparatorChar + "banned");
            if (banned)
            {
                DateTime fileCreationTime = File.GetCreationTime(accDirPath + Path.DirectorySeparatorChar + "banned");
                DamDataSet.BanListRow banRecord = dataSet.BanList.FindByAccDir(accDir);

                if (banRecord != null && !banned)
                {
                    banRecord.Delete();
                    filesUpdated++;
                }
                else if(banRecord == null)
                {
                    string banInfo = File.ReadAllText(accDirPath + Path.DirectorySeparatorChar + "banned");

                    banRecord = dataSet.BanList.NewBanListRow();
                    banRecord.AccDir = accDir;
                    banRecord.AccID = GetAccountID(accDirPath);
                    banRecord.BanReason = (banInfo.Length > 0) ? banInfo : "UNKNOWN";
                    banRecord.BanStart = File.GetCreationTime(accDirPath + Path.DirectorySeparatorChar + "banned");
                    banRecord.BanEnd = DateTime.Now.AddDays(1000).ToUniversalTime();
                    dataSet.BanList.AddBanListRow(banRecord);
                    filesUpdated++;
                }
            }

            try
            {
                if (LOGIN_ID_FILES == null)
                    LOGIN_ID_FILES = PropertiesWindow.LoadLoginIdSettings();

                foreach (var rawFile in LOGIN_ID_FILES)
                {
                    string[] loginFiles = Directory.GetFiles(accDirPath, rawFile.Key);
                    foreach (var file in loginFiles)
                    {
                        var ini = new FLDataFile(file, false);
                        var accessTime = File.GetLastWriteTime(file);
                        var result = new StringBuilder(100);
                        foreach (var kvp in rawFile.Value)
                        {
                            string loginID;
                            string sec = kvp.Key;
                            string key = kvp.Value;
                            if (sec == "")
                                sec = "ROOT";

                            loginID = ini.GetSetting(sec, kvp.Value).Str(0);
                            result.Append(key + "=" + loginID.Trim() + " ");
                        }
                        result.Remove(result.Length - 1, 1);
                        dataSet.LoginIDList.AddLoginIDListRow(accDir, result.ToString(), accessTime);
                    }
                }
            }
            catch (Exception ex)
            {
                log.AddLog(String.Format("Error '{0}' when reading hash info {1}", ex.Message, accDirPath));
            }

            // Check and update login id information.
            try
            {
                string[] loginFiles = Directory.GetFiles(accDirPath, "login_*.ini");
                foreach (string loginFilePath in loginFiles)
                {
                    // format: time=1347057642 id=3A44AC9A ip=13.33.33.37 id2=2A7A4F74
                    string content = File.ReadAllText(loginFilePath);
                    DateTime accessTime;
                    string loginID = "";
                    string loginID2 = "";
                    string ip = "";

                    string[] values = content.Split(new char[]{'\t',' '});
                    foreach (var raw in values)
                    {
                        string[] parts = raw.Split('=');
                        string key = parts[0];
                        string value = parts[1];

                        if (key == "id")
                            loginID += value.Trim();
                        else if (key == "id2")
                            loginID2 += value.Trim();
                        else if (key == "ip")
                            ip += value.Trim();
                    }

                    accessTime = File.GetLastWriteTime(loginFilePath);
                    dataSet.LoginIDList.AddLoginIDListRow(accDir, loginID, accessTime);
                    dataSet.LoginIDList.AddLoginIDListRow(accDir, loginID2, accessTime);
                    dataSet.IPList.AddIPListRow(accDir, ip, accessTime);
                }
            }
            catch (Exception ex)
            {
                log.AddLog(String.Format("Error '{0}' when reading login info {1}", ex.Message, accDirPath));
            }

            // Check for new/updated charfiles
            string[] charFiles = Directory.GetFiles(accDirPath, "??-????????.fl");
            foreach (string iCharFilePath in charFiles)
            {
                try
                {
                    string charFilePath = iCharFilePath;

                    string charPath = charFilePath.Substring(AppSettings.Default.setAccountDir.Length + 1);
                    DateTime lastUpdate = File.GetLastWriteTime(charFilePath);

                    DamDataSet.CharacterListRow charRecord = dataSet.CharacterList.FindByCharPath(charPath);
                    if (charRecord != null)
                    {
                        // Check that the character name matches the character filename otherwise fall through
                        if (FLNameToFile(charRecord.CharName) == Path.GetFileNameWithoutExtension(charFilePath))
                        {
                            // If this character was deleted and is not anymore then update the record
                            // and reset the last online time.
                            if (charRecord.IsDeleted)
                            {
                                charRecord.IsDeleted = false;
                                charRecord.LastOnLine = DateTime.Now;
                            }
                            // If this record exists in the database and it has not changed since the last time
                            // we read it then don't read it again.
                            else if (AppSettings.Default.setCheckChangedOnly)
                            {
                                if (lastUpdate == charRecord.Updated)
                                    continue;
                            }
                        }
                    }

                    // Load the charfile and decode it and get the charname
                    FLDataFile cfp = new FLDataFile(charFilePath, true);

                    // Find the char name in the ini file and ecode the name entry from ascii 
                    // hex into unicode
                    string name = " UNKNOWN NAME";
                    try
                    {
                        name = cfp.GetSetting("Player", "name").UniStr(0);
                    }
                    catch (Exception ex)
                    {
                        log.AddLog(String.Format("Error '{0}' when decoding name when reading charfile {1} using {2}", ex, charFilePath, name));
                    }

                    // Check that the character name matches the character filename and if it
                    // doesn't then rename the file and recheck.
                    string expectedCharFileName = FLNameToFile(name);
                    if (expectedCharFileName != Path.GetFileNameWithoutExtension(charFilePath))
                    {
                        log.AddLog(String.Format("Error 'path/name is corrupt' for {0} should be {1}", Path.GetFileNameWithoutExtension(charFilePath), expectedCharFileName));
                        
                        string oldCharFilePath = charFilePath;
                        charFilePath = accDirPath + "\\" + FLNameToFile(name) + ".fl";

                        File.Delete(charFilePath);
                        File.Move(oldCharFilePath, charFilePath);
                        charRecord = dataSet.CharacterList.FindByCharPath(charFilePath);
                        cfp = new FLDataFile(charFilePath, true);
                    }

                    // If the file doesn't exist in our database then create a new entry.
                    if (charRecord == null)
                    {
                        charRecord = dataSet.CharacterList.NewCharacterListRow();
                        charRecord.Created = DateTime.Now;
                        charRecord.CharName = name;
                        charRecord.CharPath = charFilePath.Substring(AppSettings.Default.setAccountDir.Length + 1);
                        charRecord.AccID = GetAccountID(accDirPath);
                        charRecord.AccDir = accDir;
                        charRecord.Updated = lastUpdate;
                        charRecord.IsDeleted = false;
                        charRecord.Location = GetLocation(gameData, cfp);
                        charRecord.Money = (int)cfp.GetSetting("Player", "money").UInt(0);
                        charRecord.Rank = (int)cfp.GetSetting("Player", "rank").UInt(0);
                        long shipHash = 0;
                        charRecord.Ship = FLUtility.GetShip(gameData, cfp, out shipHash);
                        charRecord.OnLineSecs = (int)GetOnLineTime(cfp);
                        charRecord.LastOnLine = GetTimeStamp(cfp);
                        dataSet.CharacterList.AddCharacterListRow(charRecord);
                    }
                    // Otherwise just update it.
                    else
                    {
                        charRecord.Updated = lastUpdate;
                        charRecord.IsDeleted = false;
                        charRecord.Location = GetLocation(gameData, cfp);
                        charRecord.Money = (int)cfp.GetSetting("Player", "money").UInt(0);
                        charRecord.Rank = (int)cfp.GetSetting("Player", "rank").UInt(0);
                        long shipHash = 0;
                        charRecord.Ship = FLUtility.GetShip(gameData, cfp, out shipHash);
                        charRecord.OnLineSecs = (int)GetOnLineTime(cfp);
                        charRecord.LastOnLine = GetTimeStamp(cfp);
                    }
                    filesUpdated++;

                    bool isAdmin = File.Exists(accDirPath + Path.DirectorySeparatorChar + "flhookadmin.ini");
                    CheckCharFile(dataSet, log, gameData, cfp, isAdmin, AppSettings.Default.setAutomaticFixErrors);
                }
                catch (FLDataFileException ex)
                {
                    log.AddLog(String.Format("Error '{0}' charfile corrupt {1}", ex.Message, iCharFilePath));
                    if (AppSettings.Default.setAutomaticCharClean)
                    {
                        try { File.Delete(iCharFilePath); }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    log.AddLog(String.Format("Error '{0}' when reading charfile {1}", ex.Message, iCharFilePath));
                }
            }

            return filesUpdated;
        }

        /// <summary>
        /// Find any characters that should be in this account and check that the characters
        /// or the entire account is not deleted. If they are deleted then update the database
        /// to indicate that these characters are deleted.
        /// </summary>
        /// <remarks>This is slow...use with care.</remarks>
        /// <returns>Number of files deleted</returns>
        public static int CheckForDeletedChars(DamDataSet dataSet, string accDir)
        {
            int filesUpdated = 0;
            string query = "(AccDir = '" + EscapeEqualsExpressionString(accDir) + "')";
            DamDataSet.CharacterListRow[] charRecords = (DamDataSet.CharacterListRow[])dataSet.CharacterList.Select(query);
            foreach (DamDataSet.CharacterListRow charRecord in charRecords)
            {
                string charFilePath = AppSettings.Default.setAccountDir + "\\" + charRecord.CharPath;
                if (!File.Exists(charFilePath))
                {
                    charRecord.IsDeleted = true;
                    filesUpdated++;
                }
            }
            return filesUpdated;
        }

        /// <summary>
        /// Remove old files from the account. If the account has no valid character files then the account
        /// directory is deleted too.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="log"></param>
        /// <param name="gameData"></param>
        /// <param name="accDir"></param>
        /// <returns></returns>
        private static int CleanCharAccount(DamDataSet dataSet, LogRecorderInterface log, FLGameData gameData, string accDir)
        {
            int filesDeleted = 0;
            string accDirPath = AppSettings.Default.setAccountDir + "\\" + accDir;

            List<string> charFiles = new List<string>(Directory.GetFiles(accDirPath, "??-????????.fl"));

            // Remove files that are too small
            foreach (string charFilePath in charFiles)
            {
                try
                {
                    if (new FileInfo(charFilePath).Length < 1000)
                    {
                        File.Delete(charFilePath);
                        filesDeleted++;
                    }
                }
                catch { }
            }

            // Remove old flhook renameme plugin files
            try
            {
                if (File.Exists(accDirPath + "\\rename.txt"))
                {
                    File.Delete(accDirPath + "\\rename.txt");
                    filesDeleted++;
                }
            }
            catch { }

            // Remove unused movechar plugin files
            foreach (string filePath in Directory.GetFiles(accDirPath, "??-????????-movechar.ini"))
            {
                string origCharFilePath = filePath.Remove(filePath.Length - 13) + ".fl";
                if (charFiles.Find(delegate(string charFilePath) { return origCharFilePath == charFilePath; }) == null)
                {
                    try
                    {
                        File.Delete(filePath);
                        filesDeleted++;
                    }
                    catch { }
                }
            }

            // Remove unused message plugin files.
            foreach (string filePath in Directory.GetFiles(accDirPath, "??-????????messages.ini"))
            {
                string origCharFilePath = filePath.Remove(filePath.Length - 12) + ".fl";
                if (charFiles.Find(delegate(string charFilePath) { return origCharFilePath == charFilePath; }) == null)
                {
                    try
                    {
                        File.Delete(filePath);
                        filesDeleted++;
                    }
                    catch { }
                }
            }

            // Remove unused message plugin mail files
            foreach (string filePath in Directory.GetFiles(accDirPath, "??-????????-mail.ini"))
            {
                string origCharFilePath = filePath.Remove(filePath.Length - 9) + ".fl";
                if (charFiles.Find(delegate(string charFilePath) { return origCharFilePath == charFilePath; }) == null)
                {
                    try
                    {
                        File.Delete(filePath);
                        filesDeleted++;
                    }
                    catch { }
                }
            }

            // Remove unused givecash plugin files
            foreach (string filePath in Directory.GetFiles(accDirPath, "??-????????-givecashlog.txt"))
            {
                string origCharFilePath = filePath.Remove(filePath.Length - 16) + ".fl";
                if (charFiles.Find(delegate(string charFilePath) { return origCharFilePath == charFilePath; }) == null)
                {
                    try
                    {
                        File.Delete(filePath);
                        filesDeleted++;
                    }
                    catch { }
                }
            }

            // Remove unused givecash plugin files
            foreach (string filePath in Directory.GetFiles(accDirPath, "??-????????-givecash.ini"))
            {
                string origCharFilePath = filePath.Remove(filePath.Length - 13) + ".fl";
                if (charFiles.Find(delegate(string charFilePath) { return origCharFilePath == charFilePath; }) == null)
                {
                    try
                    {
                        File.Delete(filePath);
                        filesDeleted++;
                    }
                    catch { }
                }
            }

            // Remove old corrupt files generated by the previous version of dam
            foreach (string filePath in Directory.GetFiles(accDirPath, "*.fl.corrupt"))
            {
                try
                {
                    File.Delete(filePath);
                    filesDeleted++;
                }
                catch { }
            }

            // Remove account directory if no char files left and the account 
            // is not banned
            if (Directory.GetFiles(accDirPath, "??-????????.fl").Length == 0
                && !File.Exists(accDirPath + "\\banned"))
            {
                try
                {
                    Directory.Delete(accDirPath, true);
                    filesDeleted++;
                }
                catch { }
            }

            return filesDeleted;
        }

        private static bool CheckEquipment(DamDataSet dataSet, LogRecorderInterface log, FLGameData gameData,
            FLDataFile charFile, bool admin, string setting, bool fixErrors)
        {
            bool foundErrors = false;

            uint shipHash = 0;
            if (UInt32.TryParse(charFile.GetSetting("Player", "ship_archetype").Str(0), out shipHash))
            {
                shipHash = charFile.GetSetting("Player", "ship_archetype").UInt(0);

                GameDataSet.ShipInfoListRow shipInfo = gameData.GetShipInfo(shipHash);
                GameDataSet.HardPointListRow[] hardPoints = gameData.GetHardPointListByShip(shipHash);
                if (shipInfo == null)
                {
                    log.AddLog("Error in charfile '" + charFile.filePath + "' ship " + shipHash + " does not exist in database.");
                    return true;
                }

                // Check for duplicate "internal" hardpoints or hardpoints with wrong default equipment. 
                // There may be only one engine, powerplant, scanner, tractor, sound
                bool foundEngine = false;
                bool foundPowerGen = false;
                bool foundScanner = false;
                bool foundTractor = false;
                bool foundSound = false;
                foreach (FLDataFile.Setting set in charFile.GetSettings("Player", setting))
                {
                    try
                    {
                        uint equipHash = set.UInt(0);

                        GameDataSet.HashListRow equipItem = gameData.GetItemByHash(equipHash);
                        if (equipItem == null)
                        {
                            log.AddLog("Error in charfile '" + charFile.filePath + "' unknown hash at line " + set.desc);
                            foundErrors = true;
                            if (fixErrors) { charFile.DeleteSetting(set); }
                            continue;
                        }

                        if (equipItem.ItemType == FLGameData.GAMEDATA_ENGINES)
                        {
                            if (foundEngine)
                            {
                                log.AddLog("Error in charfile '" + charFile.filePath + "' engine already present at line " + set.desc);
                                foundErrors = true;
                                if (fixErrors) { charFile.DeleteSetting(set); }
                            }
                            else if (AppSettings.Default.setCheckDefaultEngine && shipInfo.DefaultEngine != equipHash)
                            {
                                if (!admin)
                                {
                                    log.AddLog("Error in charfile '" + charFile.filePath + "' invalid engine at line " + set.desc + " should be " + shipInfo.DefaultEngine);
                                    foundErrors = true;
                                    if (fixErrors) { set.values[0] = shipInfo.DefaultEngine; }
                                }
                            }
                            foundEngine = true;
                        }
                       else if (equipItem.ItemType == FLGameData.GAMEDATA_POWERGEN)
                        {/*
                          if (foundPowerGen)
                        {
                             log.AddLog("Error in charfile '" + charFile.filePath + "' powergen already present at line " + set.desc + " should be " + shipInfo.DefaultPowerPlant);
                                foundErrors = true;
                                if (fixErrors) { charFile.DeleteSetting(set); }
                            }
                            else if (AppSettings.Default.setCheckDefaultEngine && shipInfo.DefaultPowerPlant != equipHash)
                            {
                                if (!admin)
                                {
                                    log.AddLog("Error in charfile '" + charFile.filePath + "' invalid powergen at line " + set.desc + " should be " + shipInfo.DefaultPowerPlant);
                                    foundErrors = true;
                                    if (fixErrors) { set.values[0] = shipInfo.DefaultPowerPlant; }
                                }
                            }*/
                            foundPowerGen = true;
                        }
                        else if (equipItem.ItemType == FLGameData.GAMEDATA_SCANNERS)
                        {
                            if (foundScanner)
                            {
                                log.AddLog("Error in charfile '" + charFile.filePath + "' scanner already present at line" + set.desc);
                                foundErrors = true;
                                if (fixErrors) { charFile.DeleteSetting(set); }
                            }
                            foundScanner = true;
                        }
                        else if (equipItem.ItemType == FLGameData.GAMEDATA_TRACTORS)
                        {
                            if (foundTractor)
                            {
                                log.AddLog("Error in charfile '" + charFile.filePath + "' tractor already present at line" + set.desc);
                                foundErrors = true;
                                if (fixErrors) { charFile.DeleteSetting(set); }
                            }
                            foundTractor = true;
                        }
                        else if (equipItem.ItemType == FLGameData.GAMEDATA_SOUND)
                        {
                            if (foundSound)
                            {
                                log.AddLog("Error in charfile '" + charFile.filePath + "' sound already present at line" + set.desc);
                                foundErrors = true;
                                if (fixErrors) { charFile.DeleteSetting(set); }
                            }
                            else if (shipInfo.DefaultSound != 0 && shipInfo.DefaultSound != equipHash)
                            {
                                log.AddLog("Error in charfile '" + charFile.filePath + "' invalid sound at line " + set.desc + " should be " + shipInfo.DefaultSound);
                                foundErrors = true;
                                if (fixErrors) { set.values[0] = shipInfo.DefaultSound; }
                            }
                            foundSound = true;
                        }
                    }
                    catch (Exception)
                    {
                        foundErrors = true;
                        log.AddLog("Exception in charfile '" + charFile.filePath + "' at line " + set.desc);
                        if (fixErrors) { charFile.DeleteSetting(set); }
                    }
                }


                // Create missing critical internal hardpoints.
                if (AppSettings.Default.setCheckDefaultEngine && !foundEngine && shipInfo.DefaultEngine != 0)
                {
                    foundErrors = true;
                    log.AddLog("Error in charfile '" + charFile.filePath + "' missing engine");
                    if (fixErrors)
                    {
                        charFile.AddSettingNotUnique("player", setting, new object[] { shipInfo.DefaultEngine, "", 1 });
                    }
                }
                if (AppSettings.Default.setCheckDefaultPowerPlant && !foundPowerGen && shipInfo.DefaultPowerPlant != 0)
                {
                    foundErrors = true;
                    log.AddLog("Error in charfile '" + charFile.filePath + "' incorrect powerplant");
                    if (fixErrors)
                    {
                        charFile.AddSettingNotUnique("player", setting, new object[] { shipInfo.DefaultPowerPlant, "", 1 });
                    }
                }
                if (!foundSound && shipInfo.DefaultSound != 0)
                {
                    foundErrors = true;
                    log.AddLog("Error in charfile '" + charFile.filePath + "' incorrect sound");
                    if (fixErrors)
                    {
                        charFile.AddSettingNotUnique("player", setting, new object[] { shipInfo.DefaultSound, "", 1 });
                    }
                }

                // Check for duplicate or invalid external hardpoints by making sure 
                // only hardpoints from the shipinfo exist and no more than one each.
                List<string> existingHardpoints = new List<string>();
                foreach (FLDataFile.Setting set in charFile.GetSettings("player", setting))
                {
                    try
                    {
                        if (set.NumValues() != 3)
                        {
                            log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                            foundErrors = true;
                            if (fixErrors) { charFile.DeleteSetting(set); }
                            continue;
                        }

                        uint equipHash = set.UInt(0);
                        string hardpoint = set.Str(1).ToLowerInvariant();
                        if (hardpoint.Trim().Length == 0)
                            continue;


                        // If the hardpoint already exists then this is an error.
                        if (existingHardpoints.Find(delegate(string value) { return hardpoint == value; }) != null)
                        {
                            log.AddLog("Error in charfile '" + charFile.filePath + "' dup hp '" + set.desc + "'");
                            foundErrors = true;
                            if (fixErrors) charFile.DeleteSetting(set);
                            continue;
                        }
                        else
                        {
                            existingHardpoints.Add(hardpoint);
                        }

                        // If the hardpoint shouldn't exist then delete it.
                        GameDataSet.HardPointListRow hardPointInfo = Array.Find(hardPoints, delegate(GameDataSet.HardPointListRow value) { return hardpoint == value.HPName.ToLowerInvariant(); });
                        if (hardPoints == null || hardPointInfo == null)
                        {
                            log.AddLog("Error in charfile '" + charFile.filePath + "' invalid hp '" + set.desc + "'");
                            foundErrors = true;
                            if (fixErrors)
                            {
                                if (setting == "base_equip")
                                    charFile.AddSettingNotUnique("Player", "cargo", new object[] { equipHash, 1, "", "", 0 });
                                charFile.DeleteSetting(set);
                            }
                            continue;
                        }

                        // If the hardpoint has illegal equipment on it then unmount equipment
                        GameDataSet.EquipInfoListRow equipItem = gameData.GetEquipInfo(equipHash);
                        if (equipItem != null && !hardPointInfo.MountableTypes.Contains(equipItem.MountableType))
                        {
                            if (!admin && equipItem.ItemType != FLGameData.GAMEDATA_SHIELDS && equipItem.ItemType != FLGameData.GAMEDATA_GUNS && equipItem.ItemType != FLGameData.GAMEDATA_TURRETS)
                            {
                                log.AddLog("Error in charfile '" + charFile.filePath + "' invalid equip on hp'" + set.desc + "'");
                                foundErrors = true;
                                if (fixErrors)
                                {
                                    if (setting == "base_equip" && equipItem.ItemType == FLGameData.GAMEDATA_GUNS)
                                        charFile.AddSettingNotUnique("Player", "cargo", new object[] { equipHash, 1, "", "", 0 });
                                    charFile.DeleteSetting(set);
                                }
                            }
                            continue;
                        }

                        // If this is a light/fx and it doesn't have the default equipment
                        // then fix it.
                        if (!admin && AppSettings.Default.setCheckDefaultLights)
                        {
                            GameDataSet.HashListRow defaultItem = gameData.GetItemByHash(hardPointInfo.DefaultItemHash);
                            if (defaultItem != null)
                            {
                                if (defaultItem.ItemType == FLGameData.GAMEDATA_LIGHTS
                                    || defaultItem.ItemType == FLGameData.GAMEDATA_FX
									|| defaultItem.ItemType == FLGameData.GAMEDATA_POWERGEN
									|| defaultItem.ItemType == FLGameData.GAMEDATA_SHIELDS
									|| defaultItem.ItemType == FLGameData.GAMEDATA_THRUSTERS)
                                {
                                    if (equipHash != hardPointInfo.DefaultItemHash)
                                    {
                                        log.AddLog("Error in charfile '" + charFile.filePath + "' invalid equip on light or fx hp'" + set.desc + "'");
                                        foundErrors = true;
                                        if (!admin)
                                        {

                                            if (fixErrors)
                                            {
                                                charFile.DeleteSetting(set);
                                                charFile.AddSettingNotUnique("player", setting, new object[] { hardPointInfo.DefaultItemHash, hardPointInfo.HPName, 1 });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        foundErrors = true;
                        log.AddLog("Exception in charfile '" + charFile.filePath + "' at line " + set.desc);
                        if (fixErrors) { charFile.DeleteSetting(set); }
                    }
                }
            }

            return foundErrors;
        }

        /// <summary>
        /// Check that the character file is valid.
        /// </summary>
        /// <returns>True if errors are detected</returns>
        public static bool CheckCharFile(DamDataSet dataSet, LogRecorderInterface log, FLGameData gameData, FLDataFile charFile, bool admin, bool fixErrors)
        {
            bool foundErrors = false;

            string charFilePath = charFile.filePath;
            if (File.Exists(charFilePath))
            {
                string name = charFile.GetSetting("Player", "name").UniStr(0);
                if (FLNameToFile(name) != Path.GetFileNameWithoutExtension(charFilePath))
                {
                    log.AddLog(String.Format("Error 'charfile name is corrupt' for {0}", charFilePath));
                    string oldCharFilePath = charFilePath;
                    charFilePath = Path.GetDirectoryName(charFilePath) + "\\" + FLNameToFile(name) + ".fl";
                    if (fixErrors)
                    {
                        File.Delete(charFilePath);
                        File.Move(oldCharFilePath, charFilePath);
                    }
                    charFile = new FLDataFile(charFilePath, true);
                }
            }

            // Check the hashcodes.
            foreach (FLDataFile.Setting set in charFile.GetSettings("Player"))
            {
                try
                {
                    if (set.settingName == "house")
                    {
                        string nick = set.Str(1);
                        if (gameData.GetItemDescByFactionNickName(nick) == null)
                        {
                            foundErrors = true;
                            if (fixErrors) charFile.DeleteSetting(set);
                            log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                        }
                    }
                    else if (set.settingName == "ship_archetype"
                        || set.settingName == "com_lefthand"
                        || set.settingName == "voice"
                        || set.settingName == "com_body"
                        || set.settingName == "com_head"
                        || set.settingName == "com_lefthand"
                        || set.settingName == "com_righthand"
                        || set.settingName == "body"
                        || set.settingName == "head"
                        || set.settingName == "lefthand"
                        || set.settingName == "righthand"
                        || set.settingName == "ship_archetype"
                        || set.settingName == "location"
                        || set.settingName == "base"
                        || set.settingName == "last_base"
                        || set.settingName == "system"
                        || set.settingName == "rep_group"
                        || set.settingName == "costume"
                        || set.settingName == "com_costume")
                    {
                        uint hash = 0;
                        if (UInt32.TryParse(set.Str(0), out hash))
                        {                           
                            if (gameData.GetItemByHash(hash) != null)
                                continue;
                        }

                        string nick = set.Str(0);
                        if (gameData.GetItemByNickName(nick) != null)
                            continue;

                        foundErrors = true;
                        if (fixErrors) charFile.DeleteSetting(set);
                        log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                    }
                    else if (set.settingName == "cargo" || set.settingName == "base_cargo")
                    {
                        uint hash = set.UInt(0);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            foundErrors = true;
                            if (fixErrors) charFile.DeleteSetting(set);
                            log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                        }
                        // TODO: check for valid HP
                    }
                    else if (set.settingName == "equip" || set.settingName == "base_equip")
                    {
                        // Check for valid hash
                        uint hash = set.UInt(0);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            foundErrors = true;
                            if (fixErrors) charFile.DeleteSetting(set);
                            log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                        }
                    }
                    else if (set.settingName == "wg")
                    {
                        // TODO: check for valid HP
                    }
                    else if (set.settingName == "visit")
                    {
                        uint hash = set.UInt(0);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            if (fixErrors) charFile.DeleteSetting(set);
                            if (hash != 0 && hash != 4294967295)
                            {
                                if (AppSettings.Default.setReportVisitErrors)
                                {
                                    foundErrors = true;
                                    log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                                }
                            }
                        }
                    }
                    else if (set.settingName == "description"
                        || set.settingName == "tstamp"
                        || set.settingName == "name"
                        || set.settingName == "rank"
                        || set.settingName == "money"
                        || set.settingName == "num_kills"
                        || set.settingName == "num_misn_successes"
                        || set.settingName == "num_misn_failures"
                        || set.settingName == "base_hull_status"
                        || set.settingName == "base_collision_group"
                        || set.settingName == "num_misn_failures"
                        || set.settingName == "interface"
                        || set.settingName == "pos"
                        || set.settingName == "rotate"
                        || set.settingName == "hull_status"
                        || set.settingName == "collision_group"
                        )
                    {
                        // ignore
                    }
                    else
                    {
                        log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                        if (fixErrors) charFile.DeleteSetting(set);
                        foundErrors = true;
                    }
                }
                catch (Exception e)
                {
                    log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "' " + e.Message);
                    if (fixErrors) charFile.DeleteSetting(set);
                    foundErrors = true;
                }

            }

            foreach (FLDataFile.Setting set in charFile.GetSettings("mPlayer"))
            {
                try
                {
                    if (set.settingName == "locked_gate"
                        || set.settingName == "ship_type_killed"
                        || set.settingName == "sys_visited"
                        || set.settingName == "base_visited"
                        || set.settingName == "holes_visited")
                    {
                        uint hash = set.UInt(0);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            foundErrors = true;
                            if (fixErrors) charFile.DeleteSetting(set);
                            log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                        }
                    }
                    else if (set.settingName == "vnpc")
                    {
                        uint hash = set.UInt(0);
                        uint hash2 = set.UInt(1);
                        if (gameData.GetItemByHash(hash) == null || gameData.GetItemByHash(hash2) == null)
                        {
                            foundErrors = true;
                            if (fixErrors) charFile.DeleteSetting(set);
                            log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                        }
                    }
                    else if (set.settingName == "can_dock"
                        || set.settingName == "can_tl"
                        || set.settingName == "total_cash_earned"
                        || set.settingName == "total_time_played"
                        
                        || set.settingName == "rm_completed"
                        || set.settingName == "rm_aborted"
                        || set.settingName == "rm_failed"
                        || set.settingName == "rumor")
                    
                    {
                        // ignore
                        // note rumor is the IDSNumber of the rumor as defined in the mBases.ini
                    }
                    else
                    {
                        log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "'");
                        if (fixErrors) charFile.DeleteSetting(set);
                        foundErrors = true;
                    }
                }
                catch (Exception e)
                {
                    log.AddLog("Error in charfile '" + charFile.filePath + "' invalid line '" + set.desc + "' " + e.Message);
                    if (fixErrors) charFile.DeleteSetting(set);
                    foundErrors = true;
                }
            }

            foundErrors |= CheckEquipment(dataSet, log, gameData, charFile, admin, "base_equip", fixErrors);
            foundErrors |= CheckEquipment(dataSet, log, gameData, charFile, admin, "equip", fixErrors);

            if (foundErrors && fixErrors)
            {
                charFile.SaveSettings(charFile.filePath, false);
            }

            return foundErrors;
        }


        /// <summary>
        /// Add hashcodes as comments to the file.
        /// </summary>
        /// <returns>True if errors are detected</returns>
        public static string PrettyPrintCharFile(FLGameData gameData, FLDataFile charFile)
        {
            StringBuilder sb = new StringBuilder();
          
            sb.AppendLine("[Player]");
            foreach (FLDataFile.Setting set in charFile.GetSettings("Player"))
            {
                StringBuilder sbLine = new StringBuilder();
                try
                {
                    sbLine.Append(set.settingName);
                    sbLine.Append(" = ");
                    for (int i=0; i<set.values.Length; i++)
                    {
                        if (i==0)
                            sbLine.Append(set.values[i].ToString().Trim());
                        else
                            sbLine.AppendFormat(", {0}", set.values[i].ToString().Trim());
                    }

                    int tabs = (sbLine.Length >= (6 * 7)) ? 0 : 6 - sbLine.Length / 7;
                    sbLine.Append('\t', tabs);
                    
                    if (set.settingName == "house")
                    {
                        string nick = set.Str(1);
                        if (gameData.GetItemDescByFactionNickName(nick) == null)
                        {
                            sbLine.Append(";* err code");
                        }
                    }
                    else if (set.settingName == "ship_archetype"
                        || set.settingName == "com_lefthand"
                        || set.settingName == "voice"
                        || set.settingName == "com_body"
                        || set.settingName == "com_head"
                        || set.settingName == "com_lefthand"
                        || set.settingName == "com_righthand"
                        || set.settingName == "body"
                        || set.settingName == "head"
                        || set.settingName == "lefthand"
                        || set.settingName == "righthand"
                        || set.settingName == "ship_archetype"
                        || set.settingName == "location"
                        || set.settingName == "base"
                        || set.settingName == "last_base"
                        || set.settingName == "system"
                        || set.settingName == "rep_group"
                        || set.settingName == "costume"
                        || set.settingName == "com_costume")
                    {
                        uint hash = 0;
                        if (UInt32.TryParse(set.Str(0), out hash))
                        {                           
                            GameDataSet.HashListRow item = gameData.GetItemByHash(hash);
                            if (item != null)
                                sbLine.AppendFormat("; {0}", item.ItemNickName);
                            else
                                sbLine.Append(";* err code");
                        }
                        else
                        {
                            GameDataSet.HashListRow item = gameData.GetItemByNickName(set.Str(0));
                            if (item != null)
                                sbLine.AppendFormat("; {0}", item.ItemHash);
                            else
                                sbLine.Append(";* err code");
                        }
                    }
                    else if (set.settingName == "cargo" || set.settingName == "base_cargo"
                        || set.settingName == "equip" || set.settingName == "base_equip")
                    {
                        uint hash = set.UInt(0);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            sbLine.Append(";* err code");
                        }
                        else
                        {
                            sbLine.AppendFormat("; {0}", gameData.GetItemByHash(hash).ItemNickName);
                        }
                    }
                    else if (set.settingName == "wg")
                    {
                        // TODO: check for valid HP
                    }
                    else if (set.settingName == "visit")
                    {
                        uint hash = set.UInt(0);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            sbLine.Append(";* err code");

                        }
                        else
                        {
                            sbLine.AppendFormat("; {0}", gameData.GetItemByHash(hash).ItemNickName);
                        }
                    }
                    else if (set.settingName == "description"
                        || set.settingName == "tstamp"
                        || set.settingName == "name"
                        || set.settingName == "rank"
                        || set.settingName == "money"
                        || set.settingName == "num_kills"
                        || set.settingName == "num_misn_successes"
                        || set.settingName == "num_misn_failures"
                        || set.settingName == "base_hull_status"
                        || set.settingName == "base_collision_group"
                        || set.settingName == "num_misn_failures"
                        || set.settingName == "interface"
                        || set.settingName == "pos"
                        || set.settingName == "rotation"
                        || set.settingName == "hull_status"
                        || set.settingName == "collision_group"
                        )
                    {
                        // ignore
                    }
                    else
                    {
                        sbLine.Append(";* err unknown key");
                    }
                }
                catch (Exception e)
                {
                    sbLine.Append(";* exception " + e.Message);
                }

                sb.AppendLine(sbLine.ToString().Trim());
            }

            sb.AppendLine();
            sb.AppendLine("[mPlayer]");
            foreach (FLDataFile.Setting set in charFile.GetSettings("mPlayer"))
            {
                StringBuilder sbLine = new StringBuilder();
                try
                {
                    sbLine.Append(set.settingName);
                    sbLine.Append(" = ");
                    for (int i = 0; i < set.values.Length; i++)
                    {
                        if (i == 0)
                            sbLine.Append(set.values[i]);
                        else
                            sbLine.AppendFormat(", {0}", set.values[i]);
                    }

                    int tabs = (sbLine.Length >= (6 * 7)) ? 0 : 6 - sbLine.Length / 7;
                    sbLine.Append('\t', tabs);

                    if (set.settingName == "locked_gate"
                        || set.settingName == "ship_type_killed"
                        || set.settingName == "sys_visited"
                        || set.settingName == "base_visited"
                        || set.settingName == "holes_visited")
                    {
                        uint hash = set.UInt(0);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            sbLine.Append(";* err code");
                        }
                        else
                        {
                            sbLine.AppendFormat("; {0}", gameData.GetItemByHash(hash).ItemNickName);
                        }
                    }
                    else if (set.settingName == "vnpc")
                    {
                        uint hash = set.UInt(0);
                        uint hash2 = set.UInt(1);
                        if (gameData.GetItemByHash(hash) == null)
                        {
                            sbLine.Append("; err code");
                        }
                        else
                        {
                            sbLine.AppendFormat("; {0}", gameData.GetItemByHash(hash).ItemNickName);
                        }

                        if (gameData.GetItemByHash(hash2) == null)
                        {
                            sbLine.Append(", err code");
                        }
                        else
                        {
                            sbLine.AppendFormat(", {0}", gameData.GetItemByHash(hash2).ItemNickName);
                        }
                    }
                    else if (set.settingName == "can_dock"
                        || set.settingName == "can_tl"
                        || set.settingName == "total_cash_earned"
                        || set.settingName == "total_time_played"

                        || set.settingName == "rm_completed"
                        || set.settingName == "rm_aborted"
                        || set.settingName == "rm_failed"
                        || set.settingName == "rumor")
                    {
                        // ignore
                    }
                    else
                    {
                        sbLine.Append(";* err unknown key");
                    }
                }
                catch (Exception e)
                {
                    sbLine.Append(";* exception " + e.Message);
                }
                sb.AppendLine(sbLine.ToString());

            }

            return sb.ToString();
        }

        /// <summary>
        /// Get the account id from the specified account directory.
        /// Will throw file open exceptions if the 'name' file cannot be opened.
        /// </summary>
        /// <param name="accDirPath">The account directory to search.</param>
        public static string GetPlayerInfoText(string accDirPath, string charFilePath)
        {
            string filePath = accDirPath + Path.DirectorySeparatorChar + charFilePath.Substring(0, 23) + "-info.ini";
            try
            {
                if (File.Exists(filePath))
                {
                    FLDataFile data = new FLDataFile(filePath, false);
                    string text = "";
                    for (int i = 1; i < 10; i++)
                    {
                        if (data.SettingExists("Info", i.ToString()))
                        {
                            FLDataFile.Setting set = data.GetSetting("Info", i.ToString());
                            if (set != null && set.NumValues() > 0)
                            {
                                text += DecodeUnicodeHex(set.Str(0)) + "\n\n";
                            }
                        }
                    }
                    return text;
                }
            }
            catch { }
            return "";
        }

        public static string GetPlayerInfoAdminNote(string accDirPath, string charFilePath)
        {
            string filePath = accDirPath + Path.DirectorySeparatorChar + charFilePath.Substring(0, 23) + "-info.ini";
            try
            {
                if (File.Exists(filePath))
                {
                    FLDataFile data = new FLDataFile(filePath, false);
                    if (data.SettingExists("Info", "AdminNote"))
                    {
                        FLDataFile.Setting set = data.GetSetting("Info", "AdminNote");
                        if (set != null && set.NumValues() > 0)
                            return DecodeUnicodeHex(set.Str(0));
                    }
                }
            }
            catch { }
            return "";
        }

        public static void SetPlayerInfoAdminNote(string accDirPath, string charFilePath, string text)
        {
            string filePath = accDirPath + Path.DirectorySeparatorChar + charFilePath.Substring(0, 23) + "-info.ini";
            try
            {
                FLDataFile data = new FLDataFile(false);
                if (File.Exists(filePath))
                    data = new FLDataFile(filePath, false);
                data.DeleteSetting("Info", "AdminNote");
                data.AddSetting("Info", "AdminNote", new object[] { EncodeUnicodeHex(text) });
                data.SaveSettings(filePath, false);
            }
            catch { }
        }


    }
}
