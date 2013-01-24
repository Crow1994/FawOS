using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Win32;

// TODO: Player location statistics
// TODO: Player in space randomiser
// TODO: Generator for NPC-Weapons-Layouts out of one player-file
// TODO: Generator for mpnewcharacter.fl out of one player-file

namespace DAM
{
    partial class MainWindow : Form, AppServiceInterface, LogRecorderInterface, FLHookListener
    {
        /// <summary>
        /// The background processing thread
        /// </summary>
        private BackgroundWorker bgWkr;

        /// <summary>
        /// The currently loaded char file
        /// </summary>
        private FLDataFile loadedCharFile = new FLDataFile(true);

        /// <summary>
        /// The game data including infocards.
        /// </summary>
        private FLGameData gameData = new FLGameData();

        /// <summary>
        /// The communications socket for executing flhook commands.
        /// </summary>
        private FLHookSocket flHookCmdr;

        /// <summary>
        /// The communications socket for receive flhook events.
        /// </summary>
        private FLHookEventSocket flHookEventSocket;
        
        /// <summary>
        /// The hashcode window.
        /// </summary>
        Form windowHashcode;

        /// <summary>
        /// The banned players window.
        /// </summary>
        BannedPlayers windowBannedPlayers;

        /// <summary>
        /// The general statistics window.
        /// </summary>
        Form windowGeneralStatistics;

        /// <summary>
        /// Number of pending changes in the player info dataset
        /// </summary>
        int dbUpdatesPending = 0;

        /// <summary>
        /// If true the user will not be asked if they want to exit.
        /// </summary>
        bool forceExit = false;

        /// <summary>
        /// The database access service.
        /// </summary>
        private DataAccess dataAccess;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            // If the default account directory isn't setup then create it
            if (AppSettings.Default.setAccountDir == "")
            {
                AppSettings.Default.setAccountDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                 + Path.DirectorySeparatorChar + "My Games"
                 + Path.DirectorySeparatorChar + "Freelancer"
                 + Path.DirectorySeparatorChar + "Accts"
                 + Path.DirectorySeparatorChar + "MultiPlayer";
                AppSettings.Default.Save();
            }

            if (AppSettings.Default.setFLDir == "")
            {
                string path = (string)Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft Games\\Freelancer\\1.0", "AppPath", "C:\\Program Files\\Microsoft Games\\Freelancer");
                if (path == null)
				    path = "C:\\Program Files\\Microsoft Games\\Freelancer";
                AppSettings.Default.setFLDir = path;
                AppSettings.Default.Save();
            }

            if (AppSettings.Default.setIonCrossDir == "")
            {
                string path = (string)Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft Games\\Freelancer\\1.0", "AppPath", "C:\\Program Files\\Microsoft Games\\Freelancer");
                if (path == null)
                    path = "C:\\Program Files\\Microsoft Games\\Freelancer\\IONCROSS";
                AppSettings.Default.setIonCrossDir = path;
                AppSettings.Default.Save();
            }

            // If key directories/files do not exist then open the properties
            // window to create them
            if (!Directory.Exists(AppSettings.Default.setAccountDir)
                || !Directory.Exists(AppSettings.Default.setFLDir)
                || !File.Exists(AppSettings.Default.setFLDir+"\\EXE\\Freelancer.ini"))
            {
                new PropertiesWindow().ShowDialog(this);
            }

            // Start the flhook sockets
            flHookCmdr = new FLHookSocket();
            flHookEventSocket = new FLHookEventSocket(this, this);

            // Reset the player info buttons
            UpdatePlayerInfo();
            FilterUpdate(null, null);

            // Disable menus while data is being loaded.
            itemListToolStripMenuItem.Enabled = false;
            bannedPlayersToolStripMenuItem.Enabled = false;
            rescanAccountFilesToolStripMenuItem.Enabled = false;
            statisticsToolStripMenuItem.Enabled = false;
            searchIPtoolStripMenuItem.Enabled = false;
            searchLoginIDToolStripMenuItem.Enabled = false;

