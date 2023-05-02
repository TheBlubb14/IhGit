namespace IhGit
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
            button1 = new Button();
            buttonClear = new Button();
            textBoxFeatureName = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBoxCommits = new TextBox();
            buttonUpmerge = new Button();
            textBoxDescription = new TextBox();
            label3 = new Label();
            label4 = new Label();
            textBoxTitle = new TextBox();
            textBoxOutput = new TextBox();
            buttonPullRequest = new Button();
            buttonFetch = new Button();
            buttonPush = new Button();
            checkBoxDryRun = new CheckBox();
            buttonNewBranch = new Button();
            buttonPullRequestMultiple = new Button();
            checkBoxStartOnSameVersion = new CheckBox();
            textBoxUserName = new TextBox();
            label5 = new Label();
            label6 = new Label();
            textBoxRepo = new TextBox();
            buttonDaniel = new Button();
            buttonSpike = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button1.Location = new Point(343, 295);
            button1.Name = "button1";
            button1.Size = new Size(112, 23);
            button1.TabIndex = 14;
            button1.Text = "Status";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // buttonClear
            // 
            buttonClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonClear.Location = new Point(478, 440);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(193, 23);
            buttonClear.TabIndex = 23;
            buttonClear.Text = "Clear Logs";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // textBoxFeatureName
            // 
            textBoxFeatureName.Location = new Point(13, 27);
            textBoxFeatureName.Name = "textBoxFeatureName";
            textBoxFeatureName.Size = new Size(323, 23);
            textBoxFeatureName.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 9);
            label1.Name = "label1";
            label1.Size = new Size(81, 15);
            label1.TabIndex = 0;
            label1.Text = "Feature Name";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(13, 53);
            label2.Name = "label2";
            label2.Size = new Size(56, 15);
            label2.TabIndex = 2;
            label2.Text = "Commits";
            // 
            // textBoxCommits
            // 
            textBoxCommits.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            textBoxCommits.Location = new Point(13, 71);
            textBoxCommits.Multiline = true;
            textBoxCommits.Name = "textBoxCommits";
            textBoxCommits.Size = new Size(323, 203);
            textBoxCommits.TabIndex = 3;
            // 
            // buttonUpmerge
            // 
            buttonUpmerge.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonUpmerge.Location = new Point(478, 382);
            buttonUpmerge.Name = "buttonUpmerge";
            buttonUpmerge.Size = new Size(75, 23);
            buttonUpmerge.TabIndex = 21;
            buttonUpmerge.Text = "Upmerge";
            buttonUpmerge.UseVisualStyleBackColor = true;
            buttonUpmerge.Click += buttonUpmerge_Click;
            // 
            // textBoxDescription
            // 
            textBoxDescription.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            textBoxDescription.Location = new Point(343, 71);
            textBoxDescription.Multiline = true;
            textBoxDescription.Name = "textBoxDescription";
            textBoxDescription.Size = new Size(323, 203);
            textBoxDescription.TabIndex = 7;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(343, 53);
            label3.Name = "label3";
            label3.Size = new Size(67, 15);
            label3.TabIndex = 6;
            label3.Text = "Description";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(343, 9);
            label4.Name = "label4";
            label4.Size = new Size(29, 15);
            label4.TabIndex = 4;
            label4.Text = "Title";
            // 
            // textBoxTitle
            // 
            textBoxTitle.Location = new Point(343, 27);
            textBoxTitle.Name = "textBoxTitle";
            textBoxTitle.Size = new Size(323, 23);
            textBoxTitle.TabIndex = 5;
            // 
            // textBoxOutput
            // 
            textBoxOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOutput.Location = new Point(682, 0);
            textBoxOutput.Multiline = true;
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.ScrollBars = ScrollBars.Both;
            textBoxOutput.Size = new Size(458, 474);
            textBoxOutput.TabIndex = 24;
            // 
            // buttonPullRequest
            // 
            buttonPullRequest.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonPullRequest.Location = new Point(343, 411);
            buttonPullRequest.Name = "buttonPullRequest";
            buttonPullRequest.Size = new Size(112, 23);
            buttonPullRequest.TabIndex = 18;
            buttonPullRequest.Text = "Pull Request";
            buttonPullRequest.UseVisualStyleBackColor = true;
            buttonPullRequest.Click += buttonPullRequest_Click;
            // 
            // buttonFetch
            // 
            buttonFetch.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonFetch.Location = new Point(343, 324);
            buttonFetch.Name = "buttonFetch";
            buttonFetch.Size = new Size(112, 23);
            buttonFetch.TabIndex = 15;
            buttonFetch.Text = "Fetch";
            buttonFetch.UseVisualStyleBackColor = true;
            buttonFetch.Click += buttonFetch_Click;
            // 
            // buttonPush
            // 
            buttonPush.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonPush.Location = new Point(343, 382);
            buttonPush.Name = "buttonPush";
            buttonPush.Size = new Size(112, 23);
            buttonPush.TabIndex = 17;
            buttonPush.Text = "Push";
            buttonPush.UseVisualStyleBackColor = true;
            buttonPush.Click += buttonPush_Click;
            // 
            // checkBoxDryRun
            // 
            checkBoxDryRun.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            checkBoxDryRun.AutoSize = true;
            checkBoxDryRun.Location = new Point(478, 299);
            checkBoxDryRun.Name = "checkBoxDryRun";
            checkBoxDryRun.Size = new Size(68, 19);
            checkBoxDryRun.TabIndex = 19;
            checkBoxDryRun.Text = "Dry Run";
            checkBoxDryRun.UseVisualStyleBackColor = true;
            // 
            // buttonNewBranch
            // 
            buttonNewBranch.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonNewBranch.Location = new Point(343, 353);
            buttonNewBranch.Name = "buttonNewBranch";
            buttonNewBranch.Size = new Size(112, 23);
            buttonNewBranch.TabIndex = 16;
            buttonNewBranch.Text = "New Branch";
            buttonNewBranch.UseVisualStyleBackColor = true;
            buttonNewBranch.Click += buttonNewBranch_Click;
            // 
            // buttonPullRequestMultiple
            // 
            buttonPullRequestMultiple.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonPullRequestMultiple.Location = new Point(478, 411);
            buttonPullRequestMultiple.Name = "buttonPullRequestMultiple";
            buttonPullRequestMultiple.Size = new Size(193, 23);
            buttonPullRequestMultiple.TabIndex = 22;
            buttonPullRequestMultiple.Text = "Upmerge all";
            buttonPullRequestMultiple.UseVisualStyleBackColor = true;
            buttonPullRequestMultiple.Click += buttonPullRequestMultiple_Click;
            // 
            // checkBoxStartOnSameVersion
            // 
            checkBoxStartOnSameVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            checkBoxStartOnSameVersion.AutoSize = true;
            checkBoxStartOnSameVersion.Location = new Point(478, 328);
            checkBoxStartOnSameVersion.Name = "checkBoxStartOnSameVersion";
            checkBoxStartOnSameVersion.Size = new Size(139, 19);
            checkBoxStartOnSameVersion.TabIndex = 20;
            checkBoxStartOnSameVersion.Text = "Start on same version";
            checkBoxStartOnSameVersion.UseVisualStyleBackColor = true;
            // 
            // textBoxUserName
            // 
            textBoxUserName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            textBoxUserName.Location = new Point(13, 295);
            textBoxUserName.Name = "textBoxUserName";
            textBoxUserName.Size = new Size(323, 23);
            textBoxUserName.TabIndex = 9;
            textBoxUserName.Text = "TheBlubb14";
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label5.AutoSize = true;
            label5.Location = new Point(13, 277);
            label5.Name = "label5";
            label5.Size = new Size(60, 15);
            label5.TabIndex = 8;
            label5.Text = "Username";
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label6.AutoSize = true;
            label6.Location = new Point(13, 321);
            label6.Name = "label6";
            label6.Size = new Size(34, 15);
            label6.TabIndex = 10;
            label6.Text = "Repo";
            // 
            // textBoxRepo
            // 
            textBoxRepo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            textBoxRepo.Location = new Point(13, 339);
            textBoxRepo.Name = "textBoxRepo";
            textBoxRepo.Size = new Size(323, 23);
            textBoxRepo.TabIndex = 11;
            textBoxRepo.Text = "C:\\Dev\\Projects\\GitHub\\paxcontrol";
            // 
            // buttonDaniel
            // 
            buttonDaniel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonDaniel.Location = new Point(12, 368);
            buttonDaniel.Name = "buttonDaniel";
            buttonDaniel.Size = new Size(75, 23);
            buttonDaniel.TabIndex = 12;
            buttonDaniel.Text = "Daniel";
            buttonDaniel.UseVisualStyleBackColor = true;
            buttonDaniel.Click += buttonDaniel_Click;
            // 
            // buttonSpike
            // 
            buttonSpike.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonSpike.Location = new Point(93, 368);
            buttonSpike.Name = "buttonSpike";
            buttonSpike.Size = new Size(75, 23);
            buttonSpike.TabIndex = 13;
            buttonSpike.Text = "Spike";
            buttonSpike.UseVisualStyleBackColor = true;
            buttonSpike.Click += buttonSpike_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1140, 474);
            Controls.Add(buttonSpike);
            Controls.Add(buttonDaniel);
            Controls.Add(label6);
            Controls.Add(textBoxRepo);
            Controls.Add(label5);
            Controls.Add(textBoxUserName);
            Controls.Add(checkBoxStartOnSameVersion);
            Controls.Add(buttonPullRequestMultiple);
            Controls.Add(buttonNewBranch);
            Controls.Add(checkBoxDryRun);
            Controls.Add(buttonPush);
            Controls.Add(buttonFetch);
            Controls.Add(buttonPullRequest);
            Controls.Add(textBoxOutput);
            Controls.Add(label4);
            Controls.Add(textBoxTitle);
            Controls.Add(label3);
            Controls.Add(textBoxDescription);
            Controls.Add(buttonUpmerge);
            Controls.Add(label2);
            Controls.Add(textBoxCommits);
            Controls.Add(label1);
            Controls.Add(textBoxFeatureName);
            Controls.Add(buttonClear);
            Controls.Add(button1);
            Name = "Form1";
            Text = "IhGit";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button buttonClear;
        private TextBox textBoxFeatureName;
        private Label label1;
        private Label label2;
        private TextBox textBoxCommits;
        private Button buttonUpmerge;
        private TextBox textBoxDescription;
        private Label label3;
        private Label label4;
        private TextBox textBoxTitle;
        private TextBox textBoxOutput;
        private Button buttonPullRequest;
        private Button buttonFetch;
        private Button buttonPush;
        private CheckBox checkBoxDryRun;
        private Button buttonNewBranch;
        private Button buttonPullRequestMultiple;
        private CheckBox checkBoxStartOnSameVersion;
        private TextBox textBoxUserName;
        private Label label5;
        private Label label6;
        private TextBox textBoxRepo;
        private Button buttonDaniel;
        private Button buttonSpike;
    }
}