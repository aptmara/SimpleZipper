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
using Ionic.Zip; // DotNetZip
using Ionic.Zlib; // DotNetZip
using System.Diagnostics;
using System.Text;

namespace SimpleZipper
{
    public partial class Form1 : Form // System.Windows.Forms.Form を継承
    {
        private List<string> filesToCompress = new List<string>();
        private Label notificationLabel;
        private System.Windows.Forms.Timer notificationTimer; // 明示的に指定
        private Panel warningPanel;
        private Label warningMessageLabel;
        private System.Windows.Forms.Timer animationTimer; // 明示的に指定
        private System.Windows.Forms.Timer displayTimer; // 明示的に指定
        private int warningPanelTargetY = 0;
        private int warningPanelHiddenY = -60; // warningPanelの高さに応じて調整
        private int animationStep = 5;
        private bool isWarningPanelDescending = false;
        private bool isWarningPanelAscending = false;
        private BackgroundWorker compressionWorker;
        private ToolTip listBoxToolTip;
        private string currentToolTipText = "";
        private enum ZipOperationMode { CreateNew, AddToExisting }
        private ZipOperationMode currentZipMode = ZipOperationMode.CreateNew;
        private string existingZipPathForAdd = null;
        private List<ThemeColors> availableThemes = new List<ThemeColors>();
        private enum LogLevel { Info, Warning, Error, Debug }
        private enum NotificationType { Info, Warning, Error, Success } // クラス内に定義
        private CompressionArguments argsPassedToWorker = null;

        public Form1()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            this.KeyPreview = true;
            this.AllowDrop = true;

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

            LoadSettings();
            AppendLog("アプリケーションを起動しました。", LogLevel.Info);
            InitializeFormDragDropEvents();

            // --- イベントハンドラの明示的な登録 ---
            // デザイナでの紐付けがうまくいかない場合や、確実に登録するためにコードで追加します。
            // もしデザイナで正しく紐付けられている場合、これらの行は重複登録になる可能性がありますが、
            // 通常は問題ありません（複数回呼ばれるだけ）。
            // エラー解消のため、まずはデザイナの不要な紐付けを削除することを優先してください。

            var selectFilesBtn = this.Controls.Find("selectFilesButton", true).FirstOrDefault() as Button;
            if (selectFilesBtn != null) selectFilesBtn.Click += new System.EventHandler(this.selectFilesButton_Click);

            var clearFileListBtn = this.Controls.Find("clearFileListButton", true).FirstOrDefault() as Button;
            if (clearFileListBtn != null) clearFileListBtn.Click += new System.EventHandler(this.clearFileListButton_Click);

            var removeSelectedFileBtn = this.Controls.Find("removeSelectedFileButton", true).FirstOrDefault() as Button;
            if (removeSelectedFileBtn != null) removeSelectedFileBtn.Click += new System.EventHandler(this.removeSelectedFileButton_Click);

            var selectOutputFolderBtn = this.Controls.Find("selectOutputFolderButton", true).FirstOrDefault() as Button;
            if (selectOutputFolderBtn != null) selectOutputFolderBtn.Click += new System.EventHandler(this.selectOutputFolderButton_Click);

            var compressBtn = this.Controls.Find("compressButton", true).FirstOrDefault() as Button;
            if (compressBtn != null) compressBtn.Click += new System.EventHandler(this.compressButton_Click);

            var sflb = this.Controls.Find("selectedFilesListBox", true).FirstOrDefault() as ListBox;
            if (sflb != null) sflb.KeyDown += new KeyEventHandler(this.selectedFilesListBox_KeyDown);
            // ------------------------------------
        }

        private void InitializeFormDragDropEvents()
        {
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDroppedItems((string[])e.Data.GetData(DataFormats.FileDrop));
        }