            // Load the game data.
            bgWkr = new BackgroundWorker();
            bgWkr.WorkerReportsProgress = true;
            bgWkr.WorkerSupportsCancellation = true;
            bgWkr.ProgressChanged += new ProgressChangedEventHandler(ProgressUpdate);
            bgWkr.DoWork += new DoWorkEventHandler(LoadIt);
            bgWkr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoadItCompleted);
            bgWkr.RunWorkerAsync();
        }

        /// <summary>
        /// Background worker to load databases and game data.
        /// </summary>
        private void LoadIt(object sender, DoWorkEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;

                // Load the ban list and character lists.
                ReportProgress(20, "Opening database...");
                AddLog("Opening database...");
                DamDataSet tempDataStore = new DamDataSet();
                using (DataAccess da = new DataAccess())
                {
                    // Load the game data if needed.
                    ReportProgress(40, "Loading game data...");
                    AddLog("Loading game data...");
                    da.LoadGameData(gameData.DataStore, this);
                    if (gameData.DataStore.HashList.Count == 0)
                    {
                        gameData.LoadAll(bgWkr, this);
                        da.CommitGameData(gameData.DataStore, this);
                        gameData.DataStore.AcceptChanges();
                    }

                    // foreach (GameDataSet.ShipInfoListRow r in gameData.DataStore.ShipInfoList)
                    //    Console.WriteLine(String.Format("{0} {1} {2} {3}",
                    //        r.ShipHash, r.DefaultPowerPlant, r.DefaultSound, r.DefaultEngine));

                    if (bgWkr.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    ReportProgress(60, "Loading player information...");
                    AddLog("Loading player information...");
                    da.GetBanList(tempDataStore.BanList);
                    da.GetCharacterList(tempDataStore.CharacterList);
                    AddLog("Load complete "
                        + tempDataStore.CharacterList.Count + " characters, "
                        + tempDataStore.BanList.Count + " bans");

                    string cmdFile = AppSettings.Default.setAccountDir + "\\cmdfile.sql";
                    if (File.Exists(cmdFile))
                    {
                        try
                        {
                            using (StreamReader sr = new StreamReader(cmdFile))
                            {
                                ReportProgress(70, "Executing SQL command file...");
                                AddLog("Executing SQL command file...");
                                while (true)
                                {
                                    string st = sr.ReadLine();
                                    if (st == null)
                                        break;
                                    da.ExecuteSQL(st);
                                }
                                sr.Close();
                                AddLog("Executed SQL command file.");
                            }
                        }
                        catch (Exception ex)
                        {
                            AddLog(String.Format("Error '{0}' when executing {1}", ex.Message, cmdFile));
                        }
                    }

                    // For each system add a count of the number of players in it (both docked and in space)
                    ReportProgress(80, "Updating statistics...");
                    AddLog("Loading statistics...");
                    da.BeginTransaction();
                    da.RebuildStatistics();
                    foreach (GameDataSet.HashListRow row in gameData.DataStore.HashList)
                    {
                        if (row.ItemType == FLGameData.GAMEDATA_SYSTEMS)
                        {
                            string systemName = gameData.GetItemDescByHash(row.ItemHash);
                            da.UpdateGeneralStatistics(
                                "Characters in space in " + systemName,
                                "SELECT COUNT(*) FROM CharacterList WHERE Location LIKE '" + DataAccess.SafeSqlLiteral(systemName) + " in space%' AND IsDeleted = '0'");
                            da.UpdateGeneralStatistics(
                                "Characters docked in " + systemName,
                                "SELECT COUNT(*) FROM CharacterList WHERE Location LIKE '" + DataAccess.SafeSqlLiteral(systemName) + " docked%' AND IsDeleted = '0'");
                        }
                        else if (row.ItemType == FLGameData.GAMEDATA_SHIPS)
                        {
                            string shipName = gameData.GetItemDescByHash(row.ItemHash);
                            da.UpdateGeneralStatistics(
                                "Number of " + shipName,
                                "SELECT COUNT(*) FROM CharacterList WHERE Ship LIKE '" + DataAccess.SafeSqlLiteral(shipName) + "' AND IsDeleted = '0'");
                        }
                    }
                    da.EndTransaction();
                }

                e.Result = tempDataStore;
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
            }
            catch (Exception ex)
            {
                AddLog(String.Format("Error '{0}' at {1}", ex.Message, ex.StackTrace));
            }
        }


        /// <summary>
        /// The loading of the game data and databases has completed. Save the
        /// databases and ask if the user wants to scan the player account files.
        /// </summary>
        private void LoadItCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bgWkr = null;

            if (e.Cancelled)
                return;

            if (e.Result == null)
            {
                if (MessageBox.Show("Cannot access database. Check your 'Player account directory'. Try again?", "Error", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    forceExit = true;
                    Application.Exit();
                    return;
                }
                MainWindow_Load(null, null);
                return;
            }

            ReportProgress(90, "Updating tables");
            DamDataSet dds = (DamDataSet)e.Result;
            dataSetPlayerInfo.BanList.Clear();
            dataSetPlayerInfo.Merge(dds.BanList);
            dataSetPlayerInfo.CharacterList.Clear();
            dataSetPlayerInfo.CharacterList.Merge(dds.CharacterList);
            dataSetPlayerInfo.AcceptChanges();
            ReportProgress(0, "OK");
            AddLog("OK");

            // Enable menus now that key data is loaded.
            itemListToolStripMenuItem.Enabled = true;
            bannedPlayersToolStripMenuItem.Enabled = true;
            rescanAccountFilesToolStripMenuItem.Enabled = true;
            statisticsToolStripMenuItem.Enabled = true;
            searchIPtoolStripMenuItem.Enabled = true;
            searchLoginIDToolStripMenuItem.Enabled = true;

            timerPeriodicTasks.Start();
            timerDBSave.Start();

            switch (AppSettings.Default.setShowUpdateDatabase)
            {
                case 0: // Ask
                    if (MessageBox.Show(this, "Update the player database now?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                        return; // Don't Update
                    break;
                case 1: // Update
                    break;
                case 2: // Don't update
                    return;
            }

            // Start the load data background worker.
            RescanAccountFiles();
        }

        /// <summary>
        /// Start the account directory check.
        /// </summary>
        private void RescanAccountFiles()
        {
            if (bgWkr == null)
            {
                rescanAccountFilesToolStripMenuItem.Enabled = false;
            
                // Reset the background worker to process player database stuff.
                bgWkr = new BackgroundWorker();
                bgWkr.WorkerReportsProgress = true;
                bgWkr.WorkerSupportsCancellation = true;
                bgWkr.ProgressChanged += new ProgressChangedEventHandler(ProgressUpdate);
                bgWkr.DoWork += new DoWorkEventHandler(CheckIt);
                bgWkr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CheckItCompleted);
                bgWkr.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Called to report processing progress on the main screen. Pass a null string to 
        /// not update the status text.
        /// </summary>
        public void ReportProgress(int progressPercentage, string userState)
        {
            if (InvokeRequired)
            {
                bgWkr.ReportProgress(progressPercentage, userState);
            }
            else
            {
                progressBar.Value = progressPercentage;
                if (userState is string)
                    toolStripStatusLabelStatus.Text = userState;
            }
        }

        /// <summary>
        /// Called on progress updates from the worker thread.
        /// </summary>
        private void ProgressUpdate(object sender, ProgressChangedEventArgs e)
        {
            ReportProgress(e.ProgressPercentage, (string)e.UserState);
        }

        /// <summary>
        /// Scan account directories background processing thread
        /// </summary>
        private void CheckIt(object sender, DoWorkEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            e.Result = FLUtility.CheckAccounts(bgWkr, this, gameData);
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
        }

        /// <summary>
        /// Called when account directory background processing thread completes.
        /// </summary>
        private void CheckItCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                return;

            rescanAccountFilesToolStripMenuItem.Enabled = true;
            bgWkr = null;

            SelectionInfo selection = SaveSelection();

            DamDataSet dds = (DamDataSet)e.Result;
            dataSetPlayerInfo.BanList.Clear();
            dataSetPlayerInfo.Merge(dds.BanList);
            dataSetPlayerInfo.CharacterList.Clear();
            dataSetPlayerInfo.CharacterList.Merge(dds.CharacterList);
            dataSetPlayerInfo.AcceptChanges();
            ReportProgress(0,"OK");

            RestoreSelection(selection);
        }

        /// <summary>
        /// Get the character list entry of the currently selected row
        /// in the table.
        /// </summary>
        /// <returns>The entry or null if an entry was not selected</returns>
        private DamDataSet.CharacterListRow GetCharRecordBySelectedRow()
        {
            string charFilePath = GetCharPathBySelectedRow();

            if (charFilePath == null)
                return null;

            return dataSetPlayerInfo.CharacterList.FindByCharPath(charFilePath);
        }

        /// <summary>
        /// Get all character list entrys of the currently selected rows
        /// in the table.
        /// </summary>
        /// <returns>The entrys or null if an entry was not selected</returns>
        private DamDataSet.CharacterListRow[] GetAllCharRecordsBySelectedRows()
        {
            string[] charFilePaths = GetAllCharPathsBySelectedRows();
            DamDataSet.CharacterListRow[] selectedRows = new DamDataSet.CharacterListRow[charFilePaths.Length];

            for (int i = 0; i < charFilePaths.Length; i++)
            {
                selectedRows[i] = dataSetPlayerInfo.CharacterList.FindByCharPath(charFilePaths[i]);
            }

            return selectedRows;
        }

        /// <summary>
        /// Get the charfilepath of the currently selected row
        /// in the table.
        /// </summary>
        /// <returns>The entry or null if an entry was not selected</returns>
        private string GetCharPathBySelectedRow()
        {
            // Get the deleted for the currently selected player in the character list.
            if (charListDataGridView.SelectedRows.Count == 0)
                return null;

            return (string)charListDataGridView.SelectedRows[0].Cells[charPathDataGridViewTextBoxColumn1.Index].Value;
        }

        /// <summary>
        /// Get all charfilepaths of the currently selected rows
        /// in the table.
        /// </summary>
        /// <returns>The entrys or null if an entry was not selected</returns>
        private string[] GetAllCharPathsBySelectedRows()
        {
            // don't enter the loop if only one is selected
            if (charListDataGridView.SelectedRows.Count == 1)
                return new string[] { GetCharPathBySelectedRow() };

            // array to save all selected rows
            string[] selectedRows = new string[charListDataGridView.SelectedRows.Count];

            for (int i = 0; i < selectedRows.Length; i++)
            {
                selectedRows[i] = (string)charListDataGridView.SelectedRows[i].Cells[charPathDataGridViewTextBoxColumn1.Index].Value;
            }

            return selectedRows;
        }

        /// <summary>
        /// Load a character file and display it in the player information tabs
        /// </summary>
        delegate void UpdatePlayerInfoDelegate();
        public void UpdatePlayerInfo()
        {
            // Reset the UI.
            loadedCharFile = null;
            dataSetUI.AcceptChanges();
            piPath.Text = "";

            changeBanButton.Enabled = false;
            openDirButton.Enabled = false;
            deletePlayerButton.Enabled = false;
            reloadFileButton.Enabled = false;
            buttonCheckFile.Enabled = false;

            piFileView.Text = "";
            piAccountID.Text = "";
            piAccountID.BackColor = SystemColors.ControlLight;
            changeBanButton.Text = "Ban Account";
            buttonBanInfo.Enabled = false;

            piName.Text = "";
            piName.ReadOnly = true;
            changeNameButton.Text = "Change";
            changeNameButton.Enabled = false;

            piMoney.Text = "";
            piMoney.ReadOnly = true;
            changeMoneyButton.Text = "Change";
            changeMoneyButton.Enabled = false;

            piLocation.Text = "";
            changeLocationButton.Enabled = false;

            piShip.Text = "";
            changeShipButton.Enabled = false;

            piLastOnline.Text = "";
            buttonResetLastOnline.Enabled = false;

            piOnline.Text = "";
            kickPlayerButton.Enabled = false;
            piTimePlayed.Text = "";
            piRank.Text = "";
            piKills.Text = "";
            piCreated.Text = "";

            piIPAddresses.Text = "";

            piAffiliation.Text = "";

            dataSetUI.PIFactionTable.Clear();
            saveFactionButton.Enabled = false;
            discardFactionButton.Enabled = false;

            dataSetUI.PICargoTable.Clear();
            addCargoButton.Enabled = false;
            buttonChangeCargo.Enabled = false;
            removeCargoButton.Enabled = false;
            saveCargoButton.Enabled = false;
            discardCargoButton.Enabled = false;

            dataSetUI.PIEquipmentTable.Clear();
            buttonAddEquipment.Enabled = false;
            changeEquipmentButton.Enabled = false;
            removeEquipmentButton.Enabled = false;
            saveEquipmentButton.Enabled = false;
            discardEquipmentButton.Enabled = false;

            saveFileManualButton.Enabled = false;

            richTextBoxPlayerInfoPlayerText.Clear();
            richTextBoxPlayerInfoAdminText.Clear();
            buttonPlayerInfoSaveAdminText.Enabled = false;

            buttonAddCompleteMap.Enabled = false;

            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null || charListDataGridView.SelectedRows.Count != 1)
                return;

            // Rescan the account to ensure the database is in an accurate state.
            dbUpdatesPending += FLUtility.ScanCharAccount(dataSetPlayerInfo, this, gameData, charRecord.AccDir);
            dbUpdatesPending += FLUtility.CheckForDeletedChars(dataSetPlayerInfo, charRecord.AccDir);

            changeBanButton.Enabled = true;
            openDirButton.Enabled = true;
            reloadFileButton.Enabled = true;
            saveFileManualButton.Enabled = true;

            buttonAddCompleteMap.Enabled = true;

            if (!charRecord.IsDeleted)
            {
                deletePlayerButton.Enabled = true;

                changeNameButton.Enabled = true;
                changeMoneyButton.Enabled = true;
                changeLocationButton.Enabled = true;
                changeShipButton.Enabled = true;
                buttonResetLastOnline.Enabled = true;

                saveFactionButton.Enabled = true;
                discardFactionButton.Enabled = true;

                addCargoButton.Enabled = true;
                buttonChangeCargo.Enabled = true;
                removeCargoButton.Enabled = true;
                saveCargoButton.Enabled = true;
                discardCargoButton.Enabled = true;
                buttonAddEquipment.Enabled = true;
                changeEquipmentButton.Enabled = true;
                removeEquipmentButton.Enabled = true;
                saveEquipmentButton.Enabled = true;
                discardEquipmentButton.Enabled = true;

                buttonPlayerInfoSaveAdminText.Enabled = true;
            }

            // Read special account files, admin, banned and authenticated (used by the player control plugin).
            string accDirPath = AppSettings.Default.setAccountDir + "\\" + charRecord.AccDir;
            bool isAdmin = File.Exists(accDirPath + Path.DirectorySeparatorChar + "flhookadmin.ini");
            isAdmin |= File.Exists(accDirPath + Path.DirectorySeparatorChar + "admin");
            bool isBanned = File.Exists(accDirPath + Path.DirectorySeparatorChar + "banned");
            bool isAuthenticated = File.Exists(accDirPath + Path.DirectorySeparatorChar + "authenticated");
            bool isLongIDBanned = false;

            if (!string.IsNullOrEmpty(AppSettings.Default.setLoginIDBanFile))
            {
                GetDataAccess();
                DamDataSet.LoginIDListDataTable loginIDs = new DamDataSet.LoginIDListDataTable();
                dataAccess.GetLoginIDListByAccDir(loginIDs, charRecord.AccDir);

                foreach (DamDataSet.LoginIDListRow row in loginIDs)
                {
                    if (dataAccess.GetLoginIDBanRowByLoginID(row.LoginID) != null)
                    {
                        isLongIDBanned = true;
                        break;
                    }
                }
            }
            
            if (isBanned)
            {
                changeBanButton.Text = "Unban Account";
                buttonBanInfo.Enabled = true;
            }

            try
            {
                // Do the account ID first and path. After this point we could hit an exception and
                // stop the update.
                piAccountID.Text = charRecord.AccID
                    + (isBanned ? " [BANNED]" : "")
                    + (isLongIDBanned ? " [HW-BANNED]" : "")
                    + (isAdmin ? " [ADMIN]" : "")
                    + (isAuthenticated ? " [AUTHENTICATED]" : "");
                if ((isLongIDBanned || isBanned) && AppSettings.Default.setDisplayBannedCharsRed)
                    piAccountID.BackColor = Color.Red;
                piPath.Text = AppSettings.Default.setAccountDir + "\\" + charRecord.CharPath;
                piName.Text = charRecord.CharName + (charRecord.IsDeleted ? " [DELETED]" : "");
                piCreated.Text = String.Format("{0:f}", charRecord.Created);
                piRank.Text = charRecord.Rank.ToString(NumberFormatInfo.InvariantInfo);
                piMoney.Text = charRecord.Money.ToString(NumberFormatInfo.InvariantInfo);
                piLocation.Text = charRecord.Location;
                piShip.Text = charRecord.Ship;
                piTimePlayed.Text = String.Format("{0}", new TimeSpan(0, 0, charRecord.OnLineSecs));
                piLastOnline.Text = String.Format("{0:f}", charRecord.LastOnLine);

                richTextBoxPlayerInfoPlayerText.Text = FLUtility.FLXmlToRtf(FLUtility.GetPlayerInfoText(
                    AppSettings.Default.setAccountDir, charRecord.CharPath));
                richTextBoxPlayerInfoAdminText.Text = FLUtility.GetPlayerInfoAdminNote(
                    AppSettings.Default.setAccountDir, charRecord.CharPath);

                using (DamDataSet ds = new DamDataSet())
                {
                    DateTime startTime = DateTime.Now;
                    GetDataAccess().GetIPListByAccDir(ds.IPList, charRecord.AccDir);
                    foreach (DamDataSet.IPListRow row in ds.IPList)
                        piIPAddresses.Text += String.Format("IP: {0}: {1}\r\n", row.AccessTime, row.IP);

                    GetDataAccess().GetLoginIDListByAccDir(ds.LoginIDList, charRecord.AccDir);
                    foreach (DamDataSet.LoginIDListRow row in ds.LoginIDList)
                        piIPAddresses.Text += String.Format("LoginID: {0}: {1}\r\n", row.AccessTime, row.LoginID);
                }
                
                // Load the char file if it exists.
                if (File.Exists(AppSettings.Default.setAccountDir + "\\" + charRecord.CharPath))
                {
                    loadedCharFile = new FLDataFile(AppSettings.Default.setAccountDir + "\\" + charRecord.CharPath, true);
                    buttonCheckFile.Enabled = true;

                    uint kills = 0;
                    foreach (FLDataFile.Setting e in loadedCharFile.GetSettings("mPlayer", "ship_type_killed"))
                        kills += e.UInt(1);
                    piKills.Text = kills.ToString();

                    long shipArchType = 0;
                    piShip.Text = FLUtility.GetShip(gameData, loadedCharFile, out shipArchType);

                    piOnline.Text = "No";
                    if (flHookCmdr.CmdIsOnServer(charRecord.CharName))
                    {
                        piOnline.Text = "Yes";
                        kickPlayerButton.Enabled = true;
                    }

                    // Faction tab fields
                    if (loadedCharFile.SettingExists("Player", "rep_group"))
                        piAffiliation.Text = gameData.GetItemDescByFactionNickName(loadedCharFile.GetSetting("Player", "rep_group").Str(0));

                    foreach (FLDataFile.Setting e in loadedCharFile.GetSettings("Player", "house"))
                        dataSetUI.PIFactionTable.AddPIFactionTableRow(gameData.GetItemDescByFactionNickName(e.Str(1)), e.Str(1), e.Float(0));
                    piFactionGrid.Sort(piFactionGrid.Columns[itemDescriptionDataGridViewTextBoxColumn.Index], ListSortDirection.Ascending);

                    // Populate the cargo table.
                    foreach (FLDataFile.Setting e in loadedCharFile.GetSettings("Player", "cargo"))
                    {
                        // Add the row to the table
                        UIDataSet.PICargoTableRow row = dataSetUI.PICargoTable.NewPICargoTableRow();
                        row.itemHash = e.UInt(0);
                        row.itemCount = e.UInt(1);
                        row.itemDescription = gameData.GetItemDescByHash(row.itemHash);
                        dataSetUI.PICargoTable.AddPICargoTableRow(row);
                    }
                    piCargoGrid.Sort(piCargoGrid.Columns[itemDescriptionDataGridViewTextBoxColumn.Index], ListSortDirection.Descending);

                    // Populate the equipment table with the permitted hardpoint types
                    foreach (GameDataSet.HardPointListRow hp in gameData.DataStore.HardPointList)
                    {
                        if (hp.ShipHash == shipArchType)
                        {
                            bool exists = false;
                            foreach (UIDataSet.PIEquipmentTableRow eq in dataSetUI.PIEquipmentTable)
                            {
                                if (eq.itemHardpoint == hp.HPName)
                                {
                                    eq.itemAllowedTypes += " " + hp.HPType;
                                    exists = true;
                                    break;
                                }
                            }
                            if (!exists)
                                dataSetUI.PIEquipmentTable.AddPIEquipmentTableRow(hp.HPName, "", 0, "", hp.HPType);
                        }
                    }

                    // Populate the equipment table with the contents of the charfile updating the
                    // hardpoints.
                    foreach (FLDataFile.Setting e in loadedCharFile.GetSettings("Player", "base_equip"))
                    {
                        uint itemHash = e.UInt(0);
                        string hpName = e.Str(1);

                        bool internalHardPoint = true;
                        if (hpName.Length > 0)
                        {
                            foreach (UIDataSet.PIEquipmentTableRow row in dataSetUI.PIEquipmentTable)
                            {
                                // FIXME: Are hardpoint names case sensitive?
                                if (row.itemHardpoint.ToLowerInvariant() == hpName.ToLowerInvariant())
                                {
                                    row.itemHash = itemHash;
                                    row.itemDescription = gameData.GetItemDescByHash(itemHash);
                                    if (gameData.GetItemByHash(itemHash) != null)
                                        row.itemGameDataType = gameData.GetItemByHash(itemHash).ItemType;
                                    internalHardPoint = false;
                                    break;
                                }
                            }
                        }

                        if (internalHardPoint)
                        {
                            UIDataSet.PIEquipmentTableRow row = dataSetUI.PIEquipmentTable.NewPIEquipmentTableRow();
                            row.itemHash = itemHash;
                            row.itemDescription = gameData.GetItemDescByHash(itemHash);
                            row.itemHardpoint = "*" + hpName;
                            if (gameData.GetItemByHash(itemHash) != null)
                                row.itemGameDataType = gameData.GetItemByHash(itemHash).ItemType;
                            dataSetUI.PIEquipmentTable.AddPIEquipmentTableRow(row);
                        }
                    }

                    piEquipmentGrid.Sort(piEquipmentGrid.Columns[itemHardpointDataGridViewTextBoxColumn.Index], ListSortDirection.Descending);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "' when loading " + charRecord.CharPath, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            // Load the file view tab. Don't loaded the text into the text area unless the
            // tab is selected
            tabControl1_SelectedIndexChanged(null, null);
        }


        /// <summary>
        /// Open the properties window as a modal dialog.
        /// </summary>
        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new PropertiesWindow().ShowDialog(this);
        }

        /// <summary>
        /// Exit the application.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Update the player information area when a row is selected in the character
        /// list data grid view.
        /// </summary>
        private DamDataSet.CharacterListRow selectedCharRecord = null;
        private void charListDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if ((charListDataGridView.FirstDisplayedCell == null || charListDataGridView.SelectedRows.Count != 1) && selectedCharRecord != null)
            {
                selectedCharRecord = null;
                updatePlayerInfoTimer.Start();
                return;
            }

            DamDataSet.CharacterListRow newSelectedCharRecord = GetCharRecordBySelectedRow();
            if (newSelectedCharRecord!=null && selectedCharRecord != newSelectedCharRecord)
            {
                selectedCharRecord = newSelectedCharRecord;
                updatePlayerInfoTimer.Start();
            }
        }

        /// <summary>
        /// Open the directory containing the current character file.
        /// </summary>
        private void openDirButton_Click(object sender, EventArgs e)
        {
            try
            {
                DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
                if (charRecord != null)
                {
                    Process.Start(AppSettings.Default.setAccountDir + "\\" + charRecord.AccDir);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "'", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Refresh the player information area.
        /// </summary>
        private void reloadFileButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord != null && !charRecord.IsDeleted
                && flHookCmdr.CmdIsOnServer(charRecord.CharName))
            {
                flHookCmdr.CmdSaveChar(charRecord.CharName);
            }
            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Change the name of the currently selected character. This
        /// function uses flhook to make the name change and will not work with
        /// out it.
        /// </summary>
        private void changeNameButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                if (piName.ReadOnly == true)
                {
                    piName.ReadOnly = false;
                    changeNameButton.Text = "Save";
                }
                else
                {
                    piName.ReadOnly = false;
                    changeNameButton.Text = "Change";

                    string currentName = loadedCharFile.GetSetting("Player", "name").UniStr(0);
                    string newName = piName.Text;

                    if (!flHookCmdr.CmdRename(currentName, newName))
                    {
                        MessageBox.Show(this, "Error flhook command failed '" + flHookCmdr.LastCmdError + "'", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    updatePlayerInfoTimer.Start();
                }
            }
        }

        /// <summary>
        /// Allow the money text box to be edited or save changes.
        /// </summary>
        private void changeMoneyButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                // If the money field is read only then this is a request to edit
                // so allow editing.
                if (piMoney.ReadOnly == true)
                {
                    piMoney.ReadOnly = false;
                    changeMoneyButton.Text = "Save";
                }
                // Otherwise this must be a save.
                else
                {
                    uint newMoney;
                    if (!UInt32.TryParse(piMoney.Text, out newMoney))
                    {
                        MessageBox.Show(this, "Invalid money.", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }

                    loadedCharFile.AddSetting("Player", "money", new object[] { newMoney });
                    SaveCharFile(loadedCharFile);
                }
            }
        }

        /// <summary>
        /// Toggle the ban state of the selected character in the character list.
        /// </summary>
        private void changeBanButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (changeBanButton.Text == "Ban Account")
            {
                new CreateBanWindow(this, charRecord.AccDir, charRecord.AccID, dataSetPlayerInfo, null).ShowDialog(this);
            }
            else
            {
                UnbanAccount(charRecord.AccDir);
            }

            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Show ban information for the selected player.
        /// </summary>
        private void buttonBanInfo_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            bannedPlayersToolStripMenuItem_Click(null, null);
            windowBannedPlayers.HighlightRecord(charRecord.AccID);
        }

        /// <summary>
        /// Show ban history for the selected player.
        /// </summary>
        private void buttonBanHistory_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            bannedPlayersToolStripMenuItem_Click(null, null);
            windowBannedPlayers.HighlightRecord(charRecord.AccID);
        }

        /// <summary>
        /// Delete the selected character in the character list. Always remove 
        /// the character file even if the flhook command files.
        /// </summary>
        private void deletePlayerButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (MessageBox.Show(this, "Are you sure you want to delete this player?", "Delete Player?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                return;

            DeleteChar(charRecord.CharName, charRecord.CharPath);
            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Open the location change dialog. This is modal.
        /// Kick player before location change.
        /// </summary>
        private void changeLocationButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                string charPath = AppSettings.Default.setAccountDir + "\\" + charRecord.CharPath;
                new ChangeLocationWindow(this, gameData, loadedCharFile).ShowDialog(this);
            }
        }


        /// <summary>
        /// Change the player ship.
        /// </summary>
        private void changeShipButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                string charPath = AppSettings.Default.setAccountDir + "\\" + charRecord.CharPath;
                new ChangeShipWindow(this, gameData, loadedCharFile).ShowDialog(this);
            }
        }

        /// <summary>
        /// Kick a player.
        /// </summary>
        private void kickPlayerButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (!flHookCmdr.CmdKick(charRecord.CharName))
            {
                MessageBox.Show(this, "Warning flhook command failed '" + flHookCmdr.LastCmdError + "'.", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            updatePlayerInfoTimer.Start();
        }



        /// <summary>
        /// Update the player's last online time so that it is not deleted by the 
        /// automatic player wipe.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonResetLastOnline_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                DateTime now = DateTime.Now;
                long high = (now.ToFileTime() >> 32) & 0xFFFFFFFF;
                long low = now.ToFileTime() & 0xFFFFFFFF;
                loadedCharFile.AddSetting("Player", "tstamp", new object[] { high, low });
                SaveCharFile(loadedCharFile);
            }
        }

        /// <summary>
        /// When the selected rows change update the reputation editor. Inhibit
        /// notifications when updating the reputation editor.
        /// </summary>
        private void piFactionGrid_SelectionChanged(object sender, EventArgs e)
        {
            piReputationEdit.ValueChanged -= piReputationEdit_ValueChanged;
            foreach (DataGridViewRow row in piFactionGrid.SelectedRows)
            {
                UIDataSet.PIFactionTableRow dataRow = (UIDataSet.PIFactionTableRow)((DataRowView)row.DataBoundItem).Row;
                piReputationEdit.Value = Convert.ToDecimal(dataRow.itemRep);
                trackBar1.Value = (int)(piReputationEdit.Value * 10);
            }
            piReputationEdit.ValueChanged += piReputationEdit_ValueChanged;
        }

        /// <summary>
        /// When the reputation value changes update any selected rows in the 
        /// faction with the new value.
        /// </summary>
        private void piReputationEdit_ValueChanged(object sender, EventArgs e)
        {
            trackBar1.Value = (int)(piReputationEdit.Value * 10);
            foreach (DataGridViewRow row in piFactionGrid.SelectedRows)
            {
                UIDataSet.PIFactionTableRow dataRow = (UIDataSet.PIFactionTableRow)((DataRowView)row.DataBoundItem).Row;
                dataRow.itemRep = Convert.ToSingle(piReputationEdit.Value);
            }
        }

        /// <summary>
        /// When the reputation track bar changes update the reps
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            piReputationEdit.Value = ((decimal)trackBar1.Value)/10;
        }

        /// <summary>
        /// Discard changes to the faction editor by reloading the charfile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void discardFactionButton_Click(object sender, EventArgs e)
        {
            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Save changes made to the faction editor. Note that faction ini lines
        /// have the format "house = reputation, faction_nick"
        /// </summary>
        private void saveFactionButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                while (loadedCharFile.DeleteSetting("Player", "house")) ;
                foreach (UIDataSet.PIFactionTableRow r in dataSetUI.PIFactionTable.Rows)
                    loadedCharFile.AddSettingNotUnique("Player", "house", new object[] { r.itemRep, r.itemNickname });

                SaveCharFile(loadedCharFile);
            }
        }

        /// <summary>
        /// Defer loading of the file view text box because sometimes this can take 
        /// a long time to do.
        /// </summary>
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage5)
            {
                if (piFileView.TextLength == 0 && loadedCharFile != null)
                {
                    Cursor.Current = Cursors.WaitCursor;

                    if (checkBoxShowOriginalFile.Checked)
                    {
                        piFileView.Text = loadedCharFile.GetIniFileContents();
                    }
                    else
                    {
                        piFileView.Text = FLUtility.PrettyPrintCharFile(gameData, loadedCharFile);
                    }
                    
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        /// <summary>
        /// Add a cargo item.
        /// </summary>
        private void buttonAddCargo_Click(object sender, EventArgs e)
        {
            new AddCargoWindow(gameData, dataSetUI.PICargoTable, null).ShowDialog(this);
        }

        /// <summary>
        /// Change a cargo item.
        /// </summary>
        private void buttonChangeCargo_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in piCargoGrid.SelectedRows)
            {
                UIDataSet.PICargoTableRow item = (UIDataSet.PICargoTableRow)((DataRowView)row.DataBoundItem).Row;
                new AddCargoWindow(gameData, dataSetUI.PICargoTable, item).ShowDialog(this);
            }
        }

        /// <summary>
        /// Remove any selected rows from the cargo list data grid view.
        /// </summary>
        private void buttonRemoveCargo_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in piCargoGrid.SelectedRows)
            {
                UIDataSet.PICargoTableRow item = (UIDataSet.PICargoTableRow)((DataRowView)row.DataBoundItem).Row;
                dataSetUI.PICargoTable.Rows.Remove(item);
            }
        }

        /// <summary>
        /// Save any changes made to the cargo list data grid view.
        /// </summary>
        private void saveCargoButton_Click(object sender, EventArgs e)
        {
            // Kick the player if they are online.
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            // Delete all cargo and base_cargo lines
            while (loadedCharFile.DeleteSetting("Player", "base_cargo")) ;
            while (loadedCharFile.DeleteSetting("Player", "cargo")) ;

            // Replace them with the contents of the table
            foreach (UIDataSet.PICargoTableRow r in dataSetUI.PICargoTable.Rows)
                loadedCharFile.AddSettingNotUnique("Player", "cargo", new object[] { r.itemHash, r.itemCount, "", 1, 0 });
            foreach (UIDataSet.PICargoTableRow r in dataSetUI.PICargoTable.Rows)
                loadedCharFile.AddSettingNotUnique("Player", "base_cargo", new object[] { r.itemHash, r.itemCount, "", 1, 0 });

            SaveCharFile(loadedCharFile);
        }

        /// <summary>
        /// Discard any changes to the cargo data grid view.
        /// </summary>
        private void discardCargoButton_Click(object sender, EventArgs e)
        {
            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Add a new item of internal equipment to the ship
        /// </summary>
        private void addEquipmentButton_Click(object sender, EventArgs e)
        {
            if (piEquipmentGrid.SelectedRows.Count == 0)
                return;

            new AddEquipmentWindow(gameData, dataSetUI.PIEquipmentTable, null).ShowDialog(this);
        }

        /// <summary>
        /// Change an existing item of equipment to a different type
        /// </summary>
        private void changeEquipmentButton_Click(object sender, EventArgs e)
        {
            if (piEquipmentGrid.SelectedRows.Count == 0)
                return;

            UIDataSet.PIEquipmentTableRow item = (UIDataSet.PIEquipmentTableRow)((DataRowView)piEquipmentGrid.SelectedRows[0].DataBoundItem).Row;
            new AddEquipmentWindow(gameData, dataSetUI.PIEquipmentTable, item).ShowDialog(this);
        }

        /// <summary>
        /// Remove one or more items of equipment.
        /// </summary>
        private void removeEquipmentButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow selectedRow in piEquipmentGrid.SelectedRows)
            {
                UIDataSet.PIEquipmentTableRow item = (UIDataSet.PIEquipmentTableRow)((DataRowView)selectedRow.DataBoundItem).Row;
                if (item.itemHardpoint.StartsWith("*"))
                {
                    dataSetUI.PIEquipmentTable.RemovePIEquipmentTableRow(item);
                }
                else
                {
                    item.itemHash = 0;
                    item.itemDescription = "";
                }
            }
        }

        /// <summary>
        /// Short cut for changing an item of equipment.
        /// </summary>
        private void piEquipmentGrid_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            changeEquipmentButton_Click(sender, e);
        }

        /// <summary>
        /// Copy the equipment list grid view back into the ini file and save it.
        /// </summary>
        private void saveEquipmentButton_Click(object sender, EventArgs e)
        {
            // Kick the player if they are online.
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            // Delete all cargo and base_cargo lines
            while (loadedCharFile.DeleteSetting("Player", "base_equip")) ;
            while (loadedCharFile.DeleteSetting("Player", "equip")) ;

            // Replace them with the contents of the table. Note that if the hardpoint
            // is "special" by starting with a '*' we don't write an empty string back to 
            // the charfile.
            foreach (UIDataSet.PIEquipmentTableRow r in dataSetUI.PIEquipmentTable.Rows)
            {
                if (r.itemHash != 0)
                    loadedCharFile.AddSettingNotUnique("Player", "equip", new object[] { r.itemHash, (r.itemHardpoint.StartsWith("*") ? "" : r.itemHardpoint), 1 });
            }
            foreach (UIDataSet.PIEquipmentTableRow r in dataSetUI.PIEquipmentTable.Rows)
            {
                if (r.itemHash != 0)
                    loadedCharFile.AddSettingNotUnique("Player", "base_equip", new object[] { r.itemHash, (r.itemHardpoint.StartsWith("*") ? "" : r.itemHardpoint), 1 });
            }

            SaveCharFile(loadedCharFile);
        }

        /// <summary>
        /// Discard any changes to the equipment data grid view.
        /// </summary>
        private void discardEquipmentButton_Click(object sender, EventArgs e)
        {
            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Save the "raw" charfile.
        /// </summary>
        private void saveFileManualButton_Click(object sender, EventArgs e)
        {
            FLDataFile editedCharFile = new FLDataFile(Encoding.ASCII.GetBytes(piFileView.Text), loadedCharFile.filePath, true);
            editedCharFile.SaveSettings(loadedCharFile.filePath, AppSettings.Default.setWriteEncryptedFiles);
            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Open the banned players window.
        /// </summary>
        private void bannedPlayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (windowBannedPlayers == null || windowBannedPlayers.IsDisposed)
                windowBannedPlayers = new BannedPlayers(this, dataSetPlayerInfo);
            windowBannedPlayers.Show();
            windowBannedPlayers.BringToFront();
        }

        /// <summary>
        /// Open the statistics window or bring it to the front.
        /// </summary>
        private void statisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (windowGeneralStatistics == null || windowGeneralStatistics.IsDisposed)
                windowGeneralStatistics = new Tool_UI.StatisticsWindow();
            windowGeneralStatistics.Show();
            windowGeneralStatistics.BringToFront();
        }

        /// <summary>
        /// Open the hash code window or bring it to the front.
        /// </summary>
        private void hashcodeListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (windowHashcode == null || windowHashcode.IsDisposed)
                windowHashcode = new HashWindow(gameData);
            windowHashcode.Show();
            windowHashcode.BringToFront();
        }

        /// <summary>
        /// Open a dialog showing the readme file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutWindow().ShowDialog(this);
        }

        /// <summary>
        /// The player information is updated from a timer to break any circular
        /// calling dependences.
        /// </summary>
        private void updatePlayerInfoTimer_Tick(object sender, EventArgs e)
        {
            updatePlayerInfoTimer.Stop();
            UpdatePlayerInfo();
        }

        /// <summary>
        /// Save the account data and the flhook connection when the main window closes.
        /// </summary>
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!forceExit && (AppSettings.Default.setShowQuitMsg && MessageBox.Show(this, "Are you sure you want to quit?", "Close Application?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes))
            {
                e.Cancel = true;
                return;
            }

            
            if (flHookCmdr != null)
            {
                flHookCmdr.Shutdown();
                flHookCmdr = null;
            }

            if (flHookEventSocket != null)
            {
                flHookEventSocket.Shutdown();
                flHookEventSocket = null;
            }

            // Set a timer to wait for thread shutdown.
            if (bgWkr != null)
            {
                bgWkr.CancelAsync();
                timerShutdown.Start();
                e.Cancel = true;
            }
        }

        public void BanAccount(string accDir, string accID, string banReason, DateTime banStart, DateTime banEnd)
        {
            // Add/change the ban record.
            DamDataSet.BanListRow banRecord = dataSetPlayerInfo.BanList.FindByAccDir(accDir);
            if (banRecord == null)
            {
                banRecord = dataSetPlayerInfo.BanList.NewBanListRow();
                banRecord.AccDir = accDir;
                banRecord.AccID = accID;
                banRecord.BanReason = banReason;
                banRecord.BanStart = banStart;
                banRecord.BanEnd = banEnd;
                dataSetPlayerInfo.BanList.AddBanListRow(banRecord);
                dbUpdatesPending++;
            }
            else
            {
                banRecord.BanReason = banReason;
                banRecord.BanStart = banStart;
                banRecord.BanEnd = banEnd;
                dbUpdatesPending++;
            }

            // Ban using FLHook so that the FLServer account list remains accurate.
            string query = "(AccDir = '" + FLUtility.EscapeEqualsExpressionString(accDir) + "') AND (IsDeleted = 'false')";
            DamDataSet.CharacterListRow[] charRecords = (DamDataSet.CharacterListRow[])dataSetPlayerInfo.CharacterList.Select(query);
            foreach (DamDataSet.CharacterListRow charRecord in charRecords)
            {
                if (flHookCmdr.CmdIsOnServer(charRecord.CharName))
                    flHookCmdr.CmdKick(charRecord.CharName);
            }

            foreach (DamDataSet.CharacterListRow charRecord in charRecords)
            {
                if (!flHookCmdr.CmdBan(charRecord.CharName))
                    MessageBox.Show("Warning flhook command failed \"" + flHookCmdr.LastCmdError + "\". Creating ban file in account anyway.", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                break;
            }

            // Write the ban file in manually as a backup to flhook.
            string banFilePath = AppSettings.Default.setAccountDir + "\\" + accDir + Path.DirectorySeparatorChar + "banned";
            try
            {
                File.Create(banFilePath).Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Create ban file failed '" + ex.Message + "'", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Unban the account.
        /// </summary>
        /// <param name="accDir"></param>
        public void UnbanAccount(string accDir)
        {
            // Remove the ban record.
            DamDataSet.BanListRow banRecord = dataSetPlayerInfo.BanList.FindByAccDir(accDir);
            if (banRecord != null)
            {
                banRecord.Delete();
                dbUpdatesPending++;
            }
 
            // Unban using FLHook so that the FLServer account list remains accurate.
            string query = "(AccDir = '" + FLUtility.EscapeEqualsExpressionString(accDir) + "') AND (IsDeleted = 'false')";
            DamDataSet.CharacterListRow[] charRecords = (DamDataSet.CharacterListRow[])dataSetPlayerInfo.CharacterList.Select(query);
            foreach (DamDataSet.CharacterListRow charRecord in charRecords)
            {
                if (!flHookCmdr.CmdUnban(charRecord.CharName))
                {
                    MessageBox.Show("Warning flhook command failed \"" + flHookCmdr.LastCmdError + "\". Removing ban file from account anyway.", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                break;
            }

            // Remove the ban file in manually as a backup to flhook.
            string banFilePath = AppSettings.Default.setAccountDir + "\\" + accDir + Path.DirectorySeparatorChar + "banned";
            try
            {
                if(File.Exists(banFilePath))
                    File.Delete(banFilePath);
            }
            catch {}
        }

        /// <summary>
        /// Delete the specified character
        /// </summary>
        /// <param name="charName"></param>
        /// <param name="charPath"></param>
        public void DeleteChar(string charName, string charPath)
        {
            if (!flHookCmdr.CmdDeleteChar(charName))
            {
                MessageBox.Show("Warning flhook command failed '" + flHookCmdr.LastCmdError + "'. Deleting player file from account anyway.", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            try
            {
                charPath = AppSettings.Default.setAccountDir + "\\" + charPath;
                if (File.Exists(charPath))
                    File.Delete(charPath);
            }
            catch { }
        }

        /// <summary>
        /// Save the current player ini file to disk. Displays a dialog
        /// on failure. If connected to flhook then the player is kicked and banned
        /// while making the change.
        /// </summary>
        public void SaveCharFile(FLDataFile charFile)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                string charName = charFile.GetSetting("Player", "Name").UniStr(0);

                // If player is logged in on another character then we can't kick
                // them using this char file. We have the kick the one they are
                // logged in on.
                string charPath = charFile.filePath.Substring(AppSettings.Default.setAccountDir.Length + 1);
                DamDataSet.CharacterListRow charRecord = dataSetPlayerInfo.CharacterList.FindByCharPath(charPath);
                if (charRecord != null)
                {
                    string query = "(AccDir = '" + FLUtility.EscapeEqualsExpressionString(charRecord.AccDir) + "') AND (IsDeleted = 'false')";
                    DamDataSet.CharacterListRow[] charRecords = (DamDataSet.CharacterListRow[])dataSetPlayerInfo.CharacterList.Select(query);
                    foreach (DamDataSet.CharacterListRow accCharRecord in charRecords)
                    {
                        if (flHookCmdr.CmdIsOnServer(accCharRecord.CharName))
                            flHookCmdr.CmdKick(accCharRecord.CharName);
                    }

                    foreach (DamDataSet.CharacterListRow accCharRecord in charRecords)
                    {
                        if (!flHookCmdr.CmdBan(accCharRecord.CharName))
                            MessageBox.Show(this, "Warning flhook command failed \"" + flHookCmdr.LastCmdError + "\". Creating ban file in account anyway.", null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                    }
                }

                flHookCmdr.CmdBan(charName);

                charFile.SaveSettings(charFile.filePath, AppSettings.Default.setWriteEncryptedFiles);

                flHookCmdr.CmdUnban(charName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error '" + ex.Message + "' when saving " + loadedCharFile.filePath, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            this.Cursor = oldCursor;

            updatePlayerInfoTimer.Start();
        }

        /// <summary>
        /// Manual account file scan
        /// </summary>
        private void rescanAccountFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RescanAccountFiles();
        }

        /// <summary>
        /// Updates the text filter.
        /// Called by textBoxFilter and all search-checkboxes (checkBoxSearch*) on value changes
        /// </summary>
        private void updateTextFilter(object sender, EventArgs e)
        {
            timerFilter.Start();
            if (textBoxFilter.Text.Length > 0)
            {
                checkBoxFilterSameAccount.Checked = false;
                checkBoxFilterSameIP.Checked = false;
                checkBoxFilterSameLoginID.Checked = false;
            }
        }

        private void textBoxFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (AppSettings.Default.setFilterWaitForEnter && e.KeyCode != Keys.Enter)
                return;

            updateTextFilter(sender, EventArgs.Empty);
        }

        private void checkBoxFilterSameAcc_CheckedChanged(object sender, EventArgs e)
        {
            timerFilter.Start();
            if (checkBoxFilterSameAccount.Checked)
            {
                textBoxFilter.Text = "";
                checkBoxFilterSameIP.Checked = false;
                checkBoxFilterSameLoginID.Checked = false;
            }
        }

        private void checkBoxFilterSameLoginID_CheckedChanged(object sender, EventArgs e)
        {
            timerFilter.Start();
            if (checkBoxFilterSameLoginID.Checked)
            {
                textBoxFilter.Text = "";
                checkBoxFilterSameAccount.Checked = false;
                checkBoxFilterSameIP.Checked = false;
            }
        }

        private void checkBoxFilterSameIP_CheckedChanged(object sender, EventArgs e)
        {
            timerFilter.Start();
            if (checkBoxFilterSameIP.Checked)
            {
                textBoxFilter.Text = "";
                checkBoxFilterSameAccount.Checked = false;
                checkBoxFilterSameLoginID.Checked = false;
            }
        }

        private void checkBoxFilterDeleted_CheckedChanged(object sender, EventArgs e)
        {
            timerFilter.Start();
        }

        /// <summary>
        /// Update the filter applied to the character list data grid view.
        /// </summary>
        private void FilterUpdate(object sender, EventArgs e)
        {
            timerFilter.Stop();
            List<string> parts = new List<string>(10);
            StringBuilder tmp = new StringBuilder(500);
            DateTime startTime = DateTime.Now;
            Cursor.Current = Cursors.WaitCursor;
            SelectionInfo selection = SaveSelection();

            if (!checkBoxFilterDeleted.Checked)
            {
                parts.Add("(IsDeleted = 'false')");
            }

            if (textBoxFilter.Text.Length > 0)
            {
                string filterText = textBoxFilter.Text;
                if (checkBoxSearchCharname.Checked || checkBoxSearchAccID.Checked || checkBoxSearchLocation.Checked || checkBoxSearchShip.Checked || checkBoxSearchCharPath.Checked)
                {
                    List<string> filterParts = new List<string>();

                    if (checkBoxSearchCharname.Checked)
                    {
                        filterParts.Add("(CharName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')");
                    }
                    if (checkBoxSearchAccID.Checked)
                    {
                        filterParts.Add("(AccID LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')");
                    }
                    if (checkBoxSearchLocation.Checked)
                    {
                        filterParts.Add("(Location LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')");
                    }
                    if (checkBoxSearchShip.Checked)
                    {
                        filterParts.Add("(Ship LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')");
                    }
                    if (checkBoxSearchCharPath.Checked)
                    {
                        filterParts.Add("(CharPath LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')");
                    }

                    tmp.Append('(');
                    for (int i = 0; i < filterParts.Count - 1; i++)
                    {
                        tmp.Append(filterParts[i]);
                        tmp.Append(" OR ");
                    }
                    tmp.Append(filterParts[filterParts.Count - 1]);
                    tmp.Append(')');

                    parts.Add(tmp.ToString());
                    tmp = new StringBuilder(500);
                }
            }
            else if (checkBoxFilterSameAccount.Checked)
            {
                if (selectedCharRecord != null)
                {
                    parts.Add("(AccDir = '" + FLUtility.EscapeEqualsExpressionString(selectedCharRecord.AccDir) + "')");
                }
            }
            else if (checkBoxFilterSameIP.Checked)
            {
                using (DamDataSet tds = new DamDataSet())
                {
                    if (selectedCharRecord != null)
                    {
                        // Find all IPs used by this account.
                        GetDataAccess().GetIPListByAccDir(tds.IPList, selectedCharRecord.AccDir);

                        // Extract the IP information.
                        List<string> selectedAccIPs = new List<string>();
                        for (int i = 0; i < tds.IPList.Count; i++)
                        {
                            if (!selectedAccIPs.Contains(tds.IPList[i].IP))
                                selectedAccIPs.Add(tds.IPList[i].IP);
                        }

                        // Find all other accounts with matching IPs 
                        GetDataAccess().GetIPListByIP(tds.IPList, selectedAccIPs.ToArray());
                        List<string> accDirs = new List<string>();
                        accDirs.Add(selectedCharRecord.AccDir);
                        for (int i = 0; i < tds.IPList.Count; i++)
                        {
                            if (!accDirs.Contains(tds.IPList[i].AccDir))
                                accDirs.Add(tds.IPList[i].AccDir);
                        }

                        // Construct filter to show all matching accounts
                        parts.Add("(AccDir IN ('" + String.Join("', '", accDirs.ToArray()) + "'))");
                    }
                }
            }
            else if (checkBoxFilterSameLoginID.Checked)
            {
                using (DamDataSet tds = new DamDataSet())
                {
                    if (selectedCharRecord != null)
                    {
                        // Find all LoginIDs used by this account.
                        GetDataAccess().GetLoginIDListByAccDir(tds.LoginIDList, selectedCharRecord.AccDir);

                        // Extract the LoginID information.
                        List<string> selectedAccLoginIDs = new List<string>();
                        for (int i = 0; i < tds.LoginIDList.Count; i++)
                        {
                            if (!selectedAccLoginIDs.Contains(tds.LoginIDList[i].LoginID))
                                selectedAccLoginIDs.Add(tds.LoginIDList[i].LoginID);
                        }

                        // Find all other accounts with matching LoginIDs
                        GetDataAccess().GetLoginIDListByLoginID(tds.LoginIDList, selectedAccLoginIDs.ToArray());
                        List<string> accDirs = new List<string>();
                        accDirs.Add(selectedCharRecord.AccDir);
                        for (int i = 0; i < tds.LoginIDList.Count; i++)
                        {
                            if (!accDirs.Contains(tds.LoginIDList[i].AccDir))
                                accDirs.Add(tds.LoginIDList[i].AccDir);
                        }

                        // Construct filter to show all matching accounts
                        parts.Add("(AccDir IN ('" + String.Join("', '", accDirs.ToArray()) + "'))");
                    }
                }
            }


            // put all the parts together
            for (int i = 0; i < parts.Count - 1; i++)
            {
                tmp.Append(parts[i]);
                tmp.Append(" AND ");
            }
            tmp.Append(parts[parts.Count - 1]);

            characterListBindingSource.Filter = tmp.ToString();

            RestoreSelection(selection);

            Cursor.Current = Cursors.Default;

            double opTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            if (opTime > 200)
                AddLog(String.Format("Warning: filter operation took a {0} ms to complete.", opTime));
        }

        /// <summary>
        /// Add an entry to the diagnostics log.
        /// </summary>
        /// <param name="entry">The log entry</param>
        Object logLocker = new Object();
        public void AddLog(string entry)
        {
            lock (logLocker)
            {
                if (AppSettings.Default.setAccountDir.Length > 0)
                {
                    try
                    {
                        StreamWriter writer = File.AppendText(AppSettings.Default.setAccountDir + "\\dsam.log");
                        writer.WriteLine(entry);
                        writer.Close();
                    }
                    catch (Exception e)
                    {
                        string errMsg = "Write to game log failed: " + e.Message;
                        if (InvokeRequired)
                        {
                            try { Invoke(new UpdateUILogEventDelegate(UpdateUILogEvent), new object[] { errMsg }); }
                            catch { }
                        }
                        else
                        {
                            UpdateUILogEvent(errMsg);
                        }
                    }
                }
            }

            if (InvokeRequired)
            {
                try { Invoke(new UpdateUILogEventDelegate(UpdateUILogEvent), new object[] { entry }); }
                catch { }
            }
            else
            {
                UpdateUILogEvent(entry);
            }
        }

        /// <summary>
        /// Add an entry to the on screen log and unminimise the program if required.
        /// </summary>
        /// <param name="msg">The msg</param>
        delegate void UpdateUILogEventDelegate(string msg);
        private void UpdateUILogEvent(string msg)
        {
            string oldText = richTextBoxLog.Text;
            if (oldText.Length > 50000)
            {
                oldText = oldText.Substring(0, 50000);
            }

            richTextBoxLog.Text = DateTime.Now.ToShortDateString()
                + " " + DateTime.Now.ToLongTimeString()
                + ":" + msg + "\n" + oldText;
        }

        /// <summary>
        /// Receive an event from the flhook command socket.
        /// </summary>
        /// <param name="type">The command type from the keys[0] field</param>
        /// <param name="keys">An array of parameter keys.</param>
        /// <param name="values">An array of parameter values.</param>
        /// <param name="eventLine">The unparsed event line</param>
        public void ReceiveFLHookEvent(string type, string[] keys, string[] values, string eventLine)
        {
            if (InvokeRequired)
            {
                UpdateUIOnReceiveFLHookEventDelegate updateUI = new UpdateUIOnReceiveFLHookEventDelegate(UpdateUIOnReceiveFLHookEvent);
                try
                {
                    this.Invoke(updateUI, new object[] { type, keys, values, eventLine });
                }
                catch { }
            }
            else
            {
                UpdateUIOnReceiveFLHookEvent(type, keys, values, eventLine);
            }
        }

        /// <summary>
        /// A map of client IDs to accDir so we can rescan accs when players move.
        /// </summary>
        Dictionary<int, string> clientIdAccDirs = new Dictionary<int, string>();

        /// <summary>
        /// If the current time is greater than the time listed here then recheck the account.
        /// </summary>
        Dictionary<string, DateTime> pendingAccDirsToCheck = new Dictionary<string, DateTime>();

        /// <summary>
        /// A delegate that always runs in the UI thread. This updates the database
        /// which in turn updates the online player table.
        /// </summary>
        /// <param name="type">The command type from the keys[0] field</param>
        /// <param name="keys">An array of parameter keys.</param>
        /// <param name="values">An array of parameter values.</param>
        /// <param name="eventLine">The unparsed event line</param>
        delegate void UpdateUIOnReceiveFLHookEventDelegate(string type, string[] keys, string[] values, string eventLine);
        protected void UpdateUIOnReceiveFLHookEvent(string type, string[] keys, string[] values, string eventLine)
        {
            try
            {
                if (type == "login")
                {
                    int id = Convert.ToInt32(values[3]);
                    string charname = values[1];
                    string accDir = values[2];
                    string ip = values[4];

                    clientIdAccDirs[id] = accDir;

                    // Record the IP address information.
                    dataSetPlayerInfo.IPList.AddIPListRow(accDir, ip, DateTime.Now);

                    // Rescan the account to ensure the database is in an accurate state.
                    pendingAccDirsToCheck[accDir] = DateTime.Now.AddSeconds(5);
                }
                else if (!(type == "kill"))
                {
                    if (type == "basedestroy")
                    {
                        FLHookSocket fLHookSocket = new FLHookSocket();
                        
                        int num = 0;
                        string[] array2 = values[1].Split(new char[]
												{
													'_'
												});
                        string filePath = string.Concat(new string[]
												{
													AppSettings.Default.setFLDir+"\\DATA\\UNIVERSE\\SYSTEMS\\", 
													array2[0], 
													"\\", 
													array2[0], 
													".ini"
												});
                        //fLHookSocket.CmdMsgU(filePath);
                        string filePath2 = AppSettings.Default.setFLDir+"\\DATA\\MISSIONS\\mBases.ini";
                        string[] array3 = new string[5000];
                        string text4 = array2[0] + "_" + array2[1];
                        string pBaseExcl = array2[0] + "_player_station_1";
                        string iWannaKnow = values[4];

                        //Save char before checking it's affiliation
                        fLHookSocket.CmdSaveChar(iWannaKnow);
                        string text5 = fLHookSocket.CmdReadCharFile(iWannaKnow);
                        
                        if (text5 != "ERROR" && text5 != "OK" && values[1].ToLower() != pBaseExcl && values[1].ToLower() != "rh01_05_base" && values[1].ToLower() != "li01_10_base" && values[1].ToLower() != "br01_02_base" && values[1].ToLower() != "ku06_01_base" && values[1].ToLower() != "br04_04_base" && values[1].ToLower() != "br02_03_base" && values[1].ToLower() != "br01_03_base" && values[1].ToLower() != "br01_01_base" && values[1].ToLower() != "rh04_05_base" && values[1].ToLower() != "hi02_01_base" && values[1].ToLower() != "bw06_02_base" && values[1].ToLower() != "rh01_03_base" && values[1].ToLower() != "li01_02_base" && values[1].ToLower() != "ku03_05_base" && values[1].ToLower() != "br06_03_base" && values[1].ToLower() != "bw05_01_base" && values[1].ToLower() != "br01_07_base" && values[1].ToLower() != "ku05_02_base" && values[1].ToLower() != "ku01_07_base" && values[1].ToLower() != "ew01_02_base" && values[1].ToLower() != "li01_08_base" && values[1].ToLower() != "bw05_03_base" && values[1].ToLower() != "ku01_05_base" && values[1].ToLower() != "rh01_06_base" && values[1].ToLower() != "ku05_01_base" && values[1].ToLower() != "ku01_01_base" && values[1].ToLower() != "rh03_06_base" && values[1].ToLower() != "iw03_02_base" && values[1].ToLower() != "li01_03_base" && values[1].ToLower() != "li01_01_base" && values[1].ToLower() != "li01_12_base" && values[1].ToLower() != "br05_05_base" && values[1].ToLower() != "bw07_01_base" && values[1].ToLower() != "hi01_01_base" && values[1].ToLower() != "br01_05_base" && values[1].ToLower() != "bw04_02_base" && values[1].ToLower() != "rh02_04_base" && values[1].ToLower() != "rh01_02_base" && values[1].ToLower() != "rh01_01_base" && values[1].ToLower() != "ku01_03_base" && values[1].ToLower() != "st01_01_base" && values[1].ToLower() != "iw01_01_base" && values[1].ToLower() != "li01_06_base" && values[1].ToLower() != "li03_03_base" && values[1].ToLower() != "ew01_01_base" && values[1].ToLower() != "st02_01_base")
                        //if (text5 != "ERROR" && text5 != "OK" && values[1].ToLower() != pBaseExcl && values[1].ToLower() != "st04_03_base" && values[1].ToLower() != "iw09_03_base")
                        {
                            int num2 = num + 1;
                            //string soWhatWeHaveHere2 = "md .." + "\\data\\universe\\systems\\" + array2[0] + "\\" + array2[0] + ".ini " + array2[0] + "_" + array2[1] + " reputation " + text5;
                            FLGameData.ReplaceInFile(filePath, "base = " + values[1] + "(.[^[]*)reputation = (.[^\n]*)", "base = " + values[1] + "$1reputation =" + text5);
                            
                            //Let's Change client side
                            string soWhatWeHaveHere = "rd .." + "\\data\\universe\\systems\\" + array2[0] + "\\" + array2[0] + ".ini " + array2[0] + "_" + array2[1] + " reputation " + text5;
                            fLHookSocket.CmdMsgU(soWhatWeHaveHere);
                            fLHookSocket.CmdMsgU("rd ..\\data\\missions\\mbases.ini " + values[1] + " local_faction " + text5);

                            FLGameData.ReplaceInFile(filePath, "nickname = " + values[1] + "(.[^[]*)reputation = (.[^\n]*)", "nickname = " + values[1] + "$1reputation =" + text5);
                            FLGameData.ReplaceInFile(filePath, "dock_with = " + values[1] + "(.[^[]*)reputation = (.[^\n]*)", "dock_with = " + values[1] + "$1reputation =" + text5);
                            FLGameData.ReplaceInFile(filePath, "reputation = (.[^\\n]*)(.[^[]*)dock_with = " + values[1], "reputation =" + text5 + "$2dock_with = " + values[1]);
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Planet_" + text4 + "(.[^[]*)encounter = capitalships_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Planet_", 
														text4, 
														"$2encounter = capitalships_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Planet_" + text4 + "(.[^[]*)encounter = cruisers_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Planet_", 
														text4, 
														"$2encounter = cruisers_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Planet_" + text4 + "(.[^[]*)encounter = gunboats_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Planet_", 
														text4, 
														"$2encounter = gunboats_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Planet_" + text4 + "(.[^[]*)encounter = area_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Planet_", 
														text4, 
														"$2encounter = area_$3,$4,$5$6faction =", 
														text5, 
														","
													}));

							FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)_pop_(.[^[]*)encounter = area_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1_pop_$2encounter = area_$3,$4,$5$6faction =", 
														text5, 
														","
													}));

                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Planet_" + text4 + "(.[^[]*)encounter = lone_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Planet_", 
														text4, 
														"$2encounter = lone_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Station_" + text4 + "(.[^[]*)encounter = capitalships_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Station_", 
														text4, 
														"$2encounter = capitalships_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Station_" + text4 + "(.[^[]*)encounter = cruisers_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Station_", 
														text4, 
														"$2encounter = cruisers_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Station_" + text4 + "(.[^[]*)encounter = gunboats_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Station_", 
														text4, 
														"$2encounter = gunboats_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Station_" + text4 + "(.[^[]*)encounter = area_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Station_", 
														text4, 
														"$2encounter = area_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            FLGameData.ReplaceInFile(filePath, "nickname = Zone_(.[^[]*)Station_" + text4 + "(.[^[]*)encounter = lone_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\n]*)faction =(.[^,]*),", string.Concat(new string[]
													{
														"nickname = Zone_$1Station_", 
														text4, 
														"$2encounter = lone_$3,$4,$5$6faction =", 
														text5, 
														","
													}));
                            
                            FLGameData.ReplaceInFile(filePath2, "nickname = " + values[1] + "(.[^[]*)local_faction = .[^\n]*", "nickname = " + values[1] + "$1local_faction =" + text5);

                            FLGameData.ReplaceInFile(filePath2, "faction = .[^\n]*" + "(.[^[]*)offers_missions = true", "faction =" + text5 + "$1offers_missions = true");

                            FLGameData.ReplaceInFile(filePath2, "nickname = " + array2[0] + array2[1] + "_fix_ship" + "(.[^[]*)affiliation = .[^\n]*", "nickname = " + array2[0] + array2[1] + "_fix_ship" +"$1affiliation =" + text5);
                            FLGameData.ReplaceInFile(filePath2, "nickname = " + array2[0] + array2[1] + "_fix_bartender" + "(.[^[]*)affiliation = .[^\n]*" + "(.[^[]*)bribe = .[^\n]*", "nickname = " + array2[0] + array2[1] + "_fix_bartender" + "$1affiliation =" + text5 + "$2bribe =" + text5);
                            FLGameData.ReplaceInFile(filePath2, "nickname = " + array2[0] + array2[1] + "_fix_trader" + "(.[^[]*)affiliation = .[^\n]*", "nickname = " + array2[0] + array2[1] + "_fix_trader" + "$1affiliation =" + text5);
                            FLGameData.ReplaceInFile(filePath2, "nickname = " + array2[0] + array2[1] + "_fix_weaponsdealer" + "(.[^[]*)affiliation = .[^\n]*", "nickname = " + array2[0] + array2[1] + "_fix_weaponsdealer" + "$1affiliation =" + text5);

                            //Plugin Hook Specific
                            FLGameData.ReplaceInFile(filePath, "archetype = Trade_Lane_Ring(.[^[]*)reputation = (.[^\n]*)", "archetype = Trade_Lane_Ring$1reputation = fc_uk_grp");

                            //FLGameData.ReplaceInFile(filePath3, ";" + values[1] + "(.[^[]*) = .[^\n]*", ";" + values[1] + "$1 =" + text5);

                            if (num2 == 1)
                            {
                                //fLHookSocket.CmdMsgU("SomeWhere an Object is Captured !!");
                                //fLHookSocket.CmdMsgU("To disable this message install special patch");
                            }
                            if (array2[1] == "01")
                            {
                                fLHookSocket.CmdMsgU("Control Point is captured! Whole systems will be yours after ION Storm.");
                                FLGameData.ReplaceInFile(filePath, "reputation = (.[^\\n]*)", "reputation =" + text5);
                                FLGameData.ReplaceInFile(filePath, "encounter = patrolp_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = patrolp_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = area_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = area_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = tradep_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = tradep_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = miningp_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = miningp_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = capitalships_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = capitalships_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = cruisers_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = cruisers_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = gunboats_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = gunboats_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = tradelane_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = tradelane_$1,$2,$3$4faction =" + text5 + ",");
                                FLGameData.ReplaceInFile(filePath, "encounter = lone_(.[^,]*),(.[^,]*),(.[^,]*)(.[^\\n]*)faction =(.[^,]*),", "encounter = lone_$1,$2,$3$4faction =" + text5 + ",");
                            }
                        }
                    }
                }
                else if (type == "disconnect"
                    || type == "baseenter"
                    || type == "launch"
                    || type == "spawn")
                {
                    int id = Convert.ToInt32(values[2]);
                    if (clientIdAccDirs.ContainsKey(id))
                    {
                        string accDir = clientIdAccDirs[id];
                        // Rescan the account to ensure the database is in an accurate state.
                        pendingAccDirsToCheck[accDir] = DateTime.Now.AddSeconds(5);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog("Error '" + ex.Message + "' when processing flhook event '" + eventLine + "'");
            }
        }

        /// <summary>
        /// Check the FLHook connection.
        /// </summary>
        private void timerPeriodicTasks_Tick(object sender, EventArgs e)
        {
            charListDataGridView_SelectionChanged(this, EventArgs.Empty);
            Program.ApplyProcessorAffinity();
            int load = 0;
            bool npcSpawnEnabled = false;
            string upTime = "";

            // Test the command connection.
            bool serverUp = false;
            if (flHookCmdr != null && flHookCmdr.CmdServerInfo(out load, out npcSpawnEnabled, upTime))
            {
                serverUp = true;
            }
            if (serverUp && (flHookCmdr != null))
            {
               flHookCmdr.CmdGetPlayers();
            }

            toolStripHookState.Text = serverUp ? "OK " + upTime : "-";

            // Check any accounts that need to be checked.
            List<string> processedAccDirs = new List<string>();
            foreach (KeyValuePair<string, DateTime> value in pendingAccDirsToCheck)
            {
                if (value.Value < DateTime.Now)
                {
                    string accDir = value.Key;
                    dbUpdatesPending += FLUtility.ScanCharAccount(dataSetPlayerInfo, this, gameData, accDir);
                    dbUpdatesPending += FLUtility.CheckForDeletedChars(dataSetPlayerInfo, accDir);
                    processedAccDirs.Add(accDir);
                }
            }
            foreach (string accDir in processedAccDirs)
            {
                pendingAccDirsToCheck.Remove(accDir);
            }

            // Check if another application is requested that the player accounts be checked
            RegistryKey sk1 = Registry.CurrentUser.OpenSubKey("Software\\" + Application.ProductName, true);
            if (sk1 != null)
            {
                string value = sk1.GetValue("AutocleanPending", 0).ToString();
                if (value == "1")
                {
                    AddLog("Executing registry triggered account scan");
                    sk1.SetValue("AutocleanPending", 0);
                    RescanAccountFiles();
                }
            }

            // Update the DB update display
            toolStripDBPending.Text = String.Format("{0}", dbUpdatesPending);
        }

        /// <summary>
        /// If necessary create and return a database access service. Throw an
        /// error if this is accessed outside of the gui thread.
        /// </summary>
        /// <returns></returns>
        public DataAccess GetDataAccess()
        {
            if (InvokeRequired)
                throw new Exception("Data access from invalid thread");

            if (dataAccess == null) 
                dataAccess = new DataAccess();

            return dataAccess;
        }

        /// <summary>
        /// Save any pending database changes every 60 seconds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerDBSave_Tick(object sender, EventArgs e)
        {
            if (bgWkr == null)
            {
                // Save the database changes. As these changes are small do this from
                // the gui thread but only do this is a background operation is not
                // running.
                ReportProgress(50, "Saving database...");
                GetDataAccess().CommitChanges(dataSetPlayerInfo, this);
                ReportProgress(0, "OK");
                charListDataGridView.SelectionChanged -= charListDataGridView_SelectionChanged;

                SelectionInfo selection = SaveSelection();
                // change the data in the list
                dataSetPlayerInfo.AcceptChanges();
                RestoreSelection(selection);

                charListDataGridView.SelectionChanged += charListDataGridView_SelectionChanged;
                dbUpdatesPending = 0;
                toolStripDBPending.Text = String.Format("{0}", dbUpdatesPending);
            
                // General statistics information. This takes a while do it in the
                // background
                bgWkr = new BackgroundWorker();
                bgWkr.WorkerReportsProgress = true;
                bgWkr.WorkerSupportsCancellation = true;
                bgWkr.DoWork += new DoWorkEventHandler(CrunchIt);
                bgWkr.ProgressChanged += new ProgressChangedEventHandler(ProgressUpdate);
                bgWkr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CrunchItCompleted);
                bgWkr.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Generate statistics.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CrunchIt(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (DataAccess da = new DataAccess())
                {
                    if (da.MakeActiveCharacterHistoryNeeded())
                    {
                        bgWkr.ReportProgress(30, "Cleaning up history database...");
                        da.CleanUpCharacterHistory(AppSettings.Default.setHistoryHorizon);
                        bgWkr.ReportProgress(30, "Saving history database...");
                        da.MakeActiveCharacterHistory();
                        if (!bgWkr.CancellationPending)
                            return;
                    }
                }

                using (StatisticsGenerator gen = new StatisticsGenerator(gameData, flHookCmdr))
                {
                    bgWkr.ReportProgress(50, "Generate statistics...");
                    gen.GenerateGeneralStatistics(bgWkr, this);
                    if (bgWkr.CancellationPending)
                        return;

                    gen.GeneratePlayerStats(bgWkr, this);
                    if (bgWkr.CancellationPending)
                        return;

                    gen.GenerateFactionActivity(bgWkr, this);
                }
            }
            catch (Exception ex)
            {
                AddLog(String.Format("Error '{0}' when saving database", ex.Message));
            }
        }

        /// <summary>
        /// Called when startup background processing thread completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CrunchItCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ReportProgress(0, "OK");
            bgWkr = null;
        }

        /// <summary>
        /// Check a character file for errors.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCheckFile_Click(object sender, EventArgs e)
        {
            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                if (FLUtility.CheckCharFile(dataSetPlayerInfo, this, gameData, loadedCharFile, false, false))
                {
                    if (MessageBox.Show("See the log tab for details. Fix errors?", "Errors Detected",
                        MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        FLUtility.CheckCharFile(dataSetPlayerInfo, this, gameData, loadedCharFile, false, true);
                        SaveCharFile(loadedCharFile);
                    }
                }
            }
        }

        /// <summary>
        /// Open IP search window. Allow multiple instances to be opened.
        /// </summary>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new Tool_UI.SearchForAccountsByIPWindow(this, GetDataAccess()).Show();
        }


        private void searchLoginIDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Tool_UI.SearchForAccountsByLoginIDWindow(this, GetDataAccess()).Show();
        }

        /// <summary>
        /// Open the file editor. Allow multiple instances to be opened.
        /// </summary>
        private void fLFileEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new IniFileWindow().Show();
        }

        /// <summary>
        /// Set the filter to show the specified account directory.
        /// </summary>
        /// <param name="accDir"></param>
        public void FilterOnAccDir(string accDir)
        {
            textBoxFilter.Text = accDir;

            checkBoxSearchCharname.Checked = false;
            checkBoxSearchLocation.Checked = false;
            checkBoxSearchShip.Checked = false;
            checkBoxSearchAccID.Checked = false;
            checkBoxSearchCharPath.Checked = true;

            FilterUpdate(null, EventArgs.Empty);

            // select the first char so the user can see more informations on the right
            if (charListDataGridView.Rows.Count != 0)
                charListDataGridView.Rows[0].Selected = true;
        }

        /// <summary>
        /// Add a complete map to the character.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAddCompleteMap_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            if (loadedCharFile == null)
            {
                updatePlayerInfoTimer.Start();
            }
            else
            {
                if (MessageBox.Show("Add complete map to player?", "Add Map", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;

                while (loadedCharFile.DeleteSetting("Player", "visit")) ;
                foreach (GameDataSet.HashListRow r in gameData.DataStore.HashList)
                {
                    if (r.ItemType == FLGameData.GAMEDATA_OBJECT)
                    {
                        loadedCharFile.AddSettingNotUnique("Player", "visit", new object[] { r.ItemHash, 63 });
                    }
                    else if (r.ItemType == FLGameData.GAMEDATA_ZONE)
                    {
                        loadedCharFile.AddSettingNotUnique("Player", "visit", new object[] { r.ItemHash, 41 });
                    }
                    else if (r.ItemType == FLGameData.GAMEDATA_BASES)
                    {
                        loadedCharFile.AddSettingNotUnique("Player", "visit", new object[] { r.ItemHash, 41 });
                    }
                    else if (r.ItemType == FLGameData.GAMEDATA_SYSTEMS)
                    {
                        loadedCharFile.AddSettingNotUnique("Player", "visit", new object[] { r.ItemHash, 41 });
                    }
                }

                while (loadedCharFile.DeleteSetting("mPlayer", "sys_visited")) ;
                foreach (GameDataSet.HashListRow r in gameData.DataStore.HashList)
                {
                    if (r.ItemType == FLGameData.GAMEDATA_SYSTEMS)
                    {
                        loadedCharFile.AddSettingNotUnique("mPlayer", "sys_visited", new object[] { r.ItemHash });
                    }
                }

                while (loadedCharFile.DeleteSetting("mPlayer", "base_visited")) ;
                foreach (GameDataSet.HashListRow r in gameData.DataStore.HashList)
                {
                    if (r.ItemType == FLGameData.GAMEDATA_BASES)
                    {
                        loadedCharFile.AddSettingNotUnique("mPlayer", "base_visited", new object[] { r.ItemHash });
                    }
                }

                SaveCharFile(loadedCharFile);
            }
        }

        /// <summary>
        /// A test window to decode all FLS1EncodedFiles. This is normally disabled.
        /// </summary>
        private void toolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            new Tool_UI.DecodeAllFilesWindow().Show(this);
        }

        /// <summary>
        /// If the program is shutting down, wait until the background exists and
        /// then close the form.
        /// </summary>
        private void timerShutdown_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabelStatus.Text = "Shutting down...";         
            if (bgWkr == null)
            {
                forceExit = true;
                this.Close();
            }
        }

        /// <summary>
        /// Run the background worker to reload the gamedata and the databases.
        /// </summary>
        private void reloadGameDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bgWkr == null)
            {
                GetDataAccess().ClearGameData(this);

                bgWkr = new BackgroundWorker();
                bgWkr.WorkerReportsProgress = true;
                bgWkr.WorkerSupportsCancellation = true;
                bgWkr.ProgressChanged += new ProgressChangedEventHandler(ProgressUpdate);
                bgWkr.DoWork += new DoWorkEventHandler(LoadIt);
                bgWkr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoadItCompleted);
                bgWkr.RunWorkerAsync();
            }
        }

        private void buttonPlayerInfoSaveAdminText_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow charRecord = GetCharRecordBySelectedRow();
            if (charRecord == null)
                return;

            FLUtility.SetPlayerInfoAdminNote(AppSettings.Default.setAccountDir, charRecord.CharPath,
                richTextBoxPlayerInfoAdminText.Text);
        }

        /// <summary>
        /// Handles clicks on the ban-all-selected-button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void banSelectedButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow[] charRecords = GetAllCharRecordsBySelectedRows();
            Hashtable accs = new Hashtable(charRecords.Length);

            string accDir;
            string accID;

            // Let the user input the ban-infos
            CreateBanWindow createBanWindow = new CreateBanWindow(this, string.Empty, string.Empty, dataSetPlayerInfo, null);
            DialogResult result = createBanWindow.ShowDialog();

            // user decided to don't ban them..
            if (result == DialogResult.Cancel)
                return;

            Thread banThread = new Thread(
                delegate()
                {
                    // ban all saved accounts
                    foreach (DamDataSet.CharacterListRow acc in charRecords)
                    {
                        // extract the data
                        accID = (string)acc.AccID;
                        accDir = (string)acc.AccDir;

                        if(accs.ContainsKey(accDir))
                            continue;
                        accs.Add(accDir, null);

                        // ban the account and save the data which was entered by the user
                        BanAccount(accDir,
                                   accID,
                                   createBanWindow.richTextBox1.Text,
                                   createBanWindow.dateTimePickerStartDate.Value,
                                   createBanWindow.dateTimePickerStartDate.Value.AddDays((int)createBanWindow.numericUpDownDuration.Value));
                    }

                    // show the ban in the player info (data collection on the right)
                    UpdatePlayerInfoDelegate updatePlayerInfo = new UpdatePlayerInfoDelegate(this.UpdatePlayerInfo);
                    this.Invoke(updatePlayerInfo);

                    if (AppSettings.Default.setShowMultibanSucc)
                    {
                        // output the final message
                        MessageBox.Show(string.Format("Banned {0} chars on {1} accounts!", charRecords.Length, accs.Count),
                                        "Multiban succsessfull",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                    }
                });
            banThread.Start();
        }

        /// <summary>
        /// Handles clicks on the delete-all-selected-button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteSelectedButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow[] charRecords = GetAllCharRecordsBySelectedRows();
            
            if (MessageBox.Show(this, "Are you sure you want to delete " + charRecords.Length + " players?", "Delete Players?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                return;
            
            Thread deleteThread = new Thread(
            	delegate()
            	{
                    for (int i = 0; i < charRecords.Length; i++)
                    {
                        DeleteChar(charRecords[i].CharName, charRecords[i].CharPath);
                    }

                    // show that the char is deleted in the player info (data collection on the right)
                    UpdatePlayerInfoDelegate updatePlayerInfo = new UpdatePlayerInfoDelegate(this.UpdatePlayerInfo);
                    this.Invoke(updatePlayerInfo);

                    if (AppSettings.Default.setShowMultideleteSucc)
                    {
                        // output the final message
                        MessageBox.Show(string.Format("Deleted {0} chars!", charRecords.Length),
                                        "Multidelete succsessfull",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                    }
                });
            deleteThread.Start();
        }

        /// <summary>
        /// Handles clicks on the unbanban-all-selected-button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void unbanSelectedButton_Click(object sender, EventArgs e)
        {
            DamDataSet.CharacterListRow[] charRecords = GetAllCharRecordsBySelectedRows();
            Hashtable accs = new Hashtable(charRecords.Length);

            string accDir;

            Thread unbanThread = new Thread(
                delegate()
                {
                    // ban all saved accounts
                    foreach (DamDataSet.CharacterListRow acc in charRecords)
                    {
                        // extract the data
                        accDir = (string)acc.AccDir;

                        if (accs.ContainsKey(accDir))
                            continue;
                        accs.Add(accDir, null);

                        // ban the account and save the data which was entered by the user
                        UnbanAccount(accDir);
                    }

                    // show the ban in the player info (data collection on the right)
                    UpdatePlayerInfoDelegate updatePlayerInfo = new UpdatePlayerInfoDelegate(this.UpdatePlayerInfo);
                    this.Invoke(updatePlayerInfo);

                    if (AppSettings.Default.setShowMultiunbanSucc)
                    {
                        // output the final message
                        MessageBox.Show(string.Format("Unbanned {0} chars on {1} accounts!", charRecords.Length, accs.Count),
                                        "Multiunban succsessfull",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                    }
                });
            unbanThread.Start();
        }

        /// <summary>
        /// Struct to store selections and the scroll position in the main-window and load them later
        /// Needed because the selection is reset on updates
        /// used by SaveSelection- and RestoreSelection-Methods
        /// </summary>
        private struct SelectionInfo
        {
            public string[] selectedCharPaths;

            public int scrollPos;
            public string scrollCharPath;
        }

        /// <summary>
        /// Saves the selected rows and the scroll position
        /// </summary>
        /// <returns>a SelectionInfo that contains the data (used by RestoreSelection-Method)</returns>
        private SelectionInfo SaveSelection()
        {
            SelectionInfo info = new SelectionInfo();

            // save the scroll position
            info.scrollPos = charListDataGridView.FirstDisplayedScrollingRowIndex;
            if (info.scrollPos != -1)
            {
                string charFilePath = (string)charListDataGridView.Rows[info.scrollPos].Cells[charPathDataGridViewTextBoxColumn1.Index].Value;
                info.scrollCharPath = charFilePath;
            }

            // save all selected rows
            info.selectedCharPaths = GetAllCharPathsBySelectedRows();
            return info;
        }

        /// <summary>
        /// Restores the selected rows and the scroll position saved by SaveSelection-Method
        /// </summary>
        /// <param name="info">the info to restore</param>
        private void RestoreSelection(SelectionInfo info)
        {
            // Now find the previously selected characters and reselect them if possible.
            foreach (DataGridViewRow row in charListDataGridView.Rows)
            {
                DamDataSet.CharacterListRow charRecord = (DamDataSet.CharacterListRow)((DataRowView)row.DataBoundItem).Row;
                for (int i = 0; i < info.selectedCharPaths.Length; i++)
                {
                    if (charRecord.CharPath == info.selectedCharPaths[i])
                    {
                        row.Selected = true;
                        break;
                    }
                    else
                    {
                        row.Selected = false;
                    }
                }
                if (info.scrollCharPath != null && charRecord.CharPath == info.scrollCharPath)
                {
                    charListDataGridView.FirstDisplayedScrollingRowIndex = row.Index;
                    info.scrollPos = -1;
                }
            }
            // restore the scroll-position
            if (info.scrollPos != -1 && info.scrollPos < charListDataGridView.RowCount)
                charListDataGridView.FirstDisplayedScrollingRowIndex = info.scrollPos;
        }
    }
}
