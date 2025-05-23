using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Ionic.Zip;
using Ionic.Zlib;
using System.Diagnostics;
using System.Globalization;

namespace SimpleZipper
{
    public partial class Form1 : Form
    {
        public class CompressionItem
        {
            public string FileSystemPath { get; set; }
            public string PathInZip { get; set; }
            public bool IsDirectoryItself { get; set; }
            public string RootNameInList { get; set; }
        }

        private List<CompressionItem> itemsToCompress = new List<CompressionItem>();
        private Label? notificationLabel;
        private System.Windows.Forms.Timer? notificationTimer;
        private Panel? warningPanel;
        private Label? warningMessageLabel;
        private System.Windows.Forms.Timer? animationTimer;
        private System.Windows.Forms.Timer? displayTimer;
        private int warningPanelTargetY = 0;
        private int warningPanelHiddenY = -60;
        private int animationStep = 5;
        private bool isWarningPanelDescending = false;
        private bool isWarningPanelAscending = false;
        private BackgroundWorker? compressionWorker;
        private ToolTip? listBoxToolTip;
        private string currentToolTipText = "";
        private enum ZipOperationMode { CreateNew, AddToExisting }
        private ZipOperationMode currentZipMode = ZipOperationMode.CreateNew;
        private string? existingZipPathForAdd = null;
        private List<ThemeColors> availableThemes = new List<ThemeColors>();
        private enum LogLevel { Debug, Info, Warning, Error }
        private LogLevel currentSelectedLogLevel = LogLevel.Info;
        private enum NotificationType { Info, Warning, Error, Success }
        private CompressionArguments? argsPassedToWorker;
        private Panel? dragDropOverlayPanel;
        private Color originalListBoxBackColor;
        private BorderStyle originalListBoxBorderStyle;


        public Form1()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            this.KeyPreview = true;
            this.AllowDrop = true;

            InitializeDragDropOverlayPanel();
            InitializeDragDrop();
            InitializeNotificationSystem();
            InitializeAnimatedWarningSystem();
            InitializeCompressionWorker();
            InitializePasswordControls();
            InitializeOperationModeControls();
            InitializeCommentControls();
            InitializeListBoxToolTip();
            InitializeSplitControls();
            InitializeThemes();
            InitializeLoggingControls();

            LoadSettings();
            InitializeFormDragDropEvents();
            ProcessCommandLineArgs();
            AppendLog("アプリケーションを起動しました。", LogLevel.Info);

            if (this.selectFilesButton != null) this.selectFilesButton.Click += new System.EventHandler(this.selectFilesButton_Click);
            if (this.clearFileListButton != null) this.clearFileListButton.Click += new System.EventHandler(this.clearFileListButton_Click);
            if (this.removeSelectedFileButton != null) this.removeSelectedFileButton.Click += new System.EventHandler(this.removeSelectedFileButton_Click);
            if (this.selectOutputFolderButton != null) this.selectOutputFolderButton.Click += new System.EventHandler(this.selectOutputFolderButton_Click);
            if (this.compressButton != null) this.compressButton.Click += new System.EventHandler(this.compressButton_Click);

            if (this.cancelCompressionButton != null)
            {
                this.cancelCompressionButton.Click += new System.EventHandler(this.cancelCompressionButton_Click);
                this.cancelCompressionButton.Visible = false;
            }

            if (this.selectedFilesListBox != null) this.selectedFilesListBox.KeyDown += new KeyEventHandler(this.selectedFilesListBox_KeyDown);
            if (this.logLevelComboBox != null) this.logLevelComboBox.SelectedIndexChanged += new System.EventHandler(this.logLevelComboBox_SelectedIndexChanged);
        }

        private void InitializeDragDropOverlayPanel()
        {
            this.dragDropOverlayPanel = new Panel
            {
                Name = "dragDropOverlayPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(80, SystemColors.Highlight),
                BorderStyle = BorderStyle.None,
                Visible = false,
                AllowDrop = false
            };
            this.Controls.Add(this.dragDropOverlayPanel);
            this.dragDropOverlayPanel.BringToFront();
        }

        private void InitializeLoggingControls()
        {
            if (this.logLevelComboBox != null)
            {
                this.logLevelComboBox.Items.Clear();
                foreach (string levelName in Enum.GetNames(typeof(LogLevel)))
                {
                    this.logLevelComboBox.Items.Add(levelName);
                }
            }
        }