        private void InitializeThemes()
        {
            availableThemes.Add(ThemeColors.LightTheme);
            availableThemes.Add(ThemeColors.DarkTheme);

            var themeCb = this.Controls.Find("themeComboBox", true).FirstOrDefault() as ComboBox;
            if (themeCb != null)
            {
                themeCb.DataSource = null;
                themeCb.DataSource = availableThemes;
                themeCb.DisplayMember = "Name";
                themeCb.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;
            }
        }
        private void ThemeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var themeCb = sender as ComboBox;
            if (themeCb != null && themeCb.SelectedItem is ThemeColors selectedTheme)
            {
                ApplyTheme(selectedTheme);
                Properties.Settings.Default.CurrentThemeName = selectedTheme.Name;
            }
        }
        private void ApplyTheme(ThemeColors theme)
        {
            if (theme == null) return;
            this.BackColor = theme.FormBackColor;
            this.ForeColor = theme.FormForeColor;
            ApplyThemeToControls(this.Controls, theme);
            var logTb = this.Controls.Find("logTextBox", true).FirstOrDefault() as TextBox;
            if (logTb != null)
            {
                logTb.BackColor = theme.LogBackColor;
                logTb.ForeColor = theme.LogForeColor;
            }
            UpdateNotificationColorsWithCurrentTheme();
            AppendLog($"テーマ変更: {theme.Name}", LogLevel.Info);
        }

        private void UpdateNotificationColorsWithCurrentTheme()
        {
            ThemeColors currentTheme = (this.Controls.Find("themeComboBox", true).FirstOrDefault() as ComboBox)?.SelectedItem as ThemeColors ?? ThemeColors.LightTheme;
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
        private NotificationType GetNotificationTypeFromLabelColor(Color backColor, ThemeColors theme)
        {
            if (theme == null) return NotificationType.Info;
            if (backColor == theme.SuccessNotificationBackColor) return NotificationType.Success;
            if (backColor == theme.InfoNotificationBackColor) return NotificationType.Info;
            return NotificationType.Info;
        }
        private void SetNotificationLabelColors(NotificationType type, ThemeColors theme)
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
        private void SetAnimatedWarningColors(NotificationType type, ThemeColors theme)
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

        private void ApplyThemeToControls(Control.ControlCollection controls, ThemeColors theme)
        {
            if (theme == null) return;
            foreach (Control control in controls)
            {
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
                    if (panel == warningPanel) continue;
                    panel.BackColor = theme.FormBackColor;
                    ApplyThemeToControls(panel.Controls, theme);
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.BackColor = theme.InputBackColor;
                    numericUpDown.ForeColor = theme.InputForeColor;
                }
            }
        }

        private void InitializeSplitControls()
        {
            var enableSplitCheckbox = this.Controls.Find("enableSplitZipCheckBox", true).FirstOrDefault() as CheckBox;
            var splitSizeLbl = this.Controls.Find("splitSizeLabel", true).FirstOrDefault() as Label;
            var splitSizeNum = this.Controls.Find("splitSizeNumericUpDown", true).FirstOrDefault() as NumericUpDown;
            var splitUnitCb = this.Controls.Find("splitUnitComboBox", true).FirstOrDefault() as ComboBox;

            if (enableSplitCheckbox != null && splitSizeLbl != null && splitSizeNum != null && splitUnitCb != null)
            {
                Action updateSplitControlsState = () =>
                {
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
            var sflb = this.Controls.Find("selectedFilesListBox", true).FirstOrDefault() as ListBox;
            if (sflb != null)
            {
                sflb.MouseMove += selectedFilesListBox_MouseMove;
                sflb.MouseLeave += selectedFilesListBox_MouseLeave;
            }
        }
        private void selectedFilesListBox_MouseMove(object sender, MouseEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb == null || listBoxToolTip == null) return;
            int index = lb.IndexFromPoint(e.Location);
            if (index >= 0 && index < lb.Items.Count && index < filesToCompress.Count)
            {
                string fullPath = filesToCompress[index];
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
        private void selectedFilesListBox_MouseLeave(object sender, EventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb != null && listBoxToolTip != null)
            {
                listBoxToolTip.Hide(lb);
                currentToolTipText = "";
            }
        }

        private void InitializeCommentControls()
        {
            var enableCommentCheckbox = this.Controls.Find("enableZipCommentCheckBox", true).FirstOrDefault() as CheckBox;
            var commentLabel = this.Controls.Find("zipCommentLabel", true).FirstOrDefault() as Label;
            var commentTextbox = this.Controls.Find("zipCommentTextBox", true).FirstOrDefault() as TextBox;

            if (enableCommentCheckbox != null && commentLabel != null && commentTextbox != null)
            {
                commentLabel.Enabled = enableCommentCheckbox.Checked;
                commentTextbox.Enabled = enableCommentCheckbox.Checked;
                enableCommentCheckbox.CheckedChanged += (sender, e) =>
                {
                    bool isChecked = ((CheckBox)sender).Checked;
                    commentLabel.Enabled = isChecked;
                    commentTextbox.Enabled = isChecked;
                };
            }
        }

        private void InitializeOperationModeControls()
        {
            var createNewRadio = this.Controls.Find("createNewZipRadioButton", true).FirstOrDefault() as RadioButton;
            var addToExistingRadio = this.Controls.Find("addToExistingZipRadioButton", true).FirstOrDefault() as RadioButton;
            var selectExistingBtn = this.Controls.Find("selectExistingZipButton", true).FirstOrDefault() as Button;

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
        private void OperationMode_CheckedChanged(object sender, EventArgs e)
        {
            var createNewRadio = this.Controls.Find("createNewZipRadioButton", true).FirstOrDefault() as RadioButton;
            if (createNewRadio != null && createNewRadio.Checked)
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
            var existingZipLabel = this.Controls.Find("existingZipFileLabel", true).FirstOrDefault() as Label;
            var existingZipTb = this.Controls.Find("existingZipFileTextBox", true).FirstOrDefault() as TextBox;
            var selectExistingBtn = this.Controls.Find("selectExistingZipButton", true).FirstOrDefault() as Button;
            var outputFolderTb = this.Controls.Find("outputFolderTextBox", true).FirstOrDefault() as TextBox;
            var zipFileNameTb = this.Controls.Find("zipFileNameTextBox", true).FirstOrDefault() as TextBox;
            var selectOutputFolderBtn = this.Controls.Find("selectOutputFolderButton", true).FirstOrDefault() as Button;
            var compressBtn = this.Controls.Find("compressButton", true).FirstOrDefault() as Button;
            var enableSplitCheckbox = this.Controls.Find("enableSplitZipCheckBox", true).FirstOrDefault() as CheckBox;
            var splitSizeLbl = this.Controls.Find("splitSizeLabel", true).FirstOrDefault() as Label;
            var splitSizeNum = this.Controls.Find("splitSizeNumericUpDown", true).FirstOrDefault() as NumericUpDown;
            var splitUnitCb = this.Controls.Find("splitUnitComboBox", true).FirstOrDefault() as ComboBox;

            bool isCreateNewMode = (mode == ZipOperationMode.CreateNew);

            if (existingZipLabel != null) existingZipLabel.Visible = !isCreateNewMode;
            if (existingZipTb != null) existingZipTb.Visible = !isCreateNewMode;
            if (selectExistingBtn != null) { selectExistingBtn.Visible = !isCreateNewMode; selectExistingBtn.Enabled = !isCreateNewMode; }
            if (outputFolderTb != null) outputFolderTb.Enabled = isCreateNewMode;
            if (zipFileNameTb != null) zipFileNameTb.Enabled = isCreateNewMode;
            if (selectOutputFolderBtn != null) selectOutputFolderBtn.Enabled = isCreateNewMode;

            if (compressBtn != null) compressBtn.Text = isCreateNewMode ? "圧縮実行" : "ファイルを追加";
            if (isCreateNewMode) { existingZipPathForAdd = null; if (existingZipTb != null) existingZipTb.Clear(); }

            if (enableSplitCheckbox != null)
            {
                enableSplitCheckbox.Enabled = isCreateNewMode;
                if (!isCreateNewMode) enableSplitCheckbox.Checked = false;
            }
            bool splitControlsEnabled = isCreateNewMode && (enableSplitCheckbox?.Checked ?? false);
            if (splitSizeLbl != null) splitSizeLbl.Enabled = splitControlsEnabled;
            if (splitSizeNum != null) splitSizeNum.Enabled = splitControlsEnabled;
            if (splitUnitCb != null) splitUnitCb.Enabled = splitControlsEnabled;
        }
        private void selectExistingZipButton_Click(object sender, EventArgs e)
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
                    var existingZipTb = this.Controls.Find("existingZipFileTextBox", true).FirstOrDefault() as TextBox;
                    if (existingZipTb != null) existingZipTb.Text = existingZipPathForAdd;
                    AppendLog($"既存ZIP選択: {existingZipPathForAdd}", LogLevel.Info);
                }
            }
        }

        private void InitializePasswordControls()
        {
            var enablePwdCheckbox = this.Controls.Find("enablePasswordCheckBox", true).FirstOrDefault() as CheckBox;
            var pwdTextbox = this.Controls.Find("passwordTextBox", true).FirstOrDefault() as TextBox;
            var pwdLabel = this.Controls.Find("passwordLabel", true).FirstOrDefault() as Label;

            if (enablePwdCheckbox != null && pwdTextbox != null && pwdLabel != null)
            {
                pwdTextbox.Enabled = enablePwdCheckbox.Checked;
                pwdLabel.Enabled = enablePwdCheckbox.Checked;
                enablePwdCheckbox.CheckedChanged += (sender, e) =>
                {
                    bool isChecked = ((CheckBox)sender).Checked;
                    pwdTextbox.Enabled = isChecked;
                    pwdLabel.Enabled = isChecked;
                    if (!isChecked) pwdTextbox.Clear();
                };
            }
        }
        private void InitializeCompressionWorker()
        {
            compressionWorker = new BackgroundWorker();
            compressionWorker.WorkerReportsProgress = true;
            compressionWorker.WorkerSupportsCancellation = false;
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
        private void NotificationTimer_Tick(object sender, EventArgs e)
        {
            if (notificationLabel != null) notificationLabel.Visible = false;
            if (notificationTimer != null) notificationTimer.Stop();
        }
        private void AnimationTimer_Tick(object sender, EventArgs e)
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
        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            if (displayTimer != null) displayTimer.Stop();
            isWarningPanelAscending = true;
            if (animationTimer != null) animationTimer.Start();
        }

        private void LoadSettings()
        {
            try
            {
                var outputFolderTb = this.Controls.Find("outputFolderTextBox", true).FirstOrDefault() as TextBox;
                if (outputFolderTb != null) outputFolderTb.Text = Properties.Settings.Default.LastOutputFolder;

                var zipFileNameTb = this.Controls.Find("zipFileNameTextBox", true).FirstOrDefault() as TextBox;
                if (zipFileNameTb != null) zipFileNameTb.Text = Properties.Settings.Default.LastZipFileName;

                var compressionLevelCtrl = this.Controls.Find("compressionLevelComboBox", true).FirstOrDefault() as ComboBox;
                if (compressionLevelCtrl != null)
                {
                    if (compressionLevelCtrl.Items.Count == 0)
                    {
                        compressionLevelCtrl.Items.AddRange(new object[] { "標準", "速度優先", "高圧縮" });
                    }
                    if (Properties.Settings.Default.LastCompressionLevelIndex >= 0 && Properties.Settings.Default.LastCompressionLevelIndex < compressionLevelCtrl.Items.Count)
                        compressionLevelCtrl.SelectedIndex = Properties.Settings.Default.LastCompressionLevelIndex;
                    else if (compressionLevelCtrl.Items.Count > 0)
                        compressionLevelCtrl.SelectedIndex = 0;
                }

                var openOutputFolderCtrl = this.Controls.Find("openOutputFolderCheckBox", true).FirstOrDefault() as CheckBox;
                if (openOutputFolderCtrl != null) openOutputFolderCtrl.Checked = Properties.Settings.Default.OpenOutputFolder;

                var recursiveAddCtrl = this.Controls.Find("recursiveAddCheckBox", true).FirstOrDefault() as CheckBox;
                if (recursiveAddCtrl != null) recursiveAddCtrl.Checked = Properties.Settings.Default.RecursiveAdd;

                var enablePasswordCtrl = this.Controls.Find("enablePasswordCheckBox", true).FirstOrDefault() as CheckBox;
                if (enablePasswordCtrl != null) enablePasswordCtrl.Checked = Properties.Settings.Default.EnablePassword;

                var pwdTextbox = this.Controls.Find("passwordTextBox", true).FirstOrDefault() as TextBox;
                var pwdLabel = this.Controls.Find("passwordLabel", true).FirstOrDefault() as Label;
                if (enablePasswordCtrl != null && pwdTextbox != null && pwdLabel != null) { pwdTextbox.Enabled = enablePasswordCtrl.Checked; pwdLabel.Enabled = enablePasswordCtrl.Checked; }

                var createNewRadio = this.Controls.Find("createNewZipRadioButton", true).FirstOrDefault() as RadioButton;
                var addToExistingRadio = this.Controls.Find("addToExistingZipRadioButton", true).FirstOrDefault() as RadioButton;
                if (Properties.Settings.Default.LastOperationMode == (int)ZipOperationMode.AddToExisting && addToExistingRadio != null)
                {
                    currentZipMode = ZipOperationMode.AddToExisting;
                    addToExistingRadio.Checked = true;
                }
                else if (createNewRadio != null)
                {
                    currentZipMode = ZipOperationMode.CreateNew;
                    createNewRadio.Checked = true;
                }
                else
                {
                    currentZipMode = ZipOperationMode.CreateNew;
                }

                var existingZipTb = this.Controls.Find("existingZipFileTextBox", true).FirstOrDefault() as TextBox;
                if (existingZipTb != null) existingZipTb.Text = Properties.Settings.Default.LastExistingZipPath;
                if (currentZipMode == ZipOperationMode.AddToExisting) existingZipPathForAdd = Properties.Settings.Default.LastExistingZipPath;


                var enableCommentCtrl = this.Controls.Find("enableZipCommentCheckBox", true).FirstOrDefault() as CheckBox;
                var commentTextCtrl = this.Controls.Find("zipCommentTextBox", true).FirstOrDefault() as TextBox;
                var commentLabelCtrl = this.Controls.Find("zipCommentLabel", true).FirstOrDefault() as Label;
                if (enableCommentCtrl != null) enableCommentCtrl.Checked = Properties.Settings.Default.EnableZipComment;
                if (commentTextCtrl != null) commentTextCtrl.Text = Properties.Settings.Default.LastZipComment;
                if (enableCommentCtrl != null && commentLabelCtrl != null && commentTextCtrl != null) { commentLabelCtrl.Enabled = enableCommentCtrl.Checked; commentTextCtrl.Enabled = enableCommentCtrl.Checked; }

                var enableSplitCtrl = this.Controls.Find("enableSplitZipCheckBox", true).FirstOrDefault() as CheckBox;
                var splitSizeNumCtrl = this.Controls.Find("splitSizeNumericUpDown", true).FirstOrDefault() as NumericUpDown;
                var splitUnitCbCtrl = this.Controls.Find("splitUnitComboBox", true).FirstOrDefault() as ComboBox;
                if (enableSplitCtrl != null) enableSplitCtrl.Checked = Properties.Settings.Default.EnableSplitZip;
                if (splitSizeNumCtrl != null && Properties.Settings.Default.SplitSizeValue >= splitSizeNumCtrl.Minimum && Properties.Settings.Default.SplitSizeValue <= splitSizeNumCtrl.Maximum) splitSizeNumCtrl.Value = Properties.Settings.Default.SplitSizeValue;
                else if (splitSizeNumCtrl != null) splitSizeNumCtrl.Value = 100;

                if (splitUnitCbCtrl != null)
                {
                    if (splitUnitCbCtrl.Items.Count == 0)
                    {
                        splitUnitCbCtrl.Items.AddRange(new object[] { "MB", "KB" });
                    }
                    if (Properties.Settings.Default.SplitUnitIndex >= 0 && Properties.Settings.Default.SplitUnitIndex < splitUnitCbCtrl.Items.Count)
                        splitUnitCbCtrl.SelectedIndex = Properties.Settings.Default.SplitUnitIndex;
                    else if (splitUnitCbCtrl.Items.Count > 0)
                        splitUnitCbCtrl.SelectedIndex = 0;
                }

                string savedThemeName = Properties.Settings.Default.CurrentThemeName;
                ThemeColors themeToLoad = availableThemes.FirstOrDefault(t => t.Name == savedThemeName) ?? ThemeColors.LightTheme;
                var themeCb = this.Controls.Find("themeComboBox", true).FirstOrDefault() as ComboBox;
                if (themeCb != null)
                {
                    var foundTheme = availableThemes.FirstOrDefault(t => t.Name == savedThemeName);
                    if (foundTheme != null) themeCb.SelectedItem = foundTheme;
                    else if (availableThemes.Any()) themeCb.SelectedItem = ThemeColors.LightTheme;
                }
                else { ApplyTheme(themeToLoad); }

                UpdateUIMode(currentZipMode);

            }
            catch (Exception ex) { AppendLog("設定の読み込みに失敗: " + ex.Message, LogLevel.Error); if (availableThemes.Any()) ApplyTheme(ThemeColors.LightTheme); }
        }
        private void SaveSettings()
        {
            try
            {
                var outputFolderTb = this.Controls.Find("outputFolderTextBox", true).FirstOrDefault() as TextBox;
                if (outputFolderTb != null) Properties.Settings.Default.LastOutputFolder = outputFolderTb.Text;
                var zipFileNameTb = this.Controls.Find("zipFileNameTextBox", true).FirstOrDefault() as TextBox;
                if (zipFileNameTb != null) Properties.Settings.Default.LastZipFileName = zipFileNameTb.Text;

                var compressionLevelCtrl = this.Controls.Find("compressionLevelComboBox", true).FirstOrDefault() as ComboBox;
                if (compressionLevelCtrl != null) Properties.Settings.Default.LastCompressionLevelIndex = compressionLevelCtrl.SelectedIndex;
                var openOutputFolderCtrl = this.Controls.Find("openOutputFolderCheckBox", true).FirstOrDefault() as CheckBox;
                if (openOutputFolderCtrl != null) Properties.Settings.Default.OpenOutputFolder = openOutputFolderCtrl.Checked;
                var recursiveAddCtrl = this.Controls.Find("recursiveAddCheckBox", true).FirstOrDefault() as CheckBox;
                if (recursiveAddCtrl != null) Properties.Settings.Default.RecursiveAdd = recursiveAddCtrl.Checked;
                var enablePasswordCtrl = this.Controls.Find("enablePasswordCheckBox", true).FirstOrDefault() as CheckBox;
                if (enablePasswordCtrl != null) Properties.Settings.Default.EnablePassword = enablePasswordCtrl.Checked;
                Properties.Settings.Default.LastOperationMode = (int)currentZipMode;
                Properties.Settings.Default.LastExistingZipPath = existingZipPathForAdd ?? "";
                var enableCommentCtrl = this.Controls.Find("enableZipCommentCheckBox", true).FirstOrDefault() as CheckBox;
                if (enableCommentCtrl != null) Properties.Settings.Default.EnableZipComment = enableCommentCtrl.Checked;
                var commentTextCtrl = this.Controls.Find("zipCommentTextBox", true).FirstOrDefault() as TextBox;
                if (commentTextCtrl != null) Properties.Settings.Default.LastZipComment = commentTextCtrl.Text;
                var enableSplitCtrl = this.Controls.Find("enableSplitZipCheckBox", true).FirstOrDefault() as CheckBox;
                if (enableSplitCtrl != null) Properties.Settings.Default.EnableSplitZip = enableSplitCtrl.Checked;
                var splitSizeNumCtrl = this.Controls.Find("splitSizeNumericUpDown", true).FirstOrDefault() as NumericUpDown;
                if (splitSizeNumCtrl != null) Properties.Settings.Default.SplitSizeValue = (int)splitSizeNumCtrl.Value;
                var splitUnitCbCtrl = this.Controls.Find("splitUnitComboBox", true).FirstOrDefault() as ComboBox;
                if (splitUnitCbCtrl != null) Properties.Settings.Default.SplitUnitIndex = splitUnitCbCtrl.SelectedIndex;
                var themeCb = this.Controls.Find("themeComboBox", true).FirstOrDefault() as ComboBox;
                if (themeCb != null && themeCb.SelectedItem is ThemeColors selectedTheme) Properties.Settings.Default.CurrentThemeName = selectedTheme.Name;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex) { AppendLog("設定の保存に失敗: " + ex.Message, LogLevel.Error); }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (compressionWorker != null && compressionWorker.IsBusy)
            {
                var result = MessageBox.Show("圧縮処理が実行中です。本当に終了しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            AppendLog("アプリケーションを終了します。", LogLevel.Info);
            SaveSettings();
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            var selectFilesBtn = this.Controls.Find("selectFilesButton", true).FirstOrDefault() as Button;
            var selectOutputFolderBtn = this.Controls.Find("selectOutputFolderButton", true).FirstOrDefault() as Button;
            var compressBtn = this.Controls.Find("compressButton", true).FirstOrDefault() as Button;
            var clearBtn = this.Controls.Find("clearFileListButton", true).FirstOrDefault() as Button;
            var enablePasswordCtrl = this.Controls.Find("enablePasswordCheckBox", true).FirstOrDefault() as CheckBox;

            if (e.Control && e.KeyCode == Keys.O)
            {
                if (!e.Shift && selectFilesBtn != null && selectFilesBtn.Enabled) { selectFilesBtn.PerformClick(); e.Handled = true; }
                else if (e.Shift && selectOutputFolderBtn != null && selectOutputFolderBtn.Enabled) { selectOutputFolderBtn.PerformClick(); e.Handled = true; }
            }
            else if (e.Control && e.KeyCode == Keys.Enter && compressBtn != null && compressBtn.Enabled) { compressBtn.PerformClick(); e.Handled = true; }
            else if (e.Control && e.Shift && e.KeyCode == Keys.Delete && clearBtn != null && clearBtn.Enabled) { clearBtn.PerformClick(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.P && enablePasswordCtrl != null && enablePasswordCtrl.Enabled)
            {
                enablePasswordCtrl.Checked = !enablePasswordCtrl.Checked; e.Handled = true;
            }
        }
        private void selectedFilesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            var sflb = sender as ListBox;
            var removeBtn = this.Controls.Find("removeSelectedFileButton", true).FirstOrDefault() as Button;
            if (sflb != null && removeBtn != null && e.KeyCode == Keys.Delete && sflb.SelectedItems.Count > 0)
            {
                if (removeBtn.Enabled) removeBtn.PerformClick();
                e.Handled = true;
            }
        }
        private void InitializeDragDrop()
        {
            var sflb = this.Controls.Find("selectedFilesListBox", true).FirstOrDefault() as ListBox;
            if (sflb != null)
            {
                sflb.AllowDrop = true;
                sflb.DragEnter += new DragEventHandler(selectedFilesListBox_DragEnter);
                sflb.DragDrop += new DragEventHandler(selectedFilesListBox_DragDrop);
            }
        }
        private void selectedFilesListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
        }
        private void selectedFilesListBox_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDroppedItems((string[])e.Data.GetData(DataFormats.FileDrop));
        }
        // 通常の通知（上部ラベル）とアニメーション警告を出し分けるメソッド
        private void ShowNotification(string message, NotificationType type)
        {
            ThemeColors currentTheme = (this.Controls.Find("themeComboBox", true).FirstOrDefault() as ComboBox)?.SelectedItem as ThemeColors ?? ThemeColors.LightTheme;
            if (type == NotificationType.Error || type == NotificationType.Warning)
            {
                ShowAnimatedWarning(message, type); // エラーや警告はアニメーションで表示
            }
            else
            {
                // 上部固定ラベルでの通知
                if (notificationLabel == null || notificationTimer == null) return; // 初期化チェック
                notificationLabel.Text = message;
                SetNotificationLabelColors(type, currentTheme); // テーマに基づいた色設定
                notificationLabel.Visible = true;
                notificationTimer.Stop(); // 既存のタイマーをリセット
                notificationTimer.Start();
            }
        }

        // アニメーション付き警告表示メソッド
        private void ShowAnimatedWarning(string message, NotificationType type)
        {
            ThemeColors currentTheme = (this.Controls.Find("themeComboBox", true).FirstOrDefault() as ComboBox)?.SelectedItem as ThemeColors ?? ThemeColors.LightTheme;
            if (warningPanel == null || warningMessageLabel == null || animationTimer == null || displayTimer == null) return; // 初期化チェック

            // 既存のアニメーションがあれば停止
            if (isWarningPanelDescending || (isWarningPanelAscending && warningPanel.Location.Y < warningPanelTargetY))
            {
                animationTimer.Stop();
                displayTimer.Stop();
            }

            warningMessageLabel.Text = message;
            SetAnimatedWarningColors(type, currentTheme); // テーマに基づいた色設定

            warningPanel.Location = new Point(0, warningPanelHiddenY); // 開始位置リセット
            warningPanel.Visible = true;
            warningPanel.BringToFront(); // 最前面に
            if (notificationLabel != null && notificationLabel.Visible) notificationLabel.SendToBack(); // 通常通知が見えていたら背面に

            isWarningPanelDescending = true;
            isWarningPanelAscending = false;
            animationTimer.Start(); // アニメーション開始
        }

        private void ProcessDroppedItems(string[] droppedItems)
        {
            if (droppedItems == null) return;
            AppendLog($"ドラッグ＆ドロップ操作: {droppedItems.Length} アイテム", LogLevel.Info);
            var recursiveAddCtrl = this.Controls.Find("recursiveAddCheckBox", true).FirstOrDefault() as CheckBox;
            bool isRecursive = (recursiveAddCtrl?.Checked) ?? true;
            bool directoryDroppedWithoutRecursiveSupport = false;
            foreach (string itemPath in droppedItems)
            {
                if (File.Exists(itemPath)) AddFileToList(itemPath);
                else if (Directory.Exists(itemPath))
                {
                    AddDirectoryFilesToList(itemPath, isRecursive);
                    if (!isRecursive && Directory.GetFiles(itemPath).Length == 0 && Directory.GetDirectories(itemPath).Length == 0) { }
                    else if (!isRecursive) directoryDroppedWithoutRecursiveSupport = true;
                }
            }
            if (directoryDroppedWithoutRecursiveSupport)
                ShowNotification("フォルダが追加されましたが、再帰オプションが無効のため直下のファイルのみが対象です（もしあれば）。", NotificationType.Info);
        }
        private void AddFileToList(string filePath)
        {
            if (!filesToCompress.Contains(filePath))
            {
                filesToCompress.Add(filePath);
                var sflb = this.Controls.Find("selectedFilesListBox", true).FirstOrDefault() as ListBox;
                if (sflb != null) sflb.Items.Add(Path.GetFileName(filePath));
                AppendLog($"ファイル追加: {filePath}", LogLevel.Debug);
            }
        }
        private void AddDirectoryFilesToList(string directoryPath, bool recursive)
        {
            AppendLog($"フォルダ内のファイル検索開始: {directoryPath} (再帰: {recursive})", LogLevel.Debug);
            try
            {
                foreach (string file in Directory.GetFiles(directoryPath)) AddFileToList(file);
                if (recursive)
                {
                    foreach (string subDirectory in Directory.GetDirectories(directoryPath)) AddDirectoryFilesToList(subDirectory, true);
                }
            }
            catch (Exception ex) { ShowNotification($"フォルダアクセスエラー ({Path.GetFileName(directoryPath)}): {ex.Message}", NotificationType.Warning); AppendLog($"フォルダアクセスエラー: {directoryPath} - {ex.Message}", LogLevel.Error); }
            AppendLog($"フォルダ内のファイル検索終了: {directoryPath}", LogLevel.Debug);
        }
        private void selectFilesButton_Click(object sender, EventArgs e)
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
        private void selectOutputFolderButton_Click(object sender, EventArgs e)
        {
            var outputFolderTb = this.Controls.Find("outputFolderTextBox", true).FirstOrDefault() as TextBox;
            if (outputFolderTb == null) return;
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "ZIPファイルの保存先フォルダを選択してください";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK) outputFolderTb.Text = folderBrowserDialog.SelectedPath;
            }
        }
        private void removeSelectedFileButton_Click(object sender, EventArgs e)
        {
            var sflb = this.Controls.Find("selectedFilesListBox", true).FirstOrDefault() as ListBox;
            if (sflb == null) return;

            List<int> selectedIndices = sflb.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
            if (!selectedIndices.Any()) return;

            foreach (int selectedIndex in selectedIndices)
            {
                if (selectedIndex >= 0 && selectedIndex < filesToCompress.Count && selectedIndex < sflb.Items.Count)
                {
                    filesToCompress.RemoveAt(selectedIndex);
                    sflb.Items.RemoveAt(selectedIndex);
                }
            }
            AppendLog($"{selectedIndices.Count} 個のファイルを選択リストから削除しました。", LogLevel.Debug);
        }
        private void clearFileListButton_Click(object sender, EventArgs e)
        {
            var sflb = this.Controls.Find("selectedFilesListBox", true).FirstOrDefault() as ListBox;
            if (sflb != null) sflb.Items.Clear();
            filesToCompress.Clear();
            AppendLog("ファイルリストをクリアしました。", LogLevel.Debug);
        }
        private void compressButton_Click(object sender, EventArgs e)
        {
            if (compressionWorker.IsBusy) { ShowNotification("圧縮処理が実行中です。", NotificationType.Info); return; }
            if (filesToCompress.Count == 0) { ShowNotification((currentZipMode == ZipOperationMode.CreateNew ? "圧縮" : "追加") + "するファイルが選択されていません。", NotificationType.Warning); return; }

            var outputFolderTb = this.Controls.Find("outputFolderTextBox", true).FirstOrDefault() as TextBox;
            var zipFileNameTb = this.Controls.Find("zipFileNameTextBox", true).FirstOrDefault() as TextBox;

            string outputZipPathValue; string effectiveOutputFolder; string effectiveZipFileName;
            if (currentZipMode == ZipOperationMode.AddToExisting)
            {
                if (string.IsNullOrWhiteSpace(existingZipPathForAdd) || !File.Exists(existingZipPathForAdd)) { ShowNotification("追加先の有効なZIPファイルが選択されていません。", NotificationType.Warning); return; }
                outputZipPathValue = existingZipPathForAdd; effectiveOutputFolder = Path.GetDirectoryName(outputZipPathValue); effectiveZipFileName = Path.GetFileName(outputZipPathValue);
            }
            else
            {
                if (outputFolderTb == null || string.IsNullOrWhiteSpace(outputFolderTb.Text)) { ShowNotification("保存先フォルダが選択されていません。", NotificationType.Warning); return; }
                if (zipFileNameTb == null) { ShowNotification("ZIPファイル名テキストボックスが見つかりません。", NotificationType.Error); return; }
                effectiveZipFileName = zipFileNameTb.Text;
                if (string.IsNullOrWhiteSpace(effectiveZipFileName)) { ShowNotification("ZIPファイル名を入力してください。", NotificationType.Warning); return; }
                if (!effectiveZipFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) effectiveZipFileName += ".zip";
                outputZipPathValue = Path.Combine(outputFolderTb.Text, effectiveZipFileName); effectiveOutputFolder = outputFolderTb.Text;
                if (File.Exists(outputZipPathValue))
                {
                    DialogResult userChoice = MessageBox.Show($"ファイル '{effectiveZipFileName}' は既に存在します。\n\nはい: 上書きします\nいいえ: 新しい名前で保存します\nキャンセル: 処理を中止します", "ファイル名の競合", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (userChoice == DialogResult.No) { outputZipPathValue = GetUniqueFileName(outputFolderTb.Text, effectiveZipFileName); zipFileNameTb.Text = Path.GetFileName(outputZipPathValue); }
                    else if (userChoice == DialogResult.Cancel) { ShowNotification("圧縮処理をキャンセルしました。", NotificationType.Info); return; }
                }
            }
            AppendLog($"圧縮処理開始ボタンクリック。モード: {currentZipMode}", LogLevel.Info);
            if (currentZipMode == ZipOperationMode.CreateNew) AppendLog($"新規ZIP作成: {outputZipPathValue}", LogLevel.Info); else AppendLog($"既存ZIPに追加: {existingZipPathForAdd}", LogLevel.Info);
            Ionic.Zlib.CompressionLevel ionicLevel = Ionic.Zlib.CompressionLevel.Default;
            var compressionLevelControl = this.Controls.Find("compressionLevelComboBox", true).FirstOrDefault() as ComboBox;
            if (compressionLevelControl != null && compressionLevelControl.SelectedItem != null) { switch (compressionLevelControl.SelectedItem.ToString()) { case "速度優先": ionicLevel = Ionic.Zlib.CompressionLevel.BestSpeed; break; case "高圧縮": ionicLevel = Ionic.Zlib.CompressionLevel.BestCompression; break; } }
            AppendLog($"圧縮レベル: {(compressionLevelControl?.SelectedItem?.ToString() ?? "デフォルト")}", LogLevel.Debug);
            bool enablePassword = false; string password = null;
            var enablePasswordCtrl = this.Controls.Find("enablePasswordCheckBox", true).FirstOrDefault() as CheckBox; var passwordCtrl = this.Controls.Find("passwordTextBox", true).FirstOrDefault() as TextBox;
            if (enablePasswordCtrl != null && enablePasswordCtrl.Checked) { if (passwordCtrl != null && !string.IsNullOrWhiteSpace(passwordCtrl.Text)) { enablePassword = true; password = passwordCtrl.Text; } else { ShowNotification("パスワードが設定されていません。", NotificationType.Warning); return; } }
            AppendLog($"パスワード保護: {enablePassword}", LogLevel.Debug);
            bool enableComment = false; string zipComment = null;
            var enableCommentCtrl = this.Controls.Find("enableZipCommentCheckBox", true).FirstOrDefault() as CheckBox; var commentTextCtrl = this.Controls.Find("zipCommentTextBox", true).FirstOrDefault() as TextBox;
            if (enableCommentCtrl != null && enableCommentCtrl.Checked) { if (commentTextCtrl != null) { enableComment = true; zipComment = commentTextCtrl.Text; } }
            AppendLog($"ZIPコメント: {enableComment}", LogLevel.Debug);
            long splitSizeInBytes = 0; bool enableSplit = false;
            var enableSplitCtrl = this.Controls.Find("enableSplitZipCheckBox", true).FirstOrDefault() as CheckBox;
            if (currentZipMode == ZipOperationMode.CreateNew && enableSplitCtrl != null && enableSplitCtrl.Checked)
            {
                enableSplit = true; var splitSizeNumCtrl = this.Controls.Find("splitSizeNumericUpDown", true).FirstOrDefault() as NumericUpDown; var splitUnitCbCtrl = this.Controls.Find("splitUnitComboBox", true).FirstOrDefault() as ComboBox;
                if (splitSizeNumCtrl != null && splitUnitCbCtrl != null && splitUnitCbCtrl.SelectedItem != null)
                {
                    long sizeValue = (long)splitSizeNumCtrl.Value; string unit = splitUnitCbCtrl.SelectedItem.ToString();
                    if (unit == "MB") splitSizeInBytes = sizeValue * 1024 * 1024; else if (unit == "KB") splitSizeInBytes = sizeValue * 1024;
                    if (splitSizeInBytes <= 0) { enableSplit = false; splitSizeInBytes = 0; AppendLog("分割サイズが無効なため、分割しません。", LogLevel.Warning); }
                    else AppendLog($"ZIP分割有効。サイズ: {sizeValue} {unit} ({splitSizeInBytes} bytes)", LogLevel.Debug);
                }
                else enableSplit = false;
            }
            var progressBar = this.Controls.Find("compressionProgressBar", true).FirstOrDefault() as ProgressBar; if (progressBar != null) { progressBar.Value = 0; progressBar.Visible = true; }
            var statsLabel = this.Controls.Find("compressionStatsLabel", true).FirstOrDefault() as Label; if (statsLabel != null) statsLabel.Visible = false;
            SetUIEnabledState(false);
            long originalTotalSizeBytes = 0; foreach (string filePath in filesToCompress) if (File.Exists(filePath)) try { originalTotalSizeBytes += new FileInfo(filePath).Length; } catch { AppendLog($"サイズ取得エラー(事前): {filePath}", LogLevel.Warning); }

            CompressionArguments args = new CompressionArguments { OperationMode = currentZipMode, FilesToCompress = new List<string>(filesToCompress), OutputZipPath = outputZipPathValue, LevelIonic = ionicLevel, EnablePassword = enablePassword, Password = password, ExistingZipPath = (currentZipMode == ZipOperationMode.AddToExisting) ? existingZipPathForAdd : null, EnableComment = enableComment, Comment = zipComment, EnableSplit = enableSplit, SplitSizeInBytes = splitSizeInBytes, OriginalTotalSize = originalTotalSizeBytes };
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
        "splitSizeNumericUpDown", "splitUnitComboBox", "themeComboBox"
    };

            foreach (string name in controlNames)
            {
                var ctrl = this.Controls.Find(name, true).FirstOrDefault();
                if (ctrl != null) ctrl.Enabled = enabled;
            }

            if (enabled) UpdateUIMode(currentZipMode);
            else
            {
                var pwdTb = this.Controls.Find("passwordTextBox", true).FirstOrDefault() as TextBox;
                if (pwdTb != null) pwdTb.Enabled = false;
                var commentTb = this.Controls.Find("zipCommentTextBox", true).FirstOrDefault() as TextBox;
                if (commentTb != null) commentTb.Enabled = false;
                var splitSizeNum = this.Controls.Find("splitSizeNumericUpDown", true).FirstOrDefault() as NumericUpDown;
                if (splitSizeNum != null) splitSizeNum.Enabled = false;
                var splitUnitCb = this.Controls.Find("splitUnitComboBox", true).FirstOrDefault() as ComboBox;
                if (splitUnitCb != null) splitUnitCb.Enabled = false;
                var selectExistingBtn = this.Controls.Find("selectExistingZipButton", true).FirstOrDefault() as Button;
                if (selectExistingBtn != null) selectExistingBtn.Enabled = false;
                var existingZipTb = this.Controls.Find("existingZipFileTextBox", true).FirstOrDefault() as TextBox;
                if (existingZipTb != null) existingZipTb.Enabled = false; // Also ensure this is disabled when all are disabled
            }
        }
        private class CompressionArguments { public ZipOperationMode OperationMode { get; set; } public List<string> FilesToCompress { get; set; } public string OutputZipPath { get; set; } public Ionic.Zlib.CompressionLevel LevelIonic { get; set; } public bool EnablePassword { get; set; } public string Password { get; set; } public string ExistingZipPath { get; set; } public bool EnableComment { get; set; } public string Comment { get; set; } public bool EnableSplit { get; set; } public long SplitSizeInBytes { get; set; } public long OriginalTotalSize { get; set; } }
        private void CompressionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker; CompressionArguments args = e.Argument as CompressionArguments; int filesProcessed = 0; int totalFiles = args.FilesToCompress.Count;
            try
            {
                if (args.OperationMode == ZipOperationMode.CreateNew)
                {
                    worker.ReportProgress(0, $"新規ZIP '{Path.GetFileName(args.OutputZipPath)}' 作成開始...");
                    using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile(System.Text.Encoding.GetEncoding("shift_jis")))
                    {
                        if (args.EnablePassword && !string.IsNullOrEmpty(args.Password)) { zip.Password = args.Password; zip.Encryption = EncryptionAlgorithm.WinZipAes256; }
                        zip.CompressionLevel = args.LevelIonic; if (args.EnableComment) zip.Comment = args.Comment;
                        if (args.EnableSplit && args.SplitSizeInBytes > 0)
                        {
                            if (args.SplitSizeInBytes > Int32.MaxValue) { worker.ReportProgress(0, "警告: 分割サイズが大きすぎるため、Int32.MaxValueが使用されます。"); zip.MaxOutputSegmentSize = Int32.MaxValue; }
                            else if (args.SplitSizeInBytes < 65536 && args.SplitSizeInBytes > 0) { worker.ReportProgress(0, "警告: 分割サイズが小さすぎるため、64KBが使用されます。"); zip.MaxOutputSegmentSize = 65536; }
                            else if (args.SplitSizeInBytes > 0) { zip.MaxOutputSegmentSize = (int)args.SplitSizeInBytes; }
                        }
                        zip.SaveProgress += (s, progressArgs) => { if (worker.CancellationPending) progressArgs.Cancel = true; if (progressArgs.EventType == ZipProgressEventType.Saving_BeforeWriteEntry) worker.ReportProgress(progressArgs.EntriesTotal > 0 ? progressArgs.EntriesSaved * 100 / progressArgs.EntriesTotal : 0, $"ファイル圧縮中: {progressArgs.CurrentEntry.FileName}"); else if (progressArgs.EventType == ZipProgressEventType.Saving_AfterWriteEntry) { filesProcessed++; worker.ReportProgress(totalFiles > 0 ? filesProcessed * 100 / totalFiles : 0, $"完了: {progressArgs.CurrentEntry.FileName}"); } else if (progressArgs.EventType == ZipProgressEventType.Saving_Completed) worker.ReportProgress(100); };
                        foreach (string filePath in args.FilesToCompress) if (File.Exists(filePath)) zip.AddFile(filePath, "");
                        zip.Save(args.OutputZipPath); worker.ReportProgress(100, $"新規ZIP '{Path.GetFileName(args.OutputZipPath)}'{(args.EnableSplit && args.SplitSizeInBytes > 0 ? " (分割)" : "")} 作成完了。");
                    }
                }
                else
                {
                    worker.ReportProgress(0, $"既存ZIP '{Path.GetFileName(args.ExistingZipPath)}' への追加開始...");
                    using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(args.ExistingZipPath))
                    {
                        if (args.EnablePassword && !string.IsNullOrEmpty(args.Password)) { zip.Password = args.Password; zip.Encryption = EncryptionAlgorithm.WinZipAes256; }
                        zip.CompressionLevel = args.LevelIonic; if (args.EnableComment) zip.Comment = args.Comment;
                        zip.SaveProgress += (s, progressArgs) => { if (worker.CancellationPending) progressArgs.Cancel = true; if (progressArgs.EventType == ZipProgressEventType.Saving_BeforeWriteEntry) worker.ReportProgress(progressArgs.EntriesTotal > 0 ? progressArgs.EntriesSaved * 100 / progressArgs.EntriesTotal : 0, $"ファイル追加中: {progressArgs.CurrentEntry.FileName}"); else if (progressArgs.EventType == ZipProgressEventType.Saving_AfterWriteEntry) { filesProcessed++; worker.ReportProgress(totalFiles > 0 ? filesProcessed * 100 / totalFiles : 0, $"完了: {progressArgs.CurrentEntry.FileName}"); } else if (progressArgs.EventType == ZipProgressEventType.Saving_Completed) worker.ReportProgress(100); };
                        foreach (string filePath in args.FilesToCompress) if (File.Exists(filePath)) zip.AddFile(filePath, "");
                        zip.Save();
                        worker.ReportProgress(100, $"既存ZIP '{Path.GetFileName(args.ExistingZipPath)}' への追加完了。");
                    }
                }
                e.Result = args.OutputZipPath;
            }
            catch (Exception ex) { worker.ReportProgress(0, $"エラー発生: {ex.Message}"); e.Result = ex; }
        }
        private void CompressionWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var progressBar = this.Controls.Find("compressionProgressBar", true).FirstOrDefault() as ProgressBar; if (progressBar != null) progressBar.Value = e.ProgressPercentage;
            if (e.UserState != null) AppendLog(e.UserState.ToString(), LogLevel.Debug);
        }
        private void CompressionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var progressBar = this.Controls.Find("compressionProgressBar", true).FirstOrDefault() as ProgressBar; if (progressBar != null) progressBar.Visible = false;
            var statsLabel = this.Controls.Find("compressionStatsLabel", true).FirstOrDefault() as Label; if (statsLabel != null) statsLabel.Text = "";
            string operationMessage = "処理";
            if (argsPassedToWorker != null) operationMessage = (argsPassedToWorker.OperationMode == ZipOperationMode.CreateNew) ? "圧縮" : "ファイル追加";

            if (e.Error != null) { ShowNotification($"{operationMessage}エラー: {e.Error.Message}", NotificationType.Error); AppendLog($"{operationMessage}中に予期せぬエラー: {e.Error.ToString()}", LogLevel.Error); }
            else if (e.Result is Exception ex) { ShowNotification($"{operationMessage}エラー: {ex.Message}", NotificationType.Error); AppendLog($"{operationMessage}中にエラー: {ex.ToString()}", LogLevel.Error); }
            else if (e.Cancelled) { ShowNotification($"{operationMessage}がキャンセルされました。", NotificationType.Info); AppendLog($"{operationMessage}はキャンセルされました。", LogLevel.Warning); }
            else if (e.Result is string outputZipPath)
            {
                long originalSize = argsPassedToWorker?.OriginalTotalSize ?? 0; long compressedSize = 0; string displayMessage = "";
                try
                {
                    if (argsPassedToWorker != null && argsPassedToWorker.EnableSplit && argsPassedToWorker.SplitSizeInBytes > 0 && argsPassedToWorker.OperationMode == ZipOperationMode.CreateNew)
                    {
                        string baseName = Path.Combine(Path.GetDirectoryName(outputZipPath), Path.GetFileNameWithoutExtension(outputZipPath));
                        if (File.Exists(outputZipPath)) compressedSize += new FileInfo(outputZipPath).Length;
                        int segment = 1;
                        while (true) { string segmentFile = $"{baseName}.z{segment:00}"; if (File.Exists(segmentFile)) { compressedSize += new FileInfo(segmentFile).Length; segment++; } else break; }
                        displayMessage = $"分割{operationMessage}完了: {Path.GetFileName(outputZipPath)} 他 ({segment - 1}セグメント)";
                    }
                    else if (File.Exists(outputZipPath)) { compressedSize = new FileInfo(outputZipPath).Length; displayMessage = $"{operationMessage}完了: {Path.GetFileName(outputZipPath)}"; }
                    if (statsLabel != null)
                    {
                        statsLabel.Text = $"元サイズ: {FormatBytes(originalSize)} → 圧縮後サイズ: {FormatBytes(compressedSize)}";
                        if (originalSize > 0 && argsPassedToWorker?.OperationMode == ZipOperationMode.CreateNew && compressedSize >= 0) { double ratio = (originalSize == 0) ? 0 : (double)compressedSize / originalSize; statsLabel.Text += $" (圧縮率: {ratio:P0})"; }
                        else if (originalSize == 0 && compressedSize == 0 && argsPassedToWorker?.OperationMode == ZipOperationMode.CreateNew) { statsLabel.Text += $" (圧縮率: N/A)"; }
                        statsLabel.Visible = true;
                    }
                }
                catch (Exception statEx) { AppendLog($"ファイルサイズ統計の取得エラー: {statEx.Message}", LogLevel.Warning); if (statsLabel != null) statsLabel.Visible = false; }
                ShowNotification(displayMessage, NotificationType.Success); AppendLog($"処理が正常に完了。出力: {outputZipPath}", LogLevel.Info);
                if (argsPassedToWorker != null && outputZipPath == argsPassedToWorker.OutputZipPath &&
                   (argsPassedToWorker.OperationMode == ZipOperationMode.CreateNew || argsPassedToWorker.OperationMode == ZipOperationMode.AddToExisting))
                {
                    var sflb = this.Controls.Find("selectedFilesListBox", true).FirstOrDefault() as ListBox;
                    if (sflb != null) sflb.Items.Clear();
                    filesToCompress.Clear();
                }
                var openFolderCheckBox = this.Controls.Find("openOutputFolderCheckBox", true).FirstOrDefault() as CheckBox;
                var outputFolderTb = this.Controls.Find("outputFolderTextBox", true).FirstOrDefault() as TextBox;
                string folderToOpen = (argsPassedToWorker?.OperationMode == ZipOperationMode.CreateNew && outputFolderTb != null) ? outputFolderTb.Text : Path.GetDirectoryName(argsPassedToWorker?.ExistingZipPath);
                if ((openFolderCheckBox?.Checked ?? false) && !string.IsNullOrEmpty(folderToOpen) && Directory.Exists(folderToOpen))
                { try { Process.Start("explorer.exe", folderToOpen); } catch (Exception folderEx) { ShowNotification($"フォルダを開けませんでした: {folderEx.Message}", NotificationType.Warning); AppendLog($"フォルダオープンエラー: {folderEx.Message}", LogLevel.Warning); } }
            }
            SetUIEnabledState(true);
        }
        private string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" }; int i = 0; double dblSByte = bytes;
            if (bytes == 0) return "0 B";
            for (i = 0; i < suffix.Length && bytes >= 1024; i++)
            {
                dblSByte = (double)bytes / 1024.0;
                bytes /= 1024;
            }
            if (i == 0 && dblSByte > 0) { /* bytes < 1024, dblSByte is original bytes */ }
            else if (i == 0 && dblSByte == 0 && filesToCompress.Count > 0) { /* original bytes was 0 but there were files */ }
            else if (i == 0) dblSByte = 0; // If bytes was 0 initially, and loop didn't run
            return String.Format("{0:0.##} {1}", dblSByte, suffix[i]);
        }
        private void AppendLog(string message, LogLevel level = LogLevel.Info)
        {
            var logger = this.Controls.Find("logTextBox", true).FirstOrDefault() as TextBox; if (logger == null) return;
            if (logger.InvokeRequired) logger.Invoke(new Action(() => AppendLogInternal(logger, message, level))); else AppendLogInternal(logger, message, level);
        }
        private void AppendLogInternal(TextBox logger, string message, LogLevel level)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); logger.AppendText($"{timestamp} [{level}] {message}{Environment.NewLine}"); logger.ScrollToCaret();
        }

        private void selectedFilesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
