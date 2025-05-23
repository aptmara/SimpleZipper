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
            AppendLog("�A�v���P�[�V�������N�����܂����B", LogLevel.Info);

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
                AppendLog($"{cmdArgs.Length - 1}�̃A�C�e�����R�}���h���C�������Ƃ��ēn����܂����B", LogLevel.Info);
                bool isRecursive = Properties.Settings.Default.RecursiveAdd;

                for (int i = 1; i < cmdArgs.Length; i++)
                {
                    string itemPath = cmdArgs[i];
                    AppendLog($"�������̈���: {itemPath}", LogLevel.Debug);

                    if (File.Exists(itemPath))
                    {
                        AddFileToList(itemPath);
                    }
                    else if (Directory.Exists(itemPath))
                    {
                        string directoryName = Path.GetFileName(itemPath);
                        AppendLog($"�R�}���h���C���t�H���_�����J�n: {itemPath} (�ċA�ݒ�: {isRecursive})", LogLevel.Debug);

                        if (this.selectedFilesListBox != null && !this.selectedFilesListBox.Items.Contains(directoryName + " (�t�H���_)"))
                        {
                            this.selectedFilesListBox.Items.Add(directoryName + " (�t�H���_)");
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
                                AppendLog($"  �t�@�C���ǉ�(�R�}���h���C��/��ċA): {file.FullName}", LogLevel.Debug);
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
                                AppendLog($"  �T�u�t�H���_�\���ǉ�(�R�}���h���C��/��ċA): {subDir.FullName}", LogLevel.Debug);
                            }
                            if (new DirectoryInfo(itemPath).GetDirectories().Any(sd => sd.GetFiles().Length > 0 || sd.GetDirectories().Length > 0))
                            {
                                AppendLog($"���: �t�H���_ {directoryName} ���ǉ�����܂������A�ċA�I�v�V�����������̂��ߒ����̃t�@�C���ƃT�u�t�H���_�\���݂̂��Ώۂł��B", LogLevel.Info);
                            }
                        }
                    }
                    else
                    {
                        AppendLog($"�R�}���h���C�������G���[: �p�X��������Ȃ����A�N�Z�X�ł��܂��� - {itemPath}", LogLevel.Warning);
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
                Properties.Settings.Default.CurrentThemeName = selectedTheme.Name ?? "���C�g";
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
            if (theme.Name != null) AppendLog($"�e�[�}�ύX: {theme.Name}", LogLevel.Info);
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
                Color warningPanelBackColor = (theme.Name == "�_�[�N") ? Color.FromArgb(180, 180, 100, 30) : Color.FromArgb(180, 255, 150, 0);
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
                string rootNameToSearch = displayNameInListBox.EndsWith(" (�t�H���_)")
                                        ? displayNameInListBox.Substring(0, displayNameInListBox.Length - " (�t�H���_)".Length)
                                        : displayNameInListBox;

                CompressionItem? representativeItem = itemsToCompress.FirstOrDefault(ci =>
                    ci.RootNameInList == rootNameToSearch &&
                    ((ci.IsDirectoryItself && ci.PathInZip == rootNameToSearch) || (!ci.IsDirectoryItself && ci.PathInZip == rootNameToSearch))
                );

                if (representativeItem == null && displayNameInListBox.EndsWith(" (�t�H���_)"))
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

            if (this.compressButton != null) this.compressButton.Text = isCreateNewMode ? "���k���s" : "�t�@�C����ǉ�";
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
                openFileDialog.Title = "�ǉ����ZIP�t�@�C����I��";
                openFileDialog.Filter = "ZIP�t�@�C�� (*.zip)|*.zip";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    existingZipPathForAdd = openFileDialog.FileName;
                    if (this.existingZipFileTextBox != null) this.existingZipFileTextBox.Text = existingZipPathForAdd;
                    AppendLog($"����ZIP�I��: {existingZipPathForAdd}", LogLevel.Info);
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
                    AppendLog($"�v���r���[�G���[: {Path.GetFileName(zipFilePath)} �͗L����ZIP�t�@�C���ł͂���܂���B", LogLevel.Warning);
                    ShowNotification($"�I�����ꂽ�t�@�C���͗L����ZIP�t�@�C���`���ł͂���܂���B", NotificationType.Warning);
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
                AppendLog($"ZIP�v���r���[�G���[ ({Path.GetFileName(zipFilePath)}): {zex.Message}", LogLevel.Error);
                ShowNotification($"ZIP�t�@�C���̓ǂݍ��݂Ɏ��s���܂���: {zex.Message}", NotificationType.Warning);
                treeView.Nodes.Add($"�G���[: {zex.Message}");
                treeView.Visible = true;
            }
            catch (Exception ex)
            {
                AppendLog($"�v���r���[�������ɗ\�����ʃG���[ ({Path.GetFileName(zipFilePath)}): {ex.Message}", LogLevel.Error);
                ShowNotification($"�v���r���[�̕\�����ɃG���[���������܂����B", NotificationType.Error);
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
                    fileNode.ToolTipText = $"�T�C�Y: {FormatBytes(entry.UncompressedSize)}, �X�V����: {entry.LastModified.ToLocalTime()}";
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
                        // �ۑ����ꂽ�p�X�������܂��͋�̏ꍇ�A�f�X�N�g�b�v���f�t�H���g�ɐݒ�
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        this.outputFolderTextBox.Text = desktopPath;
                        AppendLog("�o�͐�t�H���_�̕ۑ��l���������������߁A�f�X�N�g�b�v���f�t�H���g�ɐݒ肵�܂����B", LogLevel.Debug);
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
                        this.zipFileNameTextBox.Text = "archive"; // �s���܂��͋�̏ꍇ�̃f�t�H���g��
                        AppendLog("�O��ۑ����ꂽZIP�t�@�C�����������܂��͋󂾂������߁A�f�t�H���g����ݒ肵�܂����B", LogLevel.Debug);
                    }
                }

                if (this.compressionLevelComboBox != null)
                {
                    if (this.compressionLevelComboBox.Items.Count == 0)
                    {
                        this.compressionLevelComboBox.Items.AddRange(new object[] { "�W��", "���x�D��", "�����k" });
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
            catch (Exception ex) { AppendLog("�ݒ�̓ǂݍ��݂Ɏ��s: " + ex.Message, LogLevel.Error); if (availableThemes.Any()) ApplyTheme(ThemeColors.LightTheme); }
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
            catch (Exception ex) { AppendLog("�ݒ�̕ۑ��Ɏ��s: " + ex.Message, LogLevel.Error); }
        }
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (compressionWorker != null && compressionWorker.IsBusy)
            {
                var result = MessageBox.Show("���k���������s���ł��B�{���ɏI�����܂����H\n�I������Ə����̓L�����Z������܂��B", "�m�F", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    AppendLog("�A�v���P�[�V�����I���̂��߁A���k�����̃L�����Z�������݂܂��B", LogLevel.Info);
                    compressionWorker.CancelAsync();
                }
            }
            AppendLog("�A�v���P�[�V�������I�����܂��B", LogLevel.Info);
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
            AppendLog($"�h���b�O���h���b�v����: {droppedItems.Length} �A�C�e��", LogLevel.Info);
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
                    AppendLog($"�t�H���_�����J�n: {itemPath} (�ċA: {isRecursive})", LogLevel.Debug);

                    if (this.selectedFilesListBox != null && !this.selectedFilesListBox.Items.Contains(directoryName + " (�t�H���_)"))
                    {
                        this.selectedFilesListBox.Items.Add(directoryName + " (�t�H���_)");
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
                            AppendLog($"  �t�@�C���ǉ�(�t�H���_��/��ċA): {file.FullName}", LogLevel.Debug);
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
                            AppendLog($"  �T�u�t�H���_�\���ǉ�(��ċA): {subDir.FullName}", LogLevel.Debug);
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
                    AppendLog($"�h���b�O���h���b�v�G���[: �p�X��������܂��� - {itemPath}", LogLevel.Warning);
                }
            }
            if (directoryDroppedWithoutRecursiveSupportMessageShown)
            {
                ShowNotification("�t�H���_���ǉ�����܂������A�ċA�I�v�V�����������̂��ߒ����̃t�@�C���ƃT�u�t�H���_�\���݂̂��Ώۂł��B", NotificationType.Info);
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
                    AppendLog($"  �t�@�C���ǉ�(�ċA): {file.FullName}", LogLevel.Debug);
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
                    AppendLog($"  �T�u�t�H���_�\���ǉ�(�ċA): {subDir.FullName}", LogLevel.Debug);
                    AddDirectoryItemsRecursively(subDir.FullName, subDirectoryPathInZip, rootNameInList);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"�t�H���_�A�N�Z�X�G���[ ({Path.GetFileName(directoryPath)}): {ex.Message}", NotificationType.Warning);
                AppendLog($"�t�H���_�A�N�Z�X�G���[(�ċA��): {directoryPath} - {ex.Message}", LogLevel.Error);
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
                AppendLog($"�t�@�C���ǉ�: {filePath} (ZIP���p�X: {fileName})", LogLevel.Debug);
            }
        }

        private void selectFilesButton_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true; openFileDialog.Title = "���k����t�@�C����I�����Ă�������"; openFileDialog.Filter = "���ׂẴt�@�C�� (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    AppendLog($"�t�@�C���I���_�C�A���O: {openFileDialog.FileNames.Length} �t�@�C���I��", LogLevel.Info);
                    foreach (string fileName in openFileDialog.FileNames) AddFileToList(fileName);
                }
            }
        }
        private void selectOutputFolderButton_Click(object? sender, EventArgs e)
        {
            if (this.outputFolderTextBox == null) return;
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "ZIP�t�@�C���̕ۑ���t�H���_��I�����Ă�������";
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
                string rootName = displayName.EndsWith(" (�t�H���_)") ? displayName.Substring(0, displayName.Length - " (�t�H���_)".Length) : displayName;
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
            AppendLog($"{selectedRootNamesToRemove.Count} �̃��[�g�A�C�e���i�֘A�t�@�C��/�t�H���_�܂ތv {itemsRemovedCount} items�j��I�����X�g����폜���܂����B", LogLevel.Debug);
        }
        private void clearFileListButton_Click(object? sender, EventArgs e)
        {
            if (this.selectedFilesListBox != null) this.selectedFilesListBox.Items.Clear();
            itemsToCompress.Clear();
            AppendLog("�t�@�C�����X�g���N���A���܂����B", LogLevel.Debug);
        }

        private void cancelCompressionButton_Click(object? sender, EventArgs e)
        {
            if (compressionWorker != null && compressionWorker.IsBusy)
            {
                AppendLog("���k�����̃L�����Z����v�����܂����B", LogLevel.Info);
                compressionWorker.CancelAsync();
                if (sender is Button btn) btn.Enabled = false;
            }
        }

        private void compressButton_Click(object? sender, EventArgs e)
        {
            if (compressionWorker == null) { AppendLog("compressionWorker������������Ă��܂���B", LogLevel.Error); return; }
            if (compressionWorker.IsBusy) { ShowNotification("���k���������s���ł��B", NotificationType.Info); return; }
            if (itemsToCompress.Count == 0) { ShowNotification((currentZipMode == ZipOperationMode.CreateNew ? "���k" : "�ǉ�") + "����t�@�C�����I������Ă��܂���B", NotificationType.Warning); return; }

            string outputZipPathValue; string? effectiveOutputFolder; string effectiveZipFileName;

            if (currentZipMode == ZipOperationMode.AddToExisting)
            {
                if (string.IsNullOrWhiteSpace(existingZipPathForAdd) || !File.Exists(existingZipPathForAdd)) { ShowNotification("�ǉ���̗L����ZIP�t�@�C�����I������Ă��܂���B", NotificationType.Warning); return; }
                try
                {
                    if (!Ionic.Zip.ZipFile.IsZipFile(existingZipPathForAdd, true))
                    {
                        ShowNotification("�I�����ꂽ�t�@�C���͗L����ZIP�t�@�C���`���ł͂���܂���B", NotificationType.Warning); return;
                    }
                }
                catch (Exception ex)
                {
                    ShowNotification($"����ZIP�t�@�C���̊m�F���ɃG���[: {ex.Message}", NotificationType.Warning);
                    AppendLog($"����ZIP�m�F�G���[: {existingZipPathForAdd} - {ex.Message}", LogLevel.Error);
                    return;
                }
                outputZipPathValue = existingZipPathForAdd;
                effectiveOutputFolder = Path.GetDirectoryName(outputZipPathValue);
                effectiveZipFileName = Path.GetFileName(outputZipPathValue);
            }
            else
            {
                if (this.outputFolderTextBox == null || string.IsNullOrWhiteSpace(this.outputFolderTextBox.Text)) { ShowNotification("�ۑ���t�H���_���I������Ă��܂���B", NotificationType.Warning); return; }
                if (!Directory.Exists(this.outputFolderTextBox.Text))
                {
                    try { Directory.CreateDirectory(this.outputFolderTextBox.Text); AppendLog($"�o�͐�t�H���_���쐬: {this.outputFolderTextBox.Text}", LogLevel.Info); }
                    catch (Exception ex) { ShowNotification($"�o�͐�t�H���_�̍쐬���s: {ex.Message}", NotificationType.Error); AppendLog($"�o�̓t�H���_�쐬�G���[: {ex.Message}", LogLevel.Error); return; }
                }

                if (this.zipFileNameTextBox == null) { ShowNotification("ZIP�t�@�C�����e�L�X�g�{�b�N�X��������܂���B", NotificationType.Error); return; }
                string baseZipFileName = this.zipFileNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(baseZipFileName)) { ShowNotification("ZIP�t�@�C��������͂��Ă��������B", NotificationType.Warning); return; }
                if (baseZipFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { ShowNotification("ZIP�t�@�C�����Ɏg�p�ł��Ȃ��������܂܂�Ă��܂��B", NotificationType.Warning); return; }


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
                    DialogResult userChoice = MessageBox.Show($"�t�@�C�� '{effectiveZipFileName}' �͊��ɑ��݂��܂��B\n\n�͂�: �㏑�����܂�\n������: �V�������O�ŕۑ����܂�\n�L�����Z��: �����𒆎~���܂�", "�t�@�C�����̋���", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (userChoice == DialogResult.No) { outputZipPathValue = GetUniqueFileName(this.outputFolderTextBox.Text, effectiveZipFileName); effectiveZipFileName = Path.GetFileName(outputZipPathValue); }
                    else if (userChoice == DialogResult.Cancel) { ShowNotification("���k�������L�����Z�����܂����B", NotificationType.Info); return; }
                }
            }
            AppendLog($"���k�����J�n�{�^���N���b�N�B���[�h: {currentZipMode}", LogLevel.Info);
            if (currentZipMode == ZipOperationMode.CreateNew) AppendLog($"�V�KZIP�쐬: {outputZipPathValue}", LogLevel.Info); else AppendLog($"����ZIP�ɒǉ�: {existingZipPathForAdd}", LogLevel.Info);

            Ionic.Zlib.CompressionLevel ionicLevel = Ionic.Zlib.CompressionLevel.Default;
            if (this.compressionLevelComboBox != null && this.compressionLevelComboBox.SelectedItem != null) { switch (this.compressionLevelComboBox.SelectedItem.ToString()) { case "���x�D��": ionicLevel = Ionic.Zlib.CompressionLevel.BestSpeed; break; case "�����k": ionicLevel = Ionic.Zlib.CompressionLevel.BestCompression; break; } }
            AppendLog($"���k���x��: {(this.compressionLevelComboBox?.SelectedItem?.ToString() ?? "�f�t�H���g")}", LogLevel.Debug);

            bool enablePassword = false; string? password = null;
            if (this.enablePasswordCheckBox != null && this.enablePasswordCheckBox.Checked) { if (this.passwordTextBox != null && !string.IsNullOrWhiteSpace(this.passwordTextBox.Text)) { enablePassword = true; password = this.passwordTextBox.Text; } else { ShowNotification("�p�X���[�h���ݒ肳��Ă��܂���B", NotificationType.Warning); return; } }
            AppendLog($"�p�X���[�h�ی�: {enablePassword}", LogLevel.Debug);

            bool enableComment = false; string? zipComment = null;
            if (this.enableZipCommentCheckBox != null && this.enableZipCommentCheckBox.Checked) { if (this.zipCommentTextBox != null) { enableComment = true; zipComment = this.zipCommentTextBox.Text; } }
            AppendLog($"ZIP�R�����g: {enableComment}", LogLevel.Debug);

            long splitSizeInBytes = 0; bool enableSplit = false;
            if (currentZipMode == ZipOperationMode.CreateNew && this.enableSplitZipCheckBox != null && this.enableSplitZipCheckBox.Checked)
            {
                enableSplit = true;
                if (this.splitSizeNumericUpDown != null && this.splitUnitComboBox != null && this.splitUnitComboBox.SelectedItem != null)
                {
                    long sizeValue = (long)this.splitSizeNumericUpDown.Value; string? unit = this.splitUnitComboBox.SelectedItem.ToString();
                    if (unit == "MB") splitSizeInBytes = sizeValue * 1024 * 1024; else if (unit == "KB") splitSizeInBytes = sizeValue * 1024;
                    if (splitSizeInBytes <= 0) { enableSplit = false; splitSizeInBytes = 0; AppendLog("�����T�C�Y�������Ȃ��߁A�������܂���B", LogLevel.Warning); }
                    else AppendLog($"ZIP�����L���B�T�C�Y: {sizeValue} {unit} ({splitSizeInBytes} bytes)", LogLevel.Debug);
                }
                else enableSplit = false;
            }

            if (this.compressionProgressBar != null) { this.compressionProgressBar.Value = 0; this.compressionProgressBar.Visible = true; this.compressionProgressBar.Style = ProgressBarStyle.Continuous; }
            if (this.compressionStatsLabel != null) { this.compressionStatsLabel.Text = "������..."; this.compressionStatsLabel.Visible = true; }

            SetUIEnabledState(false);

            long originalTotalSizeBytes = 0;
            if (itemsToCompress != null)
            {
                foreach (CompressionItem item in itemsToCompress)
                {
                    if (!item.IsDirectoryItself && File.Exists(item.FileSystemPath))
                    {
                        try { originalTotalSizeBytes += new FileInfo(item.FileSystemPath).Length; }
                        catch (Exception ex) { AppendLog($"�T�C�Y�擾�G���[(���O): {item.FileSystemPath} - {ex.Message}", LogLevel.Warning); }
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
            worker.ReportProgress(0, "���k������...");

            try
            {
                using (Ionic.Zip.ZipFile zip = (args.OperationMode == ZipOperationMode.CreateNew) ?
                                                new Ionic.Zip.ZipFile(System.Text.Encoding.GetEncoding("shift_jis")) :
                                                Ionic.Zip.ZipFile.Read(args.ExistingZipPath ?? throw new ArgumentNullException("ExistingZipPath")))
                {
                    zip.UseZip64WhenSaving = Ionic.Zip.Zip64Option.AsNecessary;
                    if (args.OperationMode == ZipOperationMode.CreateNew)
                    {
                        worker.ReportProgress(0, $"�V�KZIP '{Path.GetFileName(args.OutputZipPath)}' �쐬����...");
                    }
                    else
                    {
                        worker.ReportProgress(0, $"����ZIP '{Path.GetFileName(args.ExistingZipPath)}' �ւ̒ǉ�����...");
                    }

                    if (args.EnablePassword && !string.IsNullOrEmpty(args.Password)) { zip.Password = args.Password; zip.Encryption = EncryptionAlgorithm.WinZipAes256; }
                    zip.CompressionLevel = args.LevelIonic;
                    if (args.EnableComment && args.Comment != null) zip.Comment = args.Comment;

                    if (args.OperationMode == ZipOperationMode.CreateNew && args.EnableSplit && args.SplitSizeInBytes > 0)
                    {
                        if (args.SplitSizeInBytes > Int32.MaxValue) { zip.MaxOutputSegmentSize = Int32.MaxValue; worker.ReportProgress(0, "�x��: �����T�C�Y���傫�����邽�߁AInt32.MaxValue���g�p����܂��B"); }
                        else if (args.SplitSizeInBytes < 65536 && args.SplitSizeInBytes > 0) { zip.MaxOutputSegmentSize = 65536; worker.ReportProgress(0, "�x��: �����T�C�Y�����������邽�߁A64KB���g�p����܂��B"); }
                        else if (args.SplitSizeInBytes > 0) { zip.MaxOutputSegmentSize = (int)args.SplitSizeInBytes; }
                    }

                    bool saveProgressStarted = false;
                    zip.SaveProgress += (s, progressArgs) =>
                    {
                        if (worker.CancellationPending) { progressArgs.Cancel = true; return; }

                        if (!saveProgressStarted)
                        {
                            worker.ReportProgress(0, (args.OperationMode == ZipOperationMode.CreateNew ? "ZIP�t�@�C���ۑ���..." : "����ZIP�֒ǉ��ۑ���..."));
                            saveProgressStarted = true;
                        }

                        string currentEntryName = progressArgs.CurrentEntry?.FileName ?? "N/A";
                        string progressMessageSuffix = $"({progressArgs.EntriesSaved}/{progressArgs.EntriesTotal}): {currentEntryName}";

                        if (progressArgs.EventType == ZipProgressEventType.Saving_EntryBytesRead)
                        {
                            if (progressArgs.TotalBytesToTransfer > 0)
                            {
                                int percentage = (int)((progressArgs.BytesTransferred * 100) / progressArgs.TotalBytesToTransfer);
                                worker.ReportProgress(percentage, $"�ۑ��� {progressMessageSuffix}");
                            }
                        }
                        else if (progressArgs.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
                        {
                            int percentage = progressArgs.EntriesTotal > 0 ? (progressArgs.EntriesSaved * 100 / progressArgs.EntriesTotal) : 0;
                            worker.ReportProgress(percentage, $"�G���g���������� {progressMessageSuffix}");
                        }
                        else if (progressArgs.EventType == ZipProgressEventType.Saving_Completed)
                        {
                            worker.ReportProgress(100, (args.OperationMode == ZipOperationMode.CreateNew ? "�V�KZIP�ۑ������B" : "����ZIP�ւ̒ǉ��ۑ������B"));
                        }
                        // Saving_BeforeWriteEntry �⑼�̃C�x���g�^�C�v���K�v�ɉ����ė��p�\
                    };

                    worker.ReportProgress(0, "�t�@�C�����X�g������...");
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
                        worker.ReportProgress(listProcessingPercentage, $"�t�@�C�����X�g����: {itemsProcessedForList}/{totalItemsInList} ({Path.GetFileName(item.FileSystemPath)})");

                        if (!item.IsDirectoryItself && File.Exists(item.FileSystemPath))
                        {
                            if (args.OperationMode == ZipOperationMode.AddToExisting && zip.ContainsEntry(item.PathInZip))
                            {
                                zip.RemoveEntry(item.PathInZip); // �㏑���̂��ߊ������폜
                            }
                            ZipEntry entry = zip.AddFile(item.FileSystemPath, "");
                            entry.FileName = item.PathInZip;
                        }
                        else if (item.IsDirectoryItself)
                        {
                            if (!string.IsNullOrEmpty(item.PathInZip) && !createdDirectoriesInZip.Contains(item.PathInZip))
                            {
                                // AddToExisting �̏ꍇ�Azip.ContainsEntry �͖����� / �����҂��邱�Ƃ�����
                                string dirPathInZipCheck = item.PathInZip.EndsWith("/") ? item.PathInZip : item.PathInZip + "/";
                                if (args.OperationMode == ZipOperationMode.AddToExisting && zip.ContainsEntry(dirPathInZipCheck))
                                {
                                    // ���ɑ��݂���ꍇ�͉������Ȃ����AcreatedDirectoriesInZip �ɒǉ�
                                    if (!createdDirectoriesInZip.Contains(item.PathInZip)) createdDirectoriesInZip.Add(item.PathInZip);
                                }
                                else
                                {
                                    zip.AddDirectoryByName(item.PathInZip); // �ʏ�A������ / �͕s�v
                                    createdDirectoriesInZip.Add(item.PathInZip);
                                }
                            }
                        }
                    }
                    currentProcessingItemForWorker = null; // ���[�v�I��

                    if (worker.CancellationPending) { e.Cancel = true; e.Result = "CancelledByUser"; return; }

                    // "�t�@�C�����X�g���������B�ۑ��������J�n���܂�..." �̂悤�ȃ��b�Z�[�W�́A
                    // SaveProgress �̍ŏ��̃C�x���g�� "ZIP�t�@�C���ۑ���..." �� ReportProgress ����邽�߁A
                    // �����ŏo���Ə㏑�������B�����K�v�Ȃ� SaveProgress �� !saveProgressStarted �u���b�N�Œ����B
                    // worker.ReportProgress(100, "�t�@�C�����X�g���������B�ۑ��������J�n���܂�...");

                    if (args.OperationMode == ZipOperationMode.CreateNew)
                    {
                        zip.Save(args.OutputZipPath);
                    }
                    else // AddToExisting
                    {
                        zip.Save();
                    }
                } // using (ZipFile)

                e.Result = args.OutputZipPath; // ����I�����̌���
            }
            catch (Ionic.Zip.ZipException zex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (�A�C�e��: {currentProcessingItemForWorker.PathInZip})" : "";
                string userMsg = $"ZIP���C�u�����ŃG���[���������܂���{itemCtx}�B";
                if (zex.Message.ToLower().Contains("password") || (zex.InnerException != null && zex.InnerException.Message.ToLower().Contains("password"))) userMsg += " �p�X���[�h�֘A�̖��̉\��������܂��B";
                else if (zex.Message.ToLower().Contains("crc")) userMsg += " �t�@�C����CRC�`�F�b�N�Ɏ��s���܂����B";
                else if (zex.Message.ToLower().Contains("access to the path") || zex.Message.ToLower().Contains("denied")) userMsg += " ZIP�t�@�C���ւ̃A�N�Z�X���ɖ�肪�������܂����B";
                else if (zex.Message.ToLower().Contains("exceeds the maximum value")) userMsg += " �t�@�C���T�C�Y�܂��̓I�t�Z�b�g��ZIP�`���̐����𒴂��܂����B"; // Zip64�֘A�̃��b�Z�[�W
                else userMsg += $" �ڍ�: {zex.Message}";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, zex);
            }
            catch (System.IO.FileNotFoundException fnfex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (�Ώ�: {currentProcessingItemForWorker.FileSystemPath})" : (fnfex.FileName != null ? $" (�Ώ�: {Path.GetFileName(fnfex.FileName)})" : "");
                string userMsg = $"�K�v�ȃt�@�C����������܂���ł���{itemCtx}�B�t�@�C�������݂��邩�m�F���Ă��������B";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, fnfex);
            }
            catch (System.IO.DirectoryNotFoundException dnfex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (�֘A�p�X: {currentProcessingItemForWorker.FileSystemPath})" : "";
                string userMsg = $"�K�v�ȃt�H���_��������܂���ł���{itemCtx}�B�p�X���m�F���Ă��������B";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, dnfex);
            }
            catch (System.IO.PathTooLongException ptlex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (�֘A�p�X: {currentProcessingItemForWorker.FileSystemPath})" : "";
                string userMsg = $"�t�@�C���p�X�܂��̓t�H���_�p�X���������܂�{itemCtx}�B�t�@�C��/�t�H���_����Z�����邩�A���󂢊K�w�Ɉړ����Ă��������B";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, ptlex);
            }
            catch (System.IO.IOException ioex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (�֘A�A�C�e��: {currentProcessingItemForWorker.PathInZip})" : "";
                string userMsg = $"�t�@�C���̓ǂݏ������ɃG���[���������܂���{itemCtx}�B";
                long diskSpaceReq = args.OriginalTotalSize > 0 ? args.OriginalTotalSize : (100 * 1024 * 1024); // 100MB�����̖ڈ���
                if (IsDiskFull(ioex)) userMsg = $"�f�B�X�N�̋󂫗e�ʂ��s�����Ă��܂�{itemCtx}�B���Ȃ��Ƃ� {FormatBytes(diskSpaceReq)} ���x�̋󂫗e�ʂ��m�ۂ��Ă��������B";
                else if (IsSharingViolation(ioex)) userMsg = $"�t�@�C�������̃v���O�����ɂ���Ďg�p����Ă��邩�A�A�N�Z�X���������܂���{itemCtx}�B�֘A����v���O�������I�����邩�A���΂炭�҂��Ă���ēx���������������B";
                else userMsg += $" �ڍ�: {ioex.Message}";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, ioex);
            }
            catch (UnauthorizedAccessException uaex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (�Ώ�: {currentProcessingItemForWorker.FileSystemPath})" : "";
                string userMsg = $"�t�@�C���܂��̓t�H���_�ւ̃A�N�Z�X��������܂���{itemCtx}�B�Ǘ��҂Ƃ��Ď��s���邩�A�Ώۂ̃A�N�Z�X�������m�F���Ă��������B";
                worker.ReportProgress(0, userMsg); e.Result = new ApplicationException(userMsg, uaex);
            }
            catch (Exception ex)
            {
                string itemCtx = currentProcessingItemForWorker != null ? $" (�������A�C�e��: {currentProcessingItemForWorker.PathInZip})" : "";
                string userMsg = $"�\�����ʃG���[���������܂���{itemCtx}�B";
                worker.ReportProgress(0, userMsg + $"�ڍׂ̓��O���m�F���Ă��������B({ex.GetType().Name})");
                e.Result = new ApplicationException(userMsg + $"�G���[�^�C�v: {ex.GetType().Name}, ���b�Z�[�W: {ex.Message}", ex);
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
            string operationMessage = "����";
            if (argsPassedToWorker != null) operationMessage = (argsPassedToWorker.OperationMode == ZipOperationMode.CreateNew) ? "���k" : "�t�@�C���ǉ�";

            if (e.Cancelled || (e.Result != null && e.Result.ToString().StartsWith("CancelledByUser")))
            {
                ShowNotification($"{operationMessage}���L�����Z������܂����B", NotificationType.Info);
                AppendLog($"{operationMessage}�̓��[�U�[�ɂ���ăL�����Z������܂����B", LogLevel.Warning);
            }
            else if (e.Error != null)
            {
                ShowNotification($"{operationMessage}�G���[: {e.Error.Message}", NotificationType.Error);
                AppendLog($"{operationMessage}���ɗ\�����ʃG���[���������܂����BType: {e.Error.GetType().FullName}, Message: {e.Error.Message}", LogLevel.Error);
            }
            else if (e.Result is Exception ex)
            {
                ShowNotification($"{operationMessage}�G���[: {ex.Message}", NotificationType.Error);
                StringBuilder logBuilder = new StringBuilder();
                logBuilder.AppendLine($"{operationMessage}���ɃG���[���������܂����B");
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
                        displayMessage = $"����{operationMessage}����: {Path.GetFileName(outputZipPath)} �� ({segment - 1}�Z�O�����g)";
                    }
                    else if (File.Exists(outputZipPath)) { compressedSize = new FileInfo(outputZipPath).Length; displayMessage = $"{operationMessage}����: {Path.GetFileName(outputZipPath)}"; }

                    if (this.compressionStatsLabel != null)
                    {
                        this.compressionStatsLabel.Text = $"���T�C�Y: {FormatBytes(originalSize)} �� ���k��T�C�Y: {FormatBytes(compressedSize)}";
                        if (originalSize > 0 && argsPassedToWorker?.OperationMode == ZipOperationMode.CreateNew && compressedSize >= 0) { double ratio = (originalSize == 0) ? 0 : (double)compressedSize / originalSize; this.compressionStatsLabel.Text += $" (���k��: {ratio:P0})"; }
                        else if (originalSize == 0 && compressedSize == 0 && argsPassedToWorker?.OperationMode == ZipOperationMode.CreateNew) { this.compressionStatsLabel.Text += $" (���k��: N/A)"; }
                        this.compressionStatsLabel.Visible = true;
                    }
                }
                catch (Exception statEx) { AppendLog($"�t�@�C���T�C�Y���v�̎擾�G���[: {statEx.Message}", LogLevel.Warning); if (this.compressionStatsLabel != null) this.compressionStatsLabel.Visible = false; }
                ShowNotification(displayMessage, NotificationType.Success); AppendLog($"����������Ɋ����B�o��: {outputZipPath}", LogLevel.Info);
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
                        ShowNotification($"�t�H���_���J���܂���ł���: {folderEx.Message}", NotificationType.Warning);
                        AppendLog($"�t�H���_�I�[�v���G���[: {argsPassedToWorker.EffectiveOutputFolder} - {folderEx.Message}", LogLevel.Warning);
                    }
                }

            }
            SetUIEnabledState(true);
        }
        private string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" }; int i = 0; double dblSByte = bytes;
            if (bytes == 0 && itemsToCompress.Count(it => !it.IsDirectoryItself) == 0) return "0 B"; // No files to compress
            if (bytes == 0 && itemsToCompress.Count(it => !it.IsDirectoryItself) > 0) return "0 B (�t�@�C���T�C�Y�擾�s��)";


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
                        AppendLog($"���O���x���� '{currentSelectedLogLevel}' �ɕύX����܂����B", LogLevel.Info);
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