        private void ProcessCommandLineArgs()
        {
            string[] cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {
                AppendLog($"{cmdArgs.Length - 1}個のアイテムがコマンドライン引数として渡されました。", LogLevel.Info);
                bool isRecursive = Properties.Settings.Default.RecursiveAdd;

                for (int i = 1; i < cmdArgs.Length; i++)
                {
                    string itemPath = cmdArgs[i];
                    AppendLog($"処理中の引数: {itemPath}", LogLevel.Debug);

                    if (File.Exists(itemPath))
                    {
                        AddFileToList(itemPath);
                    }
                    else if (Directory.Exists(itemPath))
                    {
                        string directoryName = Path.GetFileName(itemPath);
                        AppendLog($"コマンドラインフォルダ処理開始: {itemPath} (再帰設定: {isRecursive})", LogLevel.Debug);

                        if (this.selectedFilesListBox != null && !this.selectedFilesListBox.Items.Contains(directoryName + " (フォルダ)"))
                        {
                            this.selectedFilesListBox.Items.Add(directoryName + " (フォルダ)");
                        }

                        itemsToCompress.Add(new CompressionItem
                        {
                            FileSystemPath = itemPath,
                            PathInZip = directoryName,
                            IsDirectoryItself = true,
                            RootNameInList = directoryName
                        });

                        if (isRecursive)
                        {
                            AddDirectoryItemsRecursively(itemPath, directoryName, directoryName);
                        }
                        else
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(itemPath);
                            foreach (FileInfo file in dirInfo.GetFiles())
                            {
                                itemsToCompress.Add(new CompressionItem
                                {
                                    FileSystemPath = file.FullName,
                                    PathInZip = Path.Combine(directoryName, file.Name),
                                    IsDirectoryItself = false,
                                    RootNameInList = directoryName
                                });
                                AppendLog($"  ファイル追加(コマンドライン/非再帰): {file.FullName}", LogLevel.Debug);
                            }
                            foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
                            {
                                itemsToCompress.Add(new CompressionItem
                                {
                                    FileSystemPath = subDir.FullName,
                                    PathInZip = Path.Combine(directoryName, subDir.Name),
                                    IsDirectoryItself = true,
                                    RootNameInList = directoryName
                                });
                                AppendLog($"  サブフォルダ構造追加(コマンドライン/非再帰): {subDir.FullName}", LogLevel.Debug);
                            }
                            if (new DirectoryInfo(itemPath).GetDirectories().Any(sd => sd.GetFiles().Length > 0 || sd.GetDirectories().Length > 0))
                            {
                                AppendLog($"情報: フォルダ {directoryName} が追加されましたが、再帰オプションが無効のため直下のファイルとサブフォルダ構造のみが対象です。", LogLevel.Info);
                            }
                        }
                    }
                    else
                    {
                        AppendLog($"コマンドライン引数エラー: パスが見つからないかアクセスできません - {itemPath}", LogLevel.Warning);
                    }
                }
            }
        }

        private void InitializeFormDragDropEvents()
        {
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            this.DragLeave += new System.EventHandler(this.Form1_DragLeave);
        }

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                if (this.dragDropOverlayPanel != null)
                {
                    if (this.warningPanel != null && this.warningPanel.Visible) this.warningPanel.SendToBack();
                    if (this.notificationLabel != null && this.notificationLabel.Visible) this.notificationLabel.SendToBack();
                    this.dragDropOverlayPanel.BringToFront();
                    this.dragDropOverlayPanel.Visible = true;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragLeave(object? sender, EventArgs e)
        {
            if (this.dragDropOverlayPanel != null)
            {
                this.dragDropOverlayPanel.Visible = false;
            }
        }

        private void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            if (this.dragDropOverlayPanel != null)
            {
                this.dragDropOverlayPanel.Visible = false;
            }
            if (e.Data != null && e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems)
            {
                ProcessDroppedItems(droppedItems);
            }
        }

        private void InitializeThemes()
        {
            availableThemes.Add(ThemeColors.LightTheme);
            availableThemes.Add(ThemeColors.DarkTheme);

            if (this.themeComboBox != null)
            {
                this.themeComboBox.DataSource = null;
                this.themeComboBox.DataSource = availableThemes;
                this.themeComboBox.DisplayMember = "Name";
                this.themeComboBox.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;
            }
        }
        private void ThemeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is ComboBox themeCb && themeCb.SelectedItem is ThemeColors selectedTheme)
            {
                ApplyTheme(selectedTheme);
                Properties.Settings.Default.CurrentThemeName = selectedTheme.Name ?? "ライト";
            }
        }
        private void ApplyTheme(ThemeColors? theme)
        {
            if (theme == null) return;
            this.BackColor = theme.FormBackColor;
            this.ForeColor = theme.FormForeColor;
            ApplyThemeToControls(this.Controls, theme);

            if (this.logTextBox != null)
            {
                this.logTextBox.BackColor = theme.LogBackColor;
                this.logTextBox.ForeColor = theme.LogForeColor;
            }
            UpdateNotificationColorsWithCurrentTheme();
            if (theme.Name != null) AppendLog($"テーマ変更: {theme.Name}", LogLevel.Info);
        }

        private void UpdateNotificationColorsWithCurrentTheme()
        {
            ThemeColors currentTheme = (this.themeComboBox?.SelectedItem as ThemeColors) ?? ThemeColors.LightTheme;
            if (notificationLabel != null && notificationLabel.Visible)
            {
                NotificationType currentType = GetNotificationTypeFromLabelColor(notificationLabel.BackColor, currentTheme);
                SetNotificationLabelColors(currentType, currentTheme);
            }
            if (warningPanel != null && warningPanel.Visible)
            {
                NotificationType currentType = GetWarningPanelTypeFromColor(warningPanel.BackColor);
                SetAnimatedWarningColors(currentType, currentTheme);
            }
        }
        private NotificationType GetNotificationTypeFromLabelColor(Color backColor, ThemeColors? theme)
        {
            if (theme == null) return NotificationType.Info;
            if (backColor == theme.SuccessNotificationBackColor) return NotificationType.Success;
            if (backColor == theme.InfoNotificationBackColor) return NotificationType.Info;
            return NotificationType.Info;
        }

        private void ShowNotification(string message, NotificationType type)
        {
            ThemeColors? currentTheme = (this.themeComboBox?.SelectedItem as ThemeColors) ?? ThemeColors.LightTheme;
            if (type == NotificationType.Error || type == NotificationType.Warning)
            {
                ShowAnimatedWarning(message, type);
            }
            else
            {
                if (notificationLabel == null || notificationTimer == null) return;
                notificationLabel.Text = message;
                SetNotificationLabelColors(type, currentTheme);
                notificationLabel.Visible = true;
                notificationTimer.Stop();
                notificationTimer.Start();
            }
        }

        private void ShowAnimatedWarning(string message, NotificationType type)
        {
            ThemeColors? currentTheme = (this.themeComboBox?.SelectedItem as ThemeColors) ?? ThemeColors.LightTheme;
            if (warningPanel == null || warningMessageLabel == null || animationTimer == null || displayTimer == null) return;

            if (isWarningPanelDescending || (isWarningPanelAscending && warningPanel.Location.Y < warningPanelTargetY))
            {
                animationTimer.Stop();
                displayTimer.Stop();
            }

            warningMessageLabel.Text = message;
            SetAnimatedWarningColors(type, currentTheme);

            warningPanel.Location = new Point(0, warningPanelHiddenY);
            warningPanel.Visible = true;
            warningPanel.BringToFront();
            if (notificationLabel != null && notificationLabel.Visible) notificationLabel.SendToBack();
            if (this.dragDropOverlayPanel != null && this.dragDropOverlayPanel.Visible) this.dragDropOverlayPanel.SendToBack();


            isWarningPanelDescending = true;
            isWarningPanelAscending = false;
            animationTimer.Start();
        }

        private void SetNotificationLabelColors(NotificationType type, ThemeColors? theme)
        {
            if (notificationLabel == null || theme == null) return;
            switch (type)
            {
                case NotificationType.Success:
                    notificationLabel.BackColor = theme.SuccessNotificationBackColor;
                    notificationLabel.ForeColor = theme.SuccessNotificationForeColor;
                    break;
                case NotificationType.Info:
                default:
                    notificationLabel.BackColor = theme.InfoNotificationBackColor;
                    notificationLabel.ForeColor = theme.InfoNotificationForeColor;
                    break;
            }
        }
        private NotificationType GetWarningPanelTypeFromColor(Color backColor)
        {
            if (backColor.A == 180 && backColor.R > 150 && backColor.G < 100 && backColor.B < 100) return NotificationType.Error;
            return NotificationType.Warning;
        }
        private void SetAnimatedWarningColors(NotificationType type, ThemeColors? theme)
        {
            if (warningPanel == null || warningMessageLabel == null || theme == null) return;
            if (type == NotificationType.Error)
            {
                warningPanel.BackColor = Color.FromArgb(180, theme.ErrorNotificationBackColor.R, theme.ErrorNotificationBackColor.G, theme.ErrorNotificationBackColor.B);
                warningMessageLabel.ForeColor = theme.ErrorNotificationForeColor;
            }
            else
            {
                Color warningPanelBackColor = (theme.Name == "ダーク") ? Color.FromArgb(180, 180, 100, 30) : Color.FromArgb(180, 255, 150, 0);
                warningPanel.BackColor = warningPanelBackColor;
                warningMessageLabel.ForeColor = theme.WarningNotificationForeColor;
            }
        }

        private void ApplyThemeToControls(Control.ControlCollection controls, ThemeColors? theme)
        {
            if (theme == null) return;
            foreach (Control control in controls)
            {
                if (control == this.dragDropOverlayPanel) continue;

                control.ForeColor = theme.FormForeColor;
                control.Font = this.Font;

                if (control is Button button)
                {
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.Padding = new Padding(5);
                    if (button.Name == "compressButton")
                    {
                        button.BackColor = theme.AccentButtonBackColor;
                        button.ForeColor = theme.AccentButtonForeColor;
                        button.FlatAppearance.BorderColor = theme.AccentButtonBorderColor;
                        button.FlatAppearance.MouseOverBackColor = theme.AccentButtonMouseOverBackColor;
                        button.FlatAppearance.MouseDownBackColor = theme.AccentButtonMouseDownBackColor;
                    }
                    else
                    {
                        button.BackColor = theme.ButtonBackColor;
                        button.ForeColor = theme.ButtonForeColor;
                        button.FlatAppearance.BorderColor = theme.ButtonBorderColor;
                        button.FlatAppearance.MouseOverBackColor = theme.ButtonMouseOverBackColor;
                        button.FlatAppearance.MouseDownBackColor = theme.ButtonMouseDownBackColor;
                    }
                }
                else if (control is TextBox textBox)
                {
                    if (textBox.Name == "logTextBox") continue;
                    textBox.BackColor = theme.InputBackColor;
                    textBox.ForeColor = theme.InputForeColor;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = theme.InputBackColor;
                    listBox.ForeColor = theme.InputForeColor;
                    listBox.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = theme.InputBackColor;
                    comboBox.ForeColor = theme.InputForeColor;
                    comboBox.FlatStyle = FlatStyle.Flat;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.ForeColor = theme.LabelForeColor;
                }
                else if (control is RadioButton radioButton)
                {
                    radioButton.ForeColor = theme.LabelForeColor;
                }
                else if (control is Label label)
                {
                    if (label == notificationLabel || label == warningMessageLabel) continue;
                    label.ForeColor = theme.LabelForeColor;
                }
                else if (control is GroupBox groupBox)
                {
                    groupBox.ForeColor = theme.LabelForeColor;
                    ApplyThemeToControls(groupBox.Controls, theme);
                }
                else if (control is Panel panel)
                {
                    if (panel == warningPanel || panel == dragDropOverlayPanel) continue;
                    panel.BackColor = theme.FormBackColor;
                    ApplyThemeToControls(panel.Controls, theme);
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.BackColor = theme.InputBackColor;
                    numericUpDown.ForeColor = theme.InputForeColor;
                }
                else if (control is TreeView treeView)
                {
                    treeView.BackColor = theme.InputBackColor;
                    treeView.ForeColor = theme.InputForeColor;
                    treeView.BorderStyle = BorderStyle.FixedSingle;
                }
            }
        }

        private void InitializeSplitControls()
        {
            var enableSplitCheckbox = this.enableSplitZipCheckBox;
            var splitSizeLbl = this.splitSizeLabel;
            var splitSizeNum = this.splitSizeNumericUpDown;
            var splitUnitCb = this.splitUnitComboBox;

            if (enableSplitCheckbox != null && splitSizeLbl != null && splitSizeNum != null && splitUnitCb != null)
            {
                Action updateSplitControlsState = () =>
                {
                    if (enableSplitCheckbox == null || splitSizeLbl == null || splitSizeNum == null || splitUnitCb == null) return;
                    bool enable = enableSplitCheckbox.Checked && (currentZipMode == ZipOperationMode.CreateNew);
                    splitSizeLbl.Enabled = enable;
                    splitSizeNum.Enabled = enable;
                    splitUnitCb.Enabled = enable;
                };
                enableSplitCheckbox.CheckedChanged += (s, e) => updateSplitControlsState();
                if (splitUnitCb.Items.Count == 0)
                {
                    splitUnitCb.Items.AddRange(new object[] { "MB", "KB" });
                }
                if (splitUnitCb.SelectedIndex < 0 && splitUnitCb.Items.Count > 0) splitUnitCb.SelectedIndex = 0;
                updateSplitControlsState();
            }
        }

        private void InitializeListBoxToolTip()
        {
            listBoxToolTip = new ToolTip();
            var sflb = this.selectedFilesListBox;
            if (sflb != null)
            {
                sflb.MouseMove += selectedFilesListBox_MouseMove;
                sflb.MouseLeave += selectedFilesListBox_MouseLeave;
            }
        }

        private void selectedFilesListBox_MouseMove(object? sender, MouseEventArgs e)
        {
            ListBox? lb = sender as ListBox;
            if (lb == null || listBoxToolTip == null) return;
            int index = lb.IndexFromPoint(e.Location);

            if (index >= 0 && index < lb.Items.Count)
            {
                string displayNameInListBox = lb.Items[index].ToString();
                string rootNameToSearch = displayNameInListBox.EndsWith(" (フォルダ)")
                                        ? displayNameInListBox.Substring(0, displayNameInListBox.Length - " (フォルダ)".Length)
                                        : displayNameInListBox;

                CompressionItem? representativeItem = itemsToCompress.FirstOrDefault(ci =>
                    ci.RootNameInList == rootNameToSearch &&
                    ((ci.IsDirectoryItself && ci.PathInZip == rootNameToSearch) || (!ci.IsDirectoryItself && ci.PathInZip == rootNameToSearch))
                );

                if (representativeItem == null && displayNameInListBox.EndsWith(" (フォルダ)"))
                {
                    representativeItem = itemsToCompress.FirstOrDefault(ci => ci.RootNameInList == rootNameToSearch && ci.IsDirectoryItself && ci.PathInZip == rootNameToSearch);
                }


                if (representativeItem != null)
                {
                    string fullPath = representativeItem.FileSystemPath;
                    if (currentToolTipText != fullPath)
                    {
                        listBoxToolTip.SetToolTip(lb, fullPath);
                        currentToolTipText = fullPath;
                    }
                }
                else
                {
                    if (currentToolTipText != "")
                    {
                        listBoxToolTip.SetToolTip(lb, "");
                        currentToolTipText = "";
                    }
                }
            }
            else
            {
                if (currentToolTipText != "")
                {
                    listBoxToolTip.SetToolTip(lb, "");
                    currentToolTipText = "";
                }
            }
        }
        private void selectedFilesListBox_MouseLeave(object? sender, EventArgs e)
        {
            ListBox? lb = sender as ListBox;
            if (lb != null && listBoxToolTip != null)
            {
                listBoxToolTip.Hide(lb);
                currentToolTipText = "";
            }
        }

        private void InitializeCommentControls()
        {
            var enableCommentCheckbox = this.enableZipCommentCheckBox;
            var commentLabel = this.zipCommentLabel;
            var commentTextbox = this.zipCommentTextBox;

            if (enableCommentCheckbox != null && commentLabel != null && commentTextbox != null)
            {
                commentLabel.Enabled = enableCommentCheckbox.Checked;
                commentTextbox.Enabled = enableCommentCheckbox.Checked;
                enableCommentCheckbox.CheckedChanged += (sender, e) =>
                {
                    if (sender is CheckBox cb && commentLabel != null && commentTextbox != null)
                    {
                        bool isChecked = cb.Checked;
                        commentLabel.Enabled = isChecked;
                        commentTextbox.Enabled = isChecked;
                    }
                };
            }
        }

        private void InitializeOperationModeControls()
        {
            var createNewRadio = this.createNewZipRadioButton;
            var addToExistingRadio = this.addToExistingZipRadioButton;
            var selectExistingBtn = this.selectExistingZipButton;

            if (createNewRadio != null && addToExistingRadio != null && selectExistingBtn != null)
            {
                createNewRadio.CheckedChanged += OperationMode_CheckedChanged;
                addToExistingRadio.CheckedChanged += OperationMode_CheckedChanged;
                selectExistingBtn.Click += selectExistingZipButton_Click;

                if (Properties.Settings.Default.LastOperationMode == (int)ZipOperationMode.AddToExisting)
                {
                    currentZipMode = ZipOperationMode.AddToExisting;
                    addToExistingRadio.Checked = true;
                }
                else
                {
                    currentZipMode = ZipOperationMode.CreateNew;
                    createNewRadio.Checked = true;
                }
            }
            UpdateUIMode(currentZipMode);
        }
        private void OperationMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (this.createNewZipRadioButton != null && this.createNewZipRadioButton.Checked)
            {
                currentZipMode = ZipOperationMode.CreateNew;
            }
            else
            {
                currentZipMode = ZipOperationMode.AddToExisting;
            }
            UpdateUIMode(currentZipMode);
        }
        private void UpdateUIMode(ZipOperationMode mode)
        {
            bool isCreateNewMode = (mode == ZipOperationMode.CreateNew);

            if (this.existingZipFileLabel != null) this.existingZipFileLabel.Visible = !isCreateNewMode;
            if (this.existingZipFileTextBox != null) this.existingZipFileTextBox.Visible = !isCreateNewMode;
            if (this.selectExistingZipButton != null) { this.selectExistingZipButton.Visible = !isCreateNewMode; this.selectExistingZipButton.Enabled = !isCreateNewMode; }
            if (this.outputFolderTextBox != null) this.outputFolderTextBox.Enabled = isCreateNewMode;
            if (this.zipFileNameTextBox != null) this.zipFileNameTextBox.Enabled = isCreateNewMode;
            if (this.selectOutputFolderButton != null) this.selectOutputFolderButton.Enabled = isCreateNewMode;
            if (this.addTimestampToFileNameCheckBox != null) this.addTimestampToFileNameCheckBox.Enabled = isCreateNewMode;

            if (this.compressButton != null) this.compressButton.Text = isCreateNewMode ? "圧縮実行" : "ファイルを追加";
            if (isCreateNewMode) { existingZipPathForAdd = null; if (this.existingZipFileTextBox != null) this.existingZipFileTextBox.Clear(); }

            if (this.enableSplitZipCheckBox != null)
            {
                this.enableSplitZipCheckBox.Enabled = isCreateNewMode;
                if (!isCreateNewMode) this.enableSplitZipCheckBox.Checked = false;
            }
            bool splitControlsEnabled = isCreateNewMode && (this.enableSplitZipCheckBox?.Checked ?? false);
            if (this.splitSizeLabel != null) this.splitSizeLabel.Enabled = splitControlsEnabled;
            if (this.splitSizeNumericUpDown != null) this.splitSizeNumericUpDown.Enabled = splitControlsEnabled;
            if (this.splitUnitComboBox != null) this.splitUnitComboBox.Enabled = splitControlsEnabled;

            var treeView = this.existingZipContentsTreeView;
            if (treeView != null)
            {
                if (mode == ZipOperationMode.CreateNew)
                {
                    treeView.Nodes.Clear();
                    treeView.Visible = false;
                }
                else if (mode == ZipOperationMode.AddToExisting)
                {
                    if (!string.IsNullOrEmpty(this.existingZipFileTextBox?.Text))
                    {
                        PopulateZipContentsPreview(this.existingZipFileTextBox.Text);
                    }
                    else
                    {
                        treeView.Nodes.Clear();
                        treeView.Visible = false;
                    }
                }
            }
        }
        private void selectExistingZipButton_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "追加先のZIPファイルを選択";
                openFileDialog.Filter = "ZIPファイル (*.zip)|*.zip";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    existingZipPathForAdd = openFileDialog.FileName;
                    if (this.existingZipFileTextBox != null) this.existingZipFileTextBox.Text = existingZipPathForAdd;
                    AppendLog($"既存ZIP選択: {existingZipPathForAdd}", LogLevel.Info);
                    PopulateZipContentsPreview(existingZipPathForAdd);
                }
            }
        }

        private void PopulateZipContentsPreview(string zipFilePath)
        {
            var treeView = this.existingZipContentsTreeView;
            if (treeView == null) return;

            treeView.Nodes.Clear();

            if (string.IsNullOrEmpty(zipFilePath) || !File.Exists(zipFilePath))
            {
                treeView.Visible = false;
                return;
            }
            try
            {
                if (!Ionic.Zip.ZipFile.IsZipFile(zipFilePath, true))
                {
                    AppendLog($"プレビューエラー: {Path.GetFileName(zipFilePath)} は有効なZIPファイルではありません。", LogLevel.Warning);
                    ShowNotification($"選択されたファイルは有効なZIPファイル形式ではありません。", NotificationType.Warning);
                    treeView.Visible = false;
                    return;
                }
                using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(zipFilePath))
                {
                    AddZipEntriesToTreeView(zip.Entries, treeView.Nodes);
                }
                treeView.Visible = true;
            }
            catch (Ionic.Zip.ZipException zex)
            {
                AppendLog($"ZIPプレビューエラー ({Path.GetFileName(zipFilePath)}): {zex.Message}", LogLevel.Error);
                ShowNotification($"ZIPファイルの読み込みに失敗しました: {zex.Message}", NotificationType.Warning);
                treeView.Nodes.Add($"エラー: {zex.Message}");
                treeView.Visible = true;
            }
            catch (Exception ex)
            {
                AppendLog($"プレビュー処理中に予期せぬエラー ({Path.GetFileName(zipFilePath)}): {ex.Message}", LogLevel.Error);
                ShowNotification($"プレビューの表示中にエラーが発生しました。", NotificationType.Error);
                treeView.Visible = false;
            }
        }

        private void AddZipEntriesToTreeView(ICollection<Ionic.Zip.ZipEntry> entries, TreeNodeCollection rootNodes)
        {
            var directoryNodes = new Dictionary<string, TreeNode>();
            List<Ionic.Zip.ZipEntry> sortedEntries = entries
                .OrderBy(e => e.IsDirectory ? 0 : 1)
                .ThenBy(e => e.FileName.ToLowerInvariant())
                .ToList();

            foreach (var entry in sortedEntries)
            {
                string path = entry.FileName.Replace('\\', '/');
                string[] pathParts = path.TrimEnd('/').Split('/');
                TreeNodeCollection currentNodes = rootNodes;
                TreeNode? parentNode = null;

                for (int i = 0; i < (entry.IsDirectory ? pathParts.Length : pathParts.Length - 1); i++)
                {
                    string part = pathParts[i];
                    string currentPathKey = string.Join("/", pathParts.Take(i + 1));
                    if (!directoryNodes.TryGetValue(currentPathKey, out TreeNode? dirNode))
                    {
                        dirNode = new TreeNode(part);
                        directoryNodes[currentPathKey] = dirNode;
                        (parentNode?.Nodes ?? currentNodes).Add(dirNode);
                    }
                    parentNode = dirNode;
                    currentNodes = dirNode.Nodes;
                }
                if (!entry.IsDirectory)
                {
                    string fileName = pathParts.Last();
                    TreeNode fileNode = new TreeNode(fileName);
                    fileNode.ToolTipText = $"サイズ: {FormatBytes(entry.UncompressedSize)}, 更新日時: {entry.LastModified.ToLocalTime()}";
                    (parentNode?.Nodes ?? rootNodes).Add(fileNode);
                }
            }
        }


        private void InitializePasswordControls()
        {
            var enablePwdCheckbox = this.enablePasswordCheckBox;
            var pwdTextbox = this.passwordTextBox;
            var pwdLabel = this.passwordLabel;

            if (enablePwdCheckbox != null && pwdTextbox != null && pwdLabel != null)
            {
                pwdTextbox.Enabled = enablePwdCheckbox.Checked;
                pwdLabel.Enabled = enablePwdCheckbox.Checked;
                enablePwdCheckbox.CheckedChanged += (sender, e) =>
                {
                    if (sender is CheckBox cb && pwdTextbox != null && pwdLabel != null)
                    {
                        bool isChecked = cb.Checked;
                        pwdTextbox.Enabled = isChecked;
                        pwdLabel.Enabled = isChecked;
                        if (!isChecked) pwdTextbox.Clear();
                    }
                };
            }
        }
        private void InitializeCompressionWorker()
        {
            compressionWorker = new BackgroundWorker();
            compressionWorker.WorkerReportsProgress = true;
            compressionWorker.WorkerSupportsCancellation = true;
            compressionWorker.DoWork += CompressionWorker_DoWork;
            compressionWorker.ProgressChanged += CompressionWorker_ProgressChanged;
            compressionWorker.RunWorkerCompleted += CompressionWorker_RunWorkerCompleted;
        }
        private void InitializeNotificationSystem()
        {
            notificationLabel = new Label();
            notificationLabel.Visible = false;
            notificationLabel.Dock = DockStyle.Top;
            notificationLabel.TextAlign = ContentAlignment.MiddleCenter;
            notificationLabel.AutoSize = false;
            notificationLabel.Height = 30;
            notificationLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.Controls.Add(notificationLabel);
            notificationLabel.BringToFront();
            notificationTimer = new System.Windows.Forms.Timer();
            notificationTimer.Interval = 3000;
            notificationTimer.Tick += NotificationTimer_Tick;
        }
        private void InitializeAnimatedWarningSystem()
        {
            warningPanel = new Panel();
            warningPanel.Height = 60;
            warningPanelHiddenY = -warningPanel.Height;
            warningPanel.Location = new Point(0, warningPanelHiddenY);
            warningPanel.Width = this.ClientSize.Width;
            warningPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            warningMessageLabel = new Label();
            warningMessageLabel.Text = "";
            warningMessageLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            warningMessageLabel.TextAlign = ContentAlignment.MiddleCenter;
            warningMessageLabel.Dock = DockStyle.Fill;
            warningPanel.Controls.Add(warningMessageLabel);
            this.Controls.Add(warningPanel);
            warningPanel.BringToFront();
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 15;
            animationTimer.Tick += AnimationTimer_Tick;
            displayTimer = new System.Windows.Forms.Timer();
            displayTimer.Interval = 4000;
            displayTimer.Tick += DisplayTimer_Tick;
            this.SizeChanged += (s, e) => { if (warningPanel != null) warningPanel.Width = this.ClientSize.Width; };
        }
        private void NotificationTimer_Tick(object? sender, EventArgs e)
        {
            if (notificationLabel != null) notificationLabel.Visible = false;
            if (notificationTimer != null) notificationTimer.Stop();
        }
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (warningPanel == null || animationTimer == null || displayTimer == null) return;
            if (isWarningPanelDescending)
            {
                if (warningPanel.Location.Y < warningPanelTargetY)
                {
                    int newY = warningPanel.Location.Y + animationStep;
                    warningPanel.Location = new Point(0, Math.Min(newY, warningPanelTargetY));
                }
                else
                {
                    warningPanel.Location = new Point(0, warningPanelTargetY);
                    isWarningPanelDescending = false;
                    animationTimer.Stop();
                    displayTimer.Start();
                }
            }
            else if (isWarningPanelAscending)
            {
                if (warningPanel.Location.Y > warningPanelHiddenY)
                {
                    int newY = warningPanel.Location.Y - animationStep;
                    warningPanel.Location = new Point(0, Math.Max(newY, warningPanelHiddenY));
                }
                else
                {
                    warningPanel.Location = new Point(0, warningPanelHiddenY);
                    isWarningPanelAscending = false;
                    animationTimer.Stop();
                    warningPanel.Visible = false;
                }
            }
        }
        private void DisplayTimer_Tick(object? sender, EventArgs e)
        {
            if (displayTimer != null) displayTimer.Stop();
            isWarningPanelAscending = true;
            if (animationTimer != null) animationTimer.Start();
        }

        private void LoadSettings()
        {
            try
            {
                this.dragDropOverlayPanel.BackColor = Color.FromArgb(30, 53, 200, 180);

                if (this.outputFolderTextBox != null)
                {
                    string lastOutputFolder = Properties.Settings.Default.LastOutputFolder;
                    if (!string.IsNullOrEmpty(lastOutputFolder) && Directory.Exists(lastOutputFolder))
                    {
                        this.outputFolderTextBox.Text = lastOutputFolder;
                    }
                    else
                    {
                        // 保存されたパスが無効または空の場合、デスクトップをデフォルトに設定
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        this.outputFolderTextBox.Text = desktopPath;
                        AppendLog("出力先フォルダの保存値が無効だったため、デスクトップをデフォルトに設定しました。", LogLevel.Debug);
                    }
                }

                if (this.zipFileNameTextBox != null)
                {
                    string lastZipFileName = Properties.Settings.Default.LastZipFileName;
                    if (!string.IsNullOrEmpty(lastZipFileName) && lastZipFileName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
                    {
                        this.zipFileNameTextBox.Text = lastZipFileName;
                    }
                    else
                    {
                        this.zipFileNameTextBox.Text = "archive"; // 不正または空の場合のデフォルト名
                        AppendLog("前回保存されたZIPファイル名が無効または空だったため、デフォルト名を設定しました。", LogLevel.Debug);
                    }
                }

                if (this.compressionLevelComboBox != null)
                {
                    if (this.compressionLevelComboBox.Items.Count == 0)
                    {
                        this.compressionLevelComboBox.Items.AddRange(new object[] { "標準", "速度優先", "高圧縮" });
                    }
                    if (Properties.Settings.Default.LastCompressionLevelIndex >= 0 && Properties.Settings.Default.LastCompressionLevelIndex < this.compressionLevelComboBox.Items.Count)
                        this.compressionLevelComboBox.SelectedIndex = Properties.Settings.Default.LastCompressionLevelIndex;
                    else if (this.compressionLevelComboBox.Items.Count > 0)
                        this.compressionLevelComboBox.SelectedIndex = 0;
                }

                if (this.openOutputFolderCheckBox != null) this.openOutputFolderCheckBox.Checked = Properties.Settings.Default.OpenOutputFolder;
                if (this.recursiveAddCheckBox != null) this.recursiveAddCheckBox.Checked = Properties.Settings.Default.RecursiveAdd;
                if (this.enablePasswordCheckBox != null) this.enablePasswordCheckBox.Checked = Properties.Settings.Default.EnablePassword;
                if (this.passwordTextBox != null && this.passwordLabel != null && this.enablePasswordCheckBox != null) { this.passwordTextBox.Enabled = this.enablePasswordCheckBox.Checked; this.passwordLabel.Enabled = this.enablePasswordCheckBox.Checked; }

                if (Properties.Settings.Default.LastOperationMode == (int)ZipOperationMode.AddToExisting && this.addToExistingZipRadioButton != null)
                {
                    currentZipMode = ZipOperationMode.AddToExisting;
                    this.addToExistingZipRadioButton.Checked = true;
                }
                else if (this.createNewZipRadioButton != null)
                {
                    currentZipMode = ZipOperationMode.CreateNew;
                    this.createNewZipRadioButton.Checked = true;
                }

                if (this.existingZipFileTextBox != null) this.existingZipFileTextBox.Text = Properties.Settings.Default.LastExistingZipPath;
                if (currentZipMode == ZipOperationMode.AddToExisting) existingZipPathForAdd = Properties.Settings.Default.LastExistingZipPath;

                if (this.enableZipCommentCheckBox != null) this.enableZipCommentCheckBox.Checked = Properties.Settings.Default.EnableZipComment;
                if (this.zipCommentTextBox != null) this.zipCommentTextBox.Text = Properties.Settings.Default.LastZipComment;
                if (this.enableZipCommentCheckBox != null && this.zipCommentLabel != null && this.zipCommentTextBox != null) { this.zipCommentLabel.Enabled = this.enableZipCommentCheckBox.Checked; this.zipCommentTextBox.Enabled = this.enableZipCommentCheckBox.Checked; }

                if (this.enableSplitZipCheckBox != null) this.enableSplitZipCheckBox.Checked = Properties.Settings.Default.EnableSplitZip;
                if (this.splitSizeNumericUpDown != null && Properties.Settings.Default.SplitSizeValue >= this.splitSizeNumericUpDown.Minimum && Properties.Settings.Default.SplitSizeValue <= this.splitSizeNumericUpDown.Maximum) this.splitSizeNumericUpDown.Value = Properties.Settings.Default.SplitSizeValue;
                else if (this.splitSizeNumericUpDown != null) this.splitSizeNumericUpDown.Value = 100;

                if (this.splitUnitComboBox != null)
                {
                    if (this.splitUnitComboBox.Items.Count == 0)
                    {
                        this.splitUnitComboBox.Items.AddRange(new object[] { "MB", "KB" });
                    }
                    if (Properties.Settings.Default.SplitUnitIndex >= 0 && Properties.Settings.Default.SplitUnitIndex < this.splitUnitComboBox.Items.Count)
                        this.splitUnitComboBox.SelectedIndex = Properties.Settings.Default.SplitUnitIndex;
                    else if (this.splitUnitComboBox.Items.Count > 0)
                        this.splitUnitComboBox.SelectedIndex = 0;
                }

                string? savedThemeName = Properties.Settings.Default.CurrentThemeName;
                ThemeColors? themeToLoad = availableThemes.FirstOrDefault(t => t.Name == savedThemeName) ?? ThemeColors.LightTheme;

                if (this.themeComboBox != null)
                {
                    var foundTheme = availableThemes.FirstOrDefault(t => t.Name == savedThemeName);
                    if (foundTheme != null) this.themeComboBox.SelectedItem = foundTheme;
                    else if (availableThemes.Any()) this.themeComboBox.SelectedItem = ThemeColors.LightTheme;
                }
                else if (themeToLoad != null) { ApplyTheme(themeToLoad); }

                if (this.addTimestampToFileNameCheckBox != null) this.addTimestampToFileNameCheckBox.Checked = Properties.Settings.Default.AddTimestampToZipFileName;

                if (this.logLevelComboBox != null)
                {
                    string savedLogLevel = Properties.Settings.Default.LoggingLevel;
                    if (!string.IsNullOrEmpty(savedLogLevel) && this.logLevelComboBox.Items.Contains(savedLogLevel))
                    {
                        this.logLevelComboBox.SelectedItem = savedLogLevel;
                    }
                    else if (this.logLevelComboBox.Items.Contains("Info"))
                    {
                        this.logLevelComboBox.SelectedItem = "Info";
                    }
                    else if (this.logLevelComboBox.Items.Count > 0)
                    {
                        this.logLevelComboBox.SelectedIndex = 0;
                    }
                }
                UpdateCurrentLogLevel();
                UpdateUIMode(currentZipMode);
            }
            catch (Exception ex) { AppendLog("設定の読み込みに失敗: " + ex.Message, LogLevel.Error); if (availableThemes.Any()) ApplyTheme(ThemeColors.LightTheme); }
        }
        private void SaveSettings()
        {
            try
            {
                if (this.outputFolderTextBox != null) Properties.Settings.Default.LastOutputFolder = this.outputFolderTextBox.Text;
                if (this.zipFileNameTextBox != null) Properties.Settings.Default.LastZipFileName = this.zipFileNameTextBox.Text;
                if (this.compressionLevelComboBox != null) Properties.Settings.Default.LastCompressionLevelIndex = this.compressionLevelComboBox.SelectedIndex;
                if (this.openOutputFolderCheckBox != null) Properties.Settings.Default.OpenOutputFolder = this.openOutputFolderCheckBox.Checked;
                if (this.recursiveAddCheckBox != null) Properties.Settings.Default.RecursiveAdd = this.recursiveAddCheckBox.Checked;
                if (this.enablePasswordCheckBox != null) Properties.Settings.Default.EnablePassword = this.enablePasswordCheckBox.Checked;
                Properties.Settings.Default.LastOperationMode = (int)currentZipMode;
                Properties.Settings.Default.LastExistingZipPath = existingZipPathForAdd ?? "";
                if (this.enableZipCommentCheckBox != null) Properties.Settings.Default.EnableZipComment = this.enableZipCommentCheckBox.Checked;
                if (this.zipCommentTextBox != null) Properties.Settings.Default.LastZipComment = this.zipCommentTextBox.Text;
                if (this.enableSplitZipCheckBox != null) Properties.Settings.Default.EnableSplitZip = this.enableSplitZipCheckBox.Checked;
                if (this.splitSizeNumericUpDown != null) Properties.Settings.Default.SplitSizeValue = (int)this.splitSizeNumericUpDown.Value;
                if (this.splitUnitComboBox != null) Properties.Settings.Default.SplitUnitIndex = this.splitUnitComboBox.SelectedIndex;
                if (this.themeComboBox != null && this.themeComboBox.SelectedItem is ThemeColors selectedTheme && selectedTheme.Name != null) Properties.Settings.Default.CurrentThemeName = selectedTheme.Name;
                if (this.addTimestampToFileNameCheckBox != null) Properties.Settings.Default.AddTimestampToZipFileName = this.addTimestampToFileNameCheckBox.Checked;
                if (this.logLevelComboBox != null && this.logLevelComboBox.SelectedItem != null) Properties.Settings.Default.LoggingLevel = this.logLevelComboBox.SelectedItem.ToString();

                Properties.Settings.Default.Save();
            }
            catch (Exception ex) { AppendLog("設定の保存に失敗: " + ex.Message, LogLevel.Error); }
        }
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (compressionWorker != null && compressionWorker.IsBusy)
            {
                var result = MessageBox.Show("圧縮処理が実行中です。本当に終了しますか？\n終了すると処理はキャンセルされます。", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    AppendLog("アプリケーション終了のため、圧縮処理のキャンセルを試みます。", LogLevel.Info);
                    compressionWorker.CancelAsync();
                }
            }
            AppendLog("アプリケーションを終了します。", LogLevel.Info);
            SaveSettings();
        }
        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.O)
            {
                if (!e.Shift && this.selectFilesButton != null && this.selectFilesButton.Enabled) { this.selectFilesButton.PerformClick(); e.Handled = true; }
                else if (e.Shift && this.selectOutputFolderButton != null && this.selectOutputFolderButton.Enabled) { this.selectOutputFolderButton.PerformClick(); e.Handled = true; }
            }
            else if (e.Control && e.KeyCode == Keys.Enter && this.compressButton != null && this.compressButton.Enabled) { this.compressButton.PerformClick(); e.Handled = true; }
            else if (e.Control && e.Shift && e.KeyCode == Keys.Delete && this.clearFileListButton != null && this.clearFileListButton.Enabled) { this.clearFileListButton.PerformClick(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.P && this.enablePasswordCheckBox != null && this.enablePasswordCheckBox.Enabled)
            {
                this.enablePasswordCheckBox.Checked = !this.enablePasswordCheckBox.Checked; e.Handled = true;
            }
        }
        private void selectedFilesListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            var sflb = sender as ListBox;
            if (sflb != null && this.removeSelectedFileButton != null && e.KeyCode == Keys.Delete && sflb.SelectedItems.Count > 0)
            {
                if (this.removeSelectedFileButton.Enabled)
                {
                    removeSelectedFileButton_Click(this.removeSelectedFileButton, EventArgs.Empty);
                }
                e.Handled = true;
            }
        }
        private void InitializeDragDrop()
        {
            var sflb = this.selectedFilesListBox;
            if (sflb != null)
            {
                sflb.AllowDrop = true;
                sflb.DragEnter += new DragEventHandler(selectedFilesListBox_DragEnter);
                sflb.DragDrop += new DragEventHandler(selectedFilesListBox_DragDrop);
                sflb.DragLeave += new EventHandler(selectedFilesListBox_DragLeave);
                this.originalListBoxBackColor = sflb.BackColor;
                this.originalListBoxBorderStyle = sflb.BorderStyle;
            }
        }
        private void selectedFilesListBox_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                ListBox lb = sender as ListBox;
                if (lb != null)
                {
                    ThemeColors? currentTheme = (this.themeComboBox?.SelectedItem as ThemeColors) ?? ThemeColors.LightTheme;
                    if (currentTheme != null)
                    {
                        lb.BackColor = ControlPaint.Light(currentTheme.InputBackColor, 0.3f);
                        lb.BorderStyle = BorderStyle.Fixed3D;
                    }
                }
            }
            else { e.Effect = DragDropEffects.None; }
        }
        private void selectedFilesListBox_DragLeave(object? sender, EventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb != null)
            {
                ThemeColors? currentTheme = (this.themeComboBox?.SelectedItem as ThemeColors) ?? ThemeColors.LightTheme;
                if (currentTheme != null)
                {
                    lb.BackColor = currentTheme.InputBackColor;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                }
                else
                {
                    lb.BackColor = this.originalListBoxBackColor;
                    lb.BorderStyle = this.originalListBoxBorderStyle;
                }
            }
        }
        private void selectedFilesListBox_DragDrop(object? sender, DragEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb != null)
            {
                ThemeColors? currentTheme = (this.themeComboBox?.SelectedItem as ThemeColors) ?? ThemeColors.LightTheme;
                if (currentTheme != null)
                {
                    lb.BackColor = currentTheme.InputBackColor;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                }
                else
                {
                    lb.BackColor = this.originalListBoxBackColor;
                    lb.BorderStyle = this.originalListBoxBorderStyle;
                }
            }
            if (e.Data != null && e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems)
            {
                ProcessDroppedItems(droppedItems);
            }
        }
        private void ProcessDroppedItems(string[]? droppedItems)
        {
            if (droppedItems == null) return;
            AppendLog($"ドラッグ＆ドロップ操作: {droppedItems.Length} アイテム", LogLevel.Info);
            bool isRecursive = (this.recursiveAddCheckBox?.Checked) ?? Properties.Settings.Default.RecursiveAdd;
            bool directoryDroppedWithoutRecursiveSupportMessageShown = false;

            foreach (string itemPath in droppedItems)
            {
                if (File.Exists(itemPath))
                {
                    AddFileToList(itemPath);
                }
                else if (Directory.Exists(itemPath))
                {
                    string directoryName = Path.GetFileName(itemPath);
                    AppendLog($"フォルダ処理開始: {itemPath} (再帰: {isRecursive})", LogLevel.Debug);

                    if (this.selectedFilesListBox != null && !this.selectedFilesListBox.Items.Contains(directoryName + " (フォルダ)"))
                    {
                        this.selectedFilesListBox.Items.Add(directoryName + " (フォルダ)");
                    }

                    itemsToCompress.Add(new CompressionItem
                    {
                        FileSystemPath = itemPath,
                        PathInZip = directoryName,
                        IsDirectoryItself = true,
                        RootNameInList = directoryName
                    });

                    if (isRecursive)
                    {
                        AddDirectoryItemsRecursively(itemPath, directoryName, directoryName);
                    }
                    else
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(itemPath);
                        foreach (FileInfo file in dirInfo.GetFiles())
                        {
                            itemsToCompress.Add(new CompressionItem
                            {
                                FileSystemPath = file.FullName,
                                PathInZip = Path.Combine(directoryName, file.Name),
                                IsDirectoryItself = false,
                                RootNameInList = directoryName
                            });
                            AppendLog($"  ファイル追加(フォルダ内/非再帰): {file.FullName}", LogLevel.Debug);
                        }
                        bool subItemExists = false;
                        foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
                        {
                            itemsToCompress.Add(new CompressionItem
                            {
                                FileSystemPath = subDir.FullName,
                                PathInZip = Path.Combine(directoryName, subDir.Name),
                                IsDirectoryItself = true,
                                RootNameInList = directoryName
                            });
                            AppendLog($"  サブフォルダ構造追加(非再帰): {subDir.FullName}", LogLevel.Debug);
                            if (Directory.GetFiles(subDir.FullName).Length > 0 || Directory.GetDirectories(subDir.FullName).Length > 0)
                            {
                                subItemExists = true;
                            }
                        }
                        if (subItemExists) directoryDroppedWithoutRecursiveSupportMessageShown = true;
                    }
                }
                else
                {
                    AppendLog($"ドラッグ＆ドロップエラー: パスが見つかりません - {itemPath}", LogLevel.Warning);
                }
            }
            if (directoryDroppedWithoutRecursiveSupportMessageShown)
            {
                ShowNotification("フォルダが追加されましたが、再帰オプションが無効のため直下のファイルとサブフォルダ構造のみが対象です。", NotificationType.Info);
            }
        }

        private void AddDirectoryItemsRecursively(string directoryPath, string basePathInZip, string rootNameInList)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
            try
            {
                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    itemsToCompress.Add(new CompressionItem
                    {
                        FileSystemPath = file.FullName,
                        PathInZip = Path.Combine(basePathInZip, file.Name),
                        IsDirectoryItself = false,
                        RootNameInList = rootNameInList
                    });
                    AppendLog($"  ファイル追加(再帰): {file.FullName}", LogLevel.Debug);
                }

                foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
                {
                    string subDirectoryPathInZip = Path.Combine(basePathInZip, subDir.Name);
                    itemsToCompress.Add(new CompressionItem
                    {
                        FileSystemPath = subDir.FullName,
                        PathInZip = subDirectoryPathInZip,
                        IsDirectoryItself = true,
                        RootNameInList = rootNameInList
                    });
                    AppendLog($"  サブフォルダ構造追加(再帰): {subDir.FullName}", LogLevel.Debug);
                    AddDirectoryItemsRecursively(subDir.FullName, subDirectoryPathInZip, rootNameInList);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"フォルダアクセスエラー ({Path.GetFileName(directoryPath)}): {ex.Message}", NotificationType.Warning);
                AppendLog($"フォルダアクセスエラー(再帰中): {directoryPath} - {ex.Message}", LogLevel.Error);
            }
        }

        private void AddFileToList(string filePath)
        {
            if (!itemsToCompress.Any(item => item.FileSystemPath == filePath && !item.IsDirectoryItself))
            {
                string fileName = Path.GetFileName(filePath);
                CompressionItem newItem = new CompressionItem
                {
                    FileSystemPath = filePath,
                    PathInZip = fileName,
                    IsDirectoryItself = false,
                    RootNameInList = fileName
                };
                itemsToCompress.Add(newItem);

                if (this.selectedFilesListBox != null)
                {
                    this.selectedFilesListBox.Items.Add(fileName);
                }
                AppendLog($"ファイル追加: {filePath} (ZIP内パス: {fileName})", LogLevel.Debug);
            }
        }

        private void selectFilesButton_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true; openFileDialog.Title = "圧縮するファイルを選択してください"; openFileDialog.Filter = "すべてのファイル (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    AppendLog($"ファイル選択ダイアログ: {openFileDialog.FileNames.Length} ファイル選択", LogLevel.Info);
                    foreach (string fileName in openFileDialog.FileNames) AddFileToList(fileName);
                }
            }
        }
        private void selectOutputFolderButton_Click(object? sender, EventArgs e)
        {
            if (this.outputFolderTextBox == null) return;
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "ZIPファイルの保存先フォルダを選択してください";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK) this.outputFolderTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }
        private void removeSelectedFileButton_Click(object? sender, EventArgs e)
        {
            var sflb = this.selectedFilesListBox;
            if (sflb == null || sflb.SelectedItems.Count == 0) return;

            List<string> selectedRootNamesToRemove = new List<string>();
            foreach (object selectedItemObj in sflb.SelectedItems)
            {
                string displayName = selectedItemObj.ToString();
                string rootName = displayName.EndsWith(" (フォルダ)") ? displayName.Substring(0, displayName.Length - " (フォルダ)".Length) : displayName;
                if (!selectedRootNamesToRemove.Contains(rootName))
                {
                    selectedRootNamesToRemove.Add(rootName);
                }
            }

            if (!selectedRootNamesToRemove.Any()) return;
            int itemsRemovedCount = itemsToCompress.RemoveAll(item => selectedRootNamesToRemove.Contains(item.RootNameInList));

            for (int i = sflb.SelectedIndices.Count - 1; i >= 0; i--)
            {
                sflb.Items.RemoveAt(sflb.SelectedIndices[i]);
            }
            AppendLog($"{selectedRootNamesToRemove.Count} 個のルートアイテム（関連ファイル/フォルダ含む計 {itemsRemovedCount} items）を選択リストから削除しました。", LogLevel.Debug);
        }
        private void clearFileListButton_Click(object? sender, EventArgs e)
        {
            if (this.selectedFilesListBox != null) this.selectedFilesListBox.Items.Clear();
            itemsToCompress.Clear();
            AppendLog("ファイルリストをクリアしました。", LogLevel.Debug);
        }

        private void cancelCompressionButton_Click(object? sender, EventArgs e)
        {
            if (compressionWorker != null && compressionWorker.IsBusy)
            {
                AppendLog("圧縮処理のキャンセルを要求しました。", LogLevel.Info);
                compressionWorker.CancelAsync();
                if (sender is Button btn) btn.Enabled = false;
            }
        }

        private void compressButton_Click(object? sender, EventArgs e)
        {
            if (compressionWorker == null) { AppendLog("compressionWorkerが初期化されていません。", LogLevel.Error); return; }
            if (compressionWorker.IsBusy) { ShowNotification("圧縮処理が実行中です。", NotificationType.Info); return; }
            if (itemsToCompress.Count == 0) { ShowNotification((currentZipMode == ZipOperationMode.CreateNew ? "圧縮" : "追加") + "するファイルが選択されていません。", NotificationType.Warning); return; }

            string outputZipPathValue; string? effectiveOutputFolder; string effectiveZipFileName;

            if (currentZipMode == ZipOperationMode.AddToExisting)
            {
                if (string.IsNullOrWhiteSpace(existingZipPathForAdd) || !File.Exists(existingZipPathForAdd)) { ShowNotification("追加先の有効なZIPファイルが選択されていません。", NotificationType.Warning); return; }
                try
                {
                    if (!Ionic.Zip.ZipFile.IsZipFile(existingZipPathForAdd, true))
                    {
                        ShowNotification("選択されたファイルは有効なZIPファイル形式ではありません。", NotificationType.Warning); return;
                    }
                }
                catch (Exception ex)
                {
                    ShowNotification($"既存ZIPファイルの確認中にエラー: {ex.Message}", NotificationType.Warning);
                    AppendLog($"既存ZIP確認エラー: {existingZipPathForAdd} - {ex.Message}", LogLevel.Error);
                    return;
                }
                outputZipPathValue = existingZipPathForAdd;
                effectiveOutputFolder = Path.GetDirectoryName(outputZipPathValue);
                effectiveZipFileName = Path.GetFileName(outputZipPathValue);
            }
            else
            {
                if (this.outputFolderTextBox == null || string.IsNullOrWhiteSpace(this.outputFolderTextBox.Text)) { ShowNotification("保存先フォルダが選択されていません。", NotificationType.Warning); return; }
                if (!Directory.Exists(this.outputFolderTextBox.Text))
                {
                    try { Directory.CreateDirectory(this.outputFolderTextBox.Text); AppendLog($"出力先フォルダを作成: {this.outputFolderTextBox.Text}", LogLevel.Info); }
                    catch (Exception ex) { ShowNotification($"出力先フォルダの作成失敗: {ex.Message}", NotificationType.Error); AppendLog($"出力フォルダ作成エラー: {ex.Message}", LogLevel.Error); return; }
                }

                if (this.zipFileNameTextBox == null) { ShowNotification("ZIPファイル名テキストボックスが見つかりません。", NotificationType.Error); return; }
                string baseZipFileName = this.zipFileNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(baseZipFileName)) { ShowNotification("ZIPファイル名を入力してください。", NotificationType.Warning); return; }
                if (baseZipFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { ShowNotification("ZIPファイル名に使用できない文字が含まれています。", NotificationType.Warning); return; }


                if (this.addTimestampToFileNameCheckBox != null && this.addTimestampToFileNameCheckBox.Checked)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseZipFileName);
                    string fileExt = Path.GetExtension(baseZipFileName);
                    baseZipFileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                }

                if (!baseZipFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    baseZipFileName = Path.GetFileNameWithoutExtension(baseZipFileName) + ".zip";
                }
                effectiveZipFileName = baseZipFileName;
                outputZipPathValue = Path.Combine(this.outputFolderTextBox.Text, effectiveZipFileName);
                effectiveOutputFolder = this.outputFolderTextBox.Text;

                if (File.Exists(outputZipPathValue))
                {
                    DialogResult userChoice = MessageBox.Show($"ファイル '{effectiveZipFileName}' は既に存在します。\n\nはい: 上書きします\nいいえ: 新しい名前で保存します\nキャンセル: 処理を中止します", "ファイル名の競合", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (userChoice == DialogResult.No) { outputZipPathValue = GetUniqueFileName(this.outputFolderTextBox.Text, effectiveZipFileName); effectiveZipFileName = Path.GetFileName(outputZipPathValue); }
                    else if (userChoice == DialogResult.Cancel) { ShowNotification("圧縮処理をキャンセルしました。", NotificationType.Info); return; }
                }
            }
            AppendLog($"圧縮処理開始ボタンクリック。モード: {currentZipMode}", LogLevel.Info);
            if (currentZipMode == ZipOperationMode.CreateNew) AppendLog($"新規ZIP作成: {outputZipPathValue}", LogLevel.Info); else AppendLog($"既存ZIPに追加: {existingZipPathForAdd}", LogLevel.Info);

            Ionic.Zlib.CompressionLevel ionicLevel = Ionic.Zlib.CompressionLevel.Default;
            if (this.compressionLevelComboBox != null && this.compressionLevelComboBox.SelectedItem != null) { switch (this.compressionLevelComboBox.SelectedItem.ToString()) { case "速度優先": ionicLevel = Ionic.Zlib.CompressionLevel.BestSpeed; break; case "高圧縮": ionicLevel = Ionic.Zlib.CompressionLevel.BestCompression; break; } }
            AppendLog($"圧縮レベル: {(this.compressionLevelComboBox?.SelectedItem?.ToString() ?? "デフォルト")}", LogLevel.Debug);

            bool enablePassword = false; string? password = null;
            if (this.enablePasswordCheckBox != null && this.enablePasswordCheckBox.Checked) { if (this.passwordTextBox != null && !string.IsNullOrWhiteSpace(this.passwordTextBox.Text)) { enablePassword = true; password = this.passwordTextBox.Text; } else { ShowNotification("パスワードが設定されていません。", NotificationType.Warning); return; } }
            AppendLog($"パスワード保護: {enablePassword}", LogLevel.Debug);

            bool enableComment = false; string? zipComment = null;
            if (this.enableZipCommentCheckBox != null && this.enableZipCommentCheckBox.Checked) { if (this.zipCommentTextBox != null) { enableComment = true; zipComment = this.zipCommentTextBox.Text; } }
            AppendLog($"ZIPコメント: {enableComment}", LogLevel.Debug);

            long splitSizeInBytes = 0; bool enableSplit = false;
            if (currentZipMode == ZipOperationMode.CreateNew && this.enableSplitZipCheckBox != null && this.enableSplitZipCheckBox.Checked)
            {
                enableSplit = true;
                if (this.splitSizeNumericUpDown != null && this.splitUnitComboBox != null && this.splitUnitComboBox.SelectedItem != null)
                {
                    long sizeValue = (long)this.splitSizeNumericUpDown.Value; string? unit = this.splitUnitComboBox.SelectedItem.ToString();
                    if (unit == "MB") splitSizeInBytes = sizeValue * 1024 * 1024; else if (unit == "KB") splitSizeInBytes = sizeValue * 1024;
                    if (splitSizeInBytes <= 0) { enableSplit = false; splitSizeInBytes = 0; AppendLog("分割サイズが無効なため、分割しません。", LogLevel.Warning); }
                    else AppendLog($"ZIP分割有効。サイズ: {sizeValue} {unit} ({splitSizeInBytes} bytes)", LogLevel.Debug);
                }
                else enableSplit = false;
            }

            if (this.compressionProgressBar != null) { this.compressionProgressBar.Value = 0; this.compressionProgressBar.Visible = true; this.compressionProgressBar.Style = ProgressBarStyle.Continuous; }
            if (this.compressionStatsLabel != null) { this.compressionStatsLabel.Text = "準備中..."; this.compressionStatsLabel.Visible = true; }

            SetUIEnabledState(false);

            long originalTotalSizeBytes = 0;
            if (itemsToCompress != null)
            {
                foreach (CompressionItem item in itemsToCompress)
                {
                    if (!item.IsDirectoryItself && File.Exists(item.FileSystemPath))
                    {
                        try { originalTotalSizeBytes += new FileInfo(item.FileSystemPath).Length; }
                        catch (Exception ex) { AppendLog($"サイズ取得エラー(事前): {item.FileSystemPath} - {ex.Message}", LogLevel.Warning); }
                    }
                }
            }

            CompressionArguments args = new CompressionArguments
            {
                OperationMode = currentZipMode,
                ItemsToCompress = new List<CompressionItem>(itemsToCompress),
                OutputZipPath = outputZipPathValue,
                LevelIonic = ionicLevel,
                EnablePassword = enablePassword,
                Password = password,
                ExistingZipPath = (currentZipMode == ZipOperationMode.AddToExisting) ? existingZipPathForAdd : null,
                EnableComment = enableComment,
                Comment = zipComment,
                EnableSplit = enableSplit,
                SplitSizeInBytes = splitSizeInBytes,
                OriginalTotalSize = originalTotalSizeBytes,
                EffectiveOutputFolder = effectiveOutputFolder,
                OutputZipFileName = effectiveZipFileName
            };
            argsPassedToWorker = args;
            compressionWorker.RunWorkerAsync(args);
        }
        private string GetUniqueFileName(string folderPath, string originalFileName)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName); string fileExt = Path.GetExtension(originalFileName); string newFileName = originalFileName; string fullPath = Path.Combine(folderPath, newFileName); int count = 1;
            while (File.Exists(fullPath)) { newFileName = $"{fileNameWithoutExt} ({count}){fileExt}"; fullPath = Path.Combine(folderPath, newFileName); count++; }
            return fullPath;
        }
        private void SetUIEnabledState(bool enabled)
        {
            var controlNames = new List<string> {
                "selectFilesButton", "selectOutputFolderButton", "compressButton", "selectedFilesListBox",
                "zipFileNameTextBox", "outputFolderTextBox", "removeSelectedFileButton", "clearFileListButton",
                "compressionLevelComboBox", "openOutputFolderCheckBox", "recursiveAddCheckBox",
                "enablePasswordCheckBox", "passwordTextBox", "operationModeGroupBox",
                "selectExistingZipButton",
                "enableZipCommentCheckBox", "zipCommentTextBox", "enableSplitZipCheckBox",
                "splitSizeNumericUpDown", "splitUnitComboBox", "themeComboBox",
                "addTimestampToFileNameCheckBox", "logLevelComboBox"
            };

            foreach (string name in controlNames)
            {
                var ctrl = this.Controls.Find(name, true).FirstOrDefault();
                if (ctrl != null) ctrl.Enabled = enabled;
            }
            if (this.cancelCompressionButton != null)
            {
                this.cancelCompressionButton.Enabled = !enabled;
                this.cancelCompressionButton.Visible = !enabled;
            }


            if (enabled) { UpdateUIMode(currentZipMode); }
            else
            {
                if (this.passwordTextBox != null) this.passwordTextBox.Enabled = false;
                if (this.zipCommentTextBox != null) this.zipCommentTextBox.Enabled = false;
                if (this.splitSizeNumericUpDown != null) this.splitSizeNumericUpDown.Enabled = false;
                if (this.splitUnitComboBox != null) this.splitUnitComboBox.Enabled = false;
                if (this.selectExistingZipButton != null) this.selectExistingZipButton.Enabled = false;
                if (this.existingZipFileTextBox != null) this.existingZipFileTextBox.Enabled = false;
                if (this.addTimestampToFileNameCheckBox != null) this.addTimestampToFileNameCheckBox.Enabled = false;
                if (this.logLevelComboBox != null) this.logLevelComboBox.Enabled = false;

            }
        }
        private class CompressionArguments
        {
            public ZipOperationMode OperationMode { get; set; }
            public List<CompressionItem>? ItemsToCompress { get; set; }
            public string? OutputZipPath { get; set; }
            public Ionic.Zlib.CompressionLevel LevelIonic { get; set; }
            public bool EnablePassword { get; set; }
            public string? Password { get; set; }
            public string? ExistingZipPath { get; set; }
            public bool EnableComment { get; set; }
            public string? Comment { get; set; }
            public bool EnableSplit { get; set; }
            public long SplitSizeInBytes { get; set; }
            public long OriginalTotalSize { get; set; }
            public string? EffectiveOutputFolder { get; set; }
            public string? OutputZipFileName { get; set; }
        }

        private CompressionItem? currentProcessingItemForWorker = null;

        private void CompressionWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            BackgroundWorker? worker = sender as BackgroundWorker;
            CompressionArguments? args = e.Argument as CompressionArguments;
            currentProcessingItemForWorker = null;

            if (worker == null || args == null || args.ItemsToCompress == null || string.IsNullOrEmpty(args.OutputZipPath))
            {
                e.Result = new ArgumentNullException("Worker, Arguments, ItemsToCompress, or OutputZipPath is invalid.");
                return;
            }

            if (worker.CancellationPending) { e.Cancel = true; e.Result = "CancelledByUser"; return; }
            worker.ReportProgress(0, "圧縮準備中...");

            try
            {
                using (Ionic.Zip.ZipFile zip = (args.OperationMode == ZipOperationMode.CreateNew) ?
                                                new Ionic.Zip.ZipFile(System.Text.Encoding.GetEncoding("shift_jis")) :
                                                Ionic.Zip.ZipFile.Read(args.ExistingZipPath ?? throw new ArgumentNullException("ExistingZipPath")))
                {
                    zip.UseZip64WhenSaving = Ionic.Zip.Zip64Option.AsNecessary;
                    if (args.OperationMode == ZipOperationMode.CreateNew)
                    {
                        worker.ReportProgress(0, $"新規ZIP '{Path.GetFileName(args.OutputZipPath)}' 作成準備...");
                    }
                    else
                    {
                        worker.ReportProgress(0, $"既存ZIP '{Path.GetFileName(args.ExistingZipPath)}' への追加準備...");
                    }

                    if (args.EnablePassword && !string.IsNullOrEmpty(args.Password)) { zip.Password = args.Password; zip.Encryption = EncryptionAlgorithm.WinZipAes256; }
                    zip.CompressionLevel = args.LevelIonic;
                    if (args.EnableComment && args.Comment != null) zip.Comment = args.Comment;

                    if (args.OperationMode == ZipOperationMode.CreateNew && args.EnableSplit && args.SplitSizeInBytes > 0)
                    {
                        if (args.SplitSizeInBytes > Int32.MaxValue) { zip.MaxOutputSegmentSize = Int32.MaxValue; worker.ReportProgress(0, "警告: 分割サイズが大きすぎるため、Int32.MaxValueが使用されます。"); }
                        else if (args.SplitSizeInBytes < 65536 && args.SplitSizeInBytes > 0) { zip.MaxOutputSegmentSize = 65536; worker.ReportProgress(0, "警告: 分割サイズが小さすぎるため、64KBが使用されます。"); }
                        else if (args.SplitSizeInBytes > 0) { zip.MaxOutputSegmentSize = (int)args.SplitSizeInBytes; }
                    }

                    bool saveProgressStarted = false;
                    zip.SaveProgress += (s, progressArgs) =>
                    {
                        if (worker.CancellationPending) { progressArgs.Cancel = true; return; }

                        if (!saveProgressStarted)
                        {
                            worker.ReportProgress(0, (args.OperationMode == ZipOperationMode.CreateNew ? "ZIPファイル保存中..." : "既存ZIPへ追加保存中..."));
                            saveProgressStarted = true;
                        }

                        string currentEntryName = progressArgs.CurrentEntry?.FileName ?? "N/A";
                        string progressMessageSuffix = $"({progressArgs.EntriesSaved}/{progressArgs.EntriesTotal}): {currentEntryName}";

                        if (progressArgs.EventType == ZipProgressEventType.Saving_EntryBytesRead)
                        {
                            if (progressArgs.TotalBytesToTransfer > 0)
                            {
                                int percentage = (int)((progressArgs.BytesTransferred * 100) / progressArgs.TotalBytesToTransfer);
                                worker.ReportProgress(percentage, $"保存中 {progressMessageSuffix}");
                            }
                        }
                        else if (progressArgs.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
                        {
                            int percentage = progressArgs.EntriesTotal > 0 ? (progressArgs.EntriesSaved * 100 / progressArgs.EntriesTotal) : 0;
                            worker.ReportProgress(percentage, $"エントリ処理完了 {progressMessageSuffix}");
                        }
                        else if (progressArgs.EventType == ZipProgressEventType.Saving_Completed)
                        {
                            worker.ReportProgress(100, (args.OperationMode == ZipOperationMode.CreateNew ? "新規ZIP保存完了。" : "既存ZIPへの追加保存完了。"));
                        }
                        // Saving_BeforeWriteEntry や他のイベントタイプも必要に応じて利用可能
                    };

                    worker.ReportProgress(0, "ファイルリスト処理中...");
                    HashSet<string> createdDirectoriesInZip = new HashSet<string>();
                    if (args.OperationMode == ZipOperationMode.AddToExisting)
                    {
                        foreach (ZipEntry ze in zip.Entries) { if (ze.IsDirectory && !string.IsNullOrEmpty(ze.FileName)) createdDirectoriesInZip.Add(ze.FileName.TrimEnd('/')); }
                    }

                    int itemsProcessedForList = 0;
                    int totalItemsInList = args.ItemsToCompress.Count;

                    foreach (CompressionItem item in args.ItemsToCompress)
                    {
                        currentProcessingItemForWorker = item;
                        if (worker.CancellationPending) { e.Cancel = true; e.Result = "CancelledByUser"; return; }

                        itemsProcessedForList++;
                        int listProcessingPercentage = (totalItemsInList > 0) ? (itemsProcessedForList * 100 / totalItemsInList) : 0;
                        worker.ReportProgress(listProcessingPercentage, $"ファイルリスト処理: {itemsProcessedForList}/{totalItemsInList} ({Path.GetFileName(item.FileSystemPath)})");

                        if (!item.IsDirectoryItself && File.Exists(item.FileSystemPath))
                        {
                            if (args.OperationMode == ZipOperationMode.AddToExisting && zip.ContainsEntry(item.PathInZip))
                            {
                                zip.RemoveEntry(item.PathInZip); // 上書きのため既存を削除
                            }
                            ZipEntry entry = zip.AddFile(item.FileSystemPath, "");
                            entry.FileName = item.PathInZip;
                        }
                        else if (item.IsDirectoryItself)
                        {
                            if (!string.IsNullOrEmpty(item.PathInZip) && !createdDirectoriesInZip.Contains(item.PathInZip))
                            {
                                // AddToExisting の場合、zip.ContainsEntry は末尾の / を期待することがある
                                string dirPathInZipCheck = item.PathInZip.EndsWith("/") ? item.PathInZip : item.PathInZip + "/";
                                if (args.OperationMode == ZipOperationMode.AddToExisting && zip.ContainsEntry(dirPathInZipCheck))
                                {
                                    // 既に存在する場合は何もしないか、createdDirectoriesInZip に追加
                                    if (!createdDirectoriesInZip.Contains(item.PathInZip)) createdDirectoriesInZip.Add(item.PathInZip);
                                }
                                else
                                {
                                    zip.AddDirectoryByName(item.PathInZip); // 通常、末尾の / は不要
                                    createdDirectoriesInZip.Add(item.PathInZip);
                                }
                            }
                        }
                    }
                    currentProcessingItemForWorker = null; // ループ終了

                    if (worker.CancellationPending) { e.Cancel = true; e.Result = "CancelledByUser"; return; }

                    // "ファイルリスト処理完了。保存処理を開始します..." のようなメッセージは、
                    // SaveProgress の最初のイベントで "ZIPファイル保存中..." が ReportProgress されるため、
                    // ここで出すと上書きされる。もし必要なら SaveProgress の !saveProgressStarted ブロックで調整。
                    // worker.ReportProgress(100, "ファイルリスト処理完了。保存処理を開始します...");

                    if (args.OperationMode == ZipOperationMode.CreateNew)
                    {
                        zip.Save(args.OutputZipPath);
                    }
                    else // AddToExisting
                    {
                        zip.Save();
                    }
                } // using (ZipFile)

                e.Result = args.OutputZipPath; // 正常終了時の結果
            }
            catch (Ionic.Zip.ZipException zex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (アイテム: {currentProcessingItemForWorker.PathInZip})" : "";
                string userMsg = $"ZIPライブラリでエラーが発生しました{itemCtx}。";
                if (zex.Message.ToLower().Contains("password") || (zex.InnerException != null && zex.InnerException.Message.ToLower().Contains("password"))) userMsg += " パスワード関連の問題の可能性があります。";
                else if (zex.Message.ToLower().Contains("crc")) userMsg += " ファイルのCRCチェックに失敗しました。";
                else if (zex.Message.ToLower().Contains("access to the path") || zex.Message.ToLower().Contains("denied")) userMsg += " ZIPファイルへのアクセス中に問題が発生しました。";
                else if (zex.Message.ToLower().Contains("exceeds the maximum value")) userMsg += " ファイルサイズまたはオフセットがZIP形式の制限を超えました。"; // Zip64関連のメッセージ
                else userMsg += $" 詳細: {zex.Message}";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, zex);
            }
            catch (System.IO.FileNotFoundException fnfex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (対象: {currentProcessingItemForWorker.FileSystemPath})" : (fnfex.FileName != null ? $" (対象: {Path.GetFileName(fnfex.FileName)})" : "");
                string userMsg = $"必要なファイルが見つかりませんでした{itemCtx}。ファイルが存在するか確認してください。";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, fnfex);
            }
            catch (System.IO.DirectoryNotFoundException dnfex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (関連パス: {currentProcessingItemForWorker.FileSystemPath})" : "";
                string userMsg = $"必要なフォルダが見つかりませんでした{itemCtx}。パスを確認してください。";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, dnfex);
            }
            catch (System.IO.PathTooLongException ptlex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (関連パス: {currentProcessingItemForWorker.FileSystemPath})" : "";
                string userMsg = $"ファイルパスまたはフォルダパスが長すぎます{itemCtx}。ファイル/フォルダ名を短くするか、より浅い階層に移動してください。";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, ptlex);
            }
            catch (System.IO.IOException ioex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (関連アイテム: {currentProcessingItemForWorker.PathInZip})" : "";
                string userMsg = $"ファイルの読み書き中にエラーが発生しました{itemCtx}。";
                long diskSpaceReq = args.OriginalTotalSize > 0 ? args.OriginalTotalSize : (100 * 1024 * 1024); // 100MBを仮の目安に
                if (IsDiskFull(ioex)) userMsg = $"ディスクの空き容量が不足しています{itemCtx}。少なくとも {FormatBytes(diskSpaceReq)} 程度の空き容量を確保してください。";
                else if (IsSharingViolation(ioex)) userMsg = $"ファイルが他のプログラムによって使用されているか、アクセスが競合しました{itemCtx}。関連するプログラムを終了するか、しばらく待ってから再度お試しください。";
                else userMsg += $" 詳細: {ioex.Message}";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, ioex);
            }
            catch (UnauthorizedAccessException uaex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (対象: {currentProcessingItemForWorker.FileSystemPath})" : "";
                string userMsg = $"ファイルまたはフォルダへのアクセス許可がありません{itemCtx}。管理者として実行するか、対象のアクセス権限を確認してください。";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, uaex);
            }
            catch (Exception ex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (処理中アイテム: {currentProcessingItemForWorker.PathInZip})" : "";
                string userMsg = $"予期せぬエラーが発生しました{itemCtx}。";
                worker.ReportProgress(0, userMsg + $"詳細はログを確認してください。({ex.GetType().Name})");
                e.Result = new ApplicationException(userMsg + $"エラータイプ: {ex.GetType().Name}, メッセージ: {ex.Message}", ex);
            }
            finally
            {
                currentProcessingItemForWorker = null;
            }
        }

        private bool IsDiskFull(System.IO.IOException ex)
        {
            const int HR_ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);
            const int HR_ERROR_DISK_FULL = unchecked((int)0x80070070);
            int hr = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
            return hr == HR_ERROR_DISK_FULL || hr == HR_ERROR_HANDLE_DISK_FULL;
        }

        private bool IsSharingViolation(System.IO.IOException ex)
        {
            int hr = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
            const int HR_ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
            const int HR_ERROR_LOCK_VIOLATION = unchecked((int)0x80070021);
            return hr == HR_ERROR_SHARING_VIOLATION || hr == HR_ERROR_LOCK_VIOLATION;
        }


        private void CompressionWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (this.compressionProgressBar != null) this.compressionProgressBar.Value = Math.Min(100, Math.Max(0, e.ProgressPercentage));
            if (this.compressionStatsLabel != null)
            {
                if (e.UserState != null)
                {
                    this.compressionStatsLabel.Text = e.UserState.ToString();
                    this.compressionStatsLabel.Visible = true;
                }
            }
        }
        private void CompressionWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (this.compressionProgressBar != null) this.compressionProgressBar.Visible = false;
            if (this.compressionStatsLabel != null) this.compressionStatsLabel.Text = "";
            string operationMessage = "処理";
            if (argsPassedToWorker != null) operationMessage = (argsPassedToWorker.OperationMode == ZipOperationMode.CreateNew) ? "圧縮" : "ファイル追加";

            if (e.Cancelled || (e.Result != null && e.Result.ToString().StartsWith("CancelledByUser")))
            {
                ShowNotification($"{operationMessage}がキャンセルされました。", NotificationType.Info);
                AppendLog($"{operationMessage}はユーザーによってキャンセルされました。", LogLevel.Warning);
            }
            else if (e.Error != null)
            {
                ShowNotification($"{operationMessage}エラー: {e.Error.Message}", NotificationType.Error);
                AppendLog($"{operationMessage}中に予期せぬエラーが発生しました。Type: {e.Error.GetType().FullName}, Message: {e.Error.Message}", LogLevel.Error);
            }
            else if (e.Result is Exception ex)
            {
                ShowNotification($"{operationMessage}エラー: {ex.Message}", NotificationType.Error);
                StringBuilder logBuilder = new StringBuilder();
                logBuilder.AppendLine($"{operationMessage}中にエラーが発生しました。");
                logBuilder.AppendLine($"  UserMessage: {ex.Message}");
                logBuilder.AppendLine($"  ExceptionType: {ex.GetType().FullName}");
                Exception? innerEx = ex.InnerException; int nestLevel = 1;
                while (innerEx != null) { logBuilder.AppendLine($"  InnerException (Level {nestLevel}): Type: {innerEx.GetType().FullName}, Message: {innerEx.Message}"); innerEx = innerEx.InnerException; nestLevel++; }
                AppendLog(logBuilder.ToString(), LogLevel.Error);
            }
            else if (e.Result is string outputZipPath)
            {
                long originalSize = argsPassedToWorker?.OriginalTotalSize ?? 0; long compressedSize = 0; string displayMessage = "";
                try
                {
                    if (argsPassedToWorker != null && argsPassedToWorker.EnableSplit && argsPassedToWorker.SplitSizeInBytes > 0 && argsPassedToWorker.OperationMode == ZipOperationMode.CreateNew && !string.IsNullOrEmpty(outputZipPath))
                    {
                        string baseName = Path.Combine(Path.GetDirectoryName(outputZipPath) ?? "", Path.GetFileNameWithoutExtension(outputZipPath));
                        if (File.Exists(outputZipPath)) compressedSize += new FileInfo(outputZipPath).Length;
                        int segment = 1;
                        while (true) { string segmentFile = $"{baseName}.z{segment:00}"; if (File.Exists(segmentFile)) { compressedSize += new FileInfo(segmentFile).Length; segment++; } else break; }
                        displayMessage = $"分割{operationMessage}完了: {Path.GetFileName(outputZipPath)} 他 ({segment - 1}セグメント)";
                    }
                    else if (File.Exists(outputZipPath)) { compressedSize = new FileInfo(outputZipPath).Length; displayMessage = $"{operationMessage}完了: {Path.GetFileName(outputZipPath)}"; }

                    if (this.compressionStatsLabel != null)
                    {
                        this.compressionStatsLabel.Text = $"元サイズ: {FormatBytes(originalSize)} → 圧縮後サイズ: {FormatBytes(compressedSize)}";
                        if (originalSize > 0 && argsPassedToWorker?.OperationMode == ZipOperationMode.CreateNew && compressedSize >= 0) { double ratio = (originalSize == 0) ? 0 : (double)compressedSize / originalSize; this.compressionStatsLabel.Text += $" (圧縮率: {ratio:P0})"; }
                        else if (originalSize == 0 && compressedSize == 0 && argsPassedToWorker?.OperationMode == ZipOperationMode.CreateNew) { this.compressionStatsLabel.Text += $" (圧縮率: N/A)"; }
                        this.compressionStatsLabel.Visible = true;
                    }
                }
                catch (Exception statEx) { AppendLog($"ファイルサイズ統計の取得エラー: {statEx.Message}", LogLevel.Warning); if (this.compressionStatsLabel != null) this.compressionStatsLabel.Visible = false; }
                ShowNotification(displayMessage, NotificationType.Success); AppendLog($"処理が正常に完了。出力: {outputZipPath}", LogLevel.Info);
                if (argsPassedToWorker != null && outputZipPath == argsPassedToWorker.OutputZipPath && (argsPassedToWorker.OperationMode == ZipOperationMode.CreateNew || argsPassedToWorker.OperationMode == ZipOperationMode.AddToExisting))
                {
                    if (this.selectedFilesListBox != null) this.selectedFilesListBox.Items.Clear();
                    itemsToCompress.Clear();
                }
                if ((this.openOutputFolderCheckBox?.Checked ?? false) && argsPassedToWorker != null && !string.IsNullOrEmpty(argsPassedToWorker.EffectiveOutputFolder) && Directory.Exists(argsPassedToWorker.EffectiveOutputFolder))
                {
                    try
                    {
                        Process.Start("explorer.exe", argsPassedToWorker.EffectiveOutputFolder);
                    }
                    catch (Exception folderEx)
                    {
                        ShowNotification($"フォルダを開けませんでした: {folderEx.Message}", NotificationType.Warning);
                        AppendLog($"フォルダオープンエラー: {argsPassedToWorker.EffectiveOutputFolder} - {folderEx.Message}", LogLevel.Warning);
                    }
                }

            }
            SetUIEnabledState(true);
        }
        private string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" }; int i = 0; double dblSByte = bytes;
            if (bytes == 0 && itemsToCompress.Count(it => !it.IsDirectoryItself) == 0) return "0 B"; // No files to compress
            if (bytes == 0 && itemsToCompress.Count(it => !it.IsDirectoryItself) > 0) return "0 B (ファイルサイズ取得不可)";


            for (i = 0; i < suffix.Length && bytes >= 1024; i++)
            {
                dblSByte = (double)bytes / 1024.0;
                bytes /= 1024;
            }
            return String.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", dblSByte, suffix[i]);
        }

        private void UpdateCurrentLogLevel()
        {
            if (this.logLevelComboBox != null && this.logLevelComboBox.SelectedItem != null)
            {
                if (Enum.TryParse<LogLevel>(this.logLevelComboBox.SelectedItem.ToString(), out LogLevel selectedLevel))
                {
                    if (currentSelectedLogLevel != selectedLevel)
                    {
                        currentSelectedLogLevel = selectedLevel;
                        AppendLog($"ログレベルが '{currentSelectedLogLevel}' に変更されました。", LogLevel.Info);
                    }
                }
            }
        }
        private void logLevelComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateCurrentLogLevel();
        }

        private void AppendLog(string message, LogLevel level = LogLevel.Info)
        {
            if (level < currentSelectedLogLevel)
            {
                return;
            }
            var logger = this.logTextBox;
            if (logger == null) return;
            if (logger.InvokeRequired) logger.Invoke(new Action(() => AppendLogInternal(logger, message, level))); else AppendLogInternal(logger, message, level);
        }
        private void AppendLogInternal(TextBox logger, string message, LogLevel level)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); logger.AppendText($"{timestamp} [{level}] {message}{Environment.NewLine}"); logger.ScrollToCaret();
        }

        private void selectedFilesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Not used currently
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void dragDropOverlayPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void zipCommentTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}