namespace SimpleZipper
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            themeComboBox = new ComboBox();
            operationModeGroupBox = new GroupBox();
            existingZipFileTextBox = new TextBox();
            selectExistingZipButton = new Button();
            existingZipFileLabel = new Label();
            addToExistingZipRadioButton = new RadioButton();
            createNewZipRadioButton = new RadioButton();
            outputFolderTextBox = new TextBox();
            clearFileListButton = new Button();
            selectFilesButton = new Button();
            selectOutputFolderButton = new Button();
            selectedFilesListBox = new ListBox();
            recursiveAddCheckBox = new CheckBox();
            removeSelectedFileButton = new Button();
            labelForOutputFolder = new Label();
            labelForZipFileName = new Label();
            passwordTextBox = new TextBox();
            zipFileNameTextBox = new TextBox();
            compressionLevelComboBox = new ComboBox();
            CompressionOptionsGroup = new GroupBox();
            zipCommentLabel = new Label();
            openOutputFolderCheckBox = new CheckBox();
            splitSizeLabel = new Label();
            splitUnitComboBox = new ComboBox();
            zipCommentTextBox = new TextBox();
            splitSizeNumericUpDown = new NumericUpDown();
            passwordLabel = new Label();
            label1 = new Label();
            enableSplitZipCheckBox = new CheckBox();
            enablePasswordCheckBox = new CheckBox();
            enableZipCommentCheckBox = new CheckBox();
            compressButton = new Button();
            compressionProgressBar = new ProgressBar();
            compressionStatsLabel = new Label();
            logTextBox = new TextBox();
            toolTip1 = new ToolTip(components);
            operationModeGroupBox.SuspendLayout();
            CompressionOptionsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitSizeNumericUpDown).BeginInit();
            SuspendLayout();
            // 
            // themeComboBox
            // 
            themeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            themeComboBox.FormattingEnabled = true;
            themeComboBox.Location = new Point(12, 696);
            themeComboBox.Name = "themeComboBox";
            themeComboBox.Size = new Size(234, 33);
            themeComboBox.TabIndex = 0;
            // 
            // operationModeGroupBox
            // 
            operationModeGroupBox.Controls.Add(existingZipFileTextBox);
            operationModeGroupBox.Controls.Add(selectExistingZipButton);
            operationModeGroupBox.Controls.Add(existingZipFileLabel);
            operationModeGroupBox.Controls.Add(addToExistingZipRadioButton);
            operationModeGroupBox.Controls.Add(createNewZipRadioButton);
            operationModeGroupBox.Location = new Point(5, 30);
            operationModeGroupBox.Name = "operationModeGroupBox";
            operationModeGroupBox.Size = new Size(690, 109);
            operationModeGroupBox.TabIndex = 2;
            operationModeGroupBox.TabStop = false;
            operationModeGroupBox.Text = "操作モード";
            // 
            // existingZipFileTextBox
            // 
            existingZipFileTextBox.Location = new Point(389, 65);
            existingZipFileTextBox.Name = "existingZipFileTextBox";
            existingZipFileTextBox.ReadOnly = true;
            existingZipFileTextBox.Size = new Size(295, 31);
            existingZipFileTextBox.TabIndex = 11;
            // 
            // selectExistingZipButton
            // 
            selectExistingZipButton.Location = new Point(197, 26);
            selectExistingZipButton.Name = "selectExistingZipButton";
            selectExistingZipButton.Size = new Size(151, 36);
            selectExistingZipButton.TabIndex = 4;
            selectExistingZipButton.Text = "既存ZIP選択...";
            selectExistingZipButton.UseVisualStyleBackColor = true;
            // 
            // existingZipFileLabel
            // 
            existingZipFileLabel.AutoSize = true;
            existingZipFileLabel.Location = new Point(354, 32);
            existingZipFileLabel.Name = "existingZipFileLabel";
            existingZipFileLabel.Size = new Size(138, 25);
            existingZipFileLabel.TabIndex = 2;
            existingZipFileLabel.Text = "対象ZIPファイル :";
            // 
            // addToExistingZipRadioButton
            // 
            addToExistingZipRadioButton.AutoSize = true;
            addToExistingZipRadioButton.Location = new Point(16, 30);
            addToExistingZipRadioButton.Name = "addToExistingZipRadioButton";
            addToExistingZipRadioButton.Size = new Size(169, 29);
            addToExistingZipRadioButton.TabIndex = 1;
            addToExistingZipRadioButton.TabStop = true;
            addToExistingZipRadioButton.Text = "既存のZIPに追加";
            addToExistingZipRadioButton.UseVisualStyleBackColor = true;
            // 
            // createNewZipRadioButton
            // 
            createNewZipRadioButton.AutoSize = true;
            createNewZipRadioButton.Location = new Point(16, 65);
            createNewZipRadioButton.Name = "createNewZipRadioButton";
            createNewZipRadioButton.Size = new Size(138, 29);
            createNewZipRadioButton.TabIndex = 0;
            createNewZipRadioButton.TabStop = true;
            createNewZipRadioButton.Text = "新規ZIP作成";
            createNewZipRadioButton.UseVisualStyleBackColor = true;
            // 
            // outputFolderTextBox
            // 
            outputFolderTextBox.Location = new Point(380, 251);
            outputFolderTextBox.Name = "outputFolderTextBox";
            outputFolderTextBox.ReadOnly = true;
            outputFolderTextBox.Size = new Size(315, 31);
            outputFolderTextBox.TabIndex = 13;
            // 
            // clearFileListButton
            // 
            clearFileListButton.Location = new Point(528, 353);
            clearFileListButton.Name = "clearFileListButton";
            clearFileListButton.Size = new Size(129, 40);
            clearFileListButton.TabIndex = 10;
            clearFileListButton.Text = "全てクリア";
            clearFileListButton.UseVisualStyleBackColor = true;
            // 
            // selectFilesButton
            // 
            selectFilesButton.Location = new Point(12, 145);
            selectFilesButton.Name = "selectFilesButton";
            selectFilesButton.Size = new Size(134, 38);
            selectFilesButton.TabIndex = 9;
            selectFilesButton.Text = "ファイルを選択";
            selectFilesButton.UseVisualStyleBackColor = true;
            selectFilesButton.Click += selectFilesButton_Click;
            // 
            // selectOutputFolderButton
            // 
            selectOutputFolderButton.Location = new Point(207, 189);
            selectOutputFolderButton.Name = "selectOutputFolderButton";
            selectOutputFolderButton.Size = new Size(192, 40);
            selectOutputFolderButton.TabIndex = 8;
            selectOutputFolderButton.Text = "保存先フォルダを選択";
            selectOutputFolderButton.UseVisualStyleBackColor = true;
            // 
            // selectedFilesListBox
            // 
            selectedFilesListBox.AllowDrop = true;
            selectedFilesListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            selectedFilesListBox.FormattingEnabled = true;
            selectedFilesListBox.ItemHeight = 25;
            selectedFilesListBox.Location = new Point(12, 189);
            selectedFilesListBox.Name = "selectedFilesListBox";
            selectedFilesListBox.SelectionMode = SelectionMode.MultiExtended;
            selectedFilesListBox.Size = new Size(189, 204);
            selectedFilesListBox.TabIndex = 7;
            selectedFilesListBox.SelectedIndexChanged += selectedFilesListBox_SelectedIndexChanged;
            // 
            // recursiveAddCheckBox
            // 
            recursiveAddCheckBox.AutoSize = true;
            recursiveAddCheckBox.Location = new Point(390, 145);
            recursiveAddCheckBox.Name = "recursiveAddCheckBox";
            recursiveAddCheckBox.Size = new Size(267, 29);
            recursiveAddCheckBox.TabIndex = 6;
            recursiveAddCheckBox.Text = "フォルダ内を再帰的に追加する";
            recursiveAddCheckBox.UseVisualStyleBackColor = true;
            // 
            // removeSelectedFileButton
            // 
            removeSelectedFileButton.Location = new Point(296, 353);
            removeSelectedFileButton.Name = "removeSelectedFileButton";
            removeSelectedFileButton.Size = new Size(192, 40);
            removeSelectedFileButton.TabIndex = 5;
            removeSelectedFileButton.Text = "選択ファイルを削除";
            removeSelectedFileButton.UseVisualStyleBackColor = true;
            // 
            // labelForOutputFolder
            // 
            labelForOutputFolder.AutoSize = true;
            labelForOutputFolder.Location = new Point(232, 251);
            labelForOutputFolder.Name = "labelForOutputFolder";
            labelForOutputFolder.Size = new Size(134, 25);
            labelForOutputFolder.TabIndex = 12;
            labelForOutputFolder.Text = "保存先フォルダ :";
            // 
            // labelForZipFileName
            // 
            labelForZipFileName.AutoSize = true;
            labelForZipFileName.Location = new Point(252, 298);
            labelForZipFileName.Name = "labelForZipFileName";
            labelForZipFileName.Size = new Size(114, 25);
            labelForZipFileName.TabIndex = 14;
            labelForZipFileName.Text = "ZIPファイル名:";
            // 
            // passwordTextBox
            // 
            passwordTextBox.Enabled = false;
            passwordTextBox.Location = new Point(320, 74);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.PasswordChar = '*';
            passwordTextBox.Size = new Size(347, 31);
            passwordTextBox.TabIndex = 15;
            // 
            // zipFileNameTextBox
            // 
            zipFileNameTextBox.Location = new Point(380, 295);
            zipFileNameTextBox.Name = "zipFileNameTextBox";
            zipFileNameTextBox.Size = new Size(315, 31);
            zipFileNameTextBox.TabIndex = 16;
            zipFileNameTextBox.Text = "archive.zip";
            // 
            // compressionLevelComboBox
            // 
            compressionLevelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            compressionLevelComboBox.FormattingEnabled = true;
            compressionLevelComboBox.Location = new Point(115, 27);
            compressionLevelComboBox.Name = "compressionLevelComboBox";
            compressionLevelComboBox.Size = new Size(182, 33);
            compressionLevelComboBox.TabIndex = 17;
            // 
            // CompressionOptionsGroup
            // 
            CompressionOptionsGroup.Controls.Add(zipCommentLabel);
            CompressionOptionsGroup.Controls.Add(openOutputFolderCheckBox);
            CompressionOptionsGroup.Controls.Add(splitSizeLabel);
            CompressionOptionsGroup.Controls.Add(splitUnitComboBox);
            CompressionOptionsGroup.Controls.Add(zipCommentTextBox);
            CompressionOptionsGroup.Controls.Add(splitSizeNumericUpDown);
            CompressionOptionsGroup.Controls.Add(passwordLabel);
            CompressionOptionsGroup.Controls.Add(label1);
            CompressionOptionsGroup.Controls.Add(passwordTextBox);
            CompressionOptionsGroup.Controls.Add(enableSplitZipCheckBox);
            CompressionOptionsGroup.Controls.Add(enablePasswordCheckBox);
            CompressionOptionsGroup.Controls.Add(enableZipCommentCheckBox);
            CompressionOptionsGroup.Controls.Add(compressionLevelComboBox);
            CompressionOptionsGroup.Location = new Point(8, 399);
            CompressionOptionsGroup.Name = "CompressionOptionsGroup";
            CompressionOptionsGroup.Size = new Size(687, 233);
            CompressionOptionsGroup.TabIndex = 18;
            CompressionOptionsGroup.TabStop = false;
            CompressionOptionsGroup.Text = "圧縮オプション";
            // 
            // zipCommentLabel
            // 
            zipCommentLabel.AutoSize = true;
            zipCommentLabel.Enabled = false;
            zipCommentLabel.Location = new Point(306, 104);
            zipCommentLabel.Name = "zipCommentLabel";
            zipCommentLabel.Size = new Size(74, 25);
            zipCommentLabel.TabIndex = 29;
            zipCommentLabel.Text = "コメント :";
            // 
            // openOutputFolderCheckBox
            // 
            openOutputFolderCheckBox.AutoSize = true;
            openOutputFolderCheckBox.Checked = true;
            openOutputFolderCheckBox.CheckState = CheckState.Checked;
            openOutputFolderCheckBox.Location = new Point(401, 29);
            openOutputFolderCheckBox.Name = "openOutputFolderCheckBox";
            openOutputFolderCheckBox.Size = new Size(266, 29);
            openOutputFolderCheckBox.TabIndex = 28;
            openOutputFolderCheckBox.Text = "圧縮後に保存先フォルダを開く";
            openOutputFolderCheckBox.TextAlign = ContentAlignment.TopCenter;
            openOutputFolderCheckBox.UseVisualStyleBackColor = true;
            // 
            // splitSizeLabel
            // 
            splitSizeLabel.AutoSize = true;
            splitSizeLabel.Enabled = false;
            splitSizeLabel.Location = new Point(51, 190);
            splitSizeLabel.Name = "splitSizeLabel";
            splitSizeLabel.Size = new Size(100, 25);
            splitSizeLabel.TabIndex = 27;
            splitSizeLabel.Text = "分割サイズ:";
            // 
            // splitUnitComboBox
            // 
            splitUnitComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            splitUnitComboBox.Enabled = false;
            splitUnitComboBox.FormattingEnabled = true;
            splitUnitComboBox.Location = new Point(353, 186);
            splitUnitComboBox.Name = "splitUnitComboBox";
            splitUnitComboBox.Size = new Size(182, 33);
            splitUnitComboBox.TabIndex = 25;
            // 
            // zipCommentTextBox
            // 
            zipCommentTextBox.Enabled = false;
            zipCommentTextBox.Location = new Point(394, 112);
            zipCommentTextBox.Multiline = true;
            zipCommentTextBox.Name = "zipCommentTextBox";
            zipCommentTextBox.PasswordChar = '*';
            zipCommentTextBox.Size = new Size(273, 60);
            zipCommentTextBox.TabIndex = 26;
            // 
            // splitSizeNumericUpDown
            // 
            splitSizeNumericUpDown.Enabled = false;
            splitSizeNumericUpDown.Location = new Point(157, 188);
            splitSizeNumericUpDown.Maximum = new decimal(new int[] { 4096, 0, 0, 0 });
            splitSizeNumericUpDown.Name = "splitSizeNumericUpDown";
            splitSizeNumericUpDown.Size = new Size(180, 31);
            splitSizeNumericUpDown.TabIndex = 23;
            splitSizeNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // passwordLabel
            // 
            passwordLabel.AutoSize = true;
            passwordLabel.Enabled = false;
            passwordLabel.Location = new Point(224, 77);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new Size(90, 25);
            passwordLabel.TabIndex = 22;
            passwordLabel.Text = "パスワード:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 30);
            label1.Name = "label1";
            label1.Size = new Size(100, 25);
            label1.TabIndex = 21;
            label1.Text = "圧縮レベル:";
            // 
            // enableSplitZipCheckBox
            // 
            enableSplitZipCheckBox.AutoSize = true;
            enableSplitZipCheckBox.Location = new Point(9, 149);
            enableSplitZipCheckBox.Name = "enableSplitZipCheckBox";
            enableSplitZipCheckBox.Size = new Size(199, 29);
            enableSplitZipCheckBox.TabIndex = 20;
            enableSplitZipCheckBox.Text = "ZIPファイルを分割する";
            enableSplitZipCheckBox.TextAlign = ContentAlignment.TopCenter;
            enableSplitZipCheckBox.UseVisualStyleBackColor = true;
            // 
            // enablePasswordCheckBox
            // 
            enablePasswordCheckBox.AutoSize = true;
            enablePasswordCheckBox.Location = new Point(9, 76);
            enablePasswordCheckBox.Name = "enablePasswordCheckBox";
            enablePasswordCheckBox.Size = new Size(194, 29);
            enablePasswordCheckBox.TabIndex = 19;
            enablePasswordCheckBox.Text = "パスワードを設定する";
            enablePasswordCheckBox.UseVisualStyleBackColor = true;
            // 
            // enableZipCommentCheckBox
            // 
            enableZipCommentCheckBox.AutoSize = true;
            enableZipCommentCheckBox.Location = new Point(9, 110);
            enableZipCommentCheckBox.Name = "enableZipCommentCheckBox";
            enableZipCommentCheckBox.Size = new Size(267, 29);
            enableZipCommentCheckBox.TabIndex = 18;
            enableZipCommentCheckBox.Text = "ZIPファイルにコメントを追加する";
            enableZipCommentCheckBox.UseVisualStyleBackColor = true;
            // 
            // compressButton
            // 
            compressButton.Location = new Point(664, 696);
            compressButton.Name = "compressButton";
            compressButton.Size = new Size(234, 51);
            compressButton.TabIndex = 19;
            compressButton.Text = "圧縮実行";
            compressButton.UseVisualStyleBackColor = true;
            // 
            // compressionProgressBar
            // 
            compressionProgressBar.Location = new Point(12, 648);
            compressionProgressBar.Name = "compressionProgressBar";
            compressionProgressBar.Size = new Size(683, 34);
            compressionProgressBar.TabIndex = 20;
            compressionProgressBar.Visible = false;
            // 
            // compressionStatsLabel
            // 
            compressionStatsLabel.AutoSize = true;
            compressionStatsLabel.Enabled = false;
            compressionStatsLabel.Location = new Point(701, 9);
            compressionStatsLabel.Name = "compressionStatsLabel";
            compressionStatsLabel.Size = new Size(0, 25);
            compressionStatsLabel.TabIndex = 30;
            // 
            // logTextBox
            // 
            logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            logTextBox.Location = new Point(701, 40);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(213, 642);
            logTextBox.TabIndex = 31;
            // 
            // Form1
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(926, 759);
            Controls.Add(logTextBox);
            Controls.Add(compressionStatsLabel);
            Controls.Add(compressionProgressBar);
            Controls.Add(compressButton);
            Controls.Add(CompressionOptionsGroup);
            Controls.Add(zipFileNameTextBox);
            Controls.Add(labelForOutputFolder);
            Controls.Add(outputFolderTextBox);
            Controls.Add(labelForZipFileName);
            Controls.Add(operationModeGroupBox);
            Controls.Add(selectOutputFolderButton);
            Controls.Add(clearFileListButton);
            Controls.Add(themeComboBox);
            Controls.Add(selectFilesButton);
            Controls.Add(removeSelectedFileButton);
            Controls.Add(recursiveAddCheckBox);
            Controls.Add(selectedFilesListBox);
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;
            Name = "Form1";
            Text = "SimpleZipper";
            operationModeGroupBox.ResumeLayout(false);
            operationModeGroupBox.PerformLayout();
            CompressionOptionsGroup.ResumeLayout(false);
            CompressionOptionsGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitSizeNumericUpDown).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox themeComboBox;
        private GroupBox operationModeGroupBox;
        private Label existingZipFileLabel;
        private RadioButton addToExistingZipRadioButton;
        private RadioButton createNewZipRadioButton;
        private Button selectExistingZipButton;
        private Button clearFileListButton;
        private Button selectFilesButton;
        private Button selectOutputFolderButton;
        private ListBox selectedFilesListBox;
        private CheckBox recursiveAddCheckBox;
        private Button removeSelectedFileButton;
        private TextBox existingZipFileTextBox;
        private Label labelForZipFileName;
        private TextBox outputFolderTextBox;
        private Label labelForOutputFolder;
        private TextBox passwordTextBox;
        private TextBox zipFileNameTextBox;
        private ComboBox compressionLevelComboBox;
        private GroupBox CompressionOptionsGroup;
        private ComboBox splitUnitComboBox;
        private NumericUpDown splitSizeNumericUpDown;
        private Label passwordLabel;
        private Label label1;
        private CheckBox enableSplitZipCheckBox;
        private CheckBox enablePasswordCheckBox;
        private CheckBox enableZipCommentCheckBox;
        private TextBox zipCommentTextBox;
        private Label splitSizeLabel;
        private CheckBox openOutputFolderCheckBox;
        private Button compressButton;
        private ProgressBar compressionProgressBar;
        private Label zipCommentLabel;
        private Label compressionStatsLabel;
        private TextBox logTextBox;
        private ToolTip toolTip1;
    }
}